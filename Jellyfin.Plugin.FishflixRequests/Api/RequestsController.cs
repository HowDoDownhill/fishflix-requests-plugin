using System;
using System.IO;
using System.Net.Http;
using System.Net.Mime;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.FishflixRequests.Services;
using MediaBrowser.Controller.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.FishflixRequests.Api;

/// <summary>
/// API and standalone page for viewing requests.
/// </summary>
[ApiController]
[Route("FishflixRequests")]
public class RequestsController : ControllerBase
{
    private const string AdministratorRole = "Administrator";

    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly RequestService _requestService;
    private readonly IAuthorizationContext _authContext;
    private readonly ILogger<RequestsController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RequestsController"/> class.
    /// </summary>
    /// <param name="requestService">Request service.</param>
    /// <param name="authContext">Jellyfin authorization context.</param>
    /// <param name="logger">Logger.</param>
    public RequestsController(RequestService requestService, IAuthorizationContext authContext, ILogger<RequestsController> logger)
    {
        _requestService = requestService;
        _authContext = authContext;
        _logger = logger;
    }

    /// <summary>
    /// Serves the standalone requests page. The page itself authenticates against the API
    /// using the Jellyfin web client's stored credentials, so it is safe to serve anonymously.
    /// </summary>
    /// <returns>The HTML page.</returns>
    [HttpGet("Page")]
    [AllowAnonymous]
    [Produces(MediaTypeNames.Text.Html)]
    public ActionResult GetPage()
    {
        var resource = typeof(Plugin).Namespace + ".Web.requests.html";
        var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource);
        if (stream is null)
        {
            return NotFound();
        }

        using var reader = new StreamReader(stream);
        return Content(reader.ReadToEnd(), MediaTypeNames.Text.Html);
    }

    /// <summary>
    /// Gets requests grouped by user, with their progress.
    /// </summary>
    /// <param name="refresh">Bypass the cache.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The requests.</returns>
    [HttpGet("Requests")]
    [Authorize]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult> GetRequests([FromQuery] bool refresh = false, CancellationToken cancellationToken = default)
    {
        var auth = await _authContext.GetAuthorizationInfo(HttpContext).ConfigureAwait(false);
        Guid? userId = auth.UserId == Guid.Empty ? null : auth.UserId;
        var isAdmin = User.IsInRole(AdministratorRole);

        try
        {
            var result = await _requestService.GetRequestsAsync(userId, isAdmin, refresh, cancellationToken).ConfigureAwait(false);
            return Content(JsonSerializer.Serialize(result, _jsonOptions), MediaTypeNames.Application.Json);
        }
        catch (InvalidOperationException ex)
        {
            return Problem(ex.Message, statusCode: StatusCodes.Status503ServiceUnavailable);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to load requests from Jellyseerr");
            return Problem("Could not reach Jellyseerr: " + ex.Message, statusCode: StatusCodes.Status502BadGateway);
        }
    }

    /// <summary>
    /// Tests a Jellyseerr URL and API key. Administrators only.
    /// </summary>
    /// <param name="body">The settings to test.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the test.</returns>
    [HttpPost("TestConnection")]
    [Authorize]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult> TestConnection([FromBody] TestConnectionBody body, CancellationToken cancellationToken)
    {
        if (!User.IsInRole(AdministratorRole))
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(body.Url) || string.IsNullOrWhiteSpace(body.ApiKey))
        {
            return BadRequest(new { success = false, message = "Enter both a URL and an API key." });
        }

        try
        {
            var name = await _requestService.TestConnectionAsync(body.Url, body.ApiKey, cancellationToken).ConfigureAwait(false);
            return Ok(new { success = true, message = "Connected to Jellyseerr as " + name + "." });
        }
        catch (Exception ex) when (ex is HttpRequestException or UriFormatException or TaskCanceledException or JsonException)
        {
            return Ok(new { success = false, message = "Connection failed: " + ex.Message });
        }
    }

    /// <summary>
    /// Body of the test connection request.
    /// </summary>
    public class TestConnectionBody
    {
        /// <summary>Gets or sets the Jellyseerr URL.</summary>
        public string? Url { get; set; }

        /// <summary>Gets or sets the API key.</summary>
        public string? ApiKey { get; set; }
    }
}
