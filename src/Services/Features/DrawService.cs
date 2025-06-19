using Services.Abstraction.Repositories;
using Services.Entities;
using Services.Models;
using Rebus.Bus;
using Microsoft.Extensions.Logging;

namespace Services.Features;

public interface IMessagePublisher
{
    Task PublishOperationEvent(string operationName, string status, params string[] args);
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

    public async Task PublishOperationEvent(string operationName, string status, params string[] args)
    {
        try
        {
            var message = new OperationEvent(operationName, status, args);

            // Topic-based publishing
            var routingKey = $"operation.events.{operationName.ToLower()}";
            await _bus.Advanced.Topics.Publish(routingKey, message);

            _logger.LogInformation("Published event: {Operation} - {Status} to routing key: {RoutingKey}",
                operationName, status, routingKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish operation event: {Operation} - {Status}",
                operationName, status);
            throw;
        }
    }
}
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

public class RandomProvider : IRandomProvider
{
    private readonly Random _random = new();

    public int Next(int maxValue)
    {
        return _random.Next(maxValue);
    }
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
        try
        {
            _logger.LogInformation("Starting draw creation by {DrawnBy} for {NumberOfGroups} groups", drawnBy, numberOfGroups);

            if (numberOfGroups != 4 && numberOfGroups != 8)
                throw new ArgumentException("Number of groups must be 4 or 8");

            await _messagePublisher.PublishOperationEvent("CreateDraw", "Started", drawnBy, numberOfGroups.ToString());

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

            // Save to database
            await _drawRepository.SaveDrawAsync(drawResult, numberOfGroups);

            await _messagePublisher.PublishOperationEvent("CreateDraw", "Completed", drawnBy, numberOfGroups.ToString());
            _logger.LogInformation("Draw created successfully with ID: {DrawId}", drawResult.DrawnBy);

            return drawResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while creating draw");
            await _messagePublisher.PublishOperationEvent("CreateDraw", "Failed", drawnBy, numberOfGroups.ToString(), ex.Message);
            throw;
        }
    }
    public async Task<List<DrawResult>> GetAllDrawsAsync()
    {
        return await _drawRepository.GetAllDrawsAsync();
    }

    public async Task<DrawResult?> GetDrawByIdAsync(int id)
    {
        return await _drawRepository.GetDrawByIdAsync(id);
    }

    private List<Group> CreateGroups(int numberOfGroups)
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
                    teamsByCountry[country].Any() &&
                    !group.GetTeams().Any(t => t.Country == country)).ToList();

                if (availableCountries.Any())
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

