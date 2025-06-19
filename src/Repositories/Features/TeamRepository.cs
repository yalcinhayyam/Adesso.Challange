using AdessoLeague.Repositories.Contexts;
using Microsoft.EntityFrameworkCore;
using Services.Entities;
using Services.Abstraction.Repositories;

namespace Repositories.Features;
public class TeamRepository : ITeamRepository
{
    private readonly AdessoLeagueDbContext _context;

    public TeamRepository(AdessoLeagueDbContext context)
    {
        _context = context;
    }

    public async Task<List<Team>> GetAllTeamsAsync()
    {
        return await _context.Teams.ToListAsync();
    }

    public async Task<List<Team>> GetTeamsByCountryAsync(string country)
    {
        return await _context.Teams.Where(t => t.Country == country).ToListAsync();
    }

    public async Task EnsureTeamsExistAsync()
    {
        if (!await _context.Teams.AnyAsync())
        {
            var teams = new List<Team>
                {
                    // Türkiye
                    new("Adesso İstanbul", "Türkiye", "İstanbul"),
                    new("Adesso Ankara", "Türkiye", "Ankara"),
                    new("Adesso İzmir", "Türkiye", "İzmir"),
                    new("Adesso Antalya", "Türkiye", "Antalya"),

                    // Almanya
                    new("Adesso Berlin", "Almanya", "Berlin"),
                    new("Adesso Frankfurt", "Almanya", "Frankfurt"),
                    new("Adesso Münih", "Almanya", "Münih"),
                    new("Adesso Dortmund", "Almanya", "Dortmund"),

                    // Fransa
                    new("Adesso Paris", "Fransa", "Paris"),
                    new("Adesso Marsilya", "Fransa", "Marsilya"),
                    new("Adesso Nice", "Fransa", "Nice"),
                    new("Adesso Lyon", "Fransa", "Lyon"),

                    // Hollanda
                    new("Adesso Amsterdam", "Hollanda", "Amsterdam"),
                    new("Adesso Rotterdam", "Hollanda", "Rotterdam"),
                    new("Adesso Lahey", "Hollanda", "Lahey"),
                    new("Adesso Eindhoven", "Hollanda", "Eindhoven"),

                    // Portekiz
                    new("Adesso Lisbon", "Portekiz", "Lisbon"),
                    new("Adesso Porto", "Portekiz", "Porto"),
                    new("Adesso Braga", "Portekiz", "Braga"),
                    new("Adesso Coimbra", "Portekiz", "Coimbra"),

                    // İtalya
                    new("Adesso Roma", "İtalya", "Roma"),
                    new("Adesso Milano", "İtalya", "Milano"),
                    new("Adesso Venedik", "İtalya", "Venedik"),
                    new("Adesso Napoli", "İtalya", "Napoli"),

                    // İspanya
                    new("Adesso Sevilla", "İspanya", "Sevilla"),
                    new("Adesso Madrid", "İspanya", "Madrid"),
                    new("Adesso Barselona", "İspanya", "Barselona"),
                    new("Adesso Granada", "İspanya", "Granada"),

                    // Belçika
                    new("Adesso Brüksel", "Belçika", "Brüksel"),
                    new("Adesso Brugge", "Belçika", "Brugge"),
                    new("Adesso Gent", "Belçika", "Gent"),
                    new("Adesso Anvers", "Belçika", "Anvers")
                };

            _context.Teams.AddRange(teams);
            await _context.SaveChangesAsync();
        }
    }
}