using AdessoLeague.Repositories.Contexts;
using Microsoft.EntityFrameworkCore;
using Services.Entities;
using Services.Models;
using Services.Abstraction.Repositories;

namespace Repositories.Features;
public class DrawRepository : IDrawRepository
    {
        private readonly AdessoLeagueDbContext _context;

        public DrawRepository(AdessoLeagueDbContext context)
        {
            _context = context;
        }

        public async Task SaveDrawAsync(DrawResult drawResult, int numberOfGroups)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {

                var draw = new Draw(drawResult.DrawnBy, numberOfGroups);
                _context.Draws.Add(draw);
                await _context.SaveChangesAsync();

                foreach (var groupResult in drawResult.Groups)
                {
                    var group = new Group(groupResult.GroupName) { DrawId = draw.Id };
                    _context.Groups.Add(group);
                    await _context.SaveChangesAsync();

                    foreach (var teamResult in groupResult.Teams)
                    {
                        var team = await _context.Teams.FirstAsync(t => t.Name == teamResult.Name);
                        var groupTeam = new GroupTeam { GroupId = group.Id, TeamId = team.Id };
                        _context.GroupTeams.Add(groupTeam);
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<List<DrawResult>> GetAllDrawsAsync()
        {
            var draws = await _context.Draws
                .Include(d => d.Groups)
                    .ThenInclude(g => g.GroupTeams)
                        .ThenInclude(gt => gt.Team)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();

            return draws.Select(MapToDrawResult).ToList();
        }

        public async Task<DrawResult?> GetDrawByIdAsync(int id)
        {
            var draw = await _context.Draws
                .Include(d => d.Groups)
                    .ThenInclude(g => g.GroupTeams)
                        .ThenInclude(gt => gt.Team)
                .FirstOrDefaultAsync(d => d.Id == id);

            return draw != null ? MapToDrawResult(draw) : null;
        }

        private static DrawResult MapToDrawResult(Draw draw)
        {
            var groups = draw.Groups.Select(g => new GroupResult(
                g.Name,
                g.GroupTeams.Select(gt => new TeamResult(gt.Team.Name)).ToList()
            )).ToList();

            return new DrawResult(draw.DrawnBy, draw.CreatedAt, groups);
        }
    }