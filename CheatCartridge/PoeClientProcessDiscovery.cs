using System.Diagnostics;

namespace CheatCartridge;

internal static class PoeClientProcessDiscovery
{
    private const string ProcessIdEnvironmentVariable = "CHEATCARTRIDGE_POE_PROCESS_ID";

    private static readonly string[] CandidateProcessNames =
    [
        "PathOfExile2",
        "PathOfExile2Steam",
        "PathOfExileSteam",
        "PathOfExile_x64Steam",
        "PathOfExile_x64",
        "PathOfExile"
    ];

    public static Process? TryFindRunningProcess()
    {
        var explicitProcess = TryGetExplicitProcess();
        if (explicitProcess != null)
        {
            return explicitProcess;
        }

        var candidates = CandidateProcessNames
            .SelectMany(Process.GetProcessesByName)
            .ToArray();

        var selected = candidates
            .OrderByDescending(x => x.MainWindowHandle != IntPtr.Zero)
            .FirstOrDefault();

        foreach (var candidate in candidates)
        {
            if (!ReferenceEquals(candidate, selected))
            {
                candidate.Dispose();
            }
        }

        return selected;
    }

    public static string Describe(Process process)
    {
        return $"{process.ProcessName} (PID {process.Id}, window 0x{process.MainWindowHandle.ToInt64():X})";
    }

    private static Process? TryGetExplicitProcess()
    {
        var value = Environment.GetEnvironmentVariable(ProcessIdEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (!int.TryParse(value, out var processId))
        {
            return null;
        }

        try
        {
            return Process.GetProcessById(processId);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }
}
