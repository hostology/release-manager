namespace Hostology.ReleaseManager.Models;

public class Issue
{
    public string Id { get; set; }
    public string Status { get; set; }
    public string[] Labels { get; set; }
}