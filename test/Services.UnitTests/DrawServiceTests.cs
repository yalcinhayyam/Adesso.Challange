using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Services.Abstraction.Repositories;
using Services.Entities;
using Services.Features;
using Services.Models;


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

    [Fact]
    public async Task CreateDrawAsync_ShouldAssignTeamsAndReturnDrawResult()
    {
        // Arrange
        var teams = Enumerable.Range(1, 32).Select(i =>
            new Team { Name = $"Team{i}", Country = $"Country{i % 8}" }).ToList();

        _teamRepoMock.Setup(x => x.GetAllTeamsAsync()).ReturnsAsync(teams);
        _teamRepoMock.Setup(x => x.EnsureTeamsExistAsync()).Returns(Task.CompletedTask);
        _drawRepoMock.Setup(x => x.SaveDrawAsync(It.IsAny<DrawResult>(), It.IsAny<int>())).Returns(Task.CompletedTask);

        // Always return 0 (first item in any list) for random selections
        _randomMock.Setup(x => x.Next(It.IsAny<int>())).Returns(0);

        // Act
        var result = await _service.CreateDrawAsync("TestUser", 4);

        // Assert
        result.Should().NotBeNull();
        result.Groups.Should().HaveCount(4);
        result.Groups.SelectMany(g => g.Teams).Should().HaveCount(32);
        result.DrawnBy.Should().Be("TestUser");

        _teamRepoMock.Verify(x => x.EnsureTeamsExistAsync(), Times.Once);
        _drawRepoMock.Verify(x => x.SaveDrawAsync(It.IsAny<DrawResult>(), 4), Times.Once);
        
        _messagePublisherMock.Verify(x => x.PublishOperationEvent(
            "CreateDraw", "Started", "TestUser", "4"), Times.Once);
        _messagePublisherMock.Verify(x => x.PublishOperationEvent(
            "CreateDraw", "Completed", "TestUser", "4"), Times.Once);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(5)]
    public async Task CreateDrawAsync_InvalidGroupNumber_ShouldThrow(int invalidGroupCount)
    {
        // Act
        Func<Task> act = () => _service.CreateDrawAsync("TestUser", invalidGroupCount);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Number of groups must be 4 or 8");
        
        _messagePublisherMock.Verify(x => x.PublishOperationEvent(
            "CreateDraw", "Failed", "TestUser", invalidGroupCount.ToString(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task GetAllDrawsAsync_ShouldReturnAllDrawsFromRepository()
    {
        // Arrange
        var expectedDraws = new List<DrawResult>
        {
            new DrawResult("User1", DateTime.UtcNow, new List<GroupResult>()),
            new DrawResult("User2", DateTime.UtcNow, new List<GroupResult>())
        };
        
        _drawRepoMock.Setup(x => x.GetAllDrawsAsync()).ReturnsAsync(expectedDraws);

        // Act
        var result = await _service.GetAllDrawsAsync();

        // Assert
        result.Should().BeEquivalentTo(expectedDraws);
        _drawRepoMock.Verify(x => x.GetAllDrawsAsync(), Times.Once);
    }

    [Fact]
    public async Task GetDrawByIdAsync_ShouldReturnDrawFromRepository()
    {
        // Arrange
        var expectedDraw = new DrawResult("User1", DateTime.UtcNow, new List<GroupResult>());
        var drawId = 1;
        
        _drawRepoMock.Setup(x => x.GetDrawByIdAsync(drawId)).ReturnsAsync(expectedDraw);

        // Act
        var result = await _service.GetDrawByIdAsync(drawId);

        // Assert
        result.Should().BeEquivalentTo(expectedDraw);
        _drawRepoMock.Verify(x => x.GetDrawByIdAsync(drawId), Times.Once);
    }

    [Fact]
    public async Task GetDrawByIdAsync_ShouldReturnNullWhenNotFound()
    {
        // Arrange
        var drawId = 1;
        
        _drawRepoMock.Setup(x => x.GetDrawByIdAsync(drawId)).ReturnsAsync((DrawResult?)null);

        // Act
        var result = await _service.GetDrawByIdAsync(drawId);

        // Assert
        result.Should().BeNull();
        _drawRepoMock.Verify(x => x.GetDrawByIdAsync(drawId), Times.Once);
    }
}