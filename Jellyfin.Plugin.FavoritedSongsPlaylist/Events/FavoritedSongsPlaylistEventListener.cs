using System.Threading;
using System.Threading.Tasks;
using ICU4N.Impl;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.FavoritedSongsPlaylist.Services;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.FavoritedSongsPlaylist.Events;

/// <summary>
/// Event listener for when items are marked as favorite.
/// </summary>
public class FavoritedSongsPlaylistEventListener : IHostedService
{
    private readonly IUserDataManager _userDataManager;
    private readonly IUserManager _userManager;
    private readonly FavoritedSongsPlaylistService _playlistService;
    private readonly ILogger<FavoritedSongsPlaylistEventListener> _logger;

    public FavoritedSongsPlaylistEventListener(
        IUserDataManager userDataManager,
        IUserManager userManager,
        FavoritedSongsPlaylistService playlistService,
        ILogger<FavoritedSongsPlaylistEventListener> logger
    )
    {
        _userDataManager = userDataManager;
        _userManager = userManager;
        _playlistService = playlistService;
        _logger = logger;
    }

    private async void UserDataSavedHandler(object? sender, UserDataSaveEventArgs eventArgs)
    {
        var config = Plugin.Instance?.Configuration;
        if (config == null) return;
        if (eventArgs.Item is null) return;
        if (eventArgs.Item is not Audio) return;
        if (eventArgs.SaveReason != UserDataSaveReason.UpdateUserRating) return;

        var user = _userManager.GetUserById(eventArgs.UserId);
        if (user is null)
        {
            return;
        }

        _logger.LogInformation("User '{UserId}' marked item '{ItemId}' as favorite.", user.Id, eventArgs.Item.Id);

        await _playlistService.CreateFavoritedSongsPlaylistAsync(user, config.PlaylistName);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _userDataManager.UserDataSaved += UserDataSavedHandler;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _userDataManager.UserDataSaved -= UserDataSavedHandler;
        return Task.CompletedTask;
    }
}