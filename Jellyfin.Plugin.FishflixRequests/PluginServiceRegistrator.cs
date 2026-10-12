using Jellyfin.Plugin.FishflixRequests.Jellyseerr;
using Jellyfin.Plugin.FishflixRequests.Services;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.FishflixRequests;

/// <summary>
/// Registers the plugin's services with the Jellyfin DI container.
/// </summary>
public class PluginServiceRegistrator : IPluginServiceRegistrator
{
    /// <inheritdoc />
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        serviceCollection.AddSingleton<JellyseerrClient>();
        serviceCollection.AddSingleton<RequestService>();
    }
}
