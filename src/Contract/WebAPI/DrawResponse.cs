namespace WebAPI.Contracts;

public class DrawResponse
{
    public List<GroupDto> Groups { get; set; } = new();
    public string DrawnBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class GroupDto
{
    public string GroupName { get; set; } = string.Empty;
    public List<TeamDto> Teams { get; set; } = new();
}

public class TeamDto
{
    public string Name { get; set; } = string.Empty;
}