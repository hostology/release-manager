using Hostology.ReleaseManager.Clients;
using Hostology.ReleaseManager.Configuration;
using Microsoft.Extensions.Logging;

namespace Hostology.ReleaseManager.Services;

public interface IReleaseManager
{
    Task Handle(string? configurationPath, bool noSlack);
}

public sealed class ReleaseManager : IReleaseManager
{
    private readonly IConfigurationProvider _configurationProvider;
    private readonly ILogger<ReleaseManager> _logger;
    private readonly IRepositoryHandler _repositoryHandler;
    private readonly IProjectValidator _projectValidator;

    public ReleaseManager(
        IConfigurationProvider configurationProvider, 
        ILogger<ReleaseManager> logger,
        IRepositoryHandler repositoryHandler,
        IProjectValidator projectValidator)
    {
        _configurationProvider = configurationProvider ?? throw new ArgumentNullException(nameof(configurationProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _repositoryHandler = repositoryHandler ?? throw new ArgumentNullException(nameof(repositoryHandler));
        _projectValidator = projectValidator ?? throw new ArgumentNullException(nameof(projectValidator));
    }

    public async Task Handle(string? configurationPath, bool noSlack)
    {
        try
        {
            var configuration = _configurationProvider.Get(configurationPath);
            await _projectValidator.ValidateJiraTasks(configuration, !noSlack);

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