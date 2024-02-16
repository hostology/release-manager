namespace Hostology.ReleaseManager.Configuration;

public class Configuration
{
    public List<RepositoryConfiguration> Repositories { get; init; }
    public string MasterBranch { get; init; }
    public JiraConfiguration Jira { get; init; }
    public SlackConfiguration Slack { get; init; }
}