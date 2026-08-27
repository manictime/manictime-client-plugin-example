# Windows: reading from an app's COM object model

The richest source on Windows. Office, Visual Studio, AutoCAD, Adobe apps and many CAD/engineering
tools expose the open document, its full path and structured fields through COM. Prefer it over the
accessibility tree whenever the app has one.

## Probing

Find out whether the app registers a COM object while it is running:

```
powershell -ExecutionPolicy Bypass -File scripts/probe-com.ps1
```

It lists the Running Object Table (ROT) and tries common ProgIDs. Also worth a look:
`HKEY_CLASSES_ROOT\<App>.Application` in the registry, and the app's own automation/scripting
documentation ("<app> object model", "<app> automation API").

Three outcomes:
- **A moniker or ProgID answers** → COM path, read on.
- **Nothing in the ROT, but the app documents an object model** → it may still attach, see
  "Apps that never register in the ROT" below.
- **Nothing at all** → fall back to the accessibility tree (`windows-uia.md`).

## Writing the retriever

`Marshal.GetActiveObject` does not exist in modern .NET. This repository already contains a working
COM plugin — copy `MarshalEx.cs` and `ComHelper.cs` from `source/tracker-plugin/Plugins.Outlook/`
into your project and use them; `OutlookRetreiver.cs` is a complete worked example.

```csharp
object app = MarshalEx.GetActiveObject("Outlook.Application");   // throws if not running
object window = app.GetType().InvokeMember("ActiveWindow",
    BindingFlags.GetProperty, null, app, null);
```

- **Late binding only.** Call everything through `InvokeMember`/`BindingFlags.GetProperty`. Do not
  reference interop/PIA assemblies: they tie the plugin to one version of the app, and the plugin
  must load on machines where the app isn't installed at all.
- **Branch on the COM type name**, not on what you expect: `ComHelper.GetTypeName(item)` returns
  e.g. `_MailItem`, `_AppointmentItem`, `_TaskItem`. The same property means different things on
  different item types.
- **Release every COM object** you touch, in a `finally` — `Marshal.ReleaseComObject`. A tracker
  polls once per second; leaked RCWs keep the target app alive and eventually break it.
- **Multi-instance apps**: the plain ProgID gives you an arbitrary instance. Match the one you were
  asked about by walking the ROT for a moniker carrying the process id — Visual Studio registers
  `!VisualStudio.DTE:<pid>` (see `Plugins.VisualStudio` in the ManicTime client for the pattern).
- **STA**: create your objects on an STA thread when the app's server demands it. Symptom is
  `RPC_E_WRONG_THREAD` / an immediate `InvalidCastException` on a COM interface.

## Apps that never register in the ROT (Adobe-style)

Some apps expose a single-instance COM server but never publish it. `GetActiveObject` fails while
`Activator.CreateInstance(Type.GetTypeFromProgID(...))` **attaches to the running instance** instead
of starting a new one. Two cautions:

- Use the **versioned** ProgID (`InDesign.Application.2025`, not `InDesign.Application`); the
  versionless one can bind to a different installed release. Derive the version from
  `ApplicationInfo.Product` / `Filename` rather than hardcoding it.
- Only do this for apps documented as single-instance. For anything else you may launch a second
  copy of the application on the user's machine.

## Errors you will hit

- **`RPC_E_SERVERCALL_RETRYLATER` (0x8001010A)** — the app is busy (modal dialog, long operation).
  Do not retry in a loop: serve the last known value for that window title and return.
- **`COMException` / `InvalidCastException` while the app starts or exits** — normal. Catch, return
  `null`, try again on the next poll.
- **Empty or throwing path properties** — unsaved documents have no path, and some apps return "" for
  `FullName` over COM. Fall back to the document name; never return an empty `DocumentGroupName`.

## Cost

Every COM call is cross-process. Read the few properties you need, then stop, and set
`DocumentCacheOption.ForFiveSecondsOrUntilTitleChange` (or `UntilTitleChange`) on the retriever so
ManicTime isn't paying for it on every poll — see `contract.md`.
