namespace Services.Models;

public class DrawResult
{
    public string DrawnBy { get; }
    public DateTime CreatedAt { get; }
    public List<GroupResult> Groups { get; }

    public DrawResult(string drawnBy, DateTime createdAt, List<GroupResult> groups)
    {
        DrawnBy = drawnBy;
        CreatedAt = createdAt;
        Groups = groups;
    }
}

public class GroupResult
{
    public string GroupName { get; }
    public List<TeamResult> Teams { get; }

    public GroupResult(string groupName, List<TeamResult> teams)
    {
        GroupName = groupName;
        Teams = teams;
    }
}

public class TeamResult
{
    public string Name { get; }

    public TeamResult(string name)
    {
        Name = name;
    }
}