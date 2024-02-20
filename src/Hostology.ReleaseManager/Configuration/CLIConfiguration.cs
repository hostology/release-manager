namespace Hostology.ReleaseManager.Configuration;

public class CLIConfiguration
{
    public bool NoSlack { get; init; }
    public bool SkipValidation { get; init; }
    public bool DryRun { get; init; }
}