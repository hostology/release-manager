using Hostology.ReleaseManager.Configuration;
using SlackAPI;

namespace Hostology.ReleaseManager.Clients;

public interface ISlackClient
{
    Task<(bool ok, string error)> SendMessage(string message, SlackConfiguration configuration);
}

public sealed class SlackClient : ISlackClient
{
    public async Task<(bool ok, string error)> SendMessage(string message, SlackConfiguration configuration)
    {
        var client = new SlackTaskClient(configuration.Token);
        var messageAsync = await client.PostMessageAsync(configuration.Channel, message);
        return (messageAsync.ok, messageAsync.error);
    }
}