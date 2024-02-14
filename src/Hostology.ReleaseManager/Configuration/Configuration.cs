namespace Hostology.ReleaseManager.Configuration;

public class Configuration
{
    public List<RepositoryConfiguration> Repositories { get; set; }
    public string MasterBranch { get; set; }
    public JiraConfiguration Jira { get; set; }
}