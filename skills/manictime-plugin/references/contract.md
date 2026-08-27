# The document tracker contract

A plugin is a class library referencing the DLLs in `libs/`. ManicTime discovers it via two
mechanisms; a document tracker plugin needs only the first:

```csharp
public class MyPluginServiceConfigurator : ManicTime.IServiceConfigurator   // ManicTime.Api.dll
{
    public void ConfigureServices(IServiceCollection services) =>
        services.AddSingleton<IDocumentRetreiver, MyRetreiver>();
}
```

The configurator must have **exactly one public constructor** (ManicTime rejects it
otherwise). It may take a `PluginContext` (namespace
`Finkit.ManicTime.Shared.Plugins.ServiceProviders.Manager`) — gives `AssemblyDir` (where the dll
and any data files live) and `ContentDir` (writable folder that survives updates). Stick to a
parameterless or PluginContext-only constructor (parameters with default values are also fine):
ManicTime can inject other host services too, but the CLI tester supports only these shapes.

## IDocumentRetreiver

```csharp
// ManicTime.Client.Tracker.dll, namespace ManicTime.Client.Tracker.EventTracking.Publishers.ApplicationTracking
[DocumentRetreiver(DocumentCacheOption = DocumentCacheOption.ForFiveSecondsOrUntilTitleChange, CallOrder = 5)]
public class MyRetreiver : IDocumentRetreiver
{
    public DocumentInfo? GetDocument(ApplicationInfo application) { ... }
}
```

`ApplicationInfo` (what you receive): `ProcessId`, `ProcessName`, `WindowTitle`,
`WindowHandle`, plus file info when available: `Filename`, `Product`, `ProductVersion`,
`Company`, `ProductMajorPart/MinorPart`. **`ProcessName` differs per OS**: on Windows it is the
executable name without extension ("Fork"); on macOS ManicTime passes the app's localized
DISPLAY name ("Control Center", not "ControlCenter") — always verify the exact value in the
tester's output line before writing the process-name check.

`DocumentInfo` (what you return):

| Field | Meaning | Rule |
|---|---|---|
| `DocumentGroupName` | group, bottom-right in day view | **MUST be non-empty or ManicTime silently discards the result** |
| `DocumentName` | activity, bottom-left | the specific document |
| `DocumentType` | drives icon/auto-tag rules | one of `DocumentTypes.*` (Shared dll): `WebSite`, `File`, `Chat`, `Email`, `Event`, `Task`, `Other` |
| `Title` | optional | overrides the window title on the Applications timeline |

Typical mappings: website → group=host, name=URL; file → group=filename, name=full path;
chat → group=app or contact, name=conversation.

## Hard rules

- **Be fast; never block.** Document retrieval is shared across all apps and runs one at a
  time — while your retriever is working, ManicTime records nothing for any app, and a hang is
  not cancelled. A slow/hanging retriever is the worst failure; cache anything expensive (see
  DocumentCacheOption below).
- **Never throw** — catch everything, return `null`. Throws are caught and cheap, but returning
  null is the contract.
- **Return `null` for other processes** — first check `application.ProcessName`.
- **Speed**: ManicTime polls about once per second and abandons a poll that takes more than
  5 seconds in total (it also logs a warning for any single retriever over 5s). To stay useful a
  retriever should answer well under a second; anything slow (IPC, subprocess) must be cached —
  that is what `DocumentCacheOption` is for (`NoCache` | `ForFiveSecondsOrUntilTitleChange` |
  `UntilTitleChange`).
- `CallOrder`: lower runs first; first non-null result with a non-empty group wins. Default 1;
  use ~5 for app-specific retrievers.
