using System.Diagnostics;

namespace CheatCartridge.Tests.TestSupport;

/// <summary>
/// WHAT: Finds a running Path Of Exile client for live integration tests.
/// HOW: Prefers an explicit process id override, then checks common client process names.
/// </summary>
public static class PathOfExileClientProcess
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

    /// <summary>
    /// WHAT: Names the process backend CheatCartridge integration tests are expected to use.
    /// HOW: Exposes a single constant for scaffold tests and progress output.
    /// </summary>
    public static string ProcessApiName => "LocalProcess";

    /// <summary>
    /// WHAT: Resolves a live Path Of Exile process or skips the integration test clearly.
    /// HOW: Reads <c>CHEATCARTRIDGE_POE_PROCESS_ID</c> first, then searches common process names.
    /// </summary>
    public static Process FindRunningOrIgnore()
    {
        var explicitProcess = TryGetExplicitProcess();
        if (explicitProcess != null)
        {
            return explicitProcess;
        }

        var discoveredProcess = CandidateProcessNames
            .SelectMany(Process.GetProcessesByName)
            .OrderByDescending(x => x.MainWindowHandle != IntPtr.Zero)
            .FirstOrDefault();

        if (discoveredProcess != null)
        {
            return discoveredProcess;
        }

        Assert.Ignore(
            $"No running Path Of Exile client was found. Start the client or set {ProcessIdEnvironmentVariable} to a target process id.");
        throw new UnreachableException();
    }

    /// <summary>
    /// WHAT: Formats stable process facts for integration test output.
    /// HOW: Avoids MainModule access because it can require extra permissions.
    /// </summary>
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
            Assert.Ignore($"{ProcessIdEnvironmentVariable} is set to '{value}', but it is not a valid process id.");
            throw new UnreachableException();
        }

        try
        {
            return Process.GetProcessById(processId);
        }
        catch (ArgumentException)
        {
            Assert.Ignore($"{ProcessIdEnvironmentVariable} points to PID {processId}, but that process is not running.");
            throw new UnreachableException();
        }
    }
}
