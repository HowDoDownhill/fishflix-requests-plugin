# Fishflix Requests

A Jellyfin plugin that shows everyone's [Jellyseerr](https://github.com/Fallenbagel/jellyseerr) (or Overseerr) requests, grouped by user, along with how far along each one is:

| Stage | Meaning |
| --- | --- |
| Waiting for approval | Requested, waiting for an admin |
| Approved – searching | Sent to Sonarr/Radarr, no download yet |
| Downloading – 42% | Live progress from Sonarr/Radarr, per episode for series |
| Partially available | Some seasons or episodes are in the library |
| Available | In Jellyfin, with a "Watch in Jellyfin" link |
| Declined / Failed | Declined by an admin, or Jellyseerr couldn't send it on |

Users can filter by user, by stage and by title. The page refreshes itself every minute.

Built for **Jellyfin 12.x** (.NET 10).

## Install

1. In Jellyfin, go to Dashboard → Plugins → **Repositories** (the gear or "Manage repositories" button, depending on version) and add:
   - **Name:** `Fishflix`
   - **URL:** `https://raw.githubusercontent.com/HowDoDownhill/fishflix-requests-plugin/manifest/manifest.json`
2. Go to the plugin **Catalog**, find **Fishflix Requests** and install it.
3. Restart Jellyfin.

New versions show up as updates in the same place.

To install by hand instead, download `fishflix-requests_<version>.zip` from the latest GitHub release, unzip it into a new folder in your Jellyfin plugins directory (e.g. `/config/plugins/FishflixRequests_1.0.0.0/` in the official Docker image), and restart Jellyfin.

## Configure

Dashboard → Plugins → **Fishflix Requests**:

- **Jellyseerr URL**: the address Jellyfin can reach Jellyseerr at, e.g. `http://jellyseerr:5055`.
- **API key**: in Jellyseerr, Settings → General → API Key.
- Click **Test connection**, then **Save**.

Other options:

- **Let users see everyone's requests**: on by default. Turn it off to show non-admin users only their own requests. Admins always see everything.
- **Hide old available requests**: stops finished requests from piling up.
- **Cache duration**: how long results are reused before asking Jellyseerr again (default 60 s). The **Refresh** button skips the cache.

Requests are matched to Jellyfin users through the Jellyfin account linked in Jellyseerr, which is set automatically when users sign in to Jellyseerr with Jellyfin or are imported from it.

## Where users find it

- **Admins**: Dashboard → **Requests**.
- **Everyone**: `https://<your-jellyfin>/FishflixRequests/Page`. It uses the Jellyfin web sign-in already in the browser, so users just need to be signed in to Jellyfin web.

To put a link in everyone's Jellyfin sidebar, add a menu link to the web client's `config.json` (in the Docker image: `/jellyfin/jellyfin-web/config.json`):

```json
"menuLinks": [
  { "name": "Requests", "icon": "inbox", "url": "/FishflixRequests/Page" }
]
```

If Jellyfin is served under a base path such as `/jellyfin`, use `/jellyfin/FishflixRequests/Page`.

The page works in browsers that are signed in to Jellyfin web. The native apps (Android TV, Swiftfin, and so on) don't show plugin pages.

## API

| Endpoint | Auth | Description |
| --- | --- | --- |
| `GET /FishflixRequests/Requests[?refresh=true]` | Any signed-in user | Requests grouped by user, as JSON |
| `GET /FishflixRequests/Page` | None (the page signs in itself) | The requests page |
| `POST /FishflixRequests/TestConnection` | Admin | Checks a Jellyseerr URL and API key |

## Build

```sh
dotnet test Jellyfin.Plugin.FishflixRequests.slnx
dotnet publish Jellyfin.Plugin.FishflixRequests -c Release -o publish
```

The DLL in `publish/` is the plugin.

## Releasing

Push a version tag, e.g. `git tag v1.1.0 && git push origin v1.1.0`. The **Build** workflow then:

1. builds and tests the plugin with that version number,
2. attaches `fishflix-requests_1.1.0.0.zip` to a GitHub release, and
3. adds the version to `manifest.json` on the `manifest` branch (created on the first release), so Jellyfin servers that use the repository see the update.
