namespace Services.Entities
{
    public class Draw
    {
        public int Id { get; set; }
        public string DrawnBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int NumberOfGroups { get; set; }
        public List<Group> Groups { get; set; } = new();

        public Draw(string drawnBy, int numberOfGroups)
        {
            DrawnBy = drawnBy;
            NumberOfGroups = numberOfGroups;
            CreatedAt = DateTime.UtcNow;
        }

        public Draw() { } // For EF Core
    }
}