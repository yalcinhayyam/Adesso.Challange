namespace Services.Entities
{
    public class Group
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int DrawId { get; set; }
        public Draw Draw { get; set; } = null!;
        public List<GroupTeam> GroupTeams { get; set; } = new();

        public Group(string name)
        {
            Name = name;
        }

        public Group() { } // For EF Core

        public void AddTeam(Team team)
        {
            GroupTeams.Add(new GroupTeam { Group = this, Team = team });
        }

        public List<Team> GetTeams() => GroupTeams.Select(gt => gt.Team).ToList();
    }
}