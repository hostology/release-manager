namespace Hostology.ReleaseManager.Configuration;

public class JiraConfiguration
{
    public string Url { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public string[] ReleasableLabels { get; set; }
}