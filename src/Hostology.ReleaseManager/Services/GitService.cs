using System.Text.RegularExpressions;
using Hostology.ReleaseManager.Configuration;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using Microsoft.Extensions.Logging;
using Version = System.Version;
using ReleaseCommit = Hostology.ReleaseManager.Models.Commit;
using System.IO;

namespace Hostology.ReleaseManager.Services;

public interface IGitService
{
    void UpdateLocalRepo(string path, GitConfiguration gitConfiguration);
    List<ReleaseCommit> GetUnreleasedCommits(
        string path,
        string masterBranch, 
        string jiraTaskId, 
        string releasePrefix);
    Commit StageAndCommitChanges(string path, string message, GitConfiguration gitConfiguration);
    Commit PushChanges(Commit commit, string path, GitConfiguration gitConfiguration);
    void AssignAndPushTag(
        string path,
        GitConfiguration gitConfiguration,
        string commitSha,
        string tagName);
    void RemoveLocalChanges(string path);
    void RemoveCommit(string path, string commitSha);
    bool TagExist(string repositoryPath, string tagName, GitConfiguration gitConfiguration);
}

public sealed partial class GitService : IGitService
{
    private readonly ILogger<GitService> _logger;

    public GitService(ILogger<GitService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void UpdateLocalRepo(string path, GitConfiguration gitConfiguration)
    {
        using var repository = new Repository(path);

        var signature = new Signature(gitConfiguration.Name, gitConfiguration.Email, DateTimeOffset.Now);

        Commands.Checkout(repository, repository.Branches[gitConfiguration.MasterBranch]);

        Commands.Pull(repository, signature, new PullOptions
        {
            FetchOptions = new FetchOptions
            {
                TagFetchMode = TagFetchMode.All,
                CredentialsProvider = (url, fromUrl, types) => GetCredentialsHandler(gitConfiguration)
            }
        });

        Commands.Fetch(repository, "origin", new string[] {}, new FetchOptions
        { 
            TagFetchMode = TagFetchMode.All,
            CredentialsProvider = (url, fromUrl, types) => GetCredentialsHandler(gitConfiguration)
        }, null);
    }

    public List<ReleaseCommit> GetUnreleasedCommits(
        string path,
        string masterBranch, 
        string jiraTaskId, 
        string releasePrefix)
    {
        using var repository = new Repository(path);
        var branch = repository
            .Branches
            .SingleOrDefault(c => c.FriendlyName.Equals(masterBranch, StringComparison.InvariantCultureIgnoreCase));
        if (branch is null) throw new Exception($"Unable to find {masterBranch} in repository.");
        
        var tags = GetUATTags(repository, releasePrefix);
        _logger.LogDebug($"Found {tags.Count} UAT tags.");

        if (!tags.Any()) return new List<Models.Commit>();
        
        var latestTag = tags.First();
        _logger.LogInformation($"Latest UAT Tag: {latestTag.Tag.FriendlyName}");

        var commits = GetCommitsSinceLastUATTag(repository, latestTag.Commit, masterBranch, jiraTaskId);
        return commits;
    }

    public Commit StageAndCommitChanges(string path, string message, GitConfiguration gitConfiguration)
    {
        using var repository = new Repository(path);
        var signature = new Signature(gitConfiguration.Name, gitConfiguration.Email, DateTimeOffset.Now);
        Commands.Stage(repository, "package.json");
        var commit = repository.Commit(message, signature, signature, new CommitOptions
        {
            AllowEmptyCommit = false
        });

        return commit;
    }

    public Commit PushChanges(Commit commit, string path, GitConfiguration gitConfiguration)
    {
        using var repository = new Repository(path);
        var remote = repository.Network.Remotes[gitConfiguration.Remote];
        var options = new PushOptions
        {
            CredentialsProvider = (url, fromUrl, types) => GetCredentialsHandler(gitConfiguration)
        };
        
        repository.Network.Push(remote, $"refs/heads/{gitConfiguration.MasterBranch}", options);
        return commit;
    }

    public void AssignAndPushTag(string path,
        GitConfiguration gitConfiguration,
        string commitSha,
        string tagName)
    {
        using var repository = new Repository(path);
        _logger.LogInformation("Creating and pushing tag {tagName} for {path}", tagName, path);
        try
        {
            repository.ApplyTag(tagName, commitSha);
            var options = new PushOptions
            {
                CredentialsProvider = (url, fromUrl, types) => GetCredentialsHandler(gitConfiguration)
            };
        
            var remote = repository.Network.Remotes[gitConfiguration.Remote];
            repository.Network.Push(remote, $"refs/tags/{tagName}", options);
        }
        catch (Exception)
        {
            repository.Tags.Remove(tagName);
            throw;
        }
    }

    public void RemoveLocalChanges(string path)
    {
        using var repository = new Repository(path);
        var commit = repository.Head.Tip;
        
        repository.CheckoutPaths(commit.Sha, new[] { "*" }, new CheckoutOptions
        {
            CheckoutModifiers = CheckoutModifiers.Force
        });
    }

    public void RemoveCommit(string path, string sha)
    {
        using var repository = new Repository(path);
        var lastCommitHasCorrectSha = repository.Head.Commits.First().Sha.Equals(sha);
        if (!lastCommitHasCorrectSha) throw new Exception($"Invalid state in repository, please check last commits. {path}");
        
        var commitToResetTo = repository.Head.Commits.Skip(1).First();
        repository.Reset(ResetMode.Hard, commitToResetTo);
    }

    public bool TagExist(string repositoryPath, string tagName, GitConfiguration gitConfiguration)
    {
        using var repository = new Repository(repositoryPath);

        var tagExistLocally = repository
            .Tags
            .Any(t => t.FriendlyName.Equals(tagName, StringComparison.InvariantCultureIgnoreCase));
        if (tagExistLocally) return true;

        var remote = repository.Network.Remotes[gitConfiguration.Remote];
        var refs = repository.Network.ListReferences(remote, (url, fromUrl, types) => GetCredentialsHandler(gitConfiguration));

        return refs.Any(reference => reference.IsTag && reference.CanonicalName.EndsWith(tagName));
    }

    private List<Models.Commit> GetCommitsSinceLastUATTag(IRepository repository, Commit latestTagCommit, string branch, string jiraTaskId)
    {
        var jiraTagPattern = new Regex($@"\b{jiraTaskId}-\d+\b");
        var versionPatternMatch = new Regex(@"\bversion\b");

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
            var versionMatch = versionPatternMatch.Match(commit.Message);
            if (match.Success)
            {
                var jiraTag = match.Value;
                _logger.LogDebug($"({repository.Info.Path}): Found commit with JIRA ticket {jiraTag}, Commit hash: {commit.Sha}");
                commits.Add(new ReleaseCommit
                {
                    JiraTicket = jiraTag,
                    Sha = commit.Sha,
                });
            }
            else if (versionMatch.Success)
            {
                _logger.LogDebug($"({repository.Info.Path}): Found commit with version update {versionMatch}, Commit hash: {commit.Sha}");
            }
            else
            {
                _logger.LogWarning($"({repository.Info.Path}): Found commit with missing JIRA ticket. Commit hash: {commit.Sha}");
                commits.Add(new ReleaseCommit
                {
                    Sha = commit.Sha
                });
            }
        }

        return commits;
    }

    private List<(Tag Tag, Commit Commit)> GetUATTags(IRepository repository, string releasePrefix)
    {
        var tagPattern = new Regex($@"^{releasePrefix}\d+\.\d+\.\d+$");
        
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

    private UsernamePasswordCredentials GetCredentialsHandler(GitConfiguration gitConfiguration)
    {
        return new UsernamePasswordCredentials
        {
            Username = gitConfiguration.Email,
            Password = gitConfiguration.Token
        };
    }

    [GeneratedRegex(@"^uat/\d+\.\d+\.\d+$")]
    private static partial Regex GetReleaseTagPattern();
}