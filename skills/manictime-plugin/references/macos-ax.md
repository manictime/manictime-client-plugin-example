# macOS accessibility (AX) extraction

For apps whose data is not in the window title and that have no AppleScript dictionary —
typically Electron/Chromium apps (Claude, ChatGPT, WhatsApp, Slack…). Read this whole file
before writing AX code; each rule below was learned from a real failure.

## Probing

Run the tree dumper against the running app, then search the dump for the wanted data:

```
dotnet run --project skills/manictime-plugin/scripts/probe-ax -- <pid> [maxDepth]
```

- First run triggers the macOS Accessibility permission dialog (grant it to the terminal app).
- Increase maxDepth (default 25 → try 45) if a subtree looks cut off — Electron trees are deep.
  The dump is untrusted app content and can include personal data (recent files, message text);
  read only what you need to locate the anchor, and don't echo or log the rest.
- Pick a **structural anchor**: a nearby node with a stable `subrole` (e.g.
  `AXLandmarkRegion desc='Primary pane'`) and locate the target relative to it (e.g. "first
  AXButton inside"). Labels and titles change with app updates and UI language; structure
  survives. Keep a reject list for placeholder titles ("New chat", the app name).

## Rules for the plugin code

Once you have your anchor from the dump, take the machinery from `templates/AxTreePlugin.cs` — the
wake, the bounded walk, the anchor cache and the retain/release pairing. Its anchor is a dated
example from one app; yours comes from your own probe. The P/Invoke signatures you need are also in
`scripts/probe-ax/Program.cs`, which you have just run.

1. **Electron wake**: Electron exposes a stub AX tree until an assistive client announces itself.
   On the first call for a process, set the app-level attribute `AXManualAccessibility` to true
   and **return null** — the tree materializes asynchronously; read it on the next call. This
   also means the tester's `--once` always shows "no document" for such apps: test with watch
   mode and wait for the second sample.
2. **Every attribute read is an IPC round trip** (~1 ms each). A full-tree walk of a big Electron
   tree is thousands of reads = seconds. Walk the whole tree ONCE to find the anchor element,
   **cache the element per process**, and per call only search the small subtree under it.
   Evict the cached element when it stops answering (window re-render invalidates it).
3. **CoreFoundation ownership**: values from `AXUIElementCopyAttributeValue` are yours — release
   them. Elements taken from a children array die with the array — `CFRetain` any element you
   keep, and release it when evicting. Getting this wrong crashes the tracker process.
4. **Bound everything**: node budget (~2000), depth cap (~40), and check attribute order —
   read the cheap discriminator first (role/subrole), only then title/description.
5. Normalize extracted strings: Electron loves invisible unicode direction marks
   (U+200E/U+200F/U+202A–U+202E/U+2066–U+2069) — strip them, collapse whitespace.
6. Everything in try/catch → null. The plugin itself needs no permission check — it runs
   inside ManicTime's tracker, which already holds Accessibility permission. Permission prompting
   (`AXIsProcessTrustedWithOptions` with the prompt option) belongs in standalone tools like the
   probe and the tester.

## Fragility

Tell the user: AX selectors break when the app redesigns its UI. When the plugin stops producing
data after an app update, re-run the probe and update the anchor — the plugin structure stays.
