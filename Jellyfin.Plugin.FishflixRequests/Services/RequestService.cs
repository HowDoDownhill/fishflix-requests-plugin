using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.FishflixRequests.Configuration;
using Jellyfin.Plugin.FishflixRequests.Jellyseerr;
using Jellyfin.Plugin.FishflixRequests.Models;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.FishflixRequests.Services;

/// <summary>
/// Loads requests from Jellyseerr, groups them by user and works out their progress.
/// </summary>
public class RequestService
{
    private const string TmdbImageBase = "https://image.tmdb.org/t/p/w300";
    private static readonly TimeSpan _detailsTtl = TimeSpan.FromHours(12);
    private static readonly TimeSpan _failedDetailsTtl = TimeSpan.FromMinutes(10);

    private readonly JellyseerrClient _client;
    private readonly ILogger<RequestService> _logger;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private readonly ConcurrentDictionary<string, (JsMediaDetails? Details, DateTimeOffset Expires)> _details = new();

    private Snapshot? _snapshot;

    /// <summary>
    /// Initializes a new instance of the <see cref="RequestService"/> class.
    /// </summary>
    /// <param name="client">Jellyseerr client.</param>
    /// <param name="logger">Logger.</param>
    public RequestService(JellyseerrClient client, ILogger<RequestService> logger)
    {
        _client = client;
        _logger = logger;
    }

    /// <summary>
    /// Gets the requests visible to a Jellyfin user.
    /// </summary>
    /// <param name="jellyfinUserId">The calling Jellyfin user, if any.</param>
    /// <param name="isAdmin">Whether the caller is an administrator.</param>
    /// <param name="forceRefresh">Skip the cache.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The requests grouped by user.</returns>
    public async Task<RequestsResponse> GetRequestsAsync(Guid? jellyfinUserId, bool isAdmin, bool forceRefresh, CancellationToken cancellationToken)
    {
        var config = GetConfig();
        var snapshot = await GetSnapshotAsync(config, forceRefresh, cancellationToken).ConfigureAwait(false);
        var canViewAll = isAdmin || config.AllowUsersToViewAll;

        var users = new List<UserRequestsDto>();
        foreach (var group in snapshot.Users)
        {
            var isCurrent = jellyfinUserId.HasValue && SameJellyfinId(group.JellyfinUserId, jellyfinUserId.Value);
            if (!canViewAll && !isCurrent)
            {
                continue;
            }

            if (group.Requests.Count == 0 && !isCurrent)
            {
                continue;
            }

            users.Add(new UserRequestsDto
            {
                JellyseerrUserId = group.JellyseerrUserId,
                JellyfinUserId = group.JellyfinUserId,
                DisplayName = group.DisplayName,
                IsCurrentUser = isCurrent,
                Counts = group.Counts,
                Requests = group.Requests
            });
        }

        // Current user first, then whoever requested something most recently.
        users = users
            .OrderByDescending(u => u.IsCurrentUser)
            .ThenByDescending(u => u.Requests.Count == 0 ? DateTimeOffset.MinValue : u.Requests.Max(r => r.RequestedAt ?? DateTimeOffset.MinValue))
            .ToList();

        return new RequestsResponse
        {
            GeneratedAt = snapshot.GeneratedAt,
            CanViewAll = canViewAll,
            JellyseerrUrl = config.JellyseerrUrl.TrimEnd('/'),
            Users = users
        };
    }

    /// <summary>
    /// Tests a Jellyseerr URL and API key.
    /// </summary>
    /// <param name="url">Base URL.</param>
    /// <param name="apiKey">API key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The name of the user the API key authenticates as.</returns>
    public async Task<string> TestConnectionAsync(string url, string apiKey, CancellationToken cancellationToken)
    {
        var me = await _client.GetCurrentUserAsync(url, apiKey, cancellationToken).ConfigureAwait(false);
        return me is null ? "unknown user" : DisplayNameOf(me);
    }

    /// <summary>
    /// Works out the stage, status text and download progress of a request.
    /// </summary>
    /// <param name="request">The Jellyseerr request.</param>
    /// <param name="dto">The DTO to fill in.</param>
    public static void ApplyProgress(JsRequest request, RequestDto dto)
    {
        var media = request.Media;
        var mediaStatus = media is null ? 0 : (request.Is4k ? media.Status4k : media.Status);
        var downloads = (request.Is4k ? media?.DownloadStatus4k : media?.DownloadStatus) ?? [];

        dto.Downloads = downloads.Select(d => new DownloadDto
        {
            Title = d.Episode is null
                ? d.Title ?? string.Empty
                : string.Create(CultureInfo.InvariantCulture, $"S{d.Episode.SeasonNumber:00}E{d.Episode.EpisodeNumber:00}"),
            Percent = Percent(d.Size, d.SizeLeft),
            Status = d.Status,
            TimeLeft = d.TimeLeft,
            EstimatedCompletionTime = d.EstimatedCompletionTime
        }).ToList();

        if (downloads.Count > 0)
        {
            dto.DownloadPercent = Percent(downloads.Sum(d => d.Size), downloads.Sum(d => d.SizeLeft));
        }

        switch (request.Status)
        {
            case 3:
                dto.Stage = RequestStage.Declined;
                dto.StatusText = "Declined";
                return;
            case 4:
                dto.Stage = RequestStage.Failed;
                dto.StatusText = "Failed to send to Sonarr/Radarr";
                return;
            case 1:
                dto.Stage = RequestStage.PendingApproval;
                dto.StatusText = "Waiting for approval";
                return;
        }

        if (mediaStatus == 5 || request.Status == 5)
        {
            dto.Stage = RequestStage.Available;
            dto.StatusText = "Available";
        }
        else if (downloads.Count > 0)
        {
            dto.Stage = RequestStage.Downloading;
            dto.StatusText = string.Create(CultureInfo.InvariantCulture, $"Downloading – {dto.DownloadPercent:0}%");
        }
        else if (mediaStatus == 4)
        {
            dto.Stage = RequestStage.PartiallyAvailable;
            dto.StatusText = "Partially available";
        }
        else if (mediaStatus == 7)
        {
            dto.Stage = RequestStage.Processing;
            dto.StatusText = "Removed from library";
        }
        else
        {
            dto.Stage = RequestStage.Processing;
            dto.StatusText = "Approved – searching for a release";
        }
    }

    private static double Percent(double size, double sizeLeft)
        => size <= 0 ? 0 : Math.Round(Math.Clamp((size - sizeLeft) / size * 100, 0, 100), 1);

    private static PluginConfiguration GetConfig()
        => Plugin.Instance?.Configuration ?? new PluginConfiguration();

    private static bool SameJellyfinId(string? a, Guid b)
        => Guid.TryParse(a, out var parsed) && parsed == b;

    private static string DisplayNameOf(JsUser user)
        => FirstNonEmpty(user.DisplayName, user.JellyfinUsername, user.Username, user.PlexUsername, user.Email)
           ?? string.Create(CultureInfo.InvariantCulture, $"User {user.Id}");

    private static string? FirstNonEmpty(params string?[] values)
        => values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

    private async Task<Snapshot> GetSnapshotAsync(PluginConfiguration config, bool forceRefresh, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(config.JellyseerrUrl) || string.IsNullOrWhiteSpace(config.ApiKey))
        {
            throw new InvalidOperationException("The Jellyseerr URL and API key have not been configured yet.");
        }

        var key = string.Join('|', config.JellyseerrUrl, config.ApiKey, config.MaxRequests, config.HideOldAvailable, config.HideAvailableAfterDays);
        var ttl = TimeSpan.FromSeconds(Math.Max(0, config.CacheSeconds));

        var current = _snapshot;
        if (!forceRefresh && current is not null && current.Key == key && DateTimeOffset.UtcNow - current.GeneratedAt < ttl)
        {
            return current;
        }

        await _refreshLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Someone else may have refreshed while we waited.
            current = _snapshot;
            if (current is not null && current.Key == key && DateTimeOffset.UtcNow - current.GeneratedAt < (forceRefresh ? TimeSpan.FromSeconds(5) : ttl))
            {
                return current;
            }

            var snapshot = await BuildSnapshotAsync(config, key, cancellationToken).ConfigureAwait(false);
            _snapshot = snapshot;
            return snapshot;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private async Task<Snapshot> BuildSnapshotAsync(PluginConfiguration config, string key, CancellationToken cancellationToken)
    {
        var url = config.JellyseerrUrl;
        var apiKey = config.ApiKey;

        var usersTask = _client.GetUsersAsync(url, apiKey, cancellationToken);
        var requestsTask = _client.GetRequestsAsync(url, apiKey, Math.Clamp(config.MaxRequests, 1, 5000), cancellationToken);
        await Task.WhenAll(usersTask, requestsTask).ConfigureAwait(false);
        var jsUsers = usersTask.Result;
        var jsRequests = requestsTask.Result;

        await LoadDetailsAsync(url, apiKey, jsRequests, cancellationToken).ConfigureAwait(false);

        var hideBefore = config.HideOldAvailable
            ? DateTimeOffset.UtcNow.AddDays(-Math.Max(0, config.HideAvailableAfterDays))
            : (DateTimeOffset?)null;

        var groups = jsUsers.ToDictionary(
            u => u.Id,
            u => new UserRequestsDto
            {
                JellyseerrUserId = u.Id,
                JellyfinUserId = u.JellyfinUserId,
                DisplayName = DisplayNameOf(u)
            });

        foreach (var request in jsRequests)
        {
            var requester = request.RequestedBy;
            if (requester is null)
            {
                continue;
            }

            if (!groups.TryGetValue(requester.Id, out var group))
            {
                group = new UserRequestsDto
                {
                    JellyseerrUserId = requester.Id,
                    JellyfinUserId = requester.JellyfinUserId,
                    DisplayName = DisplayNameOf(requester)
                };
                groups[requester.Id] = group;
            }

            var dto = ToDto(request);
            if (hideBefore.HasValue && dto.Stage == RequestStage.Available && (dto.UpdatedAt ?? dto.RequestedAt) < hideBefore)
            {
                continue;
            }

            group.Requests.Add(dto);
        }

        foreach (var group in groups.Values)
        {
            group.Requests.Sort((a, b) => Nullable.Compare(b.RequestedAt, a.RequestedAt));
            group.Counts = group.Requests
                .GroupBy(r => r.Stage.ToString())
                .ToDictionary(g => g.Key, g => g.Count());
        }

        _logger.LogDebug("Loaded {RequestCount} Jellyseerr requests for {UserCount} users", jsRequests.Count, groups.Count);

        return new Snapshot(key, DateTimeOffset.UtcNow, groups.Values.ToList());
    }

    private RequestDto ToDto(JsRequest request)
    {
        var mediaType = request.Media?.MediaType ?? request.Type ?? "movie";
        var tmdbId = request.Media?.TmdbId ?? 0;
        _details.TryGetValue(DetailsKey(mediaType, tmdbId), out var cached);
        var details = cached.Details;
        var date = details?.ReleaseDate ?? details?.FirstAirDate;

        var dto = new RequestDto
        {
            Id = request.Id,
            MediaType = mediaType,
            TmdbId = tmdbId,
            Title = FirstNonEmpty(details?.Title, details?.Name)
                    ?? string.Create(CultureInfo.InvariantCulture, $"TMDB #{tmdbId}"),
            Year = date is { Length: >= 4 } ? date[..4] : null,
            PosterUrl = string.IsNullOrEmpty(details?.PosterPath) ? null : TmdbImageBase + details!.PosterPath,
            Is4k = request.Is4k,
            Seasons = request.Seasons?.Select(s => s.SeasonNumber).OrderBy(n => n).ToList() ?? [],
            RequestedAt = request.CreatedAt,
            UpdatedAt = request.UpdatedAt,
            JellyfinItemId = request.Is4k ? request.Media?.JellyfinMediaId4k : request.Media?.JellyfinMediaId
        };

        ApplyProgress(request, dto);
        return dto;
    }

    private async Task LoadDetailsAsync(string url, string apiKey, List<JsRequest> requests, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var missing = requests
            .Where(r => r.Media is { TmdbId: > 0 })
            .Select(r => (Type: r.Media!.MediaType ?? r.Type ?? "movie", r.Media!.TmdbId))
            .Distinct()
            .Where(m => !_details.TryGetValue(DetailsKey(m.Type, m.TmdbId), out var c) || c.Expires < now)
            .ToList();

        using var throttle = new SemaphoreSlim(8);
        await Task.WhenAll(missing.Select(async m =>
        {
            await throttle.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var details = await _client.GetDetailsAsync(url, apiKey, m.Type, m.TmdbId, cancellationToken).ConfigureAwait(false);
                _details[DetailsKey(m.Type, m.TmdbId)] = (details, DateTimeOffset.UtcNow + (details is null ? _failedDetailsTtl : _detailsTtl));
            }
            finally
            {
                throttle.Release();
            }
        })).ConfigureAwait(false);
    }

    private static string DetailsKey(string mediaType, int tmdbId)
        => string.Create(CultureInfo.InvariantCulture, $"{mediaType}:{tmdbId}");

    private sealed record Snapshot(string Key, DateTimeOffset GeneratedAt, List<UserRequestsDto> Users);
}
