using Jellyfin.Plugin.FavoritedSongsPlaylist.Events;
using Jellyfin.Plugin.FavoritedSongsPlaylist.Services;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.FavoritedSongsPlaylist;

/// <summary>
/// Fuck this how do I make it not treat every little thing as an error.
/// I have made many C# projects in the past and this is the first one I have ever had it treat every little thing as an error.
/// </summary>
public class PluginServiceRegistrator : IPluginServiceRegistrator
{
    /// <summary>
    /// Registers services with the dependency injection container.
    /// </summary>
    /// <param name="serviceCollection">Service collection.</param>
    /// <param name="applicationHost">Application Host.</param>
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        serviceCollection.AddScoped<FavoritedSongsPlaylistService>();
        serviceCollection.AddHostedService<FavoritedSongsPlaylistEventListener>();
    }
}