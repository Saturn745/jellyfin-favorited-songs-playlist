using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Model.Playlists;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.FavoritedSongsPlaylist.Services;

/// <summary>
/// Service for creating and managing favorited songs playlists.
/// </summary>
public class FavoritedSongsPlaylistService
{
    private readonly IPlaylistManager _playlistManager;
    private readonly ILibraryManager _libraryManager;
    private readonly ILogger<FavoritedSongsPlaylistService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FavoritedSongsPlaylistService"/> class.
    /// <param name="playlistManager">Instance of <see cref="IPlaylistManager"/>.</param>
    /// <param name="libraryManager">Instance of <see cref="ILibraryManager"/>.</param>
    /// <param name="logger">Instance of <see cref="ILogger{FavoritedSongsPlaylistService}"/>.</param>
    /// </summary>
    public FavoritedSongsPlaylistService(
        IPlaylistManager playlistManager,
        ILibraryManager libraryManager,
        ILogger<FavoritedSongsPlaylistService> logger)
    {
        _playlistManager = playlistManager ?? throw new ArgumentNullException(nameof(playlistManager));
        _libraryManager = libraryManager ?? throw new ArgumentNullException(nameof(libraryManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates or updates a favorited songs playlist for a specific user.
    /// </summary>
    /// <param name="user">The user to create the playlist for.</param>
    /// <param name="playlistName">The name of the playlist to create.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task CreateFavoritedSongsPlaylistAsync(User user)
    {
        var config = Plugin.Instance?.Configuration;
        if (config == null)
        {
            throw new InvalidOperationException("Plugin configuration not found");
        }

        var playlistName = config.PlaylistName.Replace("{username}", char.ToUpper(user.Username[0]) + user.Username.Substring(1));

        try
        {
            ArgumentNullException.ThrowIfNull(user);

            if (string.IsNullOrWhiteSpace(playlistName))
            {
                throw new ArgumentException("Playlist name cannot be empty.", nameof(playlistName));
            }

            _logger.LogInformation("Creating favorited songs playlist '{PlaylistName}' for user '{UserId}'",
                playlistName, user.Id);

            var favoritedSongs = GetUserFavoritedSongs(user);

            if (favoritedSongs.Count == 0)
            {
                _logger.LogInformation("No favorited songs found for user '{UserId}'. Skipping playlist creation.",
                    user.Id);
                return;
            }

            _logger.LogInformation("Found {Count} favorited songs for user '{UserId}'", favoritedSongs.Count, user.Id);

            var existingPlaylist = FindPlaylistByName(playlistName, user);

            if (existingPlaylist != null)
            {
                _logger.LogInformation("Found existing playlist '{PlaylistName}'. Clearing and updating...",
                    playlistName);
                await UpdatePlaylistAsync(existingPlaylist, favoritedSongs, user);
            }
            else
            {
                _logger.LogInformation("Creating new playlist '{PlaylistName}' for user '{UserId}'",
                    playlistName, user.Id);
                await CreatePlaylistAsync(playlistName, favoritedSongs, user);
            }

            _logger.LogInformation("Successfully created/updated favorited songs playlist for user '{UserId}'",
                user.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating favorited songs playlist for user '{UserId}'", user?.Id);
            throw;
        }
    }

    /// <summary>
    /// Gets all favorited songs for a user.
    /// </summary>
    private List<Audio> GetUserFavoritedSongs(User user)
    {
        var items = _libraryManager.GetItemsResult(new InternalItemsQuery
        {
            User = user,
            IncludeItemTypes = new[] { BaseItemKind.Audio },
            IsFavorite = true,
            Recursive = true,
            IsVirtualItem = false
        }).Items;

        return items.OfType<Audio>().ToList();
    }

    /// <summary>
    /// Finds a playlist by name for a user.
    /// </summary>
    private Playlist? FindPlaylistByName(string playlistName, User user)
    {
        var playlists = _libraryManager.GetItemsResult(new InternalItemsQuery
        {
            User = user,
            IncludeItemTypes = new[] { BaseItemKind.Playlist },
            Recursive = true
        }).Items;

        return playlists.OfType<Playlist>()
            .FirstOrDefault(p => p.Name.Equals(playlistName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Creates a new playlist with the provided songs.
    /// </summary>
    private async Task CreatePlaylistAsync(string playlistName, List<Audio> songs, User user)
    {
        var playlistItems = songs.Select(s => s.Id).ToList();

        _logger.LogInformation("Creating playlist with {Count} items for user {UserId}", playlistItems.Count, user.Id);

        var result = await _playlistManager.CreatePlaylist(new PlaylistCreationRequest
        {
            Name = playlistName,
            ItemIdList = playlistItems,
            UserId = user.Id,
            Public = false
        });

        _logger.LogInformation("Playlist created with ID {PlaylistId} for user {UserId}", result.Id, user.Id);
    }

    /// <summary>
    /// Updates an existing playlist with new songs.
    /// </summary>
    private async Task UpdatePlaylistAsync(Playlist playlist, List<Audio> songs, User user)
    {
        var favoritedSongIds = songs.Select(s => s.Id).ToList();

        _logger.LogDebug("Updating playlist {PlaylistId}. Target: {TargetCount} items",
            playlist.Id, favoritedSongIds.Count);

        // Use UpdatePlaylist instead of manual add/remove
        await _playlistManager.UpdatePlaylist(new PlaylistUpdateRequest
        {
            Id = playlist.Id,
            UserId = user.Id,
            Public = false,
            Name = playlist.Name,
            Ids = favoritedSongIds
        });

        _logger.LogDebug("Playlist update complete");
    }
}