using Hostology.ReleaseManager.Clients;
using Hostology.ReleaseManager.Configuration;
using Microsoft.Extensions.Logging;

namespace Hostology.ReleaseManager.Services;

public interface IReleaseManager
{
    Task Handle(string? configurationPath, bool noSlack, bool skipValidation, bool dryRun);
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

    public async Task Handle(string? configurationPath, bool noSlack, bool skipValidation, bool dryRun)
    {
        try
        {
            var cliConfiguration = new CLIConfiguration
            {
                NoSlack = noSlack,
                SkipValidation = skipValidation,
                DryRun = dryRun,
            };
            var configuration = _configurationProvider.Get(configurationPath);
            if(!cliConfiguration.SkipValidation) await _projectValidator.ValidateJiraTasks(configuration, !cliConfiguration.NoSlack);

            foreach (var repository in configuration.Repositories)
            {
                _logger.LogInformation("Handling repository at {Path}", repository.Path);
                await _repositoryHandler.HandleRepositoryRelease(repository, configuration, cliConfiguration);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
        }
    }
}