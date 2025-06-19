using Services.Models;

namespace Services.Abstraction.Repositories;

public interface IDrawRepository
{
    Task SaveDrawAsync(DrawResult drawResult, int numberOfGroups);
    Task<List<DrawResult>> GetAllDrawsAsync();
    Task<DrawResult?> GetDrawByIdAsync(int id);
}