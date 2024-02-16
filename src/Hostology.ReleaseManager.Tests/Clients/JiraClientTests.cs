using Hostology.ReleaseManager.Clients;

namespace Hostology.ReleaseManager.Tests.Clients;

public class JiraClientTests : IntegrationTestsBase
{
    [Test]
    [Explicit]
    public async Task JiraClient_GetIssues()
    {
        var jiraClient = new JiraClient();

        var issues = await jiraClient.GetIssues(Configuration.Jira);
        
        Assert.That(issues, Is.Not.Empty);
    }
}