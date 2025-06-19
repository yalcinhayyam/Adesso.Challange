namespace Services.Entities
{
    public class GroupTeam
    {
        public int GroupId { get; set; }
        public Group Group { get; set; } = null!;
        
        public int TeamId { get; set; }
        public Team Team { get; set; } = null!;
    }
}
