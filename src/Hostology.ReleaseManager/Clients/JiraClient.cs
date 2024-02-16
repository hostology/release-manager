using System.Text;
using Atlassian.Jira;
using Hostology.ReleaseManager.Configuration;
using Microsoft.Extensions.Primitives;
using IssueModel = Hostology.ReleaseManager.Models.Issue;

namespace Hostology.ReleaseManager.Clients;

public interface IJiraClient
{
    Task<string[]> GetLabels(string issueKey, JiraConfiguration configuration);
    Task<IssueModel[]> GetIssues(JiraConfiguration configuration);
}

public sealed class JiraClient : IJiraClient
{
    public async Task<string[]> GetLabels(string issueKey, JiraConfiguration configuration)
    {
        var client = GetClient(configuration);
        var issue = await client.Issues.GetIssueAsync(issueKey);

        return issue
            .Labels
            .Select(c => c)
            .ToArray();
    }

    public async Task<IssueModel[]> GetIssues(JiraConfiguration configuration)
    {
        var client = GetClient(configuration);
        var query = BuildQuery(configuration.Project);
        var issues = await client.Issues.GetIssuesFromJqlAsync(query);
        
        return issues.Select(c => new IssueModel
        {
            Labels = c.Labels.ToArray(),
            Status = c.Status.Name,
            Id = c.Key.ToString(),
        }).ToArray();
    }

    private static Jira GetClient(JiraConfiguration configuration)
    {
        return Jira.CreateRestClient(
            configuration.Url, 
            configuration.Username, 
            configuration.Password);
    }

    private static string BuildQuery(ProjectConfiguration projectConfiguration)
    {
        var query = new StringBuilder($"project = {projectConfiguration.Id}");
        if (!projectConfiguration.Rules.Any()) return query.ToString();
        
        var statuses = projectConfiguration.Rules.Select(c => $"'{c.Status}'");
        var statusQuery = $" and status in({string.Join(',', statuses)})";
        query.Append(statusQuery);

        return query.ToString();
    }
}