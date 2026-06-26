using System.Diagnostics;
using System.IO;
using EyeAuras.Memory;

namespace CheatCartridge.Tests.TestSupport;

/// <summary>
/// WHAT: Guards live LocalProcess tests from running with unavailable EyeAuras runtime assemblies.
/// HOW: Opens the current testhost process through LocalProcess before touching the game client.
/// </summary>
public static class LocalProcessRuntime
{
    /// <summary>
    /// WHAT: Names the concrete local process backend used by live integration tests.
    /// HOW: Matches the current EyeAuras.Memory module API exposed by local EyeAuras checkouts.
    /// </summary>
    public static string ProcessApiName => "NativeLocalProcess";

    /// <summary>
    /// WHAT: Opens a process through the current EyeAuras local-memory backend.
    /// HOW: Uses read-only native local process access because CheatCartridge only reads PoE memory.
    /// </summary>
    public static IProcess OpenProcess(int processId)
    {
        return NativeLocalProcess.ByProcessId(
            processId,
            isReadOnly: true,
            omitCreateThreadPermissions: true);
    }

    /// <summary>
    /// WHAT: Skips the test when LocalProcess is unavailable in the current runtime.
    /// HOW: Performs a self-process attach and converts known runtime-loading failures into NUnit skips.
    /// </summary>
    public static void IgnoreIfUnavailable()
    {
        try
        {
            using var process = OpenProcess(Process.GetCurrentProcess().Id);
            TestContext.Progress.WriteLine($"[local-process] Runtime available: {process.ProcessName} ({process.ProcessId})");
        }
        catch (NotImplementedException ex)
        {
            Assert.Ignore($"EyeAuras LocalProcess is not implemented in the loaded runtime assemblies: {ex.Message}");
        }
        catch (DllNotFoundException ex)
        {
            Assert.Ignore($"EyeAuras LocalProcess native dependency is missing: {ex.Message}");
        }
        catch (FileNotFoundException ex)
        {
            Assert.Ignore($"EyeAuras LocalProcess managed dependency is missing: {ex.Message}");
        }
        catch (FileLoadException ex)
        {
            Assert.Ignore($"EyeAuras LocalProcess dependency could not be loaded: {ex.Message}");
        }
        catch (MissingMethodException ex)
        {
            Assert.Ignore($"EyeAuras LocalProcess API is not compatible with these tests: {ex.Message}");
        }
        catch (TypeLoadException ex)
        {
            Assert.Ignore($"EyeAuras LocalProcess API is not compatible with these tests: {ex.Message}");
        }
    }
}
