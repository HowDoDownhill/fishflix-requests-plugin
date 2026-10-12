using System;
using System.Collections.Generic;

namespace Jellyfin.Plugin.FishflixRequests.Models;

#pragma warning disable SA1402 // File may only contain a single type - these are small response DTOs.

/// <summary>
/// Progress stage of a request, in order.
/// </summary>
public enum RequestStage
{
    /// <summary>Waiting for an admin to approve it.</summary>
    PendingApproval,

    /// <summary>Approved; waiting for Sonarr/Radarr to find and grab a release.</summary>
    Processing,

    /// <summary>Currently downloading.</summary>
    Downloading,

    /// <summary>Some of it (e.g. some seasons/episodes) is in the library.</summary>
    PartiallyAvailable,

    /// <summary>In the library and ready to watch.</summary>
    Available,

    /// <summary>Declined by an admin.</summary>
    Declined,

    /// <summary>Sending the request to Sonarr/Radarr failed.</summary>
    Failed
}

/// <summary>
/// Response of the requests endpoint.
/// </summary>
public class RequestsResponse
{
    /// <summary>Gets or sets when the data was fetched from Jellyseerr.</summary>
    public DateTimeOffset GeneratedAt { get; set; }

    /// <summary>Gets or sets a value indicating whether the caller can see everyone's requests.</summary>
    public bool CanViewAll { get; set; }

    /// <summary>Gets or sets the Jellyseerr base URL, for "open in Jellyseerr" links.</summary>
    public string? JellyseerrUrl { get; set; }

    /// <summary>Gets or sets the requests grouped by user.</summary>
    public List<UserRequestsDto> Users { get; set; } = [];
}

/// <summary>
/// All requests of one user.
/// </summary>
public class UserRequestsDto
{
    /// <summary>Gets or sets the Jellyseerr user id.</summary>
    public int JellyseerrUserId { get; set; }

    /// <summary>Gets or sets the linked Jellyfin user id, if any.</summary>
    public string? JellyfinUserId { get; set; }

    /// <summary>Gets or sets the display name.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether this is the calling user.</summary>
    public bool IsCurrentUser { get; set; }

    /// <summary>Gets or sets the number of requests per stage.</summary>
    public Dictionary<string, int> Counts { get; set; } = [];

    /// <summary>Gets or sets the requests, newest first.</summary>
    public List<RequestDto> Requests { get; set; } = [];
}

/// <summary>
/// A single request and its progress.
/// </summary>
public class RequestDto
{
    /// <summary>Gets or sets the Jellyseerr request id.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the media type ("movie" or "tv").</summary>
    public string MediaType { get; set; } = "movie";

    /// <summary>Gets or sets the TMDB id.</summary>
    public int TmdbId { get; set; }

    /// <summary>Gets or sets the title.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets the release year.</summary>
    public string? Year { get; set; }

    /// <summary>Gets or sets the poster URL.</summary>
    public string? PosterUrl { get; set; }

    /// <summary>Gets or sets a value indicating whether this is a 4K request.</summary>
    public bool Is4k { get; set; }

    /// <summary>Gets or sets the requested season numbers (TV only).</summary>
    public List<int> Seasons { get; set; } = [];

    /// <summary>Gets or sets when it was requested.</summary>
    public DateTimeOffset? RequestedAt { get; set; }

    /// <summary>Gets or sets when it last changed.</summary>
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>Gets or sets the progress stage.</summary>
    public RequestStage Stage { get; set; }

    /// <summary>Gets or sets a human readable status.</summary>
    public string StatusText { get; set; } = string.Empty;

    /// <summary>Gets or sets the overall download progress (0-100), when downloading.</summary>
    public double? DownloadPercent { get; set; }

    /// <summary>Gets or sets the individual downloads.</summary>
    public List<DownloadDto> Downloads { get; set; } = [];

    /// <summary>Gets or sets the Jellyfin item id once the media is available.</summary>
    public string? JellyfinItemId { get; set; }
}

/// <summary>
/// A download in progress.
/// </summary>
public class DownloadDto
{
    /// <summary>Gets or sets the title (release or episode).</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets the progress (0-100).</summary>
    public double Percent { get; set; }

    /// <summary>Gets or sets the download client status.</summary>
    public string? Status { get; set; }

    /// <summary>Gets or sets the time left.</summary>
    public string? TimeLeft { get; set; }

    /// <summary>Gets or sets the estimated completion time.</summary>
    public DateTimeOffset? EstimatedCompletionTime { get; set; }
}
