using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Hostology.ReleaseManager.Services;

public interface IRepositoryService
{
    Version IncrementVersion(Version version);
    Task<Version> GetVersionFromPackageJson(string repositoryPath);
    Task<Version> UpdateVersionInPackageJson(string repositoryPath, Version version);
}

public sealed class RepositoryService : IRepositoryService
{
    public async Task<Version> GetVersionFromPackageJson(string repositoryPath)
    {
        var packageJson = await GetPackageJson(repositoryPath);

        return GetVersionFromPackageJson(repositoryPath, packageJson);
    }

    public Version IncrementVersion(Version version)
    {
        return new Version(version.Major, version.Minor, version.Build + 1);
    }
    
    public async Task<Version> UpdateVersionInPackageJson(string repositoryPath, Version version)
    {
        var packageFilePath = GetPackageJsonPath(repositoryPath);
        var packageJson = await GetPackageJson(repositoryPath);
        
        packageJson["version"] = version.ToString();
        var updatedJson = JsonConvert.SerializeObject(packageJson, Formatting.Indented);
        await File.WriteAllTextAsync(packageFilePath, updatedJson);
        return version;
    }

    private static Version GetVersionFromPackageJson(string repositoryPath, JObject packageJson)
    {
        var containsVersion = packageJson.ContainsKey("version");
        if(!containsVersion) throw new Exception($"Package.json does not contains version.");
        var versionString = packageJson["version"]!.Value<string>();
        
        var parsableVersion = Version.TryParse(versionString, out var version);
        if (!parsableVersion) throw new Exception($"Unable to parse version in {repositoryPath}");
        if (version is null) throw new Exception($"Version is null in {repositoryPath}");

        return version;
    }

    private static async Task<JObject> GetPackageJson(string repositoryPath)
    {
        var packageFilePath = GetPackageJsonPath(repositoryPath);
        var packageExist = File.Exists(packageFilePath);
        if (!packageExist) throw new Exception($"Package.json does not exist in {repositoryPath}");
        var packageText = await File.ReadAllTextAsync(packageFilePath);

        var packageJson = JObject.Parse(packageText);
        return packageJson;
    }

    private static string GetPackageJsonPath(string repositoryPath)
    {
        return Path.Combine(repositoryPath, "package.json");
    }
}