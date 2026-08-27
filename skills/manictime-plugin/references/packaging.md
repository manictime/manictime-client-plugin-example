# Packaging and installing

A plugin is a folder. **The folder name must exactly equal the `Id` in PluginSpec.json** —
mismatch means ManicTime silently skips it.

```
Custom.DocumentTracker.<App>/
├── PluginSpec.json
└── Lib/
    ├── <App>Plugin.dll        <- your assembly
    └── <NuGet dependencies>   <- everything your dll needs that ManicTime doesn't provide
```

## PluginSpec.json

```json
{
  "Id": "Custom.DocumentTracker.Fork",
  "Version": "1.0.0.0",
  "Type": "DocumentTracker",
  "AssemblyName": "ForkPlugin.dll",
  "Name": "Document timeline: Fork (git repository)",
  "MinHostVersion": "2025.2"
}
```

- `Id`: use the `Custom.` prefix — never `ManicTime.*` (reserved for official plugins).
- `AssemblyName` must match the dll filename in `Lib/`.

## csproj (see templates/PluginTemplate.csproj)

- `TargetFramework`: `net10.0` for pure/P-Invoke code (works on all OSes);
  `net10.0-windows10.0.19041.0` only when Windows APIs (UIA assemblies, WPF) are needed.
- Reference `libs/*.dll` via `<Reference><HintPath>` — there is no NuGet package.
- Into `Lib/` copy your dll **plus NuGet dependency dlls** from the build output, but **never**
  the `Finkit.ManicTime.*` / `ManicTime.*` host dlls — ManicTime provides those, and a stale
  copy in `Lib/` breaks loading.

## Install

1. Locate the ManicTime data folder — defaults: macOS `~/Library/Application Support/ManicTime`,
   Windows `%LOCALAPPDATA%\Finkit\ManicTime`. Confirm by checking it contains `Plugins/` or the
   ManicTime db files; otherwise the user can open the right folder via ManicTime → Settings →
   Advanced → Open db folder.
2. Copy the package folder to `<data folder>/Plugins/Packages/<Id>/`. If `<Id>` already
   exists there, confirm with the user before replacing it — don't clobber a plugin you didn't
   create.
3. Restart ManicTime (both tray tracker and UI).
4. Verify: Plugin manager shows the plugin as Loaded (a LoadError shows the exception);
   use the target app and check the Documents timeline.

Uninstall = delete the folder (or disable in Plugin manager). Nothing else on the system is
touched; ManicTime updates never overwrite the db folder's Packages.
