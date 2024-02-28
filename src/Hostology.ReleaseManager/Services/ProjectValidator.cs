using System.Text;
using Hostology.ReleaseManager.Clients;
using Hostology.ReleaseManager.Configuration;
using Hostology.ReleaseManager.Models;
using Microsoft.Extensions.Logging;

namespace Hostology.ReleaseManager.Services;

public interface IProjectValidator
{
    Task ValidateJiraTasks(Configuration.Configuration configuration, bool sendMessageToSlack);
}

public class ProjectValidator : IProjectValidator
{
    private readonly IJiraClient _jiraClient;
    private readonly ISlackClient _slackClient;
    private readonly ILogger<ProjectValidator> _logger;

    public ProjectValidator(IJiraClient jiraClient, ISlackClient slackClient, ILogger<ProjectValidator> logger)
    {
        _jiraClient = jiraClient ?? throw new ArgumentNullException(nameof(jiraClient));
        _slackClient = slackClient ?? throw new ArgumentNullException(nameof(slackClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task ValidateJiraTasks(Configuration.Configuration configuration, bool sendMessageToSlack)
    {
        var issues = await GetIncorrectJiraIssues(configuration);
        _logger.LogInformation("Found {IssuesCount} incorrect jira issues.", issues.Length);
        if(!issues.Any()) return;

        var message = BuildMessage(issues, configuration.Jira);
        _logger.LogInformation("Validation message:\n {Message}", message);
        if (sendMessageToSlack)
        {
            _logger.LogInformation("Sending message about incorrect to slack.");
            var result = await _slackClient.SendMessage(message, configuration.Slack);

            if (!result.ok)
            {
                _logger.LogWarning("Failed to send message to slack. Error: {Error}", result.error);
            }
        }
        else
        {
            _logger.LogInformation("Message won't be send to slack.");
        }

        throw new Exception("Project validation failed. See logs for more details.");
    }

    private async Task<Issue[]> GetIncorrectJiraIssues(Configuration.Configuration configuration)
    {
        var issues = await _jiraClient.GetIssues(configuration.Jira);

        var labelsForStatus = configuration.Jira.Project.Rules;

        var incorrectIssues = new List<Issue>();
        foreach (var issue in issues)
        {
            if(!labelsForStatus.Any(c => c.Status.Equals(issue.Status, StringComparison.InvariantCultureIgnoreCase))) 
                throw new Exception($"Unable to find Jira rules for {issue.Status}. Please check configuration.");

            var rules = labelsForStatus.SingleOrDefault(c => c.Status.Equals(issue.Status, StringComparison.InvariantCultureIgnoreCase));
            var hasCorrectLabel = issue
                .Labels
                .Any(label => rules.Labels.Any(ac => ac.Equals(label)));
            if (!hasCorrectLabel)
            {
                _logger.LogDebug("Found issue {IssueId} with incorrect labels", issue.Id);
                incorrectIssues.Add(issue);
            }
        }

        return incorrectIssues.ToArray();
    }

    private static string BuildMessage(Issue[] issues, JiraConfiguration configuration)
    {
        var issuesMessage = new StringBuilder();
        foreach (var issue in issues)
        {
            var message = issue.Labels.Any()
                ? string.Format(configuration.Project.IncorrectIssueTemplate, configuration.Url, issue.Id, issue.Status, string.Join(',', issue.Labels))
                : string.Format(configuration.Project.MissingLabelsTemplate, configuration.Url, issue.Id, issue.Status);
            issuesMessage.AppendLine(message);
        }

        return string.Format(configuration.Project.FailedMessageTemplate, issuesMessage);
    }
}