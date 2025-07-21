using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Services.Abstraction.Repositories;
using Services.Entities;
using Services.Features;
using Services.Models;

namespace Tests.Services;

public class DrawServiceTests
{
    private readonly Mock<ITeamRepository> _teamRepoMock = new();
    private readonly Mock<IDrawRepository> _drawRepoMock = new();
    private readonly Mock<IRandomProvider> _randomMock = new();
    private readonly Mock<IMessagePublisher> _messagePublisherMock = new();
    private readonly Mock<ILogger<DrawService>> _loggerMock = new();

    private readonly DrawService _service;

    public DrawServiceTests()
    {
        _service = new DrawService(
            _teamRepoMock.Object, 
            _drawRepoMock.Object, 
            _randomMock.Object,
            _messagePublisherMock.Object,
            _loggerMock.Object);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(8)]
    public async Task CreateDrawAsync_ValidGroupNumbers_ShouldCreateDrawSuccessfully(int numberOfGroups)
    {
        // Arrange
        var teams = CreateTestTeams();
        var expectedTeamsPerGroup = 32 / numberOfGroups;

        SetupMocks(teams);

        // Act
        var result = await _service.CreateDrawAsync("TestUser", numberOfGroups);

        // Assert
        AssertDrawResult(result, "TestUser", numberOfGroups, expectedTeamsPerGroup);
        VerifySuccessfulDrawCreation(numberOfGroups);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task CreateDrawAsync_InvalidGroupNumbers_ShouldThrowArgumentException(int invalidGroupCount)
    {
        // Act & Assert
        var act = () => _service.CreateDrawAsync("TestUser", invalidGroupCount);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Number of groups must be 4 or 8");
        
        // Verify error event was published
        _messagePublisherMock.Verify(x => x.PublishOperationEventAsync(
            "CreateDraw", "Failed", 
            It.Is<string[]>(args => args.Contains("TestUser") && args.Contains(invalidGroupCount.ToString()))), 
            Times.Once);
    }

    [Fact]
    public async Task CreateDrawAsync_RepositoryThrowsException_ShouldPublishFailedEventAndRethrow()
    {
        // Arrange
        var teams = CreateTestTeams();
        var expectedException = new InvalidOperationException("Database error");

        _teamRepoMock.Setup(x => x.GetAllTeamsAsync()).ReturnsAsync(teams);
        _teamRepoMock.Setup(x => x.EnsureTeamsExistAsync()).Returns(Task.CompletedTask);
        _drawRepoMock.Setup(x => x.SaveDrawAsync(It.IsAny<DrawResult>(), It.IsAny<int>()))
                     .ThrowsAsync(expectedException);
        _randomMock.Setup(x => x.Next(It.IsAny<int>())).Returns(0);

        // Act & Assert
        var act = () => _service.CreateDrawAsync("TestUser", 4);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Database error");

        // Verify failure event was published
        _messagePublisherMock.Verify(x => x.PublishOperationEventAsync(
            "CreateDraw", "Failed", 
            It.Is<string[]>(args => args.Contains("TestUser") && args.Contains("Database error"))), 
            Times.Once);
    }

    [Fact]
    public async Task CreateDrawAsync_ShouldDistributeTeamsEvenlyAcrossGroups()
    {
        // Arrange
        var teams = CreateTestTeams();
        SetupMocks(teams);

        // Act
        var result = await _service.CreateDrawAsync("TestUser", 4);

        // Assert
        result.Groups.Should().HaveCount(4);
        result.Groups.Should().AllSatisfy(group => group.Teams.Should().HaveCount(8));
        
        // Verify no group has teams from same country (based on our test data structure)
        foreach (var group in result.Groups)
        {
            var countries = group.Teams.Select(t => t.Name.Split('_')[0]).ToList();
            countries.Should().OnlyHaveUniqueItems("No group should have teams from same country");
        }
    }

    [Fact]
    public async Task GetAllDrawsAsync_ShouldReturnAllDrawsAndPublishEvents()
    {
        // Arrange
        var expectedDraws = new List<DrawResult>
        {
            CreateSampleDrawResult("User1"),
            CreateSampleDrawResult("User2")
        };
        
        _drawRepoMock.Setup(x => x.GetAllDrawsAsync()).ReturnsAsync(expectedDraws);

        // Act
        var result = await _service.GetAllDrawsAsync();

        // Assert
        result.Should().BeEquivalentTo(expectedDraws);
        _drawRepoMock.Verify(x => x.GetAllDrawsAsync(), Times.Once);
        
        // Verify events were published
        _messagePublisherMock.Verify(x => x.PublishOperationEventAsync(
            "GetAllDraws", "Started"), Times.Once);
        _messagePublisherMock.Verify(x => x.PublishOperationEventAsync(
            "GetAllDraws", "Completed", "2"), Times.Once);
    }

    [Fact]
    public async Task GetAllDrawsAsync_RepositoryThrows_ShouldPublishFailedEventAndRethrow()
    {
        // Arrange
        var expectedException = new InvalidOperationException("Database connection failed");
        _drawRepoMock.Setup(x => x.GetAllDrawsAsync()).ThrowsAsync(expectedException);

        // Act & Assert
        var act = () => _service.GetAllDrawsAsync();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Database connection failed");

        // Verify failure event was published
        _messagePublisherMock.Verify(x => x.PublishOperationEventAsync(
            "GetAllDraws", "Failed", "Database connection failed"), Times.Once);
    }

    [Fact]
    public async Task GetDrawByIdAsync_ExistingDraw_ShouldReturnDrawAndPublishCompletedEvent()
    {
        // Arrange
        var expectedDraw = CreateSampleDrawResult("User1");
        var drawId = 1;
        
        _drawRepoMock.Setup(x => x.GetDrawByIdAsync(drawId)).ReturnsAsync(expectedDraw);

        // Act
        var result = await _service.GetDrawByIdAsync(drawId);

        // Assert
        result.Should().BeEquivalentTo(expectedDraw);
        _drawRepoMock.Verify(x => x.GetDrawByIdAsync(drawId), Times.Once);
        
        // Verify events
        _messagePublisherMock.Verify(x => x.PublishOperationEventAsync(
            "GetDrawById", "Started", "1"), Times.Once);
        _messagePublisherMock.Verify(x => x.PublishOperationEventAsync(
            "GetDrawById", "Completed", "1"), Times.Once);
    }

    [Fact]
    public async Task GetDrawByIdAsync_NonExistentDraw_ShouldReturnNullAndPublishNotFoundEvent()
    {
        // Arrange
        var drawId = 999;
        _drawRepoMock.Setup(x => x.GetDrawByIdAsync(drawId)).ReturnsAsync((DrawResult?)null);

        // Act
        var result = await _service.GetDrawByIdAsync(drawId);

        // Assert
        result.Should().BeNull();
        _drawRepoMock.Verify(x => x.GetDrawByIdAsync(drawId), Times.Once);
        
        // Verify NotFound event was published
        _messagePublisherMock.Verify(x => x.PublishOperationEventAsync(
            "GetDrawById", "NotFound", "999"), Times.Once);
    }

    [Fact]
    public async Task GetDrawByIdAsync_RepositoryThrows_ShouldPublishFailedEventAndRethrow()
    {
        // Arrange
        var drawId = 1;
        var expectedException = new InvalidOperationException("Database timeout");
        _drawRepoMock.Setup(x => x.GetDrawByIdAsync(drawId)).ThrowsAsync(expectedException);

        // Act & Assert
        var act = () => _service.GetDrawByIdAsync(drawId);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Database timeout");

        // Verify failure event was published
        _messagePublisherMock.Verify(x => x.PublishOperationEventAsync(
            "GetDrawById", "Failed", "1", "Database timeout"), Times.Once);
    }

    #region Helper Methods

    private List<Team> CreateTestTeams()
    {
        // Create 32 teams with 8 different countries (4 teams per country)
        return Enumerable.Range(0, 32)
            .Select(i => new Team 
            { 
                Name = $"Country{i / 4}_Team{i % 4 + 1}", 
                Country = $"Country{i / 4}" 
            }).ToList();
    }

    private void SetupMocks(List<Team> teams)
    {
        _teamRepoMock.Setup(x => x.GetAllTeamsAsync()).ReturnsAsync(teams);
        _teamRepoMock.Setup(x => x.EnsureTeamsExistAsync()).Returns(Task.CompletedTask);
        _drawRepoMock.Setup(x => x.SaveDrawAsync(It.IsAny<DrawResult>(), It.IsAny<int>()))
                     .Returns(Task.CompletedTask);
        
        // Setup sequential random returns for predictable testing
        var randomSequence = new Queue<int>(Enumerable.Repeat(0, 100));
        _randomMock.Setup(x => x.Next(It.IsAny<int>())).Returns(() => 
            randomSequence.Count > 0 ? randomSequence.Dequeue() : 0);
    }

    private static void AssertDrawResult(DrawResult result, string drawnBy, int numberOfGroups, int expectedTeamsPerGroup)
    {
        result.Should().NotBeNull();
        result.DrawnBy.Should().Be(drawnBy);
        result.Groups.Should().HaveCount(numberOfGroups);
        result.Groups.Should().AllSatisfy(group => 
            group.Teams.Should().HaveCount(expectedTeamsPerGroup));
        // result.DrawnAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        
        // Verify total team count
        result.Groups.SelectMany(g => g.Teams).Should().HaveCount(32);
    }

    private void VerifySuccessfulDrawCreation(int numberOfGroups)
    {
        _teamRepoMock.Verify(x => x.EnsureTeamsExistAsync(), Times.Once);
        _teamRepoMock.Verify(x => x.GetAllTeamsAsync(), Times.Once);
        _drawRepoMock.Verify(x => x.SaveDrawAsync(It.IsAny<DrawResult>(), numberOfGroups), Times.Once);
        
        // Verify events
        _messagePublisherMock.Verify(x => x.PublishOperationEventAsync(
            "CreateDraw", "Started", 
            It.Is<string[]>(args => args.Contains("TestUser") && args.Contains(numberOfGroups.ToString()))), 
            Times.Once);
        
        _messagePublisherMock.Verify(x => x.PublishOperationEventAsync(
            "CreateDraw", "Completed", 
            It.Is<string[]>(args => args.Contains("TestUser") && args.Contains(numberOfGroups.ToString()))), 
            Times.Once);
    }

    private static DrawResult CreateSampleDrawResult(string drawnBy)
    {
        var groups = new List<GroupResult>
        {
            new("A", new List<TeamResult> { new("Team1"), new("Team2") }),
            new("B", new List<TeamResult> { new("Team3"), new("Team4") })
        };
        
        return new DrawResult(drawnBy, DateTime.UtcNow, groups);
    }

    #endregion
}