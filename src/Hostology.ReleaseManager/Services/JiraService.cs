using Hostology.ReleaseManager.Clients;
using Microsoft.Extensions.Logging;
using ReleaseManagerConfiguration = Hostology.ReleaseManager.Configuration.Configuration;

namespace Hostology.ReleaseManager.Services;

public interface IJiraService
{
    Task<bool> CheckIsCommitReleasable(string jiraTicket, ReleaseManagerConfiguration configuration);
}

public sealed class JiraService : IJiraService
{
    private readonly IJiraClient _jiraClient;
    private readonly ILogger<JiraService> _logger;

    public JiraService(IJiraClient jiraClient, ILogger<JiraService> logger)
    {
        _jiraClient = jiraClient ?? throw new ArgumentNullException(nameof(jiraClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> CheckIsCommitReleasable(string jiraTicket, ReleaseManagerConfiguration configuration)
    {
        var acceptableLabels = configuration.Jira.ReleasableLabels;
        var labels = await _jiraClient.GetLabels(jiraTicket, configuration.Jira);
        foreach (var label in labels)
        {
            _logger.LogDebug("Jira ticket {Ticket} has {Label} label.", jiraTicket, label);
        }

        return labels.Any(label => acceptableLabels.Contains(label));
    }
}