using StackExchange.Redis;
using Services.Entities;
using Services.Models;
using Services.Abstraction.Repositories;
using System.Text.Json;

namespace Repositories.Features;

public class DrawRepositoryRedis : IDrawRepository
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _db;

    public DrawRepositoryRedis(IConnectionMultiplexer redis)
    {
        _redis = redis;
        _db = _redis.GetDatabase();
    }

    public async Task SaveDrawAsync(DrawResult drawResult, int numberOfGroups)
    {
        var draw = new Draw(drawResult.DrawnBy, numberOfGroups);
        var drawKey = $"draw:{draw.Id}";
        
        // Serialize and store the draw
        var drawJson = JsonSerializer.Serialize(draw);
        await _db.StringSetAsync(drawKey, drawJson);

        // Store groups and teams
        foreach (var groupResult in drawResult.Groups)
        {
            var group = new Group(groupResult.GroupName) { DrawId = draw.Id };
            var groupKey = $"group:{group.Id}";
            var groupJson = JsonSerializer.Serialize(group);
            await _db.StringSetAsync(groupKey, groupJson);
            
            // Store group-team relationships
            foreach (var teamResult in groupResult.Teams)
            {
                var teamKey = $"team:{teamResult.Name}";
                var groupTeamKey = $"group_team:{group.Id}:{teamResult.Name}";
                await _db.StringSetAsync(groupTeamKey, "1");
            }
        }

        // Add to recent draws sorted set
        await _db.SortedSetAddAsync("recent_draws", drawKey, DateTime.UtcNow.Ticks);
    }

    public async Task<List<DrawResult>> GetAllDrawsAsync()
    {
        var drawKeys = await _db.SortedSetRangeByRankAsync("recent_draws", order: Order.Descending);
        var draws = new List<DrawResult>();

        foreach (var key in drawKeys)
        {
            var drawJson = await _db.StringGetAsync(key.ToString());
            if (!drawJson.IsNullOrEmpty)
            {
                var draw = JsonSerializer.Deserialize<Draw>(drawJson);
                if (draw != null)
                {
                    var drawResult = await GetDrawByIdAsync(draw.Id);
                    if (drawResult != null)
                    {
                        draws.Add(drawResult);
                    }
                }
            }
        }

        return draws;
    }

    public async Task<DrawResult?> GetDrawByIdAsync(int id)
    {
        var drawKey = $"draw:{id}";
        var drawJson = await _db.StringGetAsync(drawKey);
        
        if (drawJson.IsNullOrEmpty) return null;

        var draw = JsonSerializer.Deserialize<Draw>(drawJson);
        if (draw == null) return null;

        // Find all groups for this draw
        var groupKeys = await _db.ExecuteAsync("KEYS", $"group:*");
        var groups = new List<GroupResult>();

        foreach (var groupKey in (string[])groupKeys)
        {
            var groupJson = await _db.StringGetAsync(groupKey);
            if (!groupJson.IsNullOrEmpty)
            {
                var group = JsonSerializer.Deserialize<Group>(groupJson);
                if (group != null && group.DrawId == id)
                {
                    // Find all teams for this group
                    var teamKeys = await _db.ExecuteAsync("KEYS", $"group_team:{group.Id}:*");
                    var teams = new List<TeamResult>();

                    foreach (var teamKey in (string[])teamKeys)
                    {
                        var teamName = teamKey.ToString().Split(':')[2];
                        teams.Add(new TeamResult(teamName));
                    }

                    groups.Add(new GroupResult(group.Name, teams));
                }
            }
        }

        return new DrawResult(draw.DrawnBy, draw.CreatedAt, groups);
    }
}