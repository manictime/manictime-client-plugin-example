# Windows: rules for the built-in CustomTitle plugin (no build)

ManicTime ships a plugin that extracts the document from a window title using regexes read from a
text file. When the data the user wants is in the title, a rule here does the whole job: no .NET
SDK, no compiling, no package to install, and the file is re-read as soon as you save it.

Reach for it when any of these hold:
- the user has no .NET 10 SDK and doesn't want one;
- the case is a plain title regex with no other logic;
- you want the answer in minutes and can write a real plugin later if the rule isn't enough.

Windows only. On macOS and Linux, write a title-regex plugin from `templates/TitleRegexPlugin.cs`.

## Rule format

One rule per line, **tab-delimited**, `#` starts a comment:

```
processName<TAB>groupRegex<TAB>activityRegex
```

- `processName` — the executable name without extension, exactly as the tester prints it.
- `groupRegex` — matched against the window title; the captured text becomes the group
  (bottom right in the day view).
- `activityRegex` — optional; the activity (bottom left). Omit it and the group value is used for
  both. Two further tab-separated columns can carry substitution patterns if a plain capture isn't
  enough.

Rules run after every app-specific plugin, so a rule can only add data for apps nothing else
handled — it never overrides a built-in plugin.

## Full paths for free

When the group is a file name, the plugin looks that name up in the app's Windows **Jump List**
(recent files) and, on a hit, records the **full path** as the activity. So "I want the path, not
just the file name" is often already solved here without any code. It only covers files opened
recently (jump lists are pruned), so verify against the timeline rather than assuming.

## Where the file goes

Two copies are read: one next to the shipped plugin, and a **user copy under the ManicTime data
folder** that survives updates. Write the user copy, in the CustomTitle plugin's own `Content`
folder — the plugin id is `ManicTime.DocumentTracker.CustomTitle`, and the path is of the shape:

```
<db folder>\Plugins\Storage\ManicTime.DocumentTracker.CustomTitle\Content\CustomTitle.txt
```

**Locate it, don't assume it.** Open the db folder from ManicTime (Settings → Advanced → Open db
folder), then find the existing `CustomTitle.txt` under `Plugins\` — searching for the file is more
reliable than typing the path from this page, and the folder may not exist until the plugin has
written to it. Create the folders if they are missing, and preserve any rules already in the file.

## Verify

The plugin is hidden in the Plugin manager (it ships with `IsVisible: False`), so **there is no
"Loaded" line to check** — a wrong path or a bad regex looks exactly like nothing happening. After
saving the file, use the target app and confirm the value appears on the Documents timeline. If it
doesn't, the file location is the first thing to re-check, then the tabs (spaces will not do), then
the regex against the exact title the tester prints.
