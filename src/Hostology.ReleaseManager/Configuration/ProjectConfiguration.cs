namespace Hostology.ReleaseManager.Configuration;

public class ProjectConfiguration
{
    public string Id { get; init; }
    public string FailedMessageTemplate { get; init; }
    public string IncorrectIssueTemplate { get; init; }
    public string MissingLabelsTemplate { get; init; }
    public ProjectRule[] Rules { get; init; }
}