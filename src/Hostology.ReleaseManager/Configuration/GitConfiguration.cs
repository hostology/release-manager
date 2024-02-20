namespace Hostology.ReleaseManager.Configuration;

public class GitConfiguration
{
    public string MasterBranch { get; init; }
    public string IncrementVersionMessageTemplate { get; init; }
    public string Email { get; init; }
    public string Token { get; init; }
    public string Remote { get; init; }
    public string UatVersionPrefix { get; init; }
}