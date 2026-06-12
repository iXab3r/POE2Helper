using System.IO;
using CheatCartridge.GameHelper;
using CheatCartridge.GameHelper.RemoteObjects.Components;
using CheatCartridge.Tests.TestSupport;
using EyeAuras.Memory;
using EyeAuras.Memory.Scaffolding;
using PoeShared.Logging;
using PlayerComponent = CheatCartridge.GameHelper.RemoteObjects.Components.Player;

namespace CheatCartridge.Tests.Integration;

/// <summary>
/// WHAT: Provides the first live Path Of Exile integration smoke test for CheatCartridge.
/// HOW: Finds a running client and opens its main module through EyeAuras LocalProcess.
/// </summary>
[Category("integration")]
public sealed class LocalProcessClientIntegrationTests
{
    /// <summary>
    /// WHAT: Verifies CheatCartridge can attach to a running Path Of Exile client through LocalProcess.
    /// HOW: Resolves a process, opens it with LocalProcess, and creates a module memory view.
    /// </summary>
    [Test]
    [Explicit("Requires a running Path Of Exile client and LocalProcess access.")]
    public void ShouldAttachToRunningPathOfExileClientWithLocalProcess()
    {
        // Given
        LocalProcessRuntime.IgnoreIfUnavailable();
        using var targetProcess = PathOfExileClientProcess.FindRunningOrIgnore();
        TestContext.Progress.WriteLine($"[poe-client] Target: {PathOfExileClientProcess.Describe(targetProcess)}");

        // When
        using var process = LocalProcess.ByProcessId(targetProcess.Id);
        var moduleName = Path.GetFileNameWithoutExtension(process.ProcessName) + ".exe";
        using var memory = process.MemoryOfModule(moduleName);

        // Then
        process.ProcessId.ShouldBe(targetProcess.Id);
        process.ProcessName.ShouldNotBeNullOrWhiteSpace();
        memory.ShouldNotBeNull();
        TestContext.Progress.WriteLine($"[poe-client] Attached via {PathOfExileClientProcess.ProcessApiName}: {process.ProcessName}, module {moduleName}");
    }

    /// <summary>
    /// WHAT: Verifies CheatCartridge can read the current player snapshot from a live Path Of Exile client.
    /// HOW: Opens the client through LocalProcess, refreshes <see cref="TheGame"/>, then checks player identity, area facts, and full vitals.
    /// </summary>
    [Test]
    [Explicit("Requires a running Path Of Exile client with the character loaded and at full life, mana, and energy shield.")]
    public void ShouldReadCurrentPlayerSnapshotWithFullVitalsFromObjectModel()
    {
        // Given
        LocalProcessRuntime.IgnoreIfUnavailable();
        using var targetProcess = PathOfExileClientProcess.FindRunningOrIgnore();
        TestContext.Progress.WriteLine($"[poe-client] Target: {PathOfExileClientProcess.Describe(targetProcess)}");

        // When
        using var process = LocalProcess.ByProcessId(targetProcess.Id);
        var moduleName = Path.GetFileNameWithoutExtension(process.ProcessName) + ".exe";
        using var memory = process.MemoryOfModule(moduleName);
        using var game = new TheGame(Mock.Of<IFluentLog>(), memory);
        game.UpdateData();

        var area = game.States.InGameStateObject.CurrentAreaInstance;
        var player = game.Player;
        var hasPlayerComponent = player.TryGetComponent<PlayerComponent>(out var playerComponent);
        var hasLifeComponent = player.TryGetComponent<Life>(out var life);

        TestContext.Progress.WriteLine(
            "[poe-player] " +
            $"name='{playerComponent?.Name}', " +
            $"entity=0x{player.Address.ToInt64():X}, " +
            $"id={player.Id}, " +
            $"path='{player.Path}', " +
            $"areaLevel={area.CurrentAreaLevel}, " +
            $"areaHash={area.CurrentAreaHash}, " +
            $"entities={area.EntitiesCount}");

        if (hasLifeComponent)
        {
            TestContext.Progress.WriteLine(
                "[poe-vitals] " +
                $"hp={life.Health.Current}/{life.Health.Total}, " +
                $"mp={life.Mana.Current}/{life.Mana.Total}, " +
                $"es={life.EnergyShield.Current}/{life.EnergyShield.Total}");
        }

        // Then
        process.ProcessId.ShouldBe(targetProcess.Id);
        area.Address.ShouldNotBe(IntPtr.Zero);
        area.CurrentAreaLevel.ShouldBeGreaterThan((byte)0);
        area.CurrentAreaHash.ShouldBeGreaterThan(0u);
        area.EntitiesCount.ShouldBeGreaterThan(0u);

        player.Address.ShouldNotBe(IntPtr.Zero);
        player.IsValid.ShouldBeTrue("The local player entity should be valid after TheGame.UpdateData().");
        player.Id.ShouldBeGreaterThan(0u);
        player.Path.ShouldNotBeNullOrWhiteSpace();

        hasPlayerComponent.ShouldBeTrue("The local player entity should expose a Player component.");
        playerComponent!.Name.ShouldNotBeNullOrWhiteSpace();

        hasLifeComponent.ShouldBeTrue("The local player entity should expose a Life component.");
        life!.Health.Total.ShouldBeGreaterThan(0);
        life.Mana.Total.ShouldBeGreaterThan(0);
        life.EnergyShield.Total.ShouldBeGreaterThanOrEqualTo(0);

        life.Health.Current.ShouldBe(life.Health.Total, "Expected the character to be at full health before running this integration test.");
        life.Mana.Current.ShouldBe(life.Mana.Total, "Expected the character to be at full mana before running this integration test.");
        life.EnergyShield.Current.ShouldBe(life.EnergyShield.Total, "Expected the character to be at full energy shield before running this integration test.");
    }
}
