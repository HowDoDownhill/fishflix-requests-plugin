using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.FishflixRequests.Jellyseerr;

/// <summary>
/// Minimal client for the Jellyseerr / Overseerr v1 API.
/// </summary>
public class JellyseerrClient
{
    private const int PageSize = 100;

    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<JellyseerrClient> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="JellyseerrClient"/> class.
    /// </summary>
    /// <param name="httpClientFactory">HTTP client factory.</param>
    /// <param name="logger">Logger.</param>
    public JellyseerrClient(IHttpClientFactory httpClientFactory, ILogger<JellyseerrClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Gets all Jellyseerr users.
    /// </summary>
    /// <param name="baseUrl">Jellyseerr base URL.</param>
    /// <param name="apiKey">API key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The users.</returns>
    public async Task<List<JsUser>> GetUsersAsync(string baseUrl, string apiKey, CancellationToken cancellationToken)
    {
        var users = new List<JsUser>();
        for (var skip = 0; ; skip += PageSize)
        {
            var page = await GetAsync<JsPage<JsUser>>(baseUrl, apiKey, $"api/v1/user?take={PageSize}&skip={skip}", cancellationToken).ConfigureAwait(false);
            if (page is null || page.Results.Count == 0)
            {
                break;
            }

            users.AddRange(page.Results);
            if (page.Results.Count < PageSize)
            {
                break;
            }
        }

        return users;
    }

    /// <summary>
    /// Gets the newest requests, up to <paramref name="max"/>.
    /// </summary>
    /// <param name="baseUrl">Jellyseerr base URL.</param>
    /// <param name="apiKey">API key.</param>
    /// <param name="max">Maximum number of requests to load.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The requests.</returns>
    public async Task<List<JsRequest>> GetRequestsAsync(string baseUrl, string apiKey, int max, CancellationToken cancellationToken)
    {
        var requests = new List<JsRequest>();
        for (var skip = 0; skip < max; skip += PageSize)
        {
            var take = Math.Min(PageSize, max - skip);
            var page = await GetAsync<JsPage<JsRequest>>(baseUrl, apiKey, $"api/v1/request?take={take}&skip={skip}&filter=all&sort=added", cancellationToken).ConfigureAwait(false);
            if (page is null || page.Results.Count == 0)
            {
                break;
            }

            requests.AddRange(page.Results);
            if (page.Results.Count < take)
            {
                break;
            }
        }

        return requests;
    }

    /// <summary>
    /// Gets movie or TV details for a TMDB id.
    /// </summary>
    /// <param name="baseUrl">Jellyseerr base URL.</param>
    /// <param name="apiKey">API key.</param>
    /// <param name="mediaType">"movie" or "tv".</param>
    /// <param name="tmdbId">TMDB id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The details, or null if they could not be loaded.</returns>
    public async Task<JsMediaDetails?> GetDetailsAsync(string baseUrl, string apiKey, string mediaType, int tmdbId, CancellationToken cancellationToken)
    {
        var path = string.Equals(mediaType, "tv", StringComparison.OrdinalIgnoreCase) ? "tv" : "movie";
        try
        {
            return await GetAsync<JsMediaDetails>(baseUrl, apiKey, string.Create(CultureInfo.InvariantCulture, $"api/v1/{path}/{tmdbId}"), cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Could not load {MediaType} details for TMDB id {TmdbId}", path, tmdbId);
            return null;
        }
    }

    /// <summary>
    /// Checks that the URL and API key work.
    /// </summary>
    /// <param name="baseUrl">Jellyseerr base URL.</param>
    /// <param name="apiKey">API key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user the API key authenticates as.</returns>
    public Task<JsUser?> GetCurrentUserAsync(string baseUrl, string apiKey, CancellationToken cancellationToken)
        => GetAsync<JsUser>(baseUrl, apiKey, "api/v1/auth/me", cancellationToken);

    private async Task<T?> GetAsync<T>(string baseUrl, string apiKey, string relativeUrl, CancellationToken cancellationToken)
    {
        var uri = new Uri(new Uri(baseUrl.TrimEnd('/') + "/"), relativeUrl);
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.Add("X-Api-Key", apiKey);
        request.Headers.Accept.ParseAdd("application/json");

        var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(30);
        using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(_jsonOptions, cancellationToken).ConfigureAwait(false);
    }
}
