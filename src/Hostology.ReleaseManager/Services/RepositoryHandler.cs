using Hostology.ReleaseManager.Configuration;
using Microsoft.Extensions.Logging;
using Commit = Hostology.ReleaseManager.Models.Commit;
using ReleaseManagerConfiguration = Hostology.ReleaseManager.Configuration.Configuration;

namespace Hostology.ReleaseManager.Services;

public interface IRepositoryHandler
{
    Task HandleRepositoryRelease(
        RepositoryConfiguration repository, 
        ReleaseManagerConfiguration configuration,
        CLIConfiguration cliConfiguration);
}

public sealed class RepositoryHandler : IRepositoryHandler
{
    private readonly IGitService _gitService;
    private readonly IJiraService _jiraService;
    private readonly ILogger<RepositoryHandler> _logger;
    private readonly IRepositoryReleaseService _repositoryReleaseService;

    public RepositoryHandler(
        IGitService gitService, 
        ILogger<RepositoryHandler> logger,
        IJiraService jiraService,
        IRepositoryReleaseService repositoryReleaseService)
    {
        _gitService = gitService ?? throw new ArgumentNullException(nameof(gitService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _jiraService = jiraService ?? throw new ArgumentNullException(nameof(jiraService));
        _repositoryReleaseService = repositoryReleaseService ?? throw new ArgumentNullException(nameof(repositoryReleaseService));
    }

    public async Task HandleRepositoryRelease(RepositoryConfiguration repository,
        ReleaseManagerConfiguration configuration, CLIConfiguration cliConfiguration)
    {
        Console.WriteLine();
        Console.WriteLine($"Handling repository {repository.Path}");
        Console.WriteLine("=============================================================");

        var unreleasedCommits = _gitService
            .GetUnreleasedCommits(repository.Path, configuration.Git.MasterBranch, configuration.Jira.Project.Id, configuration.Git.UatVersionPrefix);

        _gitService.UpdateLocalRepo(repository.Path, configuration.Git);

        var lastReleasableCommit = await GetLastReleasableCommit(unreleasedCommits, configuration);
        if (lastReleasableCommit.Commit is null)
        {
            _logger.LogInformation("No new commits to release.");
            return;
        }
        
        _logger.LogInformation(
            "New release candidate found task: {JiraTicket}, sha: {Sha}.", 
            lastReleasableCommit.Commit.JiraTicket, 
            lastReleasableCommit.Commit.Sha);

        if (!cliConfiguration.DryRun)
        {
            await _repositoryReleaseService.ReleaseNewVersion(
                repository, 
                configuration, 
                lastReleasableCommit.Commit, 
                lastReleasableCommit.IsLastCommit);
        }
        else
        {
            _logger.LogInformation("Dry run: commits won't be released.");
        }
    }

    private async Task<(Commit? Commit, bool IsLastCommit)> GetLastReleasableCommit(List<Commit> commits, ReleaseManagerConfiguration configuration)
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
                
                if (!releasable) return (lastReleasableCommit, false);
                lastReleasableCommit = commit;
            }
        }

        return (lastReleasableCommit, true);
    }
}