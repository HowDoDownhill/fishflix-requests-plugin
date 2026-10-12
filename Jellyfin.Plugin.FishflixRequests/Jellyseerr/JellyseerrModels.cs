using System;
using System.Collections.Generic;

namespace Jellyfin.Plugin.FishflixRequests.Jellyseerr;

#pragma warning disable SA1402 // File may only contain a single type - these are small wire DTOs.

/// <summary>
/// A paged Jellyseerr response.
/// </summary>
/// <typeparam name="T">Item type.</typeparam>
public class JsPage<T>
{
    /// <summary>Gets or sets the page info.</summary>
    public JsPageInfo? PageInfo { get; set; }

    /// <summary>Gets or sets the results.</summary>
    public List<T> Results { get; set; } = [];
}

/// <summary>
/// Paging metadata.
/// </summary>
public class JsPageInfo
{
    /// <summary>Gets or sets the number of pages.</summary>
    public int Pages { get; set; }

    /// <summary>Gets or sets the total number of results.</summary>
    public int Results { get; set; }
}

/// <summary>
/// A Jellyseerr user.
/// </summary>
public class JsUser
{
    /// <summary>Gets or sets the Jellyseerr user id.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the display name.</summary>
    public string? DisplayName { get; set; }

    /// <summary>Gets or sets the local username.</summary>
    public string? Username { get; set; }

    /// <summary>Gets or sets the Jellyfin username.</summary>
    public string? JellyfinUsername { get; set; }

    /// <summary>Gets or sets the linked Jellyfin user id.</summary>
    public string? JellyfinUserId { get; set; }

    /// <summary>Gets or sets the Plex username (Overseerr).</summary>
    public string? PlexUsername { get; set; }

    /// <summary>Gets or sets the email.</summary>
    public string? Email { get; set; }

    /// <summary>Gets or sets the avatar URL or path.</summary>
    public string? Avatar { get; set; }
}

/// <summary>
/// A Jellyseerr media request.
/// </summary>
public class JsRequest
{
    /// <summary>Gets or sets the request id.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the request status (1 pending, 2 approved, 3 declined, 4 failed, 5 completed).</summary>
    public int Status { get; set; }

    /// <summary>Gets or sets the media type ("movie" or "tv").</summary>
    public string? Type { get; set; }

    /// <summary>Gets or sets a value indicating whether this is a 4K request.</summary>
    public bool Is4k { get; set; }

    /// <summary>Gets or sets when the request was created.</summary>
    public DateTimeOffset? CreatedAt { get; set; }

    /// <summary>Gets or sets when the request was last updated.</summary>
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>Gets or sets the requested media.</summary>
    public JsMedia? Media { get; set; }

    /// <summary>Gets or sets the requesting user.</summary>
    public JsUser? RequestedBy { get; set; }

    /// <summary>Gets or sets the requested seasons (TV only).</summary>
    public List<JsSeason>? Seasons { get; set; }
}

/// <summary>
/// A requested season.
/// </summary>
public class JsSeason
{
    /// <summary>Gets or sets the season number.</summary>
    public int SeasonNumber { get; set; }

    /// <summary>Gets or sets the season status.</summary>
    public int Status { get; set; }
}

/// <summary>
/// Jellyseerr media info attached to a request.
/// </summary>
public class JsMedia
{
    /// <summary>Gets or sets the Jellyseerr media id.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the TMDB id.</summary>
    public int TmdbId { get; set; }

    /// <summary>Gets or sets the media type.</summary>
    public string? MediaType { get; set; }

    /// <summary>Gets or sets the media status (1 unknown, 2 pending, 3 processing, 4 partially available, 5 available, 6 blacklisted, 7 deleted).</summary>
    public int Status { get; set; }

    /// <summary>Gets or sets the 4K media status.</summary>
    public int Status4k { get; set; }

    /// <summary>Gets or sets the Jellyfin item id once available.</summary>
    public string? JellyfinMediaId { get; set; }

    /// <summary>Gets or sets the 4K Jellyfin item id once available.</summary>
    public string? JellyfinMediaId4k { get; set; }

    /// <summary>Gets or sets the active downloads reported by Sonarr/Radarr.</summary>
    public List<JsDownload>? DownloadStatus { get; set; }

    /// <summary>Gets or sets the active 4K downloads reported by Sonarr/Radarr.</summary>
    public List<JsDownload>? DownloadStatus4k { get; set; }
}

/// <summary>
/// A download in progress as reported by Sonarr/Radarr through Jellyseerr.
/// </summary>
public class JsDownload
{
    /// <summary>Gets or sets the download title.</summary>
    public string? Title { get; set; }

    /// <summary>Gets or sets the total size in bytes.</summary>
    public double Size { get; set; }

    /// <summary>Gets or sets the remaining size in bytes.</summary>
    public double SizeLeft { get; set; }

    /// <summary>Gets or sets the download status (e.g. "downloading", "queued").</summary>
    public string? Status { get; set; }

    /// <summary>Gets or sets the time left, as reported by the download client.</summary>
    public string? TimeLeft { get; set; }

    /// <summary>Gets or sets the estimated completion time.</summary>
    public DateTimeOffset? EstimatedCompletionTime { get; set; }

    /// <summary>Gets or sets the episode, for TV downloads.</summary>
    public JsEpisode? Episode { get; set; }
}

/// <summary>
/// Episode info for a TV download.
/// </summary>
public class JsEpisode
{
    /// <summary>Gets or sets the season number.</summary>
    public int SeasonNumber { get; set; }

    /// <summary>Gets or sets the episode number.</summary>
    public int EpisodeNumber { get; set; }
}

/// <summary>
/// Movie or TV details from Jellyseerr (only the fields we need).
/// </summary>
public class JsMediaDetails
{
    /// <summary>Gets or sets the movie title.</summary>
    public string? Title { get; set; }

    /// <summary>Gets or sets the TV series name.</summary>
    public string? Name { get; set; }

    /// <summary>Gets or sets the poster path on TMDB.</summary>
    public string? PosterPath { get; set; }

    /// <summary>Gets or sets the movie release date.</summary>
    public string? ReleaseDate { get; set; }

    /// <summary>Gets or sets the TV first air date.</summary>
    public string? FirstAirDate { get; set; }
}
