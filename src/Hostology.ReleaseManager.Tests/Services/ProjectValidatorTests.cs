using Hostology.ReleaseManager.Clients;
using Hostology.ReleaseManager.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace Hostology.ReleaseManager.Tests.Services;

public class ProjectValidatorTests : IntegrationTestsBase
{
    [Test]
    [Explicit]
    public async Task ProjectValidator_ValidateJiraTasks()
    {
        var validator = new ProjectValidator(new JiraClient(), new SlackClient(), Mock.Of<ILogger<ProjectValidator>>());

        await validator.ValidateJiraTasks(Configuration, true);
    }
}