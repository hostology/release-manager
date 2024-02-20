using Hostology.ReleaseManager.Services;

namespace Hostology.ReleaseManager.Tests.Services;

public class RepositoryServiceTests : IntegrationTestsBase
{
    [Test]
    [Explicit]
    public async Task GetVersion()
    {
        var test = Configuration.Repositories.First().Path;

        var repositoryService = new RepositoryService();

        await repositoryService.UpdateVersionInPackageJson(test, new Version(2,0,0));
    }
}