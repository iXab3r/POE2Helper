using CheatCartridge.Tests.TestSupport;

namespace CheatCartridge.Tests.Scaffold;

/// <summary>
/// WHAT: Verifies the new CheatCartridge test scaffold keeps the intended live-client backend.
/// HOW: Checks the shared test support contract without touching a real Path Of Exile process.
/// </summary>
public sealed class CheatCartridgeTestScaffoldTests
{
    /// <summary>
    /// WHAT: Documents LocalProcess as the default integration backend.
    /// HOW: Asserts the shared backend name used by live-client test support.
    /// </summary>
    [Test]
    public void ShouldUseLocalProcessAsDefaultLiveClientBackend()
    {
        // Given
        const string expectedBackend = "LocalProcess";

        // When
        var backend = PathOfExileClientProcess.ProcessApiName;

        // Then
        backend.ShouldBe(expectedBackend);
    }
}
