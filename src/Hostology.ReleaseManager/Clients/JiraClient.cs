using Atlassian.Jira;
using Hostology.ReleaseManager.Configuration;

namespace Hostology.ReleaseManager.Clients;

public interface IJiraClient
{
    Task<string[]> GetLabels(string issueKey, JiraConfiguration configuration);
}

public sealed class JiraClient : IJiraClient
{
    public async Task<string[]> GetLabels(string issueKey, JiraConfiguration configuration)
    {
        var client = Jira.CreateRestClient(
            configuration.Url, 
            configuration.Username, 
            configuration.Password);
        var issue = await client.Issues.GetIssueAsync(issueKey);

        return issue
            .Labels
            .Select(c => c)
            .ToArray();
    }
}