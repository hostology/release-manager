namespace Hostology.ReleaseManager.Configuration;

public class Configuration
{
    public List<Repository> Repositories { get; set; }
    public string MasterBranch { get; set; }
}

public class Repository
{
    public string Path { get; set; }
}