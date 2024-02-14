using System.Text.RegularExpressions;
using LibGit2Sharp;
using Microsoft.Extensions.Logging;
using Version = System.Version;

namespace Hostology.ReleaseManager.Services;

public interface IGitService
{
    List<Models.Commit> GetUnreleasedCommits(string path, string masterBranch);
}

public sealed partial class GitService : IGitService
{
    private readonly ILogger<GitService> _logger;

    public GitService(ILogger<GitService> logger)
    {
        _logger = logger;
    }

    public List<Models.Commit> GetUnreleasedCommits(string path, string masterBranch)
    {
        using var repository = new Repository(path);
        var branch = repository
            .Branches
            .SingleOrDefault(c => c.FriendlyName.Equals(masterBranch, StringComparison.InvariantCultureIgnoreCase));
        if (branch is null) throw new Exception($"Unable to find {masterBranch} in repository.");
        
        var tags = GetUATTags(repository);
        _logger.LogDebug($"Found {tags.Count} UAT tags.");

        if (!tags.Any()) return new List<Models.Commit>();
        
        var latestTag = tags.First();
        _logger.LogInformation($"Latest UAT Tag: {latestTag.Tag.FriendlyName}");

        return GetCommitsSinceLastUATTag(repository, latestTag.Commit, masterBranch);
    }

    private List<Models.Commit> GetCommitsSinceLastUATTag(IRepository repository, Commit latestTagCommit, string branch)
    {
        var jiraTagPattern = GetJiraTagPattern();

        var filter = new CommitFilter
        {
            SortBy = CommitSortStrategies.Topological | CommitSortStrategies.Time | CommitSortStrategies.Reverse,
            IncludeReachableFrom = repository.Branches[branch].Tip,
            ExcludeReachableFrom = latestTagCommit
        };

        var commitLog = repository
            .Commits
            .QueryBy(filter)
            .ToArray();
        _logger.LogDebug($"Found {commitLog.Length}");
        
        var commits = new List<Models.Commit>();
        foreach (var commit in commitLog)
        {
            var match = jiraTagPattern.Match(commit.Message);
            if (match.Success)
            {
                var jiraTag = match.Value;
                _logger.LogDebug($"Found commit with JIRA ticket {jiraTag}, Commit hash: {commit.Sha}");
                commits.Add(new Models.Commit
                {
                    JiraTicket = jiraTag,
                    Sha = commit.Sha,
                });
            }
            else
            {
                _logger.LogWarning($"Found commit with missing JIRA ticket. Commit hash: {commit.Sha}");
                commits.Add(new Models.Commit
                {
                    Sha = commit.Sha
                });
            }
        }

        return commits;
    }

    private List<(Tag Tag, Commit Commit)> GetUATTags(IRepository repository)
    {
        var tagPattern = GetReleaseTagPattern();
        
        return repository.Tags
            .Where(tag => tagPattern.IsMatch(tag.FriendlyName))
            .Select(tag => new
            {
                Tag = tag,
                Commit = tag.Target.Peel<Commit>(),
                Version = tag.FriendlyName.Split('/').Last()
            })
            .OrderByDescending(tag => Version.Parse(tag.Version))
            .Select(c => (c.Tag, c.Commit))
            .ToList();
    }

    [GeneratedRegex(@"^uat/\d+\.\d+\.\d+$")]
    private static partial Regex GetReleaseTagPattern();
    [GeneratedRegex(@"\bHOS-\d+\b")]
    private static partial Regex GetJiraTagPattern();
}