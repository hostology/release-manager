using Hostology.ReleaseManager.Configuration;
using Microsoft.Extensions.Logging;
using ReleaseManagerConfiguration = Hostology.ReleaseManager.Configuration.Configuration;
using ReleaseCommit = Hostology.ReleaseManager.Models.Commit;

namespace Hostology.ReleaseManager.Services;

public interface IRepositoryReleaseService
{
    Task ReleaseNewVersion(
        RepositoryConfiguration repositoryConfiguration,
        ReleaseManagerConfiguration configuration,
        ReleaseCommit lastReleasableCommit, 
        bool includeVersionCommit);
}

public sealed class RepositoryReleaseService : IRepositoryReleaseService
{
    private readonly IRepositoryService _repositoryService;
    private readonly IGitService _gitService;
    private readonly ILogger<RepositoryReleaseService> _logger;

    public RepositoryReleaseService(IRepositoryService repositoryService, IGitService gitService, ILogger<RepositoryReleaseService> logger)
    {
        _repositoryService = repositoryService ?? throw new ArgumentNullException(nameof(repositoryService));
        _gitService = gitService ?? throw new ArgumentNullException(nameof(gitService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task ReleaseNewVersion(
        RepositoryConfiguration repositoryConfiguration,
        ReleaseManagerConfiguration configuration,
        ReleaseCommit lastReleasableCommit, 
        bool includeVersionCommit)
    {
        var newRepositoryConfiguration = await GetNewTagNameAndVersion(repositoryConfiguration, configuration.Git);
        ValidateRepository(repositoryConfiguration.Path, newRepositoryConfiguration.TagName, configuration.Git);
        
        await _repositoryService.UpdateVersionInPackageJson(repositoryConfiguration.Path, newRepositoryConfiguration.Version);
        var message = string.Format(configuration.Git.IncrementVersionMessageTemplate, newRepositoryConfiguration.Version);
        LibGit2Sharp.Commit versionCommit;
        try
        {
            _logger.LogDebug("Staging and commiting changes in {path}.", repositoryConfiguration.Path);
            versionCommit = _gitService.StageAndCommitChanges(repositoryConfiguration.Path, message, configuration.Git);
        }
        catch (Exception)
        {
            _logger.LogDebug("Failed to stage and commit changes. Resetting repository {RepositoryPath}", repositoryConfiguration.Path);
            _gitService.RemoveLocalChanges(repositoryConfiguration.Path);
            throw;
        }

        try
        {
            _logger.LogDebug("Staging and commiting changes in {path}.", repositoryConfiguration.Path);
            _gitService.PushChanges(versionCommit, repositoryConfiguration.Path, configuration.Git);
        }
        catch (Exception)
        {
            _logger.LogDebug("Failed to stage and commit changes. Resetting repository {RepositoryPath}", repositoryConfiguration.Path);
            _gitService.RemoveCommit(repositoryConfiguration.Path, versionCommit.Sha);
            _gitService.RemoveLocalChanges(repositoryConfiguration.Path);
            throw;
        }

        var tagCommit = includeVersionCommit ? versionCommit.Sha : lastReleasableCommit.Sha;
        _gitService.AssignAndPushTag(repositoryConfiguration.Path, configuration.Git, tagCommit, newRepositoryConfiguration.TagName);
    }

    private void ValidateRepository(string repositoryPath, string tagName, GitConfiguration gitConfiguration)
    {
        if(_gitService.TagExist(repositoryPath, tagName, gitConfiguration)) 
            throw new Exception($"{tagName} tag already exist in repository: {repositoryPath}");
    }

    private async Task<(Version Version, string TagName)> GetNewTagNameAndVersion(RepositoryConfiguration repositoryConfiguration, GitConfiguration gitConfiguration)
    {
        var currentVersion = await _repositoryService.GetVersionFromPackageJson(repositoryConfiguration.Path);
        var newVersion = _repositoryService.IncrementVersion(currentVersion);
        var newTagName = $"{gitConfiguration.UatVersionPrefix}{newVersion.ToString()}";

        return (newVersion, newTagName);
    }
}