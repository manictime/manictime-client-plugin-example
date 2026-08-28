using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using Finkit.ManicTime.Shared.Plugins.ServiceProviders.Manager;
using ManicTime;
using ManicTime.Client.Tracker.EventTracking.Publishers.ApplicationTracking;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace ManicTimePluginTester.Cli;

public record LoadedRetreiver(string PluginId, IDocumentRetreiver Retreiver, int CallOrder)
{
    public string TypeName => Retreiver.GetType().FullName ?? Retreiver.GetType().Name;
}

public class PluginPackageLoader
{
    private class PluginSpecDto
    {
        public string? Id { get; set; }
        public string? Version { get; set; }
        public string? Type { get; set; }
        public string? AssemblyName { get; set; }
        public string? Name { get; set; }
        public string? MinHostVersion { get; set; }
        public string? MaxHostVersion { get; set; }
    }

    // Mirrors ManicTime's per-plugin AssemblyLoadContext: a dependency is shared with the host
    // only on an exact version match; otherwise the package's own Lib copy is loaded.
    private sealed class PluginLoadContext : AssemblyLoadContext
    {
        private readonly string _libDir;

        public PluginLoadContext(string libDir) : base(libDir)
        {
            _libDir = libDir;
        }

        protected override Assembly? Load(AssemblyName name)
        {
            Assembly? hostAssembly = Default.Assemblies.FirstOrDefault(a =>
                string.Equals(a.GetName().Name, name.Name, StringComparison.OrdinalIgnoreCase));
            if (hostAssembly != null && hostAssembly.GetName().Version == name.Version)
                return hostAssembly;
            string libPath = Path.Combine(_libDir, name.Name + ".dll");
            if (File.Exists(libPath))
                return LoadFromAssemblyPath(libPath);
            // Fall back to the default context (like ManicTime's compatible-assembly fallback).
            return null;
        }
    }

    public List<LoadedRetreiver> Retreivers { get; private set; } = new();

    // A path could not be found, or a package failed to load — the caller asked to test
    // something that cannot be tested, so the run must not report success.
    public bool HasErrors { get; private set; }

    // Resolves each input path to plugin package directories and loads their document retrievers.
    // Accepted inputs: a package dir (contains PluginSpec.json), a packages root (contains package dirs),
    // or a plain dir with plugin dlls (no PluginSpec.json).
    public void Load(IEnumerable<string> paths)
    {
        foreach (string path in paths)
        {
            string fullPath = Path.GetFullPath(path);
            if (!Directory.Exists(fullPath))
            {
                Console.WriteLine($"NOT FOUND: {Safe(fullPath)}");
                HasErrors = true;
                continue;
            }

            if (File.Exists(Path.Combine(fullPath, "PluginSpec.json")))
            {
                LoadPackage(fullPath);
                continue;
            }

            // Also accept a "Plugins" root by looking one level down into Packages.
            string packagesRoot = Directory.Exists(Path.Combine(fullPath, "Packages"))
                ? Path.Combine(fullPath, "Packages")
                : fullPath;
            // Sorted so equal-CallOrder ties resolve consistently regardless of filesystem order.
            string[] subDirs = Directory.GetDirectories(packagesRoot).OrderBy(d => d, StringComparer.Ordinal).ToArray();
            string[] packageDirs = subDirs.Where(d => File.Exists(Path.Combine(d, "PluginSpec.json"))).ToArray();
            if (packageDirs.Length > 0)
            {
                Console.WriteLine($"Packages root: {packagesRoot} — {packageDirs.Length} package(s)");
                foreach (string skipped in subDirs.Except(packageDirs))
                    Console.WriteLine($"  skipped (no PluginSpec.json): {Path.GetFileName(skipped)}");
                foreach (string packageDir in packageDirs)
                    LoadPackage(packageDir);
            }
            else
            {
                Console.WriteLine($"No PluginSpec.json found in or under {fullPath} — scanning it for plugin dlls instead.");
                foreach (string skipped in subDirs)
                    Console.WriteLine($"  not a package (no PluginSpec.json): {Path.GetFileName(skipped)}");
                LoadPlainDir(fullPath);
            }
        }

        // Stable ordering on equal CallOrder, like the host's OrderBy.
        Retreivers = Retreivers.OrderBy(r => r.CallOrder).ToList();
    }

    private void LoadPackage(string packageDir)
    {
        string specPath = Path.Combine(packageDir, "PluginSpec.json");
        try
        {
            PluginSpecDto dto = JsonConvert.DeserializeObject<PluginSpecDto>(File.ReadAllText(specPath))
                ?? throw new InvalidOperationException("PluginSpec.json is empty.");

            // Same validation as ManicTime — a package ManicTime would reject must not show up
            // as LOADED here.
            if (dto.Id == null || dto.AssemblyName == null)
                throw new InvalidOperationException("PluginSpec.json must contain Id and AssemblyName.");
            // Id and AssemblyName must be bare names, never paths — a package must not reach
            // outside its own folder (ManicTime treats them the same way).
            if (Path.GetFileName(dto.Id) != dto.Id)
                throw new InvalidOperationException($"PluginSpec.json Id '{Safe(dto.Id)}' must not contain a path.");
            if (Path.GetFileName(dto.AssemblyName) != dto.AssemblyName)
                throw new InvalidOperationException(
                    $"PluginSpec.json AssemblyName '{Safe(dto.AssemblyName)}' must be a bare file name in Lib/, not a path.");
            if (dto.Version == null || !System.Version.TryParse(dto.Version, out _))
                throw new InvalidOperationException(
                    $"PluginSpec.json must contain a parsable Version (got '{dto.Version}') — ManicTime rejects the spec otherwise.");
            if (dto.Type == null)
                throw new InvalidOperationException("PluginSpec.json must contain Type — ManicTime rejects the spec otherwise.");
            Version? minHostVersion = ParseHostVersion(dto.MinHostVersion, "MinHostVersion");
            Version? maxHostVersion = ParseHostVersion(dto.MaxHostVersion, "MaxHostVersion");
            string dirName = Path.GetFileName(packageDir.TrimEnd(Path.DirectorySeparatorChar));
            if (!string.Equals(dirName, dto.Id, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    $"package dir '{dirName}' != PluginSpec Id '{dto.Id}' — ManicTime rejects such packages.");
            if (dto.Type != "DocumentTracker")
                Console.WriteLine($"WARNING: plugin type is '{dto.Type}' — this tester only exercises DocumentTracker plugins.");

            // ManicTime only loads assemblies from the package's Lib folder.
            string libDir = Path.Combine(packageDir, "Lib");
            if (!Directory.Exists(libDir))
                throw new InvalidOperationException("package has no Lib folder — ManicTime loads the assembly from Lib only.");
            string assemblyPath = Path.Combine(libDir, dto.AssemblyName);
            if (!File.Exists(assemblyPath))
                throw new FileNotFoundException($"Plugin assembly not found: {assemblyPath}");
            // Host assemblies must not be shipped in Lib/ — a stale copy breaks loading on a
            // client built at a different version. This is exactly what ManicTime ignores silently.
            foreach (string hostDll in Directory.GetFiles(libDir, "*.dll")
                .Select(path => Path.GetFileName(path))
                .Where(f => f.StartsWith("Finkit.ManicTime", StringComparison.OrdinalIgnoreCase)
                    || f.StartsWith("ManicTime.", StringComparison.OrdinalIgnoreCase)))
                Console.WriteLine($"WARNING: Lib/ contains host assembly '{hostDll}' — remove it; " +
                    "ManicTime provides it and a version mismatch will stop the plugin loading.");

            Assembly assembly = new PluginLoadContext(libDir).LoadFromAssemblyPath(assemblyPath);
            PluginSpec spec = new(
                Id: dto.Id,
                Version: dto.Version,
                Name: dto.Name,
                Description: null,
                AllowUpdate: true,
                AllowDisable: true,
                IsVisible: true,
                HelpUrl: null,
                Type: dto.Type,
                LicenseType: null,
                MinHostVersion: minHostVersion,
                MaxHostVersion: maxHostVersion,
                AssemblyName: dto.AssemblyName,
                Icon: null);
            // Real ManicTime keeps ContentDir under its data folder ("<data>/Plugins/Storage/<Id>/Content");
            // the tester approximates with a Content folder next to the package's Lib.
            PluginContext pluginContext = new(spec, libDir, Path.Combine(packageDir, "Content"));

            int count = LoadRetreivers(dto.Id, assembly, pluginContext);
            Console.WriteLine($"LOADED: {Safe(dto.Id)} {Safe(dto.Version)} ({Safe(dto.AssemblyName)}) — {count} document retriever(s)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"LOAD ERROR: {Safe(packageDir)}: {Safe(Describe(ex))}");
            HasErrors = true;
        }
    }

    // Like ManicTime, host version bounds must be "major.minor[...]" ("2025.2").
    private static Version? ParseHostVersion(string? value, string fieldName)
    {
        if (value == null)
            return null;
        if (System.Version.TryParse(value, out Version? version))
            return version;
        throw new InvalidOperationException(
            $"PluginSpec.json {fieldName} '{value}' is not a valid version — ManicTime rejects the spec otherwise.");
    }

    private void LoadPlainDir(string dir)
    {
        // Loading an assembly runs its code (module/static/instance ctors, ConfigureServices).
        // In this mode there is no PluginSpec to vet, so only point it at dlls you built or trust.
        Console.WriteLine("NOTE: loading each dll here RUNS its code — only use this on dlls you built or trust.");
        foreach (string dllPath in Directory.GetFiles(dir, "*.dll"))
        {
            string fileName = Path.GetFileName(dllPath);
            if (fileName.StartsWith("Finkit.ManicTime", StringComparison.OrdinalIgnoreCase)
                || fileName.StartsWith("ManicTime.", StringComparison.OrdinalIgnoreCase))
                continue;
            try
            {
                Assembly assembly = new PluginLoadContext(dir).LoadFromAssemblyPath(dllPath);
                // No PluginSpec here, so synthesize a context (AssemblyDir = the dll's folder,
                // ContentDir = a temp folder) — configurators that take a PluginContext still work.
                PluginContext pluginContext = new(
                    new PluginSpec(fileName, "0.0.0.0", fileName, null, true, true, true, null,
                        "DocumentTracker", null, null, null, fileName, null),
                    dir,
                    Path.Combine(Path.GetTempPath(), "ManicTimePluginTester", fileName));
                int count = LoadRetreivers(fileName, assembly, pluginContext);
                Console.WriteLine($"LOADED: {Safe(fileName)} — {count} document retriever(s)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"LOAD ERROR: {Safe(dllPath)}: {Safe(Describe(ex))}");
                HasErrors = true;
            }
        }
    }

    // Mirrors the host's PluginActivator: exported IServiceConfigurator types populate a private
    // ServiceCollection (with PluginContext available), then IDocumentRetreiver services are resolved.
    private int LoadRetreivers(string pluginId, Assembly assembly, PluginContext? pluginContext)
    {
        ServiceCollection services = new();
        if (pluginContext != null)
            services.AddSingleton(pluginContext);

        Type[] configuratorTypes = assembly
            .GetExportedTypes()
            .Where(t => !t.IsAbstract && typeof(IServiceConfigurator).IsAssignableFrom(t))
            .ToArray();
        foreach (Type configuratorType in configuratorTypes)
        {
            try
            {
                IServiceConfigurator configurator = (IServiceConfigurator)CreateInstance(configuratorType, pluginContext);
                configurator.ConfigureServices(services);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CONFIGURATOR ERROR: {Safe(configuratorType.FullName)}: {Safe(Describe(ex))}");
            }
        }

        ServiceProvider provider = services.BuildServiceProvider();
        int count = 0;
        foreach (IDocumentRetreiver retreiver in provider.GetServices<IDocumentRetreiver>())
        {
            DocumentRetreiverAttribute attribute =
                retreiver.GetType().GetCustomAttribute<DocumentRetreiverAttribute>() ?? new DocumentRetreiverAttribute();
            Retreivers.Add(new LoadedRetreiver(pluginId, retreiver, attribute.CallOrder));
            count++;
        }
        return count;
    }

    // ManicTime requires exactly one public constructor and injects PluginContext (and other host
    // services) into its parameters; parameters with defaults may be omitted.
    private static object CreateInstance(Type type, PluginContext? pluginContext)
    {
        ConstructorInfo[] constructors = type.GetConstructors();
        if (constructors.Length != 1)
            throw new InvalidOperationException(
                $"{type.Name} has {constructors.Length} public constructors — ManicTime requires exactly one.");

        ConstructorInfo constructor = constructors[0];
        ParameterInfo[] parameters = constructor.GetParameters();
        object?[] arguments = new object?[parameters.Length];
        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].ParameterType.IsInstanceOfType(pluginContext))
                arguments[i] = pluginContext;
            else if (parameters[i].HasDefaultValue)
                arguments[i] = parameters[i].DefaultValue;
            else
                throw new InvalidOperationException(
                    $"{type.Name} constructor parameter '{parameters[i].ParameterType.Name} {parameters[i].Name}' " +
                    "is not supported by this tester — use PluginContext, a parameter with a default value, or none.");
        }
        return constructor.Invoke(arguments);
    }

    private static string Describe(Exception ex) =>
        ex is ReflectionTypeLoadException typeLoad
            ? string.Join("; ", typeLoad.LoaderExceptions.Where(e => e != null).Select(e => e!.Message).Distinct())
            : ex.GetBaseException().Message;

    // Spec fields and exception text can carry attacker-chosen control characters; escape them
    // before printing so a package can't inject terminal escape sequences into the console.
    private static string Safe(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return value ?? "";
        StringBuilder builder = new(value.Length);
        foreach (char c in value)
            builder.Append(!char.IsControl(c) || c == '\t' ? c : $"\\u{(int)c:x4}");
        return builder.ToString();
    }
}
