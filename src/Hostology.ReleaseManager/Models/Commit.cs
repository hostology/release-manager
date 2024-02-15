namespace Hostology.ReleaseManager.Models;

public class Commit
{
    public string? JiraTicket { get; init; }
    public string Sha { get; init; }
}