using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.FishflixRequests.Configuration;

/// <summary>
/// Plugin configuration.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Gets or sets the base URL of the Jellyseerr (or Overseerr) server, e.g. http://192.168.1.10:5055.
    /// </summary>
    public string JellyseerrUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Jellyseerr API key (Settings → General → API Key).
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether non-admin users can see everyone's requests.
    /// When false, non-admin users only see their own.
    /// </summary>
    public bool AllowUsersToViewAll { get; set; } = true;

    /// <summary>
    /// Gets or sets how long, in seconds, request data is cached before Jellyseerr is queried again.
    /// </summary>
    public int CacheSeconds { get; set; } = 60;

    /// <summary>
    /// Gets or sets the maximum number of requests (newest first) loaded from Jellyseerr.
    /// </summary>
    public int MaxRequests { get; set; } = 500;

    /// <summary>
    /// Gets or sets a value indicating whether available requests older than <see cref="HideAvailableAfterDays"/> are hidden.
    /// </summary>
    public bool HideOldAvailable { get; set; }

    /// <summary>
    /// Gets or sets the number of days after which available requests are hidden when <see cref="HideOldAvailable"/> is set.
    /// </summary>
    public int HideAvailableAfterDays { get; set; } = 30;
}
