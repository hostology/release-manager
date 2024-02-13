using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Hostology.ReleaseManager.Configuration;

public interface IConfigurationProvider
{
    Configuration Get(string? configurationPath);
}

public class ConfigurationProvider : IConfigurationProvider
{
    private readonly ILogger<ConfigurationProvider> _logger;
    private const string DefaultConfigName = "config.json";

    public ConfigurationProvider(ILogger<ConfigurationProvider> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Configuration Get(string? configurationPath)
    {
        if (string.IsNullOrWhiteSpace(configurationPath))
        {
            _logger.LogDebug("Missing configurationPath parameter. Looking for a configuration file in current directory.");
            configurationPath = Path.Combine(Directory.GetCurrentDirectory(), DefaultConfigName);
        }

        var configurationFileExist = File.Exists(configurationPath);
        if (!configurationFileExist)
        {
            throw new Exception("Missing configuration file.");
        }

        var configurationString = File.ReadAllText(configurationPath);
        return JsonConvert.DeserializeObject<Configuration>(configurationString);
    }
}