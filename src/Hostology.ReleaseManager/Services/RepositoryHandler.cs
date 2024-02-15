using Hostology.ReleaseManager.Configuration;
using Hostology.ReleaseManager.Models;
using Microsoft.Extensions.Logging;
using ReleaseManagerConfiguration = Hostology.ReleaseManager.Configuration.Configuration;

namespace Hostology.ReleaseManager.Services;

public interface IRepositoryHandler
{
    Task HandleRepositoryRelease(RepositoryConfiguration repository, ReleaseManagerConfiguration configuration);
}

public sealed class RepositoryHandler : IRepositoryHandler
{
    private readonly IGitService _gitService;
    private readonly IJiraService _jiraService;
    private readonly ILogger<RepositoryHandler> _logger;

    public RepositoryHandler(
        IGitService gitService, 
        ILogger<RepositoryHandler> logger,
        IJiraService jiraService)
    {
        _gitService = gitService ?? throw new ArgumentNullException(nameof(gitService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _jiraService = jiraService ?? throw new ArgumentNullException(nameof(jiraService));
    }

    public async Task HandleRepositoryRelease(RepositoryConfiguration repository, ReleaseManagerConfiguration configuration)
    {
        var commits = _gitService
            .GetUnreleasedCommits(repository.Path, configuration.MasterBranch);

        var lastReleasableCommit = await GetLastReleasableCommit(commits, configuration);
        if (lastReleasableCommit is null)
        {
            _logger.LogInformation("No new commits to release.");
            return;
        }
        
        _logger.LogInformation(
            "New release candidate found task: {JiraTicket}, sha: {Sha}.", 
            lastReleasableCommit.JiraTicket, 
            lastReleasableCommit.Sha);
    }

    private async Task<Commit?> GetLastReleasableCommit(List<Commit> commits, ReleaseManagerConfiguration configuration)
    {
        Commit? lastReleasableCommit = null;
        foreach (var commit in commits)
        {
            _logger.LogInformation("Found unreleased commit for Jira ticket {JiraTicket} with hash {Sha}.", commit.JiraTicket, commit.Sha);
            if (string.IsNullOrWhiteSpace(commit.JiraTicket))
            {
                _logger.LogDebug("Commit with hash {Sha} has no Jira ticket assigned and it will be released.", commit.Sha);
                lastReleasableCommit = commit;
            }
            else
            {
                var releasable = await _jiraService.CheckIsCommitReleasable(commit.JiraTicket, configuration);
                _logger.LogDebug(releasable 
                    ? $"Commit with ticket {commit.JiraTicket} can be released." 
                    : $"Commit with ticket {commit.JiraTicket} can't be released.");
                
                if (!releasable) return lastReleasableCommit;
                lastReleasableCommit = commit;
            }
        }

        return lastReleasableCommit;
    }
}