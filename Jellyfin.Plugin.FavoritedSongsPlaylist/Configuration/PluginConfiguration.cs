namespace Jellyfin.Plugin.FavoritedSongsPlaylist.Configuration;

using MediaBrowser.Model.Plugins;

/// <summary>
/// Plugin configuration.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginConfiguration"/> class.
    /// </summary>
    public PluginConfiguration()
    {
        this.PlaylistName = "{username}'s: Favorited Songs";
    }

    /// <summary>
    /// Gets or sets the name of the playlist to create.
    /// </summary>
    public string PlaylistName { get; set; }
}
