# Windows: reading from the UI Automation tree

For apps with no COM object model: Electron and WebView2 apps (new Outlook, chat clients, editors),
and anything where the data is on screen but nowhere else. Less robust than COM — the tree is a
rendering of the UI, so it changes when the app is redesigned. Say so when you hand the plugin over.

## Probing

```
powershell -ExecutionPolicy Bypass -File scripts/probe-uia.ps1 -ProcessName olk
```

Dumps the UIA tree (control type, AutomationId, Name, ClassName) so you can find the value and pick
an anchor. Microsoft's **Accessibility Insights for Windows** or **inspect.exe** (Windows SDK) show
the same tree interactively and are worth using alongside it.

Pick a **stable anchor**: an `AutomationId` (or a ControlType + structural position) near the value.
Do not anchor on `Name` when an id exists — names are localized and change with app language.

## Writing the retriever

Target `net10.0-windows10.0.19041.0` and add `<UseWPF>true</UseWPF>` to the csproj to get
`UIAutomationClient` / `UIAutomationTypes` (`System.Windows.Automation`). Start from the window:

```csharp
AutomationElement root = AutomationElement.FromHandle(application.WindowHandle);
```

Rules that matter, all learned from the plugins shipping in the ManicTime client:

1. **Batch property reads with a `CacheRequest`.** Every `element.Current.X` is a separate
   cross-process call. Walking a tree and reading three properties per node without caching costs
   seconds; ManicTime polls every second. Request the properties you need (`AutomationId`, `Name`)
   with `AutomationElementMode.None` around the `FindAll`/walk, then read from `Cached`.
2. **Cache the element you found, per window handle**, and reuse it on later polls instead of
   re-walking. Evict it on `ElementNotAvailableException` — that is what a re-rendered page looks
   like — and re-find on the next call.
3. **Scope the search.** `TreeScope.Descendants` from the window on a big Electron tree is the slow
   path. Narrow first (find the `ControlType.Document` element — for Chromium/WebView2 apps the web
   content lives under it), then search inside that.
4. **Electron/WebView2 apps** expose the page as a Document subtree whose interesting nodes carry
   stable ids from the web app (new Outlook uses `MSG_<id>_SUBJECT`, `MSG_<id>_FROM`). Match on the
   id pattern, not on position.
5. **Strip the label.** Web UIs often expose "From: someone@example.com" as one string, localized
   ("Od:", "发件人："). Split on the first `:` or `：` rather than matching the label text.
6. **Remember a miss.** If the anchor isn't there, don't re-walk the full tree every second — record
   the failure for that window and back off for a few minutes (the client's browser plugins use a
   5-minute cooldown).
7. **Version drift.** Chromium's tree shape changes between major versions; the client carries a
   ladder of per-version retrievers for exactly this reason. Prefer a search that tolerates an extra
   wrapper element over a fixed parent→child→child path.
8. **Private/incognito windows**: if the app has such a mode and you can detect it, implement
   `IApplicationStatusProvider` and return `ApplicationStatus.Private`; ManicTime then skips document
   tracking for that window instead of recording it.

Unlike macOS, Windows needs no permission grant and no "wake" call — Chromium enables its
accessibility layer when UIA queries arrive.

## Expectations

Set `DocumentCacheOption.ForFiveSecondsOrUntilTitleChange` (see `contract.md`), catch everything and
return `null`, and verify against the live app with the tester — a UIA selector that looks right in a
dump is not proof it fires. Tell the user which part is fragile: when the app updates its UI, re-run
the probe and update the anchor; the rest of the plugin stays.
