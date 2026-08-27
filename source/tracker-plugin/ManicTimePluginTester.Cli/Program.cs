using System.Diagnostics;
using System.Globalization;
using System.Text;
using ManicTime.Client.Tracker.EventTracking.Publishers.ApplicationTracking;
using Newtonsoft.Json;

namespace ManicTimePluginTester.Cli;

public static class Program
{
    public static int Main(string[] args)
    {
        List<string> paths = new();
        double intervalSeconds = 2;
        bool printAll = false;
        bool json = false;
        bool once = false;
        int? pid = null;

        string OptionValue(string option, ref int index)
        {
            if (index + 1 >= args.Length)
                throw new ArgumentException($"{option} requires a value.");
            return args[++index];
        }

        try
        {
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--interval":
                        // Invariant culture: "0.5" must parse the same on comma-decimal locales.
                        intervalSeconds = double.Parse(OptionValue("--interval", ref i), CultureInfo.InvariantCulture);
                        if (!double.IsFinite(intervalSeconds) || intervalSeconds <= 0)
                            throw new ArgumentException("--interval must be a positive number of seconds.");
                        break;
                    case "--pid":
                        pid = int.Parse(OptionValue("--pid", ref i), CultureInfo.InvariantCulture);
                        break;
                    case "--all":
                        printAll = true;
                        break;
                    case "--json":
                        json = true;
                        break;
                    case "--once":
                        once = true;
                        break;
                    case "--help" or "-h":
                        PrintUsage();
                        return 0;
                    default:
                        paths.Add(args[i]);
                        break;
                }
            }
        }
        catch (Exception ex) when (ex is ArgumentException or FormatException or OverflowException)
        {
            Console.Error.WriteLine($"Invalid arguments: {ex.Message}");
            PrintUsage();
            return 1;
        }

        // In JSON mode stdout carries ONLY newline-delimited JSON records; all diagnostics
        // (loader output, banners, warnings, usage) go to stderr.
        TextWriter output = Console.Out;
        if (json)
            Console.SetOut(Console.Error);

        // With no package path the tester still reports the process name and window title every
        // retriever starts from — the first thing you need when writing a plugin.
        if (args.Length == 0)
        {
            PrintUsage();
            return 1;
        }

        PluginPackageLoader loader = new();
        loader.Load(paths);
        // Something the caller asked for could not be loaded. Still sample if anything DID load
        // (a Packages root may hold one broken package next to good ones), but never report
        // success — a script or agent reads the exit code, not the log.
        if (loader.HasErrors && loader.Retreivers.Count == 0)
            return 1;

        Console.WriteLine();
        if (loader.Retreivers.Count == 0)
            // Still useful with no plugin: the sampled lines show the process name and window
            // title every retriever starts from.
            Console.WriteLine("No document retrievers loaded — showing the raw process name and window title only.");
        else
        {
            Console.WriteLine("Retrievers (in call order):");
            foreach (LoadedRetreiver retreiver in loader.Retreivers)
                Console.WriteLine($"  [{retreiver.CallOrder}] {retreiver.TypeName} ({retreiver.PluginId})");
        }
        Console.WriteLine();
        string target = pid != null ? $"process {pid}" : "the foreground window";
        Console.WriteLine(once
            ? $"Sampling {target} once."
            : $"Watching {target} every {intervalSeconds.ToString("0.#", CultureInfo.InvariantCulture)}s. Change documents in the target app. Ctrl+C to stop.");
        Console.WriteLine();

        string? lastKey = null;
        while (true)
        {
            bool sampled = false;
            try
            {
                ApplicationInfo? application = pid != null
                    ? ForegroundWindow.GetByProcessId(pid.Value)
                    : ForegroundWindow.GetCurrent();
                if (application != null)
                {
                    lastKey = Sample(loader.Retreivers, application, json, output, printAll, lastKey);
                    sampled = true;
                }
                else
                {
                    Console.WriteLine($"{Timestamp()} no active window detected");
                }
            }
            catch (ArgumentException) when (pid != null)
            {
                Console.WriteLine($"{Timestamp()} process {pid} is not running (anymore) — exiting.");
                return 1;
            }
            catch (ArgumentException)
            {
                // Foreground window's process exited between detection and inspection — benign.
                Console.WriteLine($"{Timestamp()} active window went away before it could be read");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{Timestamp()} ERROR: {Safe(ex.GetBaseException().Message)}");
            }

            if (once)
                return sampled && !loader.HasErrors ? 0 : 1;
            Thread.Sleep(TimeSpan.FromSeconds(intervalSeconds));
        }
    }

    private record RetreiverResult(LoadedRetreiver Loaded, DocumentInfo? Document, long ElapsedMs, string? Error)
    {
        public bool HasEmptyGroup => Document != null && string.IsNullOrEmpty(Document.DocumentGroupName);
        public bool IsWinnerCandidate => Document != null && !HasEmptyGroup;
    }

    // Calls EVERY retriever and prints each result, marking the one ManicTime would use
    // (first in call order returning a DocumentInfo with a non-empty DocumentGroupName).
    private static string Sample(
        List<LoadedRetreiver> retreivers,
        ApplicationInfo application,
        bool json,
        TextWriter output,
        bool printAll,
        string? lastKey)
    {
        List<RetreiverResult> results = new();
        foreach (LoadedRetreiver loaded in retreivers)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            DocumentInfo? document = null;
            string? error = null;
            try
            {
                document = loaded.Retreiver.GetDocument(application);
            }
            catch (Exception ex)
            {
                error = ex.GetBaseException().Message;
            }
            stopwatch.Stop();
            results.Add(new RetreiverResult(loaded, document, stopwatch.ElapsedMilliseconds, error));
        }

        int winnerIndex = results.FindIndex(r => r.IsWinnerCandidate);

        string key = $"{application.ProcessId}|{application.ProcessName}|{application.WindowTitle}|" + string.Join("|",
            results.Select(r => $"{r.Document?.DocumentGroupName};{r.Document?.DocumentName};{r.Document?.DocumentType};{r.Document?.Title};{r.Error}"));
        if (!printAll && key == lastKey)
            return key;

        if (json)
        {
            output.WriteLine(JsonConvert.SerializeObject(new
            {
                time = DateTime.Now,
                processName = application.ProcessName,
                processId = application.ProcessId,
                windowTitle = application.WindowTitle,
                winner = winnerIndex >= 0 ? results[winnerIndex].Loaded.TypeName : null,
                results = results.Select((r, i) => new
                {
                    retreiver = r.Loaded.TypeName,
                    pluginId = r.Loaded.PluginId,
                    isWinner = i == winnerIndex,
                    elapsedMs = r.ElapsedMs,
                    error = r.Error,
                    emptyDocumentGroupName = r.HasEmptyGroup,
                    document = r.Document == null
                        ? null
                        : new
                        {
                            documentGroupName = r.Document.DocumentGroupName,
                            documentName = r.Document.DocumentName,
                            documentType = r.Document.DocumentType,
                            title = r.Document.Title
                        }
                })
            }));
        }
        else
        {
            Console.WriteLine($"{Timestamp()} {Safe(application.ProcessName)} \"{Safe(application.WindowTitle)}\"");
            for (int i = 0; i < results.Count; i++)
            {
                RetreiverResult result = results[i];
                bool isWinner = i == winnerIndex;
                string marker = isWinner ? "=>" : "->";
                string outcome = result switch
                {
                    { Error: not null } => $"threw: {Safe(result.Error)} (ManicTime would log this and move on)",
                    { HasEmptyGroup: true } =>
                        $"EMPTY DocumentGroupName (name='{Safe(result.Document!.DocumentName)}') — ManicTime discards such results",
                    { Document: not null } =>
                        $"group='{Safe(result.Document.DocumentGroupName)}' name='{Safe(result.Document.DocumentName)}' " +
                        $"type='{Safe(result.Document.DocumentType)}'" +
                        (result.Document.Title != null ? $" title='{Safe(result.Document.Title)}'" : ""),
                    _ => "no document"
                };
                Console.WriteLine($"    {marker} {result.Loaded.TypeName} ({result.ElapsedMs} ms): {outcome}" +
                    (isWinner ? "   [ManicTime would use this]" : ""));
                if (result.ElapsedMs > 1000)
                    Console.WriteLine($"       !! slow: ManicTime polls about once per second and abandons a poll after 5s total");
            }
        }
        return key;
    }

    private static string Timestamp() => DateTime.Now.ToString("HH:mm:ss");

    // Window titles, process names and plugin output are chosen by the target application —
    // untrusted text. Escape control characters (so a malicious title can't emit terminal escape
    // sequences that retitle the terminal or hide text from a human) and cap the length.
    private static string Safe(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return value ?? "";
        StringBuilder builder = new(value.Length);
        foreach (char c in value)
            builder.Append(!char.IsControl(c) || c == '\t' ? c : $"\\u{(int)c:x4}");
        const int max = 512;
        return builder.Length <= max ? builder.ToString() : builder.ToString(0, max) + "…";
    }

    private static void PrintUsage()
    {
        Console.WriteLine("""
            ManicTimePluginTester.Cli — test ManicTime document tracker plugins without ManicTime.

            Usage:
              ManicTimePluginTester.Cli <path>... [--pid <id>] [--interval <seconds>] [--all] [--json] [--once]

            <path> can be:
              a plugin package dir   (contains PluginSpec.json, dll in Lib/)
              a Packages root        (contains package dirs; a Plugins/ root also works)
              a plain dir with dlls  (retrievers are discovered without a PluginSpec)

            Options:
              --pid <id>             watch this process instead of the foreground window
                                     (test retrievers without switching focus to the app)
              --interval <seconds>   polling interval, default 2
              --all                  print every poll, not only changes
              --json                 one JSON object per line instead of text
              --once                 sample once and exit

            Exit codes:
              0  sampled successfully
              1  bad arguments, a path that could not be found or loaded, or no sample taken

            Example (from the repository root; installable-plugin/ exists after building the
            sample plugins, which requires Windows — elsewhere point at your own package):
              dotnet run --project source/tracker-plugin/ManicTimePluginTester.Cli -- installable-plugin/Debug/Plugins/Packages
            """);
    }
}
