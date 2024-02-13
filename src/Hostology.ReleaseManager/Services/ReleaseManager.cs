using Hostology.ReleaseManager.Configuration;
using Microsoft.Extensions.Logging;

namespace Hostology.ReleaseManager.Services;

public interface IReleaseManager
{
    void Handle(string? configurationPath);
}

public sealed class ReleaseManager : IReleaseManager
{
    private readonly IConfigurationProvider _configurationProvider;
    private readonly ILogger<ReleaseManager> _logger;
    private readonly IGitService _gitService;

    public ReleaseManager(
        IConfigurationProvider configurationProvider, 
        ILogger<ReleaseManager> logger, 
        IGitService gitService)
    {
        _configurationProvider = configurationProvider;
        _logger = logger;
        _gitService = gitService;
    }

    public void Handle(string? configurationPath)
    {
        try
        {
            var configuration = _configurationProvider.Get(configurationPath);

            foreach (var repository in configuration.Repositories)
            {
                _logger.LogInformation($"Handling repository at {repository.Path}");
                var commits = _gitService.GetUnreleasedCommits(repository.Path, configuration.MasterBranch);
                foreach (var commit in commits)
                {
                    _logger.LogInformation($"Found unreleased commit for Jira ticket {commit.JiraTicket} with hash {commit.Sha}.");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
        }
    }
}