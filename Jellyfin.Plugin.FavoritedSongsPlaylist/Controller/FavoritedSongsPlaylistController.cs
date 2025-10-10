namespace Jellyfin.Plugin.FavoritedSongsPlaylist.Controller;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Plugin.FavoritedSongsPlaylist.Services;
using MediaBrowser.Controller.Library;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

/// <summary>
/// The favorited songs playlist api controller.
/// </summary>
[ApiController]
[Route("Plugins/FavoritedSongsPlaylist")]
[Authorize(Policy = "RequiresElevation")]
public class FavoritedSongsPlaylistController : ControllerBase
{
    private readonly FavoritedSongsPlaylistService _playlistService;
    private readonly IUserManager _userManager;
    private readonly ILogger<FavoritedSongsPlaylistController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FavoritedSongsPlaylistController"/> class.
    /// </summary>
    /// <param name="playlistService">Instance of <see cref="FavoritedSongsPlaylistService"/>.</param>
    /// <param name="userManager">Instance of <see cref="IUserManager"/>.</param>
    /// <param name="logger">Instance of <see cref="ILogger{FavoritedSongsPlaylistController}"/>.</param>
    public FavoritedSongsPlaylistController(
        FavoritedSongsPlaylistService playlistService,
        IUserManager userManager,
        ILogger<FavoritedSongsPlaylistController> logger)
    {
        _playlistService = playlistService;
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>
    /// Force sync favorited songs playlists for all users.
    /// </summary>
    /// <returns>An async task.</returns>
    [HttpPost("SyncNow")]
    public async Task<ActionResult> SyncNow()
    {
        try
        {
            var config = Plugin.Instance?.Configuration;
            if (config == null)
            {
                return BadRequest("Plugin configuration not found");
            }

            var playlistName = config.PlaylistName;
            var users = _userManager.Users.ToList();

            _logger.LogInformation("Starting forced sync for {UserCount} users", users.Count);

            var failedUsers = new List<string>();

            foreach (var user in users)
            {
                try
                {
                    await _playlistService.CreateFavoritedSongsPlaylistAsync(user, playlistName).ConfigureAwait(false);
                }
                catch (System.Exception ex)
                {
                    _logger.LogError(ex, "Failed to sync playlist for user {UserId}", user.Id);
                    failedUsers.Add($"{user.Username} ({user.Id})");
                }
            }

            _logger.LogInformation("Force sync completed. Failed users: {FailedCount}", failedUsers.Count);

            if (failedUsers.Count != 0)
            {
                return Ok(new { success = false, message = $"Sync completed with errors. Failed users: {string.Join(", ", failedUsers)}" });
            }

            return Ok(new { success = true, message = "Sync completed successfully for all users" });
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error during force sync");
            return StatusCode(500, new { success = false, message = "An error occurred during sync", error = ex.Message });
        }
    }
}