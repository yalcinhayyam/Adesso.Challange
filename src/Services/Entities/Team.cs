namespace Services.Entities
{
    public class Team
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;

        public Team(string name, string country, string city)
        {
            Name = name;
            Country = country;
            City = city;
        }

        public Team() { } // For EF Core
    }
}