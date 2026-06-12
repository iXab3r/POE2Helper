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
    /// WHAT: Skips the test when LocalProcess is unavailable in the current runtime.
    /// HOW: Performs a self-process attach and converts known runtime-loading failures into NUnit skips.
    /// </summary>
    public static void IgnoreIfUnavailable()
    {
        try
        {
            using var process = LocalProcess.ByProcessId(Process.GetCurrentProcess().Id);
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
    }
}
