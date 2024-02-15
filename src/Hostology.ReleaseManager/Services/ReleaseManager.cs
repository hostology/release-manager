using Hostology.ReleaseManager.Clients;
using Hostology.ReleaseManager.Configuration;
using Microsoft.Extensions.Logging;

namespace Hostology.ReleaseManager.Services;

public interface IReleaseManager
{
    Task Handle(string? configurationPath);
}

public sealed class ReleaseManager : IReleaseManager
{
    private readonly IConfigurationProvider _configurationProvider;
    private readonly ILogger<ReleaseManager> _logger;
    private readonly IRepositoryHandler _repositoryHandler;

    public ReleaseManager(
        IConfigurationProvider configurationProvider, 
        ILogger<ReleaseManager> logger,
        IRepositoryHandler repositoryHandler)
    {
        _configurationProvider = configurationProvider ?? throw new ArgumentNullException(nameof(configurationProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _repositoryHandler = repositoryHandler ?? throw new ArgumentNullException(nameof(repositoryHandler));
    }

    public async Task Handle(string? configurationPath)
    {
        try
        {
            var configuration = _configurationProvider.Get(configurationPath);

            foreach (var repository in configuration.Repositories)
            {
                _logger.LogInformation("Handling repository at {Path}", repository.Path);
                await _repositoryHandler.HandleRepositoryRelease(repository, configuration);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
        }
    }
}