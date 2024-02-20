namespace Hostology.ReleaseManager.Configuration;

public class Configuration
{
    public List<RepositoryConfiguration> Repositories { get; init; }
    public GitConfiguration Git { get; init; }
    public JiraConfiguration Jira { get; init; }
    public SlackConfiguration Slack { get; init; }
}