namespace Hostology.ReleaseManager.Configuration;

public class JiraConfiguration
{
    public string Url { get; init; }
    public string Username { get; init; }
    public string Password { get; init; }
    public string[] ReleasableLabels { get; init; }
    public ProjectConfiguration Project { get; init; }
}