using Hostology.ReleaseManager.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace Hostology.ReleaseManager.Tests;

[TestFixture]
public class IntegrationTestsBase
{
    protected Configuration.Configuration Configuration;
    
    [SetUp]
    public void Setup()
    {
        var configurationProvider = new ConfigurationProvider(Mock.Of<ILogger<ConfigurationProvider>>());
        Configuration = configurationProvider.Get(null);
    }
}