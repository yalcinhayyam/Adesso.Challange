using Nest;
using Services.Entities;
using Services.Models;
using Services.Abstraction.Repositories;

namespace Repositories.Features;

public class DrawRepositoryElasticsearch : IDrawRepository
{
    private readonly IElasticClient _elasticClient;
    private const string IndexName = "draws";

    public DrawRepositoryElasticsearch(IElasticClient elasticClient)
    {
        _elasticClient = elasticClient;
        CreateIndexIfNotExists().Wait();
    }

    private async Task CreateIndexIfNotExists()
    {
        var exists = await _elasticClient.Indices.ExistsAsync(IndexName);
        if (!exists.Exists)
        {
            await _elasticClient.Indices.CreateAsync(IndexName, c => c
                .Map<Draw>(m => m
                    .AutoMap()
                    .Properties(p => p
                        .Nested<Group>(n => n
                            .Name(nn => nn.Groups)
                            .AutoMap()
                            .Properties(pp => pp
                                .Nested<GroupTeam>(nn => nn
                                    .Name(nnn => nnn.GroupTeams)
                                )
                            )
                        )
                    )
                )
            );
        }
    }

    public async Task SaveDrawAsync(DrawResult drawResult, int numberOfGroups)
    {
        try
        {
            var draw = new Draw(drawResult.DrawnBy, numberOfGroups);
            
            // Index complete draw with nested groups and teams
            var groups = drawResult.Groups.Select(g => new Group(g.GroupName)
                {
                    DrawId = draw.Id,
                    GroupTeams = g.Teams.Select(t => new GroupTeam
                    {
                        Team = new Team { Name = t.Name }
                    }).ToList()
                }).ToList();

            draw.Groups = groups;

            var response = await _elasticClient.IndexAsync(draw, i => i.Index(IndexName));
            
            if (!response.IsValid)
                throw new Exception($"Failed to index draw: {response.DebugInformation}");
        }
        catch (Exception ex)
        {
            throw new Exception("Error saving draw to Elasticsearch", ex);
        }
    }

    public async Task<List<DrawResult>> GetAllDrawsAsync()
    {
        try
        {
            var searchResponse = await _elasticClient.SearchAsync<Draw>(s => s
                .Index(IndexName)
                .Sort(ss => ss.Descending(d => d.CreatedAt))
                .Size(1000));

            if (!searchResponse.IsValid)
                throw new Exception($"Search failed: {searchResponse.DebugInformation}");

            return searchResponse.Documents.Select(d => new DrawResult(
                d.DrawnBy,
                d.CreatedAt,
                d.Groups.Select(g => new GroupResult(
                    g.Name,
                    g.GroupTeams.Select(gt => new TeamResult(gt.Team.Name)).ToList()
                )).ToList()
            )).ToList();
        }
        catch (Exception ex)
        {
            throw new Exception("Error retrieving draws from Elasticsearch", ex);
        }
    }

    public async Task<DrawResult?> GetDrawByIdAsync(int id)
    {
        try
        {
            var response = await _elasticClient.GetAsync<Draw>(id, g => g.Index(IndexName));
            
            if (!response.Found) 
                return null;

            var draw = response.Source;
            return new DrawResult(
                draw.DrawnBy,
                draw.CreatedAt,
                draw.Groups.Select(g => new GroupResult(
                    g.Name,
                    g.GroupTeams.Select(gt => new TeamResult(gt.Team.Name)).ToList()
                )).ToList()
            );
        }
        catch (Exception ex)
        {
            throw new Exception($"Error retrieving draw {id} from Elasticsearch", ex);
        }
    }
}