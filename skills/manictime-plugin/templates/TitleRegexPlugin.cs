// TEMPLATE — working example: extracts the git repository (and branch, when shown) from the
// window title of Fork. Per-app parts to change: namespace/class names, the process-name check,
// the regex, and the DocumentInfo mapping. Verify the regex against REAL titles from the tester
// output — e.g. repo names can contain hyphens, so this regex only splits on a spaced dash.
using System.Text.RegularExpressions;
using Finkit.ManicTime.Shared.DocumentTracking;
using ManicTime;
using ManicTime.Client.Tracker.EventTracking.Publishers.ApplicationTracking;
using Microsoft.Extensions.DependencyInjection;

namespace ForkPlugin;

public class ForkPluginServiceConfigurator : IServiceConfigurator
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IDocumentRetreiver, ForkRetreiver>();
    }
}

[DocumentRetreiver(DocumentCacheOption = DocumentCacheOption.ForFiveSecondsOrUntilTitleChange, CallOrder = 5)]
public class ForkRetreiver : IDocumentRetreiver
{
    private static readonly Regex TitleRegex = new(
        @"^(?<repo>.+?) [-–—] (?<branch>.+)$",
        RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(100));

    public DocumentInfo? GetDocument(ApplicationInfo application)
    {
        if (!string.Equals(application.ProcessName, "Fork", StringComparison.OrdinalIgnoreCase))
            return null;
        string title = application.WindowTitle ?? "";
        if (title.Length == 0)
            return null;

        // Never throw into the tracker (see contract.md); the title is attacker-controllable and
        // the regex may time out, especially after you edit it for another app.
        try
        {
            Match match = TitleRegex.Match(title);
            string repo = match.Success ? match.Groups["repo"].Value : title;
            string? branch = match.Success ? match.Groups["branch"].Value : null;

            return new DocumentInfo
            {
                DocumentGroupName = repo,
                DocumentName = branch ?? repo,
                // Set a DocumentTypes.* value that fits the app (File, WebSite, Chat, Email,
                // Event, Task) — it drives icons and auto-tag rules. Leave it unset for "other".
                DocumentType = DocumentTypes.File
            };
        }
        catch
        {
            return null;
        }
    }
}
