using Services.Entities;
namespace Services.Abstraction.Repositories;

public interface ITeamRepository
{
    Task<List<Team>> GetAllTeamsAsync();
    Task<List<Team>> GetTeamsByCountryAsync(string country);
    Task EnsureTeamsExistAsync();
}