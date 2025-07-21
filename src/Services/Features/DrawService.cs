using Services.Abstraction.Repositories;
using Services.Entities;
using Services.Models;
using Microsoft.Extensions.Logging;
using Rebus.Bus;
using Contract.Events;


namespace Services.Features;

public interface IDrawService
{
    Task<DrawResult> CreateDrawAsync(string drawnBy, int numberOfGroups);
    Task<List<DrawResult>> GetAllDrawsAsync();
    Task<DrawResult?> GetDrawByIdAsync(int id);
}

public interface IRandomProvider
{
    int Next(int maxValue);
}

public interface IMessagePublisher
{
    Task PublishOperationEventAsync(string operationName, string status, params string[] args);
}


public class MessagePublisher : IMessagePublisher
{
    private readonly IBus _bus;
    private readonly ILogger<MessagePublisher> _logger;

    public MessagePublisher(IBus bus, ILogger<MessagePublisher> logger)
    {
        _bus = bus;
        _logger = logger;
    }

    public async Task PublishOperationEventAsync(string operationName, string status, params string[] args)
    {
        try
        {
            var message = new OperationEvent(operationName, status, args);
            var routingKey = $"operation.events.{operationName.ToLowerInvariant()}";

            await _bus.Advanced.Topics.Publish(routingKey, message);

            _logger.LogInformation("Published event: {Operation} - {Status} -> {RoutingKey}",
                operationName, status, routingKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish operation event: {Operation} - {Status}",
                operationName, status);
        }
    }
}

public class RandomProvider : IRandomProvider
{
    private readonly Random _random = new();

    public int Next(int maxValue) => _random.Next(maxValue);
}

public class DrawService : IDrawService
{
    private readonly ITeamRepository _teamRepository;
    private readonly IDrawRepository _drawRepository;
    private readonly IRandomProvider _randomProvider;
    private readonly IMessagePublisher _messagePublisher;
    private readonly ILogger<DrawService> _logger;

    public DrawService(
        ITeamRepository teamRepository,
        IDrawRepository drawRepository,
        IRandomProvider randomProvider,
        IMessagePublisher messagePublisher,
        ILogger<DrawService> logger)
    {
        _teamRepository = teamRepository;
        _drawRepository = drawRepository;
        _randomProvider = randomProvider;
        _messagePublisher = messagePublisher;
        _logger = logger;
    }

    public async Task<DrawResult> CreateDrawAsync(string drawnBy, int numberOfGroups)
    {
        var operationId = Guid.NewGuid().ToString("N")[..8];

        try
        {
            _logger.LogInformation("Starting draw creation {OperationId} by {DrawnBy} for {NumberOfGroups} groups",
                operationId, drawnBy, numberOfGroups);

            if (numberOfGroups is not 4 and not 8)
                throw new ArgumentException("Number of groups must be 4 or 8");

            await _messagePublisher.PublishOperationEventAsync("CreateDraw", "Started",
                drawnBy, numberOfGroups.ToString(), operationId);

            await _teamRepository.EnsureTeamsExistAsync();
            var allTeams = await _teamRepository.GetAllTeamsAsync();

            var groups = CreateGroups(numberOfGroups);
            var teamsByCountry = allTeams.GroupBy(t => t.Country).ToDictionary(g => g.Key, g => g.ToList());

            AssignTeamsToGroups(groups, teamsByCountry);

            var groupResults = groups.Select(g => new GroupResult(
                g.Name,
                g.GetTeams().Select(t => new TeamResult(t.Name)).ToList()
            )).ToList();

            var drawResult = new DrawResult(drawnBy, DateTime.UtcNow, groupResults);

            await _drawRepository.SaveDrawAsync(drawResult, numberOfGroups);

            await _messagePublisher.PublishOperationEventAsync("CreateDraw", "Completed",
                drawnBy, numberOfGroups.ToString(), operationId);

            _logger.LogInformation("Draw {OperationId} created successfully", operationId);

            return drawResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in draw creation {OperationId}", operationId);
            await _messagePublisher.PublishOperationEventAsync("CreateDraw", "Failed",
                drawnBy, numberOfGroups.ToString(), operationId, ex.Message);
            throw;
        }
    }

    public async Task<List<DrawResult>> GetAllDrawsAsync()
    {
        try
        {
            await _messagePublisher.PublishOperationEventAsync("GetAllDraws", "Started");
            var result = await _drawRepository.GetAllDrawsAsync();
            await _messagePublisher.PublishOperationEventAsync("GetAllDraws", "Completed", result.Count.ToString());
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all draws");
            await _messagePublisher.PublishOperationEventAsync("GetAllDraws", "Failed", ex.Message);
            throw;
        }
    }

    public async Task<DrawResult?> GetDrawByIdAsync(int id)
    {
        try
        {
            await _messagePublisher.PublishOperationEventAsync("GetDrawById", "Started", id.ToString());
            var result = await _drawRepository.GetDrawByIdAsync(id);
            var status = result != null ? "Completed" : "NotFound";
            await _messagePublisher.PublishOperationEventAsync("GetDrawById", status, id.ToString());
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting draw by ID: {DrawId}", id);
            await _messagePublisher.PublishOperationEventAsync("GetDrawById", "Failed", id.ToString(), ex.Message);
            throw;
        }
    }

    private static List<Group> CreateGroups(int numberOfGroups)
    {
        var groupNames = new[] { "A", "B", "C", "D", "E", "F", "G", "H" };
        return groupNames.Take(numberOfGroups).Select(name => new Group(name)).ToList();
    }

    private void AssignTeamsToGroups(List<Group> groups, Dictionary<string, List<Team>> teamsByCountry)
    {
        var teamsPerGroup = 32 / groups.Count;
        var countries = teamsByCountry.Keys.ToList();

        for (int round = 0; round < teamsPerGroup; round++)
        {
            for (int groupIndex = 0; groupIndex < groups.Count; groupIndex++)
            {
                var group = groups[groupIndex];
                var availableCountries = countries.Where(country =>
                    teamsByCountry[country].Count > 0 &&
                    !group.GetTeams().Any(t => t.Country == country)).ToList();

                if (availableCountries.Count > 0)
                {
                    var selectedCountry = availableCountries[_randomProvider.Next(availableCountries.Count)];
                    var availableTeams = teamsByCountry[selectedCountry];
                    var selectedTeam = availableTeams[_randomProvider.Next(availableTeams.Count)];

                    group.AddTeam(selectedTeam);
                    teamsByCountry[selectedCountry].Remove(selectedTeam);
                }
            }
        }
    }
}