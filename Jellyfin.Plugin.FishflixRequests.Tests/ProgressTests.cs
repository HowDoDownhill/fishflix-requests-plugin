using System.Text.Json;
using Jellyfin.Plugin.FishflixRequests.Jellyseerr;
using Jellyfin.Plugin.FishflixRequests.Models;
using Jellyfin.Plugin.FishflixRequests.Services;
using Xunit;

namespace Jellyfin.Plugin.FishflixRequests.Tests;

public class ProgressTests
{
    private static RequestDto Apply(JsRequest request)
    {
        var dto = new RequestDto();
        RequestService.ApplyProgress(request, dto);
        return dto;
    }

    [Theory]
    [InlineData(1, 2, RequestStage.PendingApproval)]
    [InlineData(3, 2, RequestStage.Declined)]
    [InlineData(4, 3, RequestStage.Failed)]
    [InlineData(2, 3, RequestStage.Processing)]
    [InlineData(2, 4, RequestStage.PartiallyAvailable)]
    [InlineData(2, 5, RequestStage.Available)]
    [InlineData(5, 5, RequestStage.Available)]
    public void StageFollowsRequestAndMediaStatus(int requestStatus, int mediaStatus, RequestStage expected)
    {
        var dto = Apply(new JsRequest { Status = requestStatus, Media = new JsMedia { Status = mediaStatus } });
        Assert.Equal(expected, dto.Stage);
    }

    [Fact]
    public void DownloadsAreAggregated()
    {
        var dto = Apply(new JsRequest
        {
            Status = 2,
            Media = new JsMedia
            {
                Status = 3,
                DownloadStatus =
                [
                    new JsDownload { Size = 100, SizeLeft = 50, Episode = new JsEpisode { SeasonNumber = 1, EpisodeNumber = 2 } },
                    new JsDownload { Size = 300, SizeLeft = 50, Title = "x" }
                ]
            }
        });

        Assert.Equal(RequestStage.Downloading, dto.Stage);
        Assert.Equal(75, dto.DownloadPercent);
        Assert.Equal("S01E02", dto.Downloads[0].Title);
        Assert.Equal(50, dto.Downloads[0].Percent);
    }

    [Fact]
    public void FourKRequestsUseFourKStatus()
    {
        var dto = Apply(new JsRequest { Status = 2, Is4k = true, Media = new JsMedia { Status = 5, Status4k = 3 } });
        Assert.Equal(RequestStage.Processing, dto.Stage);
    }

    [Fact]
    public void ParsesJellyseerrRequestJson()
    {
        const string json = """
        {
          "pageInfo": { "pages": 1, "pageSize": 10, "results": 1, "page": 1 },
          "results": [{
            "id": 7, "status": 2, "type": "tv", "is4k": false,
            "createdAt": "2026-10-01T12:00:00.000Z", "updatedAt": "2026-10-02T12:00:00.000Z",
            "seasons": [{ "id": 1, "seasonNumber": 2, "status": 2 }],
            "media": {
              "id": 3, "tmdbId": 1399, "mediaType": "tv", "status": 3, "status4k": 1,
              "jellyfinMediaId": null,
              "downloadStatus": [{ "externalId": 1, "size": 1000, "sizeLeft": 250, "status": "downloading", "timeLeft": "00:10:00", "title": "Show S02E01", "estimatedCompletionTime": "2026-10-02T13:00:00.000Z", "episode": { "seasonNumber": 2, "episodeNumber": 1 } }]
            },
            "requestedBy": { "id": 4, "displayName": "Fish", "jellyfinUserId": "0123456789abcdef0123456789abcdef" }
          }]
        }
        """;

        var page = JsonSerializer.Deserialize<JsPage<JsRequest>>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        var request = Assert.Single(page!.Results);
        Assert.Equal(1399, request.Media!.TmdbId);
        Assert.Equal("Fish", request.RequestedBy!.DisplayName);

        var dto = Apply(request);
        Assert.Equal(RequestStage.Downloading, dto.Stage);
        Assert.Equal(75, dto.DownloadPercent);
    }
}
