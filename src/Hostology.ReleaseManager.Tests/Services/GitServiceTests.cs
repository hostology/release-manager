using Hostology.ReleaseManager.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace Hostology.ReleaseManager.Tests.Services;

public class GitServiceTests : IntegrationTestsBase
{
    [Test]
    [Explicit]
    public async Task RemoveLocalChanges()
    {
        var repositoryConfiguration = Configuration.Repositories.First();
        var repositoryService = new RepositoryService();
        var _ = await repositoryService.UpdateVersionInPackageJson(repositoryConfiguration.Path, new Version(2,0,0));

        var sut = new GitService(Mock.Of<ILogger<GitService>>());
        
        sut.RemoveLocalChanges(repositoryConfiguration.Path);
    }
    
    [Test]
    [Explicit]
    public async Task CommitChanges()
    {
        var repositoryConfiguration = Configuration.Repositories.First();
        var repositoryService = new RepositoryService();
        var _ = await repositoryService.UpdateVersionInPackageJson(repositoryConfiguration.Path, new Version(2,0,0));

        var sut = new GitService(Mock.Of<ILogger<GitService>>());
        
        sut.StageAndCommitChanges(repositoryConfiguration.Path, "Test message", Configuration.Git);
    }

    [Test]
    [Explicit]
    public async Task ResetCommittedChanges()
    {
        var repositoryConfiguration = Configuration.Repositories.First();
        var repositoryService = new RepositoryService();
        var _ = await repositoryService.UpdateVersionInPackageJson(repositoryConfiguration.Path, new Version(2,0,0));

        var sut = new GitService(Mock.Of<ILogger<GitService>>());
        var commit = sut.StageAndCommitChanges(repositoryConfiguration.Path, "Test message", Configuration.Git);
        
        sut.RemoveCommit(repositoryConfiguration.Path, commit.Sha);
    }

    [Test]
    [Explicit]
    public async Task PushChanges()
    {
        var repositoryConfiguration = Configuration.Repositories.First();
        var repositoryService = new RepositoryService();
        var _ = await repositoryService.UpdateVersionInPackageJson(repositoryConfiguration.Path, new Version(2,0,0));
        
        var sut = new GitService(Mock.Of<ILogger<GitService>>());
        var commit = sut.StageAndCommitChanges(repositoryConfiguration.Path, "Test message", Configuration.Git);

        sut.PushChanges(commit, repositoryConfiguration.Path, Configuration.Git);
    }
}