using System;
using System.Collections.Generic;
using System.Globalization;
using Jellyfin.Plugin.FishflixRequests.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.FishflixRequests;

/// <summary>
/// Fishflix Requests plugin: shows Jellyseerr requests grouped by user, with their progress.
/// </summary>
public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Plugin"/> class.
    /// </summary>
    /// <param name="applicationPaths">Instance of the <see cref="IApplicationPaths"/> interface.</param>
    /// <param name="xmlSerializer">Instance of the <see cref="IXmlSerializer"/> interface.</param>
    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
    }

    /// <inheritdoc />
    public override string Name => "Fishflix Requests";

    /// <inheritdoc />
    public override Guid Id => Guid.Parse("40dc34ee-7e8d-4ebc-b34d-cc1253f07de0");

    /// <inheritdoc />
    public override string Description => "View Jellyseerr requests by user and track their progress.";

    /// <summary>
    /// Gets the current plugin instance.
    /// </summary>
    public static Plugin? Instance { get; private set; }

    /// <inheritdoc />
    public IEnumerable<PluginPageInfo> GetPages()
    {
        var ns = GetType().Namespace;
        return
        [
            new PluginPageInfo
            {
                Name = Name,
                EmbeddedResourcePath = string.Format(CultureInfo.InvariantCulture, "{0}.Configuration.configPage.html", ns)
            },
            new PluginPageInfo
            {
                Name = "FishflixRequestsDashboard",
                DisplayName = "Requests",
                EmbeddedResourcePath = string.Format(CultureInfo.InvariantCulture, "{0}.Web.requestsDashboard.html", ns),
                EnableInMainMenu = true,
                MenuIcon = "inbox"
            }
        ];
    }
}
