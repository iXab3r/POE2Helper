using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using CheatCartridge.GameHelper;
using CheatCartridge.GameHelper.GameOffsets.Objects.Components;
using CheatCartridge.GameHelper.GameOffsets;
using CheatCartridge.GameHelper.GameOffsets.States;
using CheatCartridge.GameHelper.GameOffsets.States.InGameState;
using CheatCartridge.GameHelper.Natives;
using CheatCartridge.GameHelper.RemoteEnums;
using CheatCartridge.GameHelper.RemoteObjects;
using CheatCartridge.GameHelper.RemoteObjects.Components;
using CheatCartridge.Tests.Scaffold.Poe;
using CheatCartridge.Tests.TestSupport;
using CheatCartridge.Tests.Scaffold.Poe.RuntimeLayouts;
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
        using var process = LocalProcessRuntime.OpenProcess(targetProcess.Id);
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
        using var process = LocalProcessRuntime.OpenProcess(targetProcess.Id);
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

            TestContext.Progress.WriteLine(
                "[poe-vital-stats] " +
                $"hp=0x{life.Health.UnknownStatId0:X}/0x{life.Health.UnknownStatId1:X}/0x{life.Health.UnknownStatId2:X}/0x{life.Health.UnknownStatId3:X}, " +
                $"mp=0x{life.Mana.UnknownStatId0:X}/0x{life.Mana.UnknownStatId1:X}/0x{life.Mana.UnknownStatId2:X}/0x{life.Mana.UnknownStatId3:X}, " +
                $"es=0x{life.EnergyShield.UnknownStatId0:X}/0x{life.EnergyShield.UnknownStatId1:X}/0x{life.EnergyShield.UnknownStatId2:X}/0x{life.EnergyShield.UnknownStatId3:X}");
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

    /// <summary>
    /// WHAT: Exports the currently resolved runtime layout as an FF/ReClass proto document.
    /// HOW: Resolves the live keypoints through <see cref="PoeRuntimeLayouts"/> and writes the FF artifact under docs/PoE/RE/layouts/resolved/live.
    /// </summary>
    [Test]
    [Explicit("Requires a running Path Of Exile client. Writes a resolved FF layout artifact into docs/PoE/RE/layouts/resolved/live.")]
    public void ShouldExportResolvedRuntimeLayoutAsFfProto()
    {
        // Given
        LocalProcessRuntime.IgnoreIfUnavailable();
        using var targetProcess = PathOfExileClientProcess.FindRunningOrIgnore();
        TestContext.Progress.WriteLine($"[poe-client] Target: {PathOfExileClientProcess.Describe(targetProcess)}");

        // When
        using var process = LocalProcessRuntime.OpenProcess(targetProcess.Id);
        var moduleName = Path.GetFileNameWithoutExtension(process.ProcessName) + ".exe";
        using var memory = process.MemoryOfModule(moduleName);
        var capturedAt = DateTimeOffset.UtcNow;
        var executablePath = RequireMainModuleFileName(targetProcess);
        var executableSha256 = ComputeSha256(executablePath);
        var buildId = $"sha256-{executableSha256[..8]}";
        var moduleKey = Path.GetFileNameWithoutExtension(moduleName);
        var layouts = PoeRuntimeLayouts.Resolve(memory);
        var proto = PoeFfLayoutProtoWriter.WriteResolved(
            layouts,
            new PoeFfLayoutProtoWriterOptions
            {
                SourceModuleName = moduleName,
                BuildId = buildId,
                SourceSha256 = executableSha256,
                CapturedAt = capturedAt
            });

        var repositoryRoot = FindRepositoryRoot();
        var outputDirectory = Path.Combine(
            repositoryRoot.FullName,
            "docs",
            "PoE",
            "RE",
            "layouts",
            "resolved",
            buildId,
            moduleKey);
        Directory.CreateDirectory(outputDirectory);
        var layoutFileName = CreateResolvedLayoutFileName(buildId);
        var outputPath = Path.Combine(outputDirectory, layoutFileName);
        File.WriteAllText(outputPath, proto, Encoding.UTF8);

        var relativeOutputPath = NormalizeRelativePath(repositoryRoot, outputPath);
        var currentManifestPath = Path.Combine(repositoryRoot.FullName, "docs", "PoE", "RE", "layouts", "resolved", "current.json");
        var manifest = new PoeFrameFormatLayoutCurrentManifest(
            BuildId: buildId,
            SourceSha256: executableSha256,
            ModuleName: moduleName,
            ModuleKey: moduleKey,
            LayoutPath: relativeOutputPath,
            GeneratedAtUtc: capturedAt);
        File.WriteAllText(
            currentManifestPath,
            JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }) + Environment.NewLine,
            Encoding.UTF8);

        TestContext.Progress.WriteLine($"[poe-ff-layout] Wrote {proto.Length} chars to {outputPath}");
        TestContext.Progress.WriteLine($"[poe-ff-layout] Updated current manifest at {currentManifestPath}");

        // Then
        File.Exists(outputPath).ShouldBeTrue();
        File.Exists(currentManifestPath).ShouldBeTrue();
        buildId.ShouldStartWith("sha256-");
        proto.ShouldContain("message InGameState");
        proto.ShouldContain($"@ffmeta build_id={buildId}");
        proto.ShouldContain($"@ffmeta source_sha256={executableSha256}");
        proto.ShouldContain("int32 ms_elapsed = 2;");
        proto.ShouldNotContain("@ffanchor");
        proto.ShouldNotContain("@ffkeypoint");
        proto.ShouldNotContain("@ffresolve");
        proto.ShouldNotContain("@reclass size=");
    }

    /// <summary>
    /// WHAT: Locks down the first RE keypoint: the GameStates singleton is reachable from the signature in <see cref="Offsets"/>.
    /// HOW: Resolves the live RIP-relative global slot, reads the state owner, and verifies the InGame state chain is coherent.
    /// </summary>
    [Test]
    [Explicit("Requires a running Path Of Exile client with the character loaded in-world.")]
    public void ShouldResolveGameStatesSingletonFromSignatureKeypoint()
    {
        // Given
        LocalProcessRuntime.IgnoreIfUnavailable();
        using var targetProcess = PathOfExileClientProcess.FindRunningOrIgnore();
        TestContext.Progress.WriteLine($"[poe-client] Target: {PathOfExileClientProcess.Describe(targetProcess)}");

        // When
        using var process = LocalProcessRuntime.OpenProcess(targetProcess.Id);
        var moduleName = Path.GetFileNameWithoutExtension(process.ProcessName) + ".exe";
        using var memory = process.MemoryOfModule(moduleName);

        var patternOffsets = MemoryUtils
            .GetOffsets(memory, Offsets.Patterns)
            .ToDictionary(x => x.Key.Name!, x => x.Value);
        patternOffsets.ContainsKey(nameof(GameStates)).ShouldBeTrue("Offsets.Patterns must resolve the GameStates signature.");

        var displacementRva = patternOffsets[nameof(GameStates)];
        var displacementAddress = Add(memory.BaseAddress, displacementRva);
        var relativeOffset = memory.Read<int>(displacementAddress);
        var gameStatesGlobalSlot = Add(displacementAddress, relativeOffset + sizeof(int));
        var staticObject = memory.Read<GameStateStaticOffset>(gameStatesGlobalSlot);
        var stateOwner = memory.Read<GameStateOffset>(staticObject.GameState);
        var inGameStateAddress = stateOwner.States[(int)GameStateTypes.InGameState].X;
        var inGameState = memory.Read<InGameStateOffset>(inGameStateAddress);

        TestContext.Progress.WriteLine(
            "[poe-keypoint] " +
            $"patternRva=0x{displacementRva:X}, " +
            $"globalSlot=0x{gameStatesGlobalSlot.ToInt64():X}, " +
            $"gameStates=0x{staticObject.GameState.ToInt64():X}, " +
            $"inGameState=0x{inGameStateAddress.ToInt64():X}, " +
            $"area=0x{inGameState.AreaInstanceData.ToInt64():X}");

        // Then
        process.ProcessId.ShouldBe(targetProcess.Id);
        displacementRva.ShouldBeGreaterThan(0);
        relativeOffset.ShouldNotBe(0);
        gameStatesGlobalSlot.ShouldNotBe(IntPtr.Zero);
        staticObject.GameState.ShouldNotBe(IntPtr.Zero);

        stateOwner.CurrentStatePtr.First.ShouldNotBe(IntPtr.Zero);
        stateOwner.CurrentStatePtr.Last.ShouldNotBe(IntPtr.Zero);
        stateOwner.CurrentStatePtr.Last.ToInt64().ShouldBeGreaterThan(stateOwner.CurrentStatePtr.First.ToInt64());

        var knownStates = stateOwner.States.Enumerate().Where(x => x.StatePtr.X != IntPtr.Zero).ToArray();
        knownStates.Length.ShouldBeGreaterThanOrEqualTo(8);
        inGameStateAddress.ShouldNotBe(IntPtr.Zero);
        inGameState.AreaInstanceData.ShouldNotBe(IntPtr.Zero);
    }

    /// <summary>
    /// WHAT: Verifies the GameStates shared wrapper layout returned by the singleton getter.
    /// HOW: Resolves the getter tail that writes owner/sidecar pointers into the output buffer and compares it to the live global slots.
    /// </summary>
    [Test]
    [Explicit("Requires a running Path Of Exile client with the character loaded in-world.")]
    public void ShouldResolveGameStateStaticWrapperFromGetterPattern()
    {
        // Given
        LocalProcessRuntime.IgnoreIfUnavailable();
        using var targetProcess = PathOfExileClientProcess.FindRunningOrIgnore();
        TestContext.Progress.WriteLine($"[poe-client] Target: {PathOfExileClientProcess.Describe(targetProcess)}");

        // When
        using var process = LocalProcessRuntime.OpenProcess(targetProcess.Id);
        var moduleName = Path.GetFileNameWithoutExtension(process.ProcessName) + ".exe";
        using var memory = process.MemoryOfModule(moduleName);

        var staticPatternOffsets = MemoryUtils
            .GetOffsets(memory, Offsets.Patterns)
            .ToDictionary(x => x.Key.Name!, x => x.Value);
        var keypointOffsets = MemoryUtils
            .GetOffsets(memory, Offsets.KeypointPatterns)
            .ToDictionary(x => x.Key.Name!, x => x.Value);

        var gameStatesGlobalSlot = ResolveRipRelativeSlot(memory, staticPatternOffsets[nameof(GameStates)]);
        var wrapperCopyRva = keypointOffsets[Offsets.KeypointNames.GameStateStaticWrapper];
        var wrapperCopyAddress = Add(memory.BaseAddress, wrapperCopyRva);
        var ownerGlobalSlot = ResolveRipRelativeSlot(memory, wrapperCopyRva - 0x04);
        var sidecarGlobalSlot = ResolveRipRelativeSlot(memory, wrapperCopyRva + 0x06);
        var ownerStoreOpcode = memory.Read<byte>(wrapperCopyAddress, 3);
        var sidecarWrapperOffset = memory.Read<byte>(Add(wrapperCopyAddress, 0x0D));
        var wrapper = memory.Read<StdTuple2D<IntPtr>>(ownerGlobalSlot);
        var staticObject = memory.Read<GameStateStaticOffset>(ownerGlobalSlot);
        var stateOwner = memory.Read<GameStateOffset>(staticObject.GameState);
        var inGameEntry = stateOwner.States[(int)GameStateTypes.InGameState];

        TestContext.Progress.WriteLine(
            "[poe-keypoint] " +
            $"wrapperCopyRva=0x{wrapperCopyRva:X}, " +
            $"ownerGlobalSlot=0x{ownerGlobalSlot.ToInt64():X}, " +
            $"sidecarGlobalSlot=0x{sidecarGlobalSlot.ToInt64():X}, " +
            $"ownerWrapperOffset=0x0, " +
            $"sidecarWrapperOffset=0x{sidecarWrapperOffset:X}, " +
            $"gameStates=0x{wrapper.X.ToInt64():X}, " +
            $"sidecar=0x{wrapper.Y.ToInt64():X}, " +
            $"inGameState=0x{inGameEntry.X.ToInt64():X}");

        // Then
        process.ProcessId.ShouldBe(targetProcess.Id);
        gameStatesGlobalSlot.ShouldBe(ownerGlobalSlot);
        sidecarGlobalSlot.ShouldBe(Add(ownerGlobalSlot, IntPtr.Size));
        ownerStoreOpcode.ShouldBe(new byte[] { 0x48, 0x89, 0x06 });
        ((int)sidecarWrapperOffset).ShouldBe(IntPtr.Size);

        Marshal.OffsetOf<GameStateStaticOffset>(nameof(GameStateStaticOffset.GameState)).ToInt32().ShouldBe(0);
        staticObject.GameState.ShouldBe(wrapper.X);
        staticObject.GameState.ShouldNotBe(IntPtr.Zero);
        wrapper.Y.ShouldBe(memory.Read<IntPtr>(sidecarGlobalSlot));
        wrapper.Y.ShouldNotBe(IntPtr.Zero);

        stateOwner.States[(int)GameStateTypes.InGameState].X.ShouldBe(inGameEntry.X);
        inGameEntry.X.ShouldNotBe(IntPtr.Zero);
        inGameEntry.Y.ShouldNotBe(IntPtr.Zero);
    }

    /// <summary>
    /// WHAT: Verifies the current-state vector offset is recovered from the GameStates dirty-copy path.
    /// HOW: Resolves FUN_1415B0360's source-vector count calculation, then reads the live current-state entry through the recovered vector.
    /// </summary>
    [Test]
    [Explicit("Requires a running Path Of Exile client with the character loaded in-world.")]
    public void ShouldResolveCurrentStateVectorFromDirtyCopyPattern()
    {
        // Given
        LocalProcessRuntime.IgnoreIfUnavailable();
        using var targetProcess = PathOfExileClientProcess.FindRunningOrIgnore();
        TestContext.Progress.WriteLine($"[poe-client] Target: {PathOfExileClientProcess.Describe(targetProcess)}");

        // When
        using var process = LocalProcessRuntime.OpenProcess(targetProcess.Id);
        var moduleName = Path.GetFileNameWithoutExtension(process.ProcessName) + ".exe";
        using var memory = process.MemoryOfModule(moduleName);

        var staticPatternOffsets = MemoryUtils
            .GetOffsets(memory, Offsets.Patterns)
            .ToDictionary(x => x.Key.Name!, x => x.Value);
        var keypointOffsets = MemoryUtils
            .GetOffsets(memory, Offsets.KeypointPatterns)
            .ToDictionary(x => x.Key.Name!, x => x.Value);

        var gameStatesGlobalSlot = ResolveRipRelativeSlot(memory, staticPatternOffsets[nameof(GameStates)]);
        var staticObject = memory.Read<GameStateStaticOffset>(gameStatesGlobalSlot);
        var stateOwner = memory.Read<GameStateOffset>(staticObject.GameState);
        var runtimeOffsets = RuntimeGameOffsets.Resolve(memory);

        var currentVectorOffsetAddress = Add(memory.BaseAddress, keypointOffsets[Offsets.KeypointNames.GameStateCurrentStateVector]);
        var currentVectorFirstOffset = memory.Read<byte>(currentVectorOffsetAddress);
        var currentVectorLastOffset = memory.Read<byte>(Add(currentVectorOffsetAddress, -0x04));
        var dispatchVectorLastOffset = memory.Read<byte>(Add(currentVectorOffsetAddress, 0x08));
        var dispatchVectorFirstOffset = memory.Read<byte>(Add(currentVectorOffsetAddress, 0x0C));
        var dirtyFlagOffset = memory.Read<byte>(Add(currentVectorOffsetAddress, -0x0F));

        var recoveredCurrentVector = memory.Read<StdVector>(Add(staticObject.GameState, currentVectorFirstOffset));
        var recoveredDispatchVector = memory.Read<StdVector>(Add(staticObject.GameState, dispatchVectorFirstOffset));
        var currentEntry = memory.Read<StdTuple2D<IntPtr>>(Add(recoveredCurrentVector.Last, -0x10));
        var providerCurrentEntry = runtimeOffsets.ReadCurrentStateEntry(memory, staticObject.GameState);
        var providerDispatchVector = runtimeOffsets.ReadDispatchStateVector(memory, staticObject.GameState);
        var inGameEntry = stateOwner.States[(int)GameStateTypes.InGameState];

        TestContext.Progress.WriteLine(
            "[poe-keypoint] " +
            $"gameStates=0x{staticObject.GameState.ToInt64():X}, " +
            $"currentVectorFirstOffset=0x{currentVectorFirstOffset:X}, " +
            $"currentVectorLastOffset=0x{currentVectorLastOffset:X}, " +
            $"dispatchVectorFirstOffset=0x{dispatchVectorFirstOffset:X}, " +
            $"dispatchVectorLastOffset=0x{dispatchVectorLastOffset:X}, " +
            $"dirtyFlagOffset=0x{dirtyFlagOffset:X}, " +
            $"currentVector={recoveredCurrentVector}, " +
            $"dispatchVector={recoveredDispatchVector}, " +
            $"currentState=0x{currentEntry.X.ToInt64():X}, " +
            $"inGameState=0x{inGameEntry.X.ToInt64():X}");

        // Then
        process.ProcessId.ShouldBe(targetProcess.Id);
        staticObject.GameState.ShouldNotBe(IntPtr.Zero);

        currentVectorFirstOffset.ShouldBe((byte)Marshal.OffsetOf<GameStateOffset>(nameof(GameStateOffset.CurrentStatePtr)));
        currentVectorLastOffset.ShouldBe((byte)(currentVectorFirstOffset + IntPtr.Size));
        dispatchVectorFirstOffset.ShouldBe((byte)0x20);
        dispatchVectorLastOffset.ShouldBe((byte)(dispatchVectorFirstOffset + IntPtr.Size));
        dirtyFlagOffset.ShouldBe((byte)0x40);
        runtimeOffsets.GameStateCurrentStateVectorOffset.ShouldBe((int)currentVectorFirstOffset);
        runtimeOffsets.GameStateCurrentStateVectorLastOffset.ShouldBe((int)currentVectorLastOffset);
        runtimeOffsets.GameStateDispatchVectorOffset.ShouldBe((int)dispatchVectorFirstOffset);
        runtimeOffsets.GameStateDispatchVectorLastOffset.ShouldBe((int)dispatchVectorLastOffset);
        runtimeOffsets.GameStateDispatchVectorEndOffset.ShouldBe(dispatchVectorLastOffset + IntPtr.Size);
        runtimeOffsets.GameStateDirtyFlagOffset.ShouldBe(dirtyFlagOffset);

        recoveredCurrentVector.First.ShouldBe(stateOwner.CurrentStatePtr.First);
        recoveredCurrentVector.Last.ShouldBe(stateOwner.CurrentStatePtr.Last);
        recoveredCurrentVector.End.ShouldBe(stateOwner.CurrentStatePtr.End);
        recoveredCurrentVector.TotalElements(0x10).ShouldBeGreaterThan(0);
        recoveredDispatchVector.TotalElements(0x10).ShouldBeGreaterThan(0);
        providerDispatchVector.First.ShouldBe(recoveredDispatchVector.First);
        providerDispatchVector.Last.ShouldBe(recoveredDispatchVector.Last);
        providerDispatchVector.End.ShouldBe(recoveredDispatchVector.End);

        currentEntry.X.ShouldNotBe(IntPtr.Zero);
        currentEntry.X.ShouldBe(inGameEntry.X, "The test requires the character to be loaded in-world, so the latest current-state entry should be InGameState.");
        currentEntry.Y.ShouldNotBe(IntPtr.Zero);
        providerCurrentEntry.X.ShouldBe(currentEntry.X);
        providerCurrentEntry.Y.ShouldBe(currentEntry.Y);
    }

    /// <summary>
    /// WHAT: Verifies the next RE keypoints can recover state-table and InGame area offsets from game code.
    /// HOW: Reads immediates from unique live byte patterns, then uses those derived offsets to walk GameStates -> InGameState -> AreaInstance.
    /// </summary>
    [Test]
    [Explicit("Requires a running Path Of Exile client with the character loaded in-world.")]
    public void ShouldResolveInGameAreaKeypointsFromCodePatterns()
    {
        // Given
        LocalProcessRuntime.IgnoreIfUnavailable();
        using var targetProcess = PathOfExileClientProcess.FindRunningOrIgnore();
        TestContext.Progress.WriteLine($"[poe-client] Target: {PathOfExileClientProcess.Describe(targetProcess)}");

        // When
        using var process = LocalProcessRuntime.OpenProcess(targetProcess.Id);
        var moduleName = Path.GetFileNameWithoutExtension(process.ProcessName) + ".exe";
        using var memory = process.MemoryOfModule(moduleName);

        var staticPatternOffsets = MemoryUtils
            .GetOffsets(memory, Offsets.Patterns)
            .ToDictionary(x => x.Key.Name!, x => x.Value);
        var keypointOffsets = MemoryUtils
            .GetOffsets(memory, Offsets.KeypointPatterns)
            .ToDictionary(x => x.Key.Name!, x => x.Value);

        var gameStatesGlobalSlot = ResolveRipRelativeSlot(memory, staticPatternOffsets[nameof(GameStates)]);
        var staticObject = memory.Read<GameStateStaticOffset>(gameStatesGlobalSlot);

        var tableShapeAddress = Add(memory.BaseAddress, keypointOffsets[Offsets.KeypointNames.GameStateTableShape]);
        var stateTableOffset = memory.Read<byte>(tableShapeAddress);
        var stateEntrySize = memory.Read<int>(Add(tableShapeAddress, 21));
        var stateCount = memory.Read<int>(Add(tableShapeAddress, 27));

        var inGameStateIndex = (int)GameStateTypes.InGameState;
        var inGameEntryOffset = stateTableOffset + inGameStateIndex * stateEntrySize;
        var inGameEntry = memory.Read<StdTuple2D<IntPtr>>(Add(staticObject.GameState, inGameEntryOffset));

        var areaOffsetAddress = Add(memory.BaseAddress, keypointOffsets[Offsets.KeypointNames.InGameStateAreaInstanceData]);
        var areaInstanceOffset = memory.Read<int>(areaOffsetAddress);
        var areaInstanceAddress = memory.Read<IntPtr>(Add(inGameEntry.X, areaInstanceOffset));
        var inGameStateByStruct = memory.Read<InGameStateOffset>(inGameEntry.X);

        TestContext.Progress.WriteLine(
            "[poe-keypoint] " +
            $"stateTableOffset=0x{stateTableOffset:X}, " +
            $"stateEntrySize=0x{stateEntrySize:X}, " +
            $"stateCount=0x{stateCount:X}, " +
            $"inGameEntryOffset=0x{inGameEntryOffset:X}, " +
            $"inGameStateIndex={inGameStateIndex}, " +
            $"areaInstanceOffset=0x{areaInstanceOffset:X}, " +
            $"inGameState=0x{inGameEntry.X.ToInt64():X}, " +
            $"area=0x{areaInstanceAddress.ToInt64():X}");

        // Then
        process.ProcessId.ShouldBe(targetProcess.Id);
        staticObject.GameState.ShouldNotBe(IntPtr.Zero);

        stateTableOffset.ShouldBe((byte)Marshal.OffsetOf<GameStateOffset>(nameof(GameStateOffset.States)));
        stateEntrySize.ShouldBe(IntPtr.Size * 2);
        stateCount.ShouldBe(GameStateBuffer.TOTAL_STATES);
        stateCount.ShouldBeGreaterThan(inGameStateIndex);

        inGameEntry.X.ShouldNotBe(IntPtr.Zero);
        inGameEntry.Y.ShouldNotBe(IntPtr.Zero);

        areaInstanceOffset.ShouldBe(Marshal.OffsetOf<InGameStateOffset>(nameof(InGameStateOffset.AreaInstanceData)).ToInt32());
        areaInstanceAddress.ShouldNotBe(IntPtr.Zero);
        areaInstanceAddress.ShouldBe(inGameStateByStruct.AreaInstanceData);
    }

    /// <summary>
    /// WHAT: Verifies the production root object model can consume runtime offsets recovered from static code patterns.
    /// HOW: Resolves <see cref="RuntimeGameOffsets"/>, walks GameStates -> InGameState -> AreaInstance, then compares the same values to <see cref="TheGame"/>.
    /// </summary>
    [Test]
    [Explicit("Requires a running Path Of Exile client with the character loaded in-world.")]
    public void ShouldReadRootObjectModelThroughRuntimeGameOffsets()
    {
        // Given
        LocalProcessRuntime.IgnoreIfUnavailable();
        using var targetProcess = PathOfExileClientProcess.FindRunningOrIgnore();
        TestContext.Progress.WriteLine($"[poe-client] Target: {PathOfExileClientProcess.Describe(targetProcess)}");

        // When
        using var process = LocalProcessRuntime.OpenProcess(targetProcess.Id);
        var moduleName = Path.GetFileNameWithoutExtension(process.ProcessName) + ".exe";
        using var memory = process.MemoryOfModule(moduleName);
        var runtimeOffsets = RuntimeGameOffsets.Resolve(memory);

        var gameStateOwner = memory.Read<IntPtr>(Add(runtimeOffsets.GameStatesGlobalSlot, runtimeOffsets.GameStateStaticGameStateOffset));
        var currentStateVector = runtimeOffsets.ReadCurrentStateVector(memory, gameStateOwner);
        var currentStateEntry = runtimeOffsets.ReadCurrentStateEntry(memory, gameStateOwner);
        var currentStateAddress = currentStateEntry.X;
        var inGameEntry = runtimeOffsets.ReadInGameStateEntry(memory, gameStateOwner);
        var enumInGameEntry = runtimeOffsets.ReadGameStateEntry(memory, gameStateOwner, GameStateTypes.InGameState);
        var areaAddress = memory.Read<IntPtr>(Add(inGameEntry.X, runtimeOffsets.InGameStateAreaInstanceDataOffset));
        var currentAreaLevel = memory.Read<byte>(Add(areaAddress, runtimeOffsets.AreaInstanceCurrentAreaLevelOffset));
        var currentAreaHash = memory.Read<uint>(Add(areaAddress, runtimeOffsets.AreaInstanceCurrentAreaHashOffset));
        var entitiesCount = memory.Read<uint>(Add(areaAddress, runtimeOffsets.AreaInstanceEntitiesCountOffset));
        var localPlayersVector = runtimeOffsets.ReadStdVectorHeader(
            memory,
            Add(areaAddress, runtimeOffsets.AreaInstanceLocalPlayersOffset));
        var localPlayers = runtimeOffsets.ReadStdVector<IntPtr>(memory, localPlayersVector, maxElements: 16);

        using var game = new TheGame(Mock.Of<IFluentLog>(), memory);
        game.UpdateData();
        var objectModelArea = game.States.InGameStateObject.CurrentAreaInstance;

        TestContext.Progress.WriteLine(
            "[poe-runtime-offsets] " +
            $"gameStatesGlobalSlot=0x{runtimeOffsets.GameStatesGlobalSlot.ToInt64():X}, " +
            $"gameStateOwner=0x{gameStateOwner.ToInt64():X}, " +
            $"currentState=0x{currentStateAddress.ToInt64():X}, " +
            $"inGameState=0x{inGameEntry.X.ToInt64():X}, " +
            $"area=0x{areaAddress.ToInt64():X}, " +
            $"level={currentAreaLevel}, " +
            $"hash=0x{currentAreaHash:X8}, " +
            $"entities={entitiesCount}, " +
            $"player=0x{localPlayers.FirstOrDefault().ToInt64():X}, " +
            $"currentStateVector={runtimeOffsets.CountStdVector<StdTuple2D<IntPtr>>(currentStateVector)}/{runtimeOffsets.CapacityStdVector<StdTuple2D<IntPtr>>(currentStateVector)}, " +
            $"localPlayersVector={runtimeOffsets.CountStdVector<IntPtr>(localPlayersVector)}/{runtimeOffsets.CapacityStdVector<IntPtr>(localPlayersVector)}");

        // Then
        process.ProcessId.ShouldBe(targetProcess.Id);
        runtimeOffsets.GameStatesGlobalSlot.ShouldNotBe(IntPtr.Zero);
        gameStateOwner.ShouldNotBe(IntPtr.Zero);

        runtimeOffsets.GameStateStaticGameStateOffset.ShouldBe(Marshal.OffsetOf<GameStateStaticOffset>(nameof(GameStateStaticOffset.GameState)).ToInt32());
        runtimeOffsets.GameStateStaticSidecarOffset.ShouldBe(IntPtr.Size);
        runtimeOffsets.GameStateCurrentStateVectorOffset.ShouldBe(Marshal.OffsetOf<GameStateOffset>(nameof(GameStateOffset.CurrentStatePtr)).ToInt32());
        runtimeOffsets.GameStateCurrentStateVectorLastOffset.ShouldBe(runtimeOffsets.GameStateCurrentStateVectorOffset + IntPtr.Size);
        runtimeOffsets.GameStateDispatchVectorOffset.ShouldBe(0x20);
        runtimeOffsets.GameStateDispatchVectorLastOffset.ShouldBe(runtimeOffsets.GameStateDispatchVectorOffset + IntPtr.Size);
        runtimeOffsets.GameStateDispatchVectorEndOffset.ShouldBe(runtimeOffsets.GameStateDispatchVectorLastOffset + IntPtr.Size);
        runtimeOffsets.GameStateDirtyFlagOffset.ShouldBe(0x40);
        runtimeOffsets.StdVectorFirstOffset.ShouldBe(Marshal.OffsetOf<StdVector>(nameof(StdVector.First)).ToInt32());
        runtimeOffsets.StdVectorLastOffset.ShouldBe(Marshal.OffsetOf<StdVector>(nameof(StdVector.Last)).ToInt32());
        runtimeOffsets.StdVectorEndOffset.ShouldBe(Marshal.OffsetOf<StdVector>(nameof(StdVector.End)).ToInt32());
        runtimeOffsets.IsValidStdVector<StdTuple2D<IntPtr>>(currentStateVector).ShouldBeTrue();
        runtimeOffsets.CountStdVector<StdTuple2D<IntPtr>>(currentStateVector).ShouldBeGreaterThan(0);
        runtimeOffsets.CapacityStdVector<StdTuple2D<IntPtr>>(currentStateVector)
            .ShouldBeGreaterThanOrEqualTo(runtimeOffsets.CountStdVector<StdTuple2D<IntPtr>>(currentStateVector));
        runtimeOffsets.IsValidStdVector<IntPtr>(localPlayersVector).ShouldBeTrue();
        runtimeOffsets.CountStdVector<IntPtr>(localPlayersVector).ShouldBe(localPlayers.Length);
        runtimeOffsets.CapacityStdVector<IntPtr>(localPlayersVector).ShouldBeGreaterThanOrEqualTo(localPlayers.Length);
        runtimeOffsets.GameStateTableOffset.ShouldBe(Marshal.OffsetOf<GameStateOffset>(nameof(GameStateOffset.States)).ToInt32());
        runtimeOffsets.GameStateEntrySize.ShouldBe(IntPtr.Size * 2);
        runtimeOffsets.GameStateCount.ShouldBe(GameStateBuffer.TOTAL_STATES);
        runtimeOffsets.GameStateVirtualUpdateSlotOffset.ShouldBe(0x08);
        runtimeOffsets.InGameStateMsElapsedWriterFunctionAddress.ShouldNotBe(IntPtr.Zero);
        runtimeOffsets.StateUpdateSlotBranchesToElapsedWriter(memory, inGameEntry.X).ShouldBeTrue();

        runtimeOffsets.InGameStateAreaInstanceDataOffset.ShouldBe(Marshal.OffsetOf<InGameStateOffset>(nameof(InGameStateOffset.AreaInstanceData)).ToInt32());
        runtimeOffsets.InGameStateMsElapsedOffset.ShouldBe(Marshal.OffsetOf<InGameStateOffset>(nameof(InGameStateOffset.MsElapsed)).ToInt32());
        runtimeOffsets.InGameStateZoneSwitchCounterOffset.ShouldBe(Marshal.OffsetOf<InGameStateOffset>(nameof(InGameStateOffset.ZoneSwitchCounter)).ToInt32());

        runtimeOffsets.AreaInstanceCurrentAreaLevelOffset.ShouldBe(Marshal.OffsetOf<AreaInstanceOffsets>(nameof(AreaInstanceOffsets.CurrentAreaLevel)).ToInt32());
        runtimeOffsets.AreaInstanceCurrentAreaHashOffset.ShouldBe(Marshal.OffsetOf<AreaInstanceOffsets>(nameof(AreaInstanceOffsets.CurrentAreaHash)).ToInt32());
        runtimeOffsets.AreaInstanceLocalPlayersOffset.ShouldBe(Marshal.OffsetOf<AreaInstanceOffsets>(nameof(AreaInstanceOffsets.LocalPlayers)).ToInt32());
        runtimeOffsets.AreaInstanceEntityTreeRootOffset.ShouldBe(Marshal.OffsetOf<AreaInstanceOffsets>(nameof(AreaInstanceOffsets.EntitiesCount)).ToInt32() - IntPtr.Size);
        runtimeOffsets.AreaInstanceEntitiesCountOffset.ShouldBe(Marshal.OffsetOf<AreaInstanceOffsets>(nameof(AreaInstanceOffsets.EntitiesCount)).ToInt32());

        currentStateAddress.ShouldBe(inGameEntry.X, "The test requires the client to be loaded in-world.");
        currentStateEntry.Y.ShouldNotBe(IntPtr.Zero);
        inGameEntry.X.ShouldBe(enumInGameEntry.X, "The behavioral InGame-state lookup should still agree with the known enum order for this build.");
        areaAddress.ShouldNotBe(IntPtr.Zero);
        localPlayers.Length.ShouldBeGreaterThan(0);

        objectModelArea.Address.ShouldBe(areaAddress);
        objectModelArea.CurrentAreaLevel.ShouldBe(currentAreaLevel);
        objectModelArea.CurrentAreaHash.ShouldBe(currentAreaHash);
        objectModelArea.EntitiesCount.ShouldBe(entitiesCount);
        game.Player.Address.ShouldBe(localPlayers[0]);
    }

    /// <summary>
    /// WHAT: Verifies the production entity/component object model consumes runtime offsets recovered from static code patterns.
    /// HOW: Resolves player entity, Player component, Life component, and vitals through <see cref="RuntimeGameOffsets"/>, then compares raw reads to <see cref="TheGame"/>.
    /// </summary>
    [Test]
    [Explicit("Requires a running Path Of Exile client with the character loaded in-world.")]
    public void ShouldReadEntityComponentsThroughRuntimeGameOffsets()
    {
        // Given
        LocalProcessRuntime.IgnoreIfUnavailable();
        using var targetProcess = PathOfExileClientProcess.FindRunningOrIgnore();
        TestContext.Progress.WriteLine($"[poe-client] Target: {PathOfExileClientProcess.Describe(targetProcess)}");

        // When
        using var process = LocalProcessRuntime.OpenProcess(targetProcess.Id);
        var moduleName = Path.GetFileNameWithoutExtension(process.ProcessName) + ".exe";
        using var memory = process.MemoryOfModule(moduleName);
        var runtimeOffsets = RuntimeGameOffsets.Resolve(memory);

        var gameStateOwner = memory.Read<IntPtr>(Add(runtimeOffsets.GameStatesGlobalSlot, runtimeOffsets.GameStateStaticGameStateOffset));
        var inGameEntry = runtimeOffsets.ReadGameStateEntry(memory, gameStateOwner, GameStateTypes.InGameState);
        var areaAddress = memory.Read<IntPtr>(Add(inGameEntry.X, runtimeOffsets.InGameStateAreaInstanceDataOffset));
        var playerEntityAddress = runtimeOffsets
            .ReadStdVector<IntPtr>(
                memory,
                Add(areaAddress, runtimeOffsets.AreaInstanceLocalPlayersOffset),
                maxElements: 16)
            .FirstOrDefault();

        var entityStatus = memory.Read<byte>(Add(playerEntityAddress, runtimeOffsets.EntityStatusOffset));
        var entityActiveFlags = memory.Read<byte>(Add(playerEntityAddress, runtimeOffsets.EntityActiveFlagOffset));
        var entityId = memory.Read<uint>(Add(playerEntityAddress, runtimeOffsets.EntityIdOffset));
        var entityDetailsAddress = memory.Read<IntPtr>(Add(playerEntityAddress, runtimeOffsets.EntityDetailsPtrOffset));
        var entityPath = runtimeOffsets.ReadStdWString(memory, Add(entityDetailsAddress, runtimeOffsets.EntityDetailsNameOffset));
        var componentLookupAddress = memory.Read<IntPtr>(Add(entityDetailsAddress, runtimeOffsets.EntityDetailsComponentLookupOffset));
        var componentBucketHeader = runtimeOffsets.ReadStdVectorHeader(
            memory,
            Add(componentLookupAddress, runtimeOffsets.ComponentLookupNameAndIndexBucketOffset));
        var componentNameIndexes = runtimeOffsets.ReadComponentNameIndexEntries(
            memory,
            Add(componentLookupAddress, runtimeOffsets.ComponentLookupNameAndIndexBucketOffset),
            maxEntries: 50);
        var componentVectorHeader = runtimeOffsets.ReadStdVectorHeader(
            memory,
            Add(playerEntityAddress, runtimeOffsets.EntityComponentListOffset));
        var componentAddresses = runtimeOffsets.ReadStdVector<IntPtr>(memory, componentVectorHeader, maxElements: 50);

        var componentIndexes = componentNameIndexes
            .Where(x => !string.IsNullOrWhiteSpace(x.Name))
            .GroupBy(x => x.Name)
            .ToDictionary(x => x.Key, x => x.First().Index);

        var playerComponentName = typeof(PlayerComponent).Name;
        var lifeComponentName = typeof(Life).Name;
        var playerIndex = componentIndexes.GetValueOrDefault(playerComponentName, -1);
        var lifeIndex = componentIndexes.GetValueOrDefault(lifeComponentName, -1);
        var playerComponentAddress = playerIndex >= 0 && playerIndex < componentAddresses.Length
            ? componentAddresses[playerIndex]
            : IntPtr.Zero;
        var lifeComponentAddress = lifeIndex >= 0 && lifeIndex < componentAddresses.Length
            ? componentAddresses[lifeIndex]
            : IntPtr.Zero;

        var playerOwnerAddress = memory.Read<IntPtr>(Add(playerComponentAddress, runtimeOffsets.ComponentHeaderOwnerEntityOffset));
        var lifeOwnerAddress = memory.Read<IntPtr>(Add(lifeComponentAddress, runtimeOffsets.ComponentHeaderOwnerEntityOffset));
        var playerName = runtimeOffsets.ReadStdWString(memory, Add(playerComponentAddress, runtimeOffsets.PlayerNameOffset));
        var health = runtimeOffsets.ReadVitalStruct(memory, Add(lifeComponentAddress, runtimeOffsets.LifeHealthOffset));
        var mana = runtimeOffsets.ReadVitalStruct(memory, Add(lifeComponentAddress, runtimeOffsets.LifeManaOffset));
        var energyShield = runtimeOffsets.ReadVitalStruct(memory, Add(lifeComponentAddress, runtimeOffsets.LifeEnergyShieldOffset));

        using var game = new TheGame(Mock.Of<IFluentLog>(), memory);
        game.UpdateData();
        var hasPlayer = game.Player.TryGetComponent<PlayerComponent>(out var playerComponent);
        var hasLife = game.Player.TryGetComponent<Life>(out var lifeComponent);

        TestContext.Progress.WriteLine(
            "[poe-runtime-offsets] " +
            $"entity=0x{playerEntityAddress.ToInt64():X}, " +
            $"entityId={entityId}, " +
            $"status=0x{entityStatus:X2}, " +
            $"activeFlags=0x{entityActiveFlags:X2}, " +
            $"path='{entityPath}', " +
            $"playerComponent=0x{playerComponentAddress.ToInt64():X}, " +
            $"lifeComponent=0x{lifeComponentAddress.ToInt64():X}, " +
            $"playerName='{playerName}', " +
            $"hp={health.Current}/{health.Total}, " +
            $"mp={mana.Current}/{mana.Total}, " +
            $"es={energyShield.Current}/{energyShield.Total}, " +
            $"componentBucket={componentNameIndexes.Count}/{runtimeOffsets.CapacityStdVector<ComponentNameAndIndexStruct>(componentBucketHeader)}, " +
            $"componentVector={componentAddresses.Length}/{runtimeOffsets.CapacityStdVector<IntPtr>(componentVectorHeader)}");

        // Then
        process.ProcessId.ShouldBe(targetProcess.Id);
        gameStateOwner.ShouldNotBe(IntPtr.Zero);
        inGameEntry.X.ShouldNotBe(IntPtr.Zero);
        areaAddress.ShouldNotBe(IntPtr.Zero);
        playerEntityAddress.ShouldNotBe(IntPtr.Zero);

        runtimeOffsets.EntityDetailsPtrOffset.ShouldBe(Marshal.OffsetOf<ItemStruct>(nameof(ItemStruct.EntityDetailsPtr)).ToInt32());
        runtimeOffsets.EntityComponentListOffset.ShouldBe(Marshal.OffsetOf<ItemStruct>(nameof(ItemStruct.ComponentListPtr)).ToInt32());
        runtimeOffsets.EntityIdOffset.ShouldBe(Marshal.OffsetOf<EntityOffsets>(nameof(EntityOffsets.Id)).ToInt32());
        runtimeOffsets.EntityStatusOffset.ShouldBe(Marshal.OffsetOf<EntityOffsets>(nameof(EntityOffsets.IsValid)).ToInt32());
        runtimeOffsets.EntityActiveFlagOffset.ShouldBe(runtimeOffsets.EntityStatusOffset + 1);
        runtimeOffsets.EntityStatusInvalidMask.ShouldBe((byte)0x01);
        runtimeOffsets.EntityIdUpperBound.ShouldBe(0x40000000u);
        runtimeOffsets.EntityActiveFlagRequiredMask.ShouldBe((byte)0x04);
        runtimeOffsets.EntityDetailsNameOffset.ShouldBe(Marshal.OffsetOf<EntityDetails>(nameof(EntityDetails.name)).ToInt32());
        runtimeOffsets.EntityDetailsComponentLookupOffset.ShouldBe(Marshal.OffsetOf<EntityDetails>(nameof(EntityDetails.ComponentLookUpPtr)).ToInt32());
        runtimeOffsets.ComponentLookupNameAndIndexBucketOffset.ShouldBe(Marshal.OffsetOf<ComponentLookUpStruct>(nameof(ComponentLookUpStruct.ComponentsNameAndIndex)).ToInt32());
        runtimeOffsets.ComponentNameAndIndexNamePointerOffset.ShouldBe(Marshal.OffsetOf<ComponentNameAndIndexStruct>(nameof(ComponentNameAndIndexStruct.NamePtr)).ToInt32());
        runtimeOffsets.ComponentNameAndIndexIndexOffset.ShouldBe(Marshal.OffsetOf<ComponentNameAndIndexStruct>(nameof(ComponentNameAndIndexStruct.Index)).ToInt32());
        runtimeOffsets.ComponentNameAndIndexEntrySize.ShouldBe(Marshal.SizeOf<ComponentNameAndIndexStruct>());
        runtimeOffsets.IsValidStdVectorHeader(componentBucketHeader, runtimeOffsets.ComponentNameAndIndexEntrySize).ShouldBeTrue();
        componentBucketHeader.Count(runtimeOffsets.ComponentNameAndIndexEntrySize).ShouldBeGreaterThanOrEqualTo(componentNameIndexes.Count);
        componentBucketHeader.Capacity(runtimeOffsets.ComponentNameAndIndexEntrySize).ShouldBeGreaterThanOrEqualTo(componentBucketHeader.Count(runtimeOffsets.ComponentNameAndIndexEntrySize));
        runtimeOffsets.IsValidStdVector<IntPtr>(componentVectorHeader).ShouldBeTrue();
        runtimeOffsets.CountStdVector<IntPtr>(componentVectorHeader).ShouldBe(componentAddresses.Length);
        runtimeOffsets.CapacityStdVector<IntPtr>(componentVectorHeader).ShouldBeGreaterThanOrEqualTo(componentAddresses.Length);
        runtimeOffsets.ComponentHeaderOwnerEntityOffset.ShouldBe(Marshal.OffsetOf<ComponentHeader>(nameof(ComponentHeader.EntityPtr)).ToInt32());
        runtimeOffsets.PlayerNameOffset.ShouldBe(Marshal.OffsetOf<PlayerOffsets>(nameof(PlayerOffsets.Name)).ToInt32());
        runtimeOffsets.StdWStringBufferOffset.ShouldBe(Marshal.OffsetOf<StdWString>(nameof(StdWString.Buffer)).ToInt32());
        runtimeOffsets.StdWStringInlineBufferOffset.ShouldBe(Marshal.OffsetOf<StdWString>(nameof(StdWString.Buffer)).ToInt32());
        runtimeOffsets.StdWStringLengthOffset.ShouldBe(Marshal.OffsetOf<StdWString>(nameof(StdWString.Length)).ToInt32());
        runtimeOffsets.StdWStringCapacityOffset.ShouldBe(Marshal.OffsetOf<StdWString>(nameof(StdWString.Capacity)).ToInt32());
        runtimeOffsets.StdWStringSmallCapacityLimit.ShouldBe(7);
        runtimeOffsets.LifeHealthOffset.ShouldBe(Marshal.OffsetOf<LifeOffset>(nameof(LifeOffset.Health)).ToInt32());
        runtimeOffsets.LifeManaOffset.ShouldBe(Marshal.OffsetOf<LifeOffset>(nameof(LifeOffset.Mana)).ToInt32());
        runtimeOffsets.LifeEnergyShieldOffset.ShouldBe(Marshal.OffsetOf<LifeOffset>(nameof(LifeOffset.EnergyShield)).ToInt32());
        runtimeOffsets.VitalUnknownStatId0Offset.ShouldBe(Marshal.OffsetOf<VitalStruct>(nameof(VitalStruct.UnknownStatId0)).ToInt32());
        runtimeOffsets.VitalUnknownStatId1Offset.ShouldBe(Marshal.OffsetOf<VitalStruct>(nameof(VitalStruct.UnknownStatId1)).ToInt32());
        runtimeOffsets.VitalLifeComponentPtrOffset.ShouldBe(Marshal.OffsetOf<VitalStruct>(nameof(VitalStruct.LifeComponentPtr)).ToInt32());
        runtimeOffsets.VitalReservedFlatOffset.ShouldBe(Marshal.OffsetOf<VitalStruct>(nameof(VitalStruct.ReservedFlat)).ToInt32());
        runtimeOffsets.VitalReservedPercentOffset.ShouldBe(Marshal.OffsetOf<VitalStruct>(nameof(VitalStruct.ReservedPercent)).ToInt32());
        runtimeOffsets.VitalTotalStatIdOffset.ShouldBe(Marshal.OffsetOf<VitalStruct>(nameof(VitalStruct.TotalStatId)).ToInt32());
        runtimeOffsets.VitalUnknownStatId2Offset.ShouldBe(Marshal.OffsetOf<VitalStruct>(nameof(VitalStruct.UnknownStatId2)).ToInt32());
        runtimeOffsets.VitalUnknownStatId3Offset.ShouldBe(Marshal.OffsetOf<VitalStruct>(nameof(VitalStruct.UnknownStatId3)).ToInt32());
        runtimeOffsets.VitalTotalOffset.ShouldBe(Marshal.OffsetOf<VitalStruct>(nameof(VitalStruct.Total)).ToInt32());
        runtimeOffsets.VitalCurrentOffset.ShouldBe(Marshal.OffsetOf<VitalStruct>(nameof(VitalStruct.Current)).ToInt32());

        runtimeOffsets.PassesEntityIdentityFilter(entityStatus, entityId, entityActiveFlags).ShouldBeTrue();
        entityId.ShouldBe(game.Player.Id);
        game.Player.IsValid.ShouldBeTrue();
        entityPath.ShouldBe(game.Player.Path);
        entityPath.ShouldStartWith("Metadata/");

        componentIndexes.ContainsKey(playerComponentName).ShouldBeTrue();
        componentIndexes.ContainsKey(lifeComponentName).ShouldBeTrue();
        playerComponentAddress.ShouldNotBe(IntPtr.Zero);
        lifeComponentAddress.ShouldNotBe(IntPtr.Zero);
        playerOwnerAddress.ShouldBe(playerEntityAddress);
        lifeOwnerAddress.ShouldBe(playerEntityAddress);

        hasPlayer.ShouldBeTrue();
        hasLife.ShouldBeTrue();
        playerComponent!.Address.ShouldBe(playerComponentAddress);
        lifeComponent!.Address.ShouldBe(lifeComponentAddress);
        playerComponent.Name.ShouldBe(playerName);

        health.Current.ShouldBe(lifeComponent.Health.Current);
        health.Total.ShouldBe(lifeComponent.Health.Total);
        health.ReservedFlat.ShouldBe(lifeComponent.Health.ReservedFlat);
        health.ReservedPercent.ShouldBe(lifeComponent.Health.ReservedPercent);
        mana.Current.ShouldBe(lifeComponent.Mana.Current);
        mana.Total.ShouldBe(lifeComponent.Mana.Total);
        mana.ReservedFlat.ShouldBe(lifeComponent.Mana.ReservedFlat);
        mana.ReservedPercent.ShouldBe(lifeComponent.Mana.ReservedPercent);
        energyShield.Current.ShouldBe(lifeComponent.EnergyShield.Current);
        energyShield.Total.ShouldBe(lifeComponent.EnergyShield.Total);
        energyShield.ReservedFlat.ShouldBe(lifeComponent.EnergyShield.ReservedFlat);
        energyShield.ReservedPercent.ShouldBe(lifeComponent.EnergyShield.ReservedPercent);

        health.Total.ShouldBeGreaterThan(0);
        mana.Total.ShouldBeGreaterThan(0);
        energyShield.Total.ShouldBeGreaterThanOrEqualTo(0);
        health.Current.ShouldBeInRange(0, health.Total);
        mana.Current.ShouldBeInRange(0, mana.Total);
        energyShield.Current.ShouldBeInRange(0, energyShield.Total);
    }

    /// <summary>
    /// WHAT: Verifies the AreaInstance current area level offset is recovered from code that reads the current area pointer.
    /// HOW: Resolves a unique block that reads InGameState.AreaInstanceData, then reads AreaInstance + recovered level offset.
    /// </summary>
    [Test]
    [Explicit("Requires a running Path Of Exile client with the character loaded in-world.")]
    public void ShouldResolveAreaInstanceCurrentAreaLevelFromCodePattern()
    {
        // Given
        LocalProcessRuntime.IgnoreIfUnavailable();
        using var targetProcess = PathOfExileClientProcess.FindRunningOrIgnore();
        TestContext.Progress.WriteLine($"[poe-client] Target: {PathOfExileClientProcess.Describe(targetProcess)}");

        // When
        using var process = LocalProcessRuntime.OpenProcess(targetProcess.Id);
        var moduleName = Path.GetFileNameWithoutExtension(process.ProcessName) + ".exe";
        using var memory = process.MemoryOfModule(moduleName);

        var staticPatternOffsets = MemoryUtils
            .GetOffsets(memory, Offsets.Patterns)
            .ToDictionary(x => x.Key.Name!, x => x.Value);
        var keypointOffsets = MemoryUtils
            .GetOffsets(memory, Offsets.KeypointPatterns)
            .ToDictionary(x => x.Key.Name!, x => x.Value);

        var gameStatesGlobalSlot = ResolveRipRelativeSlot(memory, staticPatternOffsets[nameof(GameStates)]);
        var staticObject = memory.Read<GameStateStaticOffset>(gameStatesGlobalSlot);

        var tableShapeAddress = Add(memory.BaseAddress, keypointOffsets[Offsets.KeypointNames.GameStateTableShape]);
        var stateTableOffset = memory.Read<byte>(tableShapeAddress);
        var stateEntrySize = memory.Read<int>(Add(tableShapeAddress, 21));
        var inGameEntryOffset = stateTableOffset + (int)GameStateTypes.InGameState * stateEntrySize;
        var inGameEntry = memory.Read<StdTuple2D<IntPtr>>(Add(staticObject.GameState, inGameEntryOffset));

        var areaOffsetAddress = Add(memory.BaseAddress, keypointOffsets[Offsets.KeypointNames.InGameStateAreaInstanceData]);
        var areaInstanceOffset = memory.Read<int>(areaOffsetAddress);
        var areaInstanceAddress = memory.Read<IntPtr>(Add(inGameEntry.X, areaInstanceOffset));

        var areaLevelOffsetAddress = Add(memory.BaseAddress, keypointOffsets[Offsets.KeypointNames.AreaInstanceCurrentAreaLevel]);
        var areaInstanceOffsetFromLevelBlock = memory.Read<int>(Add(areaLevelOffsetAddress, -7));
        var currentAreaLevelOffset = memory.Read<int>(areaLevelOffsetAddress);
        var areaInstanceAddressFromLevelBlock = memory.Read<IntPtr>(Add(inGameEntry.X, areaInstanceOffsetFromLevelBlock));
        var currentAreaLevel = memory.Read<byte>(Add(areaInstanceAddressFromLevelBlock, currentAreaLevelOffset));
        var areaByStruct = memory.Read<AreaInstanceOffsets>(areaInstanceAddress);

        using var game = new TheGame(Mock.Of<IFluentLog>(), memory);
        game.UpdateData();
        var objectModelArea = game.States.InGameStateObject.CurrentAreaInstance;

        TestContext.Progress.WriteLine(
            "[poe-keypoint] " +
            $"inGameState=0x{inGameEntry.X.ToInt64():X}, " +
            $"areaInstanceOffset=0x{areaInstanceOffset:X}, " +
            $"areaInstanceOffsetFromLevelBlock=0x{areaInstanceOffsetFromLevelBlock:X}, " +
            $"area=0x{areaInstanceAddress.ToInt64():X}, " +
            $"areaFromLevelBlock=0x{areaInstanceAddressFromLevelBlock.ToInt64():X}, " +
            $"currentAreaLevelOffset=0x{currentAreaLevelOffset:X}, " +
            $"level={currentAreaLevel}");

        // Then
        process.ProcessId.ShouldBe(targetProcess.Id);
        staticObject.GameState.ShouldNotBe(IntPtr.Zero);
        inGameEntry.X.ShouldNotBe(IntPtr.Zero);
        areaInstanceAddress.ShouldNotBe(IntPtr.Zero);

        areaInstanceOffsetFromLevelBlock.ShouldBe(areaInstanceOffset);
        areaInstanceOffsetFromLevelBlock.ShouldBe(Marshal.OffsetOf<InGameStateOffset>(nameof(InGameStateOffset.AreaInstanceData)).ToInt32());
        areaInstanceAddressFromLevelBlock.ShouldBe(areaInstanceAddress);

        currentAreaLevelOffset.ShouldBe(Marshal.OffsetOf<AreaInstanceOffsets>(nameof(AreaInstanceOffsets.CurrentAreaLevel)).ToInt32());
        currentAreaLevel.ShouldBe(areaByStruct.CurrentAreaLevel);
        currentAreaLevel.ShouldBe(objectModelArea.CurrentAreaLevel);
        currentAreaLevel.ShouldBeGreaterThan((byte)0);
        objectModelArea.Address.ShouldBe(areaInstanceAddress);
    }

    /// <summary>
    /// WHAT: Verifies the AreaInstance current area hash offset is recovered from a current-area holder read.
    /// HOW: Resolves the FUN_142061AD0 code pattern, reads AreaInstance + recovered hash offset, and compares it to the object model.
    /// </summary>
    [Test]
    [Explicit("Requires a running Path Of Exile client with the character loaded in-world.")]
    public void ShouldResolveAreaInstanceCurrentAreaHashFromCodePattern()
    {
        // Given
        LocalProcessRuntime.IgnoreIfUnavailable();
        using var targetProcess = PathOfExileClientProcess.FindRunningOrIgnore();
        TestContext.Progress.WriteLine($"[poe-client] Target: {PathOfExileClientProcess.Describe(targetProcess)}");

        // When
        using var process = LocalProcessRuntime.OpenProcess(targetProcess.Id);
        var moduleName = Path.GetFileNameWithoutExtension(process.ProcessName) + ".exe";
        using var memory = process.MemoryOfModule(moduleName);

        var staticPatternOffsets = MemoryUtils
            .GetOffsets(memory, Offsets.Patterns)
            .ToDictionary(x => x.Key.Name!, x => x.Value);
        var keypointOffsets = MemoryUtils
            .GetOffsets(memory, Offsets.KeypointPatterns)
            .ToDictionary(x => x.Key.Name!, x => x.Value);

        var gameStatesGlobalSlot = ResolveRipRelativeSlot(memory, staticPatternOffsets[nameof(GameStates)]);
        var staticObject = memory.Read<GameStateStaticOffset>(gameStatesGlobalSlot);

        var tableShapeAddress = Add(memory.BaseAddress, keypointOffsets[Offsets.KeypointNames.GameStateTableShape]);
        var stateTableOffset = memory.Read<byte>(tableShapeAddress);
        var stateEntrySize = memory.Read<int>(Add(tableShapeAddress, 21));
        var inGameEntryOffset = stateTableOffset + (int)GameStateTypes.InGameState * stateEntrySize;
        var inGameEntry = memory.Read<StdTuple2D<IntPtr>>(Add(staticObject.GameState, inGameEntryOffset));

        var areaOffsetAddress = Add(memory.BaseAddress, keypointOffsets[Offsets.KeypointNames.InGameStateAreaInstanceData]);
        var areaInstanceOffset = memory.Read<int>(areaOffsetAddress);
        var areaInstanceAddress = memory.Read<IntPtr>(Add(inGameEntry.X, areaInstanceOffset));

        var areaHashOffsetAddress = Add(memory.BaseAddress, keypointOffsets[Offsets.KeypointNames.AreaInstanceCurrentAreaHash]);
        var holderAreaInstanceOffset = memory.Read<int>(Add(areaHashOffsetAddress, -6));
        var currentAreaHashOffset = memory.Read<int>(areaHashOffsetAddress);
        var siblingHashRelatedOffset = memory.Read<int>(Add(areaHashOffsetAddress, 0x0A));
        var currentAreaHash = memory.Read<uint>(Add(areaInstanceAddress, currentAreaHashOffset));
        var areaByStruct = memory.Read<AreaInstanceOffsets>(areaInstanceAddress);

        using var game = new TheGame(Mock.Of<IFluentLog>(), memory);
        game.UpdateData();
        var objectModelArea = game.States.InGameStateObject.CurrentAreaInstance;

        TestContext.Progress.WriteLine(
            "[poe-keypoint] " +
            $"inGameState=0x{inGameEntry.X.ToInt64():X}, " +
            $"area=0x{areaInstanceAddress.ToInt64():X}, " +
            $"holderAreaInstanceOffset=0x{holderAreaInstanceOffset:X}, " +
            $"currentAreaHashOffset=0x{currentAreaHashOffset:X}, " +
            $"siblingHashRelatedOffset=0x{siblingHashRelatedOffset:X}, " +
            $"hash=0x{currentAreaHash:X8}");

        // Then
        process.ProcessId.ShouldBe(targetProcess.Id);
        staticObject.GameState.ShouldNotBe(IntPtr.Zero);
        inGameEntry.X.ShouldNotBe(IntPtr.Zero);
        areaInstanceAddress.ShouldNotBe(IntPtr.Zero);

        holderAreaInstanceOffset.ShouldBe(0x2170);
        currentAreaHashOffset.ShouldBe(Marshal.OffsetOf<AreaInstanceOffsets>(nameof(AreaInstanceOffsets.CurrentAreaHash)).ToInt32());
        currentAreaHash.ShouldBe(areaByStruct.CurrentAreaHash);
        currentAreaHash.ShouldBe(objectModelArea.CurrentAreaHash);
        currentAreaHash.ShouldBeGreaterThan(0u);
        objectModelArea.Address.ShouldBe(areaInstanceAddress);
    }

    /// <summary>
    /// WHAT: Verifies the AreaInstance local-player vector offset is recovered from the AreaInstance vtable accessor.
    /// HOW: Resolves the FUN_1420731D0 code pattern, reads AreaInstance + recovered offset, and compares it to the object model player.
    /// </summary>
    [Test]
    [Explicit("Requires a running Path Of Exile client with the character loaded in-world.")]
    public void ShouldResolveAreaInstanceLocalPlayersFromCodePattern()
    {
        // Given
        LocalProcessRuntime.IgnoreIfUnavailable();
        using var targetProcess = PathOfExileClientProcess.FindRunningOrIgnore();
        TestContext.Progress.WriteLine($"[poe-client] Target: {PathOfExileClientProcess.Describe(targetProcess)}");

        // When
        using var process = LocalProcessRuntime.OpenProcess(targetProcess.Id);
        var moduleName = Path.GetFileNameWithoutExtension(process.ProcessName) + ".exe";
        using var memory = process.MemoryOfModule(moduleName);

        var staticPatternOffsets = MemoryUtils
            .GetOffsets(memory, Offsets.Patterns)
            .ToDictionary(x => x.Key.Name!, x => x.Value);
        var keypointOffsets = MemoryUtils
            .GetOffsets(
                memory,
                SelectKeypointPatterns(
                    Offsets.KeypointNames.GameStateTableShape,
                    Offsets.KeypointNames.InGameStateAreaInstanceData,
                    Offsets.KeypointNames.AreaInstanceLocalPlayers))
            .ToDictionary(x => x.Key.Name!, x => x.Value);

        var gameStatesGlobalSlot = ResolveRipRelativeSlot(memory, staticPatternOffsets[nameof(GameStates)]);
        var staticObject = memory.Read<GameStateStaticOffset>(gameStatesGlobalSlot);

        var tableShapeAddress = Add(memory.BaseAddress, keypointOffsets[Offsets.KeypointNames.GameStateTableShape]);
        var stateTableOffset = memory.Read<byte>(tableShapeAddress);
        var stateEntrySize = memory.Read<int>(Add(tableShapeAddress, 21));
        var inGameEntryOffset = stateTableOffset + (int)GameStateTypes.InGameState * stateEntrySize;
        var inGameEntry = memory.Read<StdTuple2D<IntPtr>>(Add(staticObject.GameState, inGameEntryOffset));

        var areaOffsetAddress = Add(memory.BaseAddress, keypointOffsets[Offsets.KeypointNames.InGameStateAreaInstanceData]);
        var areaInstanceOffset = memory.Read<int>(areaOffsetAddress);
        var areaInstanceAddress = memory.Read<IntPtr>(Add(inGameEntry.X, areaInstanceOffset));

        var localPlayersOffsetAddress = Add(memory.BaseAddress, keypointOffsets[Offsets.KeypointNames.AreaInstanceLocalPlayers]);
        var localPlayersOffset = memory.Read<int>(localPlayersOffsetAddress);
        var localPlayersVector = memory.Read<StdVector>(Add(areaInstanceAddress, localPlayersOffset));
        var localPlayers = ReadStdVector<IntPtr>(memory, localPlayersVector);

        var areaByStruct = memory.Read<AreaInstanceOffsets>(areaInstanceAddress);
        var structLocalPlayers = ReadStdVector<IntPtr>(memory, areaByStruct.LocalPlayers);

        using var game = new TheGame(Mock.Of<IFluentLog>(), memory);
        game.UpdateData();
        var objectModelPlayer = game.Player.Address;

        TestContext.Progress.WriteLine(
            "[poe-keypoint] " +
            $"inGameState=0x{inGameEntry.X.ToInt64():X}, " +
            $"area=0x{areaInstanceAddress.ToInt64():X}, " +
            $"localPlayersOffset=0x{localPlayersOffset:X}, " +
            $"first=0x{localPlayersVector.First.ToInt64():X}, " +
            $"last=0x{localPlayersVector.Last.ToInt64():X}, " +
            $"end=0x{localPlayersVector.End.ToInt64():X}, " +
            $"count={localPlayers.Length}, " +
            $"player=0x{objectModelPlayer.ToInt64():X}");

        // Then
        process.ProcessId.ShouldBe(targetProcess.Id);
        staticObject.GameState.ShouldNotBe(IntPtr.Zero);
        inGameEntry.X.ShouldNotBe(IntPtr.Zero);
        areaInstanceAddress.ShouldNotBe(IntPtr.Zero);

        localPlayersOffset.ShouldBe(Marshal.OffsetOf<AreaInstanceOffsets>(nameof(AreaInstanceOffsets.LocalPlayers)).ToInt32());
        localPlayersVector.First.ShouldBe(areaByStruct.LocalPlayers.First);
        localPlayersVector.Last.ShouldBe(areaByStruct.LocalPlayers.Last);
        localPlayersVector.End.ShouldBe(areaByStruct.LocalPlayers.End);
        localPlayersVector.First.ShouldNotBe(IntPtr.Zero);
        localPlayersVector.Last.ToInt64().ShouldBeGreaterThanOrEqualTo(localPlayersVector.First.ToInt64());
        ((localPlayersVector.Last.ToInt64() - localPlayersVector.First.ToInt64()) % IntPtr.Size).ShouldBe(0);

        localPlayers.Length.ShouldBeGreaterThan(0);
        structLocalPlayers.Length.ShouldBe(localPlayers.Length);
        localPlayers[0].ShouldBe(structLocalPlayers[0]);
        localPlayers[0].ShouldBe(objectModelPlayer);
    }

    /// <summary>
    /// WHAT: Verifies the AreaInstance entity-count offset is derived from the AreaInstance-owned entity tree constructor.
    /// HOW: Resolves the FUN_14206E780 tree-root pattern, derives count as root + pointer-size, and compares raw count to the object model.
    /// </summary>
    [Test]
    [Explicit("Requires a running Path Of Exile client with the character loaded in-world.")]
    public void ShouldResolveAreaInstanceEntitiesCountFromCodePattern()
    {
        // Given
        LocalProcessRuntime.IgnoreIfUnavailable();
        using var targetProcess = PathOfExileClientProcess.FindRunningOrIgnore();
        TestContext.Progress.WriteLine($"[poe-client] Target: {PathOfExileClientProcess.Describe(targetProcess)}");

        // When
        using var process = LocalProcessRuntime.OpenProcess(targetProcess.Id);
        var moduleName = Path.GetFileNameWithoutExtension(process.ProcessName) + ".exe";
        using var memory = process.MemoryOfModule(moduleName);

        var staticPatternOffsets = MemoryUtils
            .GetOffsets(memory, Offsets.Patterns)
            .ToDictionary(x => x.Key.Name!, x => x.Value);
        var keypointOffsets = MemoryUtils
            .GetOffsets(
                memory,
                SelectKeypointPatterns(
                    Offsets.KeypointNames.GameStateTableShape,
                    Offsets.KeypointNames.InGameStateAreaInstanceData,
                    Offsets.KeypointNames.AreaInstanceEntityTreeRoot))
            .ToDictionary(x => x.Key.Name!, x => x.Value);

        var gameStatesGlobalSlot = ResolveRipRelativeSlot(memory, staticPatternOffsets[nameof(GameStates)]);
        var staticObject = memory.Read<GameStateStaticOffset>(gameStatesGlobalSlot);

        var tableShapeAddress = Add(memory.BaseAddress, keypointOffsets[Offsets.KeypointNames.GameStateTableShape]);
        var stateTableOffset = memory.Read<byte>(tableShapeAddress);
        var stateEntrySize = memory.Read<int>(Add(tableShapeAddress, 21));
        var inGameEntryOffset = stateTableOffset + (int)GameStateTypes.InGameState * stateEntrySize;
        var inGameEntry = memory.Read<StdTuple2D<IntPtr>>(Add(staticObject.GameState, inGameEntryOffset));

        var areaOffsetAddress = Add(memory.BaseAddress, keypointOffsets[Offsets.KeypointNames.InGameStateAreaInstanceData]);
        var areaInstanceOffset = memory.Read<int>(areaOffsetAddress);
        var areaInstanceAddress = memory.Read<IntPtr>(Add(inGameEntry.X, areaInstanceOffset));

        var entityTreeRootOffsetAddress = Add(memory.BaseAddress, keypointOffsets[Offsets.KeypointNames.AreaInstanceEntityTreeRoot]);
        var entityTreeRootOffset = memory.Read<int>(entityTreeRootOffsetAddress);
        var entitiesCountOffset = entityTreeRootOffset + IntPtr.Size;
        var entityTreeRoot = memory.Read<IntPtr>(Add(areaInstanceAddress, entityTreeRootOffset));
        var entitiesCount = memory.Read<uint>(Add(areaInstanceAddress, entitiesCountOffset));
        var entitiesCount64 = memory.Read<ulong>(Add(areaInstanceAddress, entitiesCountOffset));
        var areaByStruct = memory.Read<AreaInstanceOffsets>(areaInstanceAddress);

        using var game = new TheGame(Mock.Of<IFluentLog>(), memory);
        game.UpdateData();
        var objectModelArea = game.States.InGameStateObject.CurrentAreaInstance;

        TestContext.Progress.WriteLine(
            "[poe-keypoint] " +
            $"inGameState=0x{inGameEntry.X.ToInt64():X}, " +
            $"area=0x{areaInstanceAddress.ToInt64():X}, " +
            $"entityTreeRootOffset=0x{entityTreeRootOffset:X}, " +
            $"entityTreeRoot=0x{entityTreeRoot.ToInt64():X}, " +
            $"entitiesCountOffset=0x{entitiesCountOffset:X}, " +
            $"entitiesCount={entitiesCount}");

        // Then
        process.ProcessId.ShouldBe(targetProcess.Id);
        IntPtr.Size.ShouldBe(8);
        staticObject.GameState.ShouldNotBe(IntPtr.Zero);
        inGameEntry.X.ShouldNotBe(IntPtr.Zero);
        areaInstanceAddress.ShouldNotBe(IntPtr.Zero);
        entityTreeRoot.ShouldNotBe(IntPtr.Zero);

        entityTreeRootOffset.ShouldBe(Marshal.OffsetOf<AreaInstanceOffsets>(nameof(AreaInstanceOffsets.EntitiesCount)).ToInt32() - IntPtr.Size);
        entitiesCountOffset.ShouldBe(Marshal.OffsetOf<AreaInstanceOffsets>(nameof(AreaInstanceOffsets.EntitiesCount)).ToInt32());
        entitiesCount64.ShouldBe((ulong)entitiesCount);
        entitiesCount.ShouldBe(areaByStruct.EntitiesCount);
        entitiesCount.ShouldBe(objectModelArea.EntitiesCount);
        entitiesCount.ShouldBeGreaterThan(0u);
        entitiesCount.ShouldBeLessThan(100_000u);
        objectModelArea.Address.ShouldBe(areaInstanceAddress);
    }

    /// <summary>
    /// WHAT: Verifies entity id/status offsets are recovered from the AreaInstance entity-tree filter.
    /// HOW: Resolves the destructor filter that walks the AreaInstance entity tree, then reads the live player entity through recovered offsets.
    /// </summary>
    [Test]
    [Explicit("Requires a running Path Of Exile client with the character loaded in-world.")]
    public void ShouldResolveEntityIdentityFromAreaInstanceTreeFilter()
    {
        // Given
        LocalProcessRuntime.IgnoreIfUnavailable();
        using var targetProcess = PathOfExileClientProcess.FindRunningOrIgnore();
        TestContext.Progress.WriteLine($"[poe-client] Target: {PathOfExileClientProcess.Describe(targetProcess)}");

        // When
        using var process = LocalProcessRuntime.OpenProcess(targetProcess.Id);
        var moduleName = Path.GetFileNameWithoutExtension(process.ProcessName) + ".exe";
        using var memory = process.MemoryOfModule(moduleName);

        var staticPatternOffsets = MemoryUtils
            .GetOffsets(memory, Offsets.Patterns)
            .ToDictionary(x => x.Key.Name!, x => x.Value);
        var keypointOffsets = MemoryUtils
            .GetOffsets(
                memory,
                SelectKeypointPatterns(
                    Offsets.KeypointNames.GameStateTableShape,
                    Offsets.KeypointNames.InGameStateAreaInstanceData,
                    Offsets.KeypointNames.AreaInstanceLocalPlayers,
                    Offsets.KeypointNames.AreaInstanceEntityTreeRoot,
                    Offsets.KeypointNames.EntityIdentityFilter))
            .ToDictionary(x => x.Key.Name!, x => x.Value);

        var gameStatesGlobalSlot = ResolveRipRelativeSlot(memory, staticPatternOffsets[nameof(GameStates)]);
        var staticObject = memory.Read<GameStateStaticOffset>(gameStatesGlobalSlot);

        var tableShapeAddress = Add(memory.BaseAddress, keypointOffsets[Offsets.KeypointNames.GameStateTableShape]);
        var stateTableOffset = memory.Read<byte>(tableShapeAddress);
        var stateEntrySize = memory.Read<int>(Add(tableShapeAddress, 21));
        var inGameEntryOffset = stateTableOffset + (int)GameStateTypes.InGameState * stateEntrySize;
        var inGameEntry = memory.Read<StdTuple2D<IntPtr>>(Add(staticObject.GameState, inGameEntryOffset));

        var areaOffsetAddress = Add(memory.BaseAddress, keypointOffsets[Offsets.KeypointNames.InGameStateAreaInstanceData]);
        var areaInstanceOffset = memory.Read<int>(areaOffsetAddress);
        var areaInstanceAddress = memory.Read<IntPtr>(Add(inGameEntry.X, areaInstanceOffset));

        var localPlayersOffsetAddress = Add(memory.BaseAddress, keypointOffsets[Offsets.KeypointNames.AreaInstanceLocalPlayers]);
        var localPlayersOffset = memory.Read<int>(localPlayersOffsetAddress);
        var localPlayersVector = memory.Read<StdVector>(Add(areaInstanceAddress, localPlayersOffset));
        var localPlayers = ReadStdVector<IntPtr>(memory, localPlayersVector);
        var playerEntityAddress = localPlayers.FirstOrDefault();

        var entityTreeRootAddress = Add(memory.BaseAddress, keypointOffsets[Offsets.KeypointNames.AreaInstanceEntityTreeRoot]);
        var entityTreeRootOffset = memory.Read<int>(entityTreeRootAddress);

        var identityFilterAddress = Add(memory.BaseAddress, keypointOffsets[Offsets.KeypointNames.EntityIdentityFilter]);
        var entityTreeRootOffsetFromFilter = memory.Read<int>(Add(identityFilterAddress, -0x18));
        var treeNodeEntityPointerOffset = memory.Read<byte>(Add(identityFilterAddress, -0x03));
        var statusOffset = memory.Read<int>(identityFilterAddress);
        var statusInvalidMask = memory.Read<byte>(Add(identityFilterAddress, 0x04));
        var idOffset = memory.Read<int>(Add(identityFilterAddress, 0x09));
        var idUpperBound = memory.Read<uint>(Add(identityFilterAddress, 0x0D));
        var activeFlagOffset = memory.Read<int>(Add(identityFilterAddress, 0x15));
        var activeFlagRequiredMask = memory.Read<byte>(Add(identityFilterAddress, 0x19));

        var statusByte = memory.Read<byte>(Add(playerEntityAddress, statusOffset));
        var activeFlagByte = memory.Read<byte>(Add(playerEntityAddress, activeFlagOffset));
        var entityId = memory.Read<uint>(Add(playerEntityAddress, idOffset));
        var entityData = memory.Read<EntityOffsets>(playerEntityAddress);

        using var game = new TheGame(Mock.Of<IFluentLog>(), memory);
        game.UpdateData();

        TestContext.Progress.WriteLine(
            "[poe-keypoint] " +
            $"entity=0x{playerEntityAddress.ToInt64():X}, " +
            $"treeRootOffset=0x{entityTreeRootOffsetFromFilter:X}, " +
            $"nodeEntityPtrOffset=0x{treeNodeEntityPointerOffset:X}, " +
            $"idOffset=0x{idOffset:X}, " +
            $"statusOffset=0x{statusOffset:X}, " +
            $"statusMask=0x{statusInvalidMask:X2}, " +
            $"idUpperBound=0x{idUpperBound:X8}, " +
            $"activeFlagOffset=0x{activeFlagOffset:X}, " +
            $"activeMask=0x{activeFlagRequiredMask:X2}, " +
            $"id={entityId}, " +
            $"status=0x{statusByte:X2}, " +
            $"activeFlags=0x{activeFlagByte:X2}, " +
            $"objectModelId={game.Player.Id}, " +
            $"objectModelIsValid={game.Player.IsValid}");

        // Then
        process.ProcessId.ShouldBe(targetProcess.Id);
        staticObject.GameState.ShouldNotBe(IntPtr.Zero);
        inGameEntry.X.ShouldNotBe(IntPtr.Zero);
        areaInstanceAddress.ShouldNotBe(IntPtr.Zero);
        playerEntityAddress.ShouldNotBe(IntPtr.Zero);
        playerEntityAddress.ShouldBe(game.Player.Address);

        entityTreeRootOffsetFromFilter.ShouldBe(entityTreeRootOffset);
        entityTreeRootOffsetFromFilter.ShouldBe(Marshal.OffsetOf<AreaInstanceOffsets>(nameof(AreaInstanceOffsets.EntitiesCount)).ToInt32() - IntPtr.Size);
        treeNodeEntityPointerOffset.ShouldBe((byte)0x28);
        idOffset.ShouldBe(Marshal.OffsetOf<EntityOffsets>(nameof(EntityOffsets.Id)).ToInt32());
        statusOffset.ShouldBe(Marshal.OffsetOf<EntityOffsets>(nameof(EntityOffsets.IsValid)).ToInt32());
        activeFlagOffset.ShouldBe(statusOffset + 1);
        statusInvalidMask.ShouldBe((byte)0x01);
        idUpperBound.ShouldBe(0x40000000u);
        activeFlagRequiredMask.ShouldBe((byte)0x04);

        entityId.ShouldBe(entityData.Id);
        entityId.ShouldBe(game.Player.Id);
        entityId.ShouldBeGreaterThan(0u);
        entityId.ShouldBeLessThan(idUpperBound);

        statusByte.ShouldBe(entityData.IsValid);
        (statusByte & statusInvalidMask).ShouldBe(0);
        (activeFlagByte & activeFlagRequiredMask).ShouldBe(activeFlagRequiredMask);
        game.Player.IsValid.ShouldBeTrue();
    }

    /// <summary>
    /// WHAT: Verifies EntityDetails.name/path is recovered from the live Entity vtable accessor.
    /// HOW: Resolves the tiny accessor returning EntityDetails + recovered offset, then compares the raw StdWString with the object model.
    /// </summary>
    [Test]
    [Explicit("Requires a running Path Of Exile client with the character loaded in-world.")]
    public void ShouldResolveEntityDetailsNameFromVtableAccessor()
    {
        // Given
        LocalProcessRuntime.IgnoreIfUnavailable();
        using var targetProcess = PathOfExileClientProcess.FindRunningOrIgnore();
        TestContext.Progress.WriteLine($"[poe-client] Target: {PathOfExileClientProcess.Describe(targetProcess)}");

        // When
        using var process = LocalProcessRuntime.OpenProcess(targetProcess.Id);
        var moduleName = Path.GetFileNameWithoutExtension(process.ProcessName) + ".exe";
        using var memory = process.MemoryOfModule(moduleName);

        var staticPatternOffsets = MemoryUtils
            .GetOffsets(memory, Offsets.Patterns)
            .ToDictionary(x => x.Key.Name!, x => x.Value);
        var keypointOffsets = MemoryUtils
            .GetOffsets(memory, Offsets.KeypointPatterns)
            .ToDictionary(x => x.Key.Name!, x => x.Value);

        var gameStatesGlobalSlot = ResolveRipRelativeSlot(memory, staticPatternOffsets[nameof(GameStates)]);
        var staticObject = memory.Read<GameStateStaticOffset>(gameStatesGlobalSlot);

        var tableShapeAddress = Add(memory.BaseAddress, keypointOffsets[Offsets.KeypointNames.GameStateTableShape]);
        var stateTableOffset = memory.Read<byte>(tableShapeAddress);
        var stateEntrySize = memory.Read<int>(Add(tableShapeAddress, 21));
        var inGameEntryOffset = stateTableOffset + (int)GameStateTypes.InGameState * stateEntrySize;
        var inGameEntry = memory.Read<StdTuple2D<IntPtr>>(Add(staticObject.GameState, inGameEntryOffset));

        var areaOffsetAddress = Add(memory.BaseAddress, keypointOffsets[Offsets.KeypointNames.InGameStateAreaInstanceData]);
        var areaInstanceOffset = memory.Read<int>(areaOffsetAddress);
        var areaInstanceAddress = memory.Read<IntPtr>(Add(inGameEntry.X, areaInstanceOffset));

        var localPlayersOffsetAddress = Add(memory.BaseAddress, keypointOffsets[Offsets.KeypointNames.AreaInstanceLocalPlayers]);
        var localPlayersOffset = memory.Read<int>(localPlayersOffsetAddress);
        var localPlayersVector = memory.Read<StdVector>(Add(areaInstanceAddress, localPlayersOffset));
        var localPlayers = ReadStdVector<IntPtr>(memory, localPlayersVector);
        var playerEntityAddress = localPlayers.FirstOrDefault();

        var nameAccessorAddress = Add(memory.BaseAddress, keypointOffsets[Offsets.KeypointNames.EntityDetailsName]);
        var entityDetailsNameOffset = memory.Read<byte>(nameAccessorAddress);
        var entityDetailsPtrOffset = memory.Read<byte>(Add(nameAccessorAddress, -0x04));

        var entityDetailsAddress = memory.Read<IntPtr>(Add(playerEntityAddress, entityDetailsPtrOffset));
        var recoveredNameStruct = memory.Read<StdWString>(Add(entityDetailsAddress, entityDetailsNameOffset));
        var recoveredPath = ReadStdWString(memory, recoveredNameStruct);
        var runtimeOffsets = RuntimeGameOffsets.Resolve(memory);
        var providerPath = runtimeOffsets.ReadStdWString(memory, Add(entityDetailsAddress, runtimeOffsets.EntityDetailsNameOffset));

        var entityData = memory.Read<EntityOffsets>(playerEntityAddress);
        var structDetails = memory.Read<EntityDetails>(entityData.ItemBase.EntityDetailsPtr);
        var structPath = ReadStdWString(memory, structDetails.name);

        using var game = new TheGame(Mock.Of<IFluentLog>(), memory);
        game.UpdateData();

        TestContext.Progress.WriteLine(
            "[poe-keypoint] " +
            $"entity=0x{playerEntityAddress.ToInt64():X}, " +
            $"detailsPtrOffset=0x{entityDetailsPtrOffset:X}, " +
            $"details=0x{entityDetailsAddress.ToInt64():X}, " +
            $"nameOffset=0x{entityDetailsNameOffset:X}, " +
            $"path='{recoveredPath}', " +
            $"length={recoveredNameStruct.Length}, " +
            $"capacity={recoveredNameStruct.Capacity}, " +
            $"objectModelPath='{game.Player.Path}'");

        // Then
        process.ProcessId.ShouldBe(targetProcess.Id);
        staticObject.GameState.ShouldNotBe(IntPtr.Zero);
        inGameEntry.X.ShouldNotBe(IntPtr.Zero);
        areaInstanceAddress.ShouldNotBe(IntPtr.Zero);
        playerEntityAddress.ShouldNotBe(IntPtr.Zero);
        playerEntityAddress.ShouldBe(game.Player.Address);

        ((int)entityDetailsPtrOffset).ShouldBe(Marshal.OffsetOf<ItemStruct>(nameof(ItemStruct.EntityDetailsPtr)).ToInt32());
        ((int)entityDetailsNameOffset).ShouldBe(Marshal.OffsetOf<EntityDetails>(nameof(EntityDetails.name)).ToInt32());
        entityDetailsAddress.ShouldBe(entityData.ItemBase.EntityDetailsPtr);
        entityDetailsAddress.ShouldNotBe(IntPtr.Zero);

        recoveredNameStruct.Length.ShouldBeGreaterThan(0);
        recoveredNameStruct.Capacity.ShouldBeGreaterThanOrEqualTo(recoveredNameStruct.Length);
        recoveredPath.ShouldNotBeNullOrWhiteSpace();
        recoveredPath.ShouldStartWith("Metadata/");
        providerPath.ShouldBe(recoveredPath);
        structPath.ShouldBe(recoveredPath);
        game.Player.Path.ShouldBe(recoveredPath);
    }

    /// <summary>
    /// WHAT: Verifies the entity component lookup chain is recovered from the game's generic named-component resolver.
    /// HOW: Starts from pattern-backed player entity, extracts lookup displacements from FUN_1401512E0, then resolves Player/Life components.
    /// </summary>
    [Test]
    [Explicit("Requires a running Path Of Exile client with the character loaded in-world.")]
    public void ShouldResolveEntityComponentLookupFromCodePattern()
    {
        // Given
        LocalProcessRuntime.IgnoreIfUnavailable();
        using var targetProcess = PathOfExileClientProcess.FindRunningOrIgnore();
        TestContext.Progress.WriteLine($"[poe-client] Target: {PathOfExileClientProcess.Describe(targetProcess)}");

        // When
        using var process = LocalProcessRuntime.OpenProcess(targetProcess.Id);
        var moduleName = Path.GetFileNameWithoutExtension(process.ProcessName) + ".exe";
        using var memory = process.MemoryOfModule(moduleName);

        var staticPatternOffsets = MemoryUtils
            .GetOffsets(memory, Offsets.Patterns)
            .ToDictionary(x => x.Key.Name!, x => x.Value);
        var keypointOffsets = MemoryUtils
            .GetOffsets(memory, Offsets.KeypointPatterns)
            .ToDictionary(x => x.Key.Name!, x => x.Value);

        var gameStatesGlobalSlot = ResolveRipRelativeSlot(memory, staticPatternOffsets[nameof(GameStates)]);
        var staticObject = memory.Read<GameStateStaticOffset>(gameStatesGlobalSlot);

        var tableShapeAddress = Add(memory.BaseAddress, keypointOffsets[Offsets.KeypointNames.GameStateTableShape]);
        var stateTableOffset = memory.Read<byte>(tableShapeAddress);
        var stateEntrySize = memory.Read<int>(Add(tableShapeAddress, 21));
        var inGameEntryOffset = stateTableOffset + (int)GameStateTypes.InGameState * stateEntrySize;
        var inGameEntry = memory.Read<StdTuple2D<IntPtr>>(Add(staticObject.GameState, inGameEntryOffset));

        var areaOffsetAddress = Add(memory.BaseAddress, keypointOffsets[Offsets.KeypointNames.InGameStateAreaInstanceData]);
        var areaInstanceOffset = memory.Read<int>(areaOffsetAddress);
        var areaInstanceAddress = memory.Read<IntPtr>(Add(inGameEntry.X, areaInstanceOffset));

        var localPlayersOffsetAddress = Add(memory.BaseAddress, keypointOffsets[Offsets.KeypointNames.AreaInstanceLocalPlayers]);
        var localPlayersOffset = memory.Read<int>(localPlayersOffsetAddress);
        var localPlayersVector = memory.Read<StdVector>(Add(areaInstanceAddress, localPlayersOffset));
        var localPlayers = ReadStdVector<IntPtr>(memory, localPlayersVector);
        var playerEntityAddress = localPlayers.FirstOrDefault();

        var lookupShapeAddress = Add(memory.BaseAddress, keypointOffsets[Offsets.KeypointNames.EntityComponentLookupShape]);
        var entityDetailsOffset = memory.Read<byte>(lookupShapeAddress);
        var entityDetailsOffsetSecondRead = memory.Read<byte>(Add(lookupShapeAddress, 0x0E));
        var entityDetailsComponentLookupOffset = memory.Read<byte>(Add(lookupShapeAddress, 0x1C));
        var componentNameAndIndexBucketOffset = memory.Read<byte>(Add(lookupShapeAddress, 0x2C));
        var componentNameAndIndexBucketEndOffset = memory.Read<byte>(Add(lookupShapeAddress, 0x38));
        var componentNameAndIndexIndexOffset = memory.Read<byte>(Add(lookupShapeAddress, 0x3E));
        var componentListOffset = memory.Read<byte>(Add(lookupShapeAddress, 0x47));
        var componentEntryStrideShiftAddress = Add(memory.BaseAddress, keypointOffsets[Offsets.KeypointNames.ComponentNameAndIndexEntryStride]);
        var componentNameAndIndexEntryShift = memory.Read<byte>(componentEntryStrideShiftAddress);
        var componentNameAndIndexEntrySize = 1 << componentNameAndIndexEntryShift;
        var componentNameAndIndexNamePointerOffset = 0;
        var componentNamePointerReadOpcode = memory.Read<byte>(Add(componentEntryStrideShiftAddress, 0x04), 3);

        var entityDetailsAddress = memory.Read<IntPtr>(Add(playerEntityAddress, entityDetailsOffset));
        var componentLookupAddress = memory.Read<IntPtr>(Add(entityDetailsAddress, entityDetailsComponentLookupOffset));
        var componentBucket = memory.Read<StdBucket>(Add(componentLookupAddress, componentNameAndIndexBucketOffset));
        var componentNameIndexes = ReadComponentNameIndexEntries(
            memory,
            componentBucket,
            componentNameAndIndexNamePointerOffset,
            componentNameAndIndexIndexOffset,
            componentNameAndIndexEntrySize);
        var componentListVector = memory.Read<StdVector>(Add(playerEntityAddress, componentListOffset));
        var componentAddresses = ReadStdVector<IntPtr>(memory, componentListVector);

        var componentIndexes = componentNameIndexes
            .Where(x => !string.IsNullOrWhiteSpace(x.Name))
            .GroupBy(x => x.Name)
            .ToDictionary(x => x.Key, x => x.First().Index);

        using var game = new TheGame(Mock.Of<IFluentLog>(), memory);
        game.UpdateData();
        var hasPlayer = game.Player.TryGetComponent<PlayerComponent>(out var playerComponent);
        var hasLife = game.Player.TryGetComponent<Life>(out var lifeComponent);

        var playerComponentName = typeof(PlayerComponent).Name;
        var lifeComponentName = typeof(Life).Name;
        var playerIndex = componentIndexes.GetValueOrDefault(playerComponentName, -1);
        var lifeIndex = componentIndexes.GetValueOrDefault(lifeComponentName, -1);
        var playerComponentAddress = playerIndex >= 0 && playerIndex < componentAddresses.Length
            ? componentAddresses[playerIndex]
            : IntPtr.Zero;
        var lifeComponentAddress = lifeIndex >= 0 && lifeIndex < componentAddresses.Length
            ? componentAddresses[lifeIndex]
            : IntPtr.Zero;
        var playerHeader = memory.Read<ComponentHeader>(playerComponentAddress);
        var lifeHeader = memory.Read<ComponentHeader>(lifeComponentAddress);

        TestContext.Progress.WriteLine(
            "[poe-keypoint] " +
            $"entity=0x{playerEntityAddress.ToInt64():X}, " +
            $"detailsOffset=0x{entityDetailsOffset:X}, " +
            $"componentListOffset=0x{componentListOffset:X}, " +
            $"detailsLookupOffset=0x{entityDetailsComponentLookupOffset:X}, " +
            $"lookupBucketOffset=0x{componentNameAndIndexBucketOffset:X}, " +
            $"lookupEndOffset=0x{componentNameAndIndexBucketEndOffset:X}, " +
            $"entryNameOffset=0x{componentNameAndIndexNamePointerOffset:X}, " +
            $"entryIndexOffset=0x{componentNameAndIndexIndexOffset:X}, " +
            $"entrySize=0x{componentNameAndIndexEntrySize:X}, " +
            $"components={componentAddresses.Length}, " +
            $"names={componentIndexes.Count}, " +
            $"playerIndex={playerIndex}, " +
            $"lifeIndex={lifeIndex}, " +
            $"playerComponent=0x{playerComponentAddress.ToInt64():X}, " +
            $"lifeComponent=0x{lifeComponentAddress.ToInt64():X}");

        // Then
        process.ProcessId.ShouldBe(targetProcess.Id);
        staticObject.GameState.ShouldNotBe(IntPtr.Zero);
        playerEntityAddress.ShouldNotBe(IntPtr.Zero);
        playerEntityAddress.ShouldBe(game.Player.Address);

        ((int)entityDetailsOffset).ShouldBe(Marshal.OffsetOf<ItemStruct>(nameof(ItemStruct.EntityDetailsPtr)).ToInt32());
        entityDetailsOffsetSecondRead.ShouldBe(entityDetailsOffset);
        ((int)componentListOffset).ShouldBe(Marshal.OffsetOf<ItemStruct>(nameof(ItemStruct.ComponentListPtr)).ToInt32());
        ((int)entityDetailsComponentLookupOffset).ShouldBe(Marshal.OffsetOf<EntityDetails>(nameof(EntityDetails.ComponentLookUpPtr)).ToInt32());
        ((int)componentNameAndIndexBucketOffset).ShouldBe(Marshal.OffsetOf<ComponentLookUpStruct>(nameof(ComponentLookUpStruct.ComponentsNameAndIndex)).ToInt32());
        ((int)componentNameAndIndexBucketEndOffset).ShouldBe(componentNameAndIndexBucketOffset + Marshal.OffsetOf<StdVector>(nameof(StdVector.Last)).ToInt32());
        componentNameAndIndexNamePointerOffset.ShouldBe(Marshal.OffsetOf<ComponentNameAndIndexStruct>(nameof(ComponentNameAndIndexStruct.NamePtr)).ToInt32());
        ((int)componentNameAndIndexIndexOffset).ShouldBe(Marshal.OffsetOf<ComponentNameAndIndexStruct>(nameof(ComponentNameAndIndexStruct.Index)).ToInt32());
        componentNameAndIndexEntrySize.ShouldBe(Marshal.SizeOf<ComponentNameAndIndexStruct>());
        componentNamePointerReadOpcode.ShouldBe(new byte[] { 0x48, 0x8B, 0x0A });

        entityDetailsAddress.ShouldNotBe(IntPtr.Zero);
        componentLookupAddress.ShouldNotBe(IntPtr.Zero);
        componentBucket.Data.First.ShouldNotBe(IntPtr.Zero);
        componentBucket.Data.Last.ShouldBe(memory.Read<IntPtr>(Add(componentLookupAddress, componentNameAndIndexBucketEndOffset)));
        componentNameIndexes.Length.ShouldBeGreaterThan(0);
        componentAddresses.Length.ShouldBeGreaterThan(0);

        componentIndexes.ContainsKey(playerComponentName).ShouldBeTrue("The player entity lookup bucket should contain a Player component entry.");
        componentIndexes.ContainsKey(lifeComponentName).ShouldBeTrue("The player entity lookup bucket should contain a Life component entry.");
        playerIndex.ShouldBeInRange(0, componentAddresses.Length - 1);
        lifeIndex.ShouldBeInRange(0, componentAddresses.Length - 1);
        playerComponentAddress.ShouldNotBe(IntPtr.Zero);
        lifeComponentAddress.ShouldNotBe(IntPtr.Zero);

        playerHeader.EntityPtr.ShouldBe(playerEntityAddress);
        lifeHeader.EntityPtr.ShouldBe(playerEntityAddress);
        hasPlayer.ShouldBeTrue();
        hasLife.ShouldBeTrue();
        playerComponent!.Address.ShouldBe(playerComponentAddress);
        lifeComponent!.Address.ShouldBe(lifeComponentAddress);
    }

    /// <summary>
    /// WHAT: Verifies the generic entity component resolver pattern is stable even though it is not unique.
    /// HOW: Scans the live client binary for every resolver-shaped byte sequence and proves every match encodes the same component lookup field layout.
    /// </summary>
    [Test]
    [Explicit("Requires a running Path Of Exile client with the character loaded in-world.")]
    public void ShouldResolveEntityComponentLookupShapeConsistentlyAcrossGenericResolvers()
    {
        // Given
        LocalProcessRuntime.IgnoreIfUnavailable();
        using var targetProcess = PathOfExileClientProcess.FindRunningOrIgnore();
        TestContext.Progress.WriteLine($"[poe-client] Target: {PathOfExileClientProcess.Describe(targetProcess)}");

        // When
        using var process = LocalProcessRuntime.OpenProcess(targetProcess.Id);
        var moduleName = Path.GetFileNameWithoutExtension(process.ProcessName) + ".exe";
        using var memory = process.MemoryOfModule(moduleName);
        var runtimeOffsets = RuntimeGameOffsets.GetOrResolve(memory);

        var modulePath = targetProcess.MainModule?.FileName;
        modulePath.ShouldNotBeNullOrWhiteSpace();
        var moduleBytes = File.ReadAllBytes(modulePath!);

        const string resolverShapePattern =
            "48 89 5C 24 18 57 48 83 EC 20 48 8B F9 48 8B 49 08 48 85 C9 74 ?? E8 ?? ?? ?? ?? " +
            "48 8B 47 08 4C 8D 44 24 30 48 8D 54 24 38 48 8B 58 28 48 8D 05 ?? ?? ?? ?? " +
            "48 89 44 24 30 48 8D 4B 28 E8 ?? ?? ?? ?? 48 8B 00 48 3B 43 30 74 ?? " +
            "48 63 48 08 83 F9 FF 74 ?? 48 8B 47 10 48 8B 04 C8";

        var matches = FindBytePatternMatches(moduleBytes, resolverShapePattern).ToArray();
        var layouts = matches
            .Select(match => ReadComponentLookupResolverLayout(moduleBytes, match))
            .Distinct()
            .ToArray();
        var layout = layouts.Length == 1 ? layouts[0] : default;

        TestContext.Progress.WriteLine(
            "[poe-keypoint] " +
            $"genericResolverMatches={matches.Length}, " +
            $"distinctLayouts={layouts.Length}, " +
            $"detailsOffset=0x{layout.EntityDetailsOffset:X}, " +
            $"componentListOffset=0x{layout.ComponentListOffset:X}, " +
            $"detailsLookupOffset=0x{layout.EntityDetailsComponentLookupOffset:X}, " +
            $"lookupBucketOffset=0x{layout.ComponentNameAndIndexBucketOffset:X}, " +
            $"lookupEndOffset=0x{layout.ComponentNameAndIndexBucketEndOffset:X}, " +
            $"entryIndexOffset=0x{layout.ComponentNameAndIndexIndexOffset:X}");

        // Then
        matches.Length.ShouldBeGreaterThan(
            1,
            "This keypoint is expected to describe a repeated generic resolver shape, not a unique function address.");
        layouts.Length.ShouldBe(
            1,
            "Every generic named-component resolver instantiation must encode the same field layout.");

        layout.EntityDetailsOffset.ShouldBe(Marshal.OffsetOf<ItemStruct>(nameof(ItemStruct.EntityDetailsPtr)).ToInt32());
        layout.EntityDetailsOffsetSecondRead.ShouldBe(layout.EntityDetailsOffset);
        layout.ComponentListOffset.ShouldBe(Marshal.OffsetOf<ItemStruct>(nameof(ItemStruct.ComponentListPtr)).ToInt32());
        layout.EntityDetailsComponentLookupOffset.ShouldBe(Marshal.OffsetOf<EntityDetails>(nameof(EntityDetails.ComponentLookUpPtr)).ToInt32());
        layout.ComponentNameAndIndexBucketOffset.ShouldBe(Marshal.OffsetOf<ComponentLookUpStruct>(nameof(ComponentLookUpStruct.ComponentsNameAndIndex)).ToInt32());
        layout.ComponentNameAndIndexBucketEndOffset.ShouldBe(layout.ComponentNameAndIndexBucketOffset + Marshal.OffsetOf<StdVector>(nameof(StdVector.Last)).ToInt32());
        layout.ComponentNameAndIndexIndexOffset.ShouldBe(Marshal.OffsetOf<ComponentNameAndIndexStruct>(nameof(ComponentNameAndIndexStruct.Index)).ToInt32());

        runtimeOffsets.EntityDetailsPtrOffset.ShouldBe(layout.EntityDetailsOffset);
        runtimeOffsets.EntityComponentListOffset.ShouldBe(layout.ComponentListOffset);
        runtimeOffsets.EntityDetailsComponentLookupOffset.ShouldBe(layout.EntityDetailsComponentLookupOffset);
        runtimeOffsets.ComponentLookupNameAndIndexBucketOffset.ShouldBe(layout.ComponentNameAndIndexBucketOffset);
        runtimeOffsets.ComponentNameAndIndexIndexOffset.ShouldBe(layout.ComponentNameAndIndexIndexOffset);
    }

    /// <summary>
    /// WHAT: Verifies ComponentHeader offsets from a concrete Player component virtual method.
    /// HOW: Resolves the Player component through the pattern-backed component lookup, then verifies vtable slot +0x68 reaches the formatter that reads owner entity at +0x08.
    /// </summary>
    [Test]
    [Explicit("Requires a running Path Of Exile client with the character loaded in-world.")]
    public void ShouldResolveComponentHeaderFromPlayerFormatter()
    {
        // Given
        LocalProcessRuntime.IgnoreIfUnavailable();
        using var targetProcess = PathOfExileClientProcess.FindRunningOrIgnore();
        TestContext.Progress.WriteLine($"[poe-client] Target: {PathOfExileClientProcess.Describe(targetProcess)}");

        // When
        using var process = LocalProcessRuntime.OpenProcess(targetProcess.Id);
        var moduleName = Path.GetFileNameWithoutExtension(process.ProcessName) + ".exe";
        using var memory = process.MemoryOfModule(moduleName);

        var staticPatternOffsets = MemoryUtils
            .GetOffsets(memory, Offsets.Patterns)
            .ToDictionary(x => x.Key.Name!, x => x.Value);
        var keypointOffsets = MemoryUtils
            .GetOffsets(memory, Offsets.KeypointPatterns)
            .ToDictionary(x => x.Key.Name!, x => x.Value);

        var gameStatesGlobalSlot = ResolveRipRelativeSlot(memory, staticPatternOffsets[nameof(GameStates)]);
        var staticObject = memory.Read<GameStateStaticOffset>(gameStatesGlobalSlot);

        var tableShapeAddress = Add(memory.BaseAddress, keypointOffsets[Offsets.KeypointNames.GameStateTableShape]);
        var stateTableOffset = memory.Read<byte>(tableShapeAddress);
        var stateEntrySize = memory.Read<int>(Add(tableShapeAddress, 21));
        var inGameEntryOffset = stateTableOffset + (int)GameStateTypes.InGameState * stateEntrySize;
        var inGameEntry = memory.Read<StdTuple2D<IntPtr>>(Add(staticObject.GameState, inGameEntryOffset));

        var areaOffsetAddress = Add(memory.BaseAddress, keypointOffsets[Offsets.KeypointNames.InGameStateAreaInstanceData]);
        var areaInstanceOffset = memory.Read<int>(areaOffsetAddress);
        var areaInstanceAddress = memory.Read<IntPtr>(Add(inGameEntry.X, areaInstanceOffset));

        var localPlayersOffsetAddress = Add(memory.BaseAddress, keypointOffsets[Offsets.KeypointNames.AreaInstanceLocalPlayers]);
        var localPlayersOffset = memory.Read<int>(localPlayersOffsetAddress);
        var localPlayersVector = memory.Read<StdVector>(Add(areaInstanceAddress, localPlayersOffset));
        var localPlayers = ReadStdVector<IntPtr>(memory, localPlayersVector);
        var playerEntityAddress = localPlayers.FirstOrDefault();

        var lookupShapeAddress = Add(memory.BaseAddress, keypointOffsets[Offsets.KeypointNames.EntityComponentLookupShape]);
        var entityDetailsOffset = memory.Read<byte>(lookupShapeAddress);
        var entityDetailsComponentLookupOffset = memory.Read<byte>(Add(lookupShapeAddress, 0x1C));
        var componentNameAndIndexBucketOffset = memory.Read<byte>(Add(lookupShapeAddress, 0x2C));
        var componentListOffset = memory.Read<byte>(Add(lookupShapeAddress, 0x47));

        var entityDetailsAddress = memory.Read<IntPtr>(Add(playerEntityAddress, entityDetailsOffset));
        var componentLookupAddress = memory.Read<IntPtr>(Add(entityDetailsAddress, entityDetailsComponentLookupOffset));
        var componentBucket = memory.Read<StdBucket>(Add(componentLookupAddress, componentNameAndIndexBucketOffset));
        var componentNameIndexes = ReadComponentNameIndexEntriesFromKeypoints(memory, keypointOffsets, lookupShapeAddress, componentBucket);
        var componentListVector = memory.Read<StdVector>(Add(playerEntityAddress, componentListOffset));
        var componentAddresses = ReadStdVector<IntPtr>(memory, componentListVector);

        var componentIndexes = componentNameIndexes
            .Where(x => !string.IsNullOrWhiteSpace(x.Name))
            .GroupBy(x => x.Name)
            .ToDictionary(x => x.Key, x => x.First().Index);

        var playerComponentName = typeof(PlayerComponent).Name;
        var lifeComponentName = typeof(Life).Name;
        var playerIndex = componentIndexes.GetValueOrDefault(playerComponentName, -1);
        var lifeIndex = componentIndexes.GetValueOrDefault(lifeComponentName, -1);
        var playerComponentAddress = playerIndex >= 0 && playerIndex < componentAddresses.Length
            ? componentAddresses[playerIndex]
            : IntPtr.Zero;
        var lifeComponentAddress = lifeIndex >= 0 && lifeIndex < componentAddresses.Length
            ? componentAddresses[lifeIndex]
            : IntPtr.Zero;

        var ownerOffsetAddress = Add(memory.BaseAddress, keypointOffsets[Offsets.KeypointNames.PlayerComponentHeaderOwnerEntity]);
        var ownerOffset = memory.Read<byte>(ownerOffsetAddress);
        var formatterAddress = Add(memory.BaseAddress, keypointOffsets[Offsets.KeypointNames.PlayerComponentHeaderOwnerEntity] - 0x23);
        var ownerReadInstructionAddress = Add(ownerOffsetAddress, -0x03);
        var ownerReadInstructionBytes = memory.Read<byte>(ownerReadInstructionAddress, 4);

        var playerHeader = memory.Read<ComponentHeader>(playerComponentAddress);
        var lifeHeader = memory.Read<ComponentHeader>(lifeComponentAddress);
        var playerFormatterFromVtable = memory.Read<IntPtr>(Add(playerHeader.StaticPtr, 0x68));
        var playerOwnerFromRecoveredOffset = memory.Read<IntPtr>(Add(playerComponentAddress, ownerOffset));
        var lifeOwnerFromRecoveredOffset = memory.Read<IntPtr>(Add(lifeComponentAddress, ownerOffset));

        using var game = new TheGame(Mock.Of<IFluentLog>(), memory);
        game.UpdateData();
        var hasPlayer = game.Player.TryGetComponent<PlayerComponent>(out var playerComponent);
        var hasLife = game.Player.TryGetComponent<Life>(out var lifeComponent);

        TestContext.Progress.WriteLine(
            "[poe-keypoint] " +
            $"entity=0x{playerEntityAddress.ToInt64():X}, " +
            $"playerComponent=0x{playerComponentAddress.ToInt64():X}, " +
            $"lifeComponent=0x{lifeComponentAddress.ToInt64():X}, " +
            $"playerVtable=0x{playerHeader.StaticPtr.ToInt64():X}, " +
            $"playerVtableSlot68=0x{playerFormatterFromVtable.ToInt64():X}, " +
            $"formatter=0x{formatterAddress.ToInt64():X}, " +
            $"componentOwnerOffset=0x{ownerOffset:X}, " +
            $"playerOwner=0x{playerOwnerFromRecoveredOffset.ToInt64():X}, " +
            $"lifeOwner=0x{lifeOwnerFromRecoveredOffset.ToInt64():X}");

        // Then
        process.ProcessId.ShouldBe(targetProcess.Id);
        staticObject.GameState.ShouldNotBe(IntPtr.Zero);
        playerEntityAddress.ShouldBe(game.Player.Address);
        hasPlayer.ShouldBeTrue();
        hasLife.ShouldBeTrue();
        playerComponent!.Address.ShouldBe(playerComponentAddress);
        lifeComponent!.Address.ShouldBe(lifeComponentAddress);

        playerComponentAddress.ShouldNotBe(IntPtr.Zero);
        lifeComponentAddress.ShouldNotBe(IntPtr.Zero);
        ownerReadInstructionBytes.ShouldBe(new byte[] { 0x48, 0x8B, 0x49, 0x08 });

        Marshal.OffsetOf<ComponentHeader>(nameof(ComponentHeader.StaticPtr)).ToInt32().ShouldBe(0);
        ((int)ownerOffset).ShouldBe(Marshal.OffsetOf<ComponentHeader>(nameof(ComponentHeader.EntityPtr)).ToInt32());

        playerHeader.StaticPtr.ShouldNotBe(IntPtr.Zero);
        lifeHeader.StaticPtr.ShouldNotBe(IntPtr.Zero);
        playerFormatterFromVtable.ShouldBe(formatterAddress);
        playerHeader.EntityPtr.ShouldBe(playerEntityAddress);
        lifeHeader.EntityPtr.ShouldBe(playerEntityAddress);
        playerOwnerFromRecoveredOffset.ShouldBe(playerEntityAddress);
        lifeOwnerFromRecoveredOffset.ShouldBe(playerEntityAddress);
    }

    /// <summary>
    /// WHAT: Verifies the Player component name offset is recovered from the Player vtable formatter.
    /// HOW: Resolves the player Player component through pattern-backed component lookup, then derives Player.Name and StdWString field offsets from code.
    /// </summary>
    [Test]
    [Explicit("Requires a running Path Of Exile client with the character loaded in-world.")]
    public void ShouldResolvePlayerNameFromCodePattern()
    {
        // Given
        LocalProcessRuntime.IgnoreIfUnavailable();
        using var targetProcess = PathOfExileClientProcess.FindRunningOrIgnore();
        TestContext.Progress.WriteLine($"[poe-client] Target: {PathOfExileClientProcess.Describe(targetProcess)}");

        // When
        using var process = LocalProcessRuntime.OpenProcess(targetProcess.Id);
        var moduleName = Path.GetFileNameWithoutExtension(process.ProcessName) + ".exe";
        using var memory = process.MemoryOfModule(moduleName);

        var staticPatternOffsets = MemoryUtils
            .GetOffsets(memory, Offsets.Patterns)
            .ToDictionary(x => x.Key.Name!, x => x.Value);
        var keypointOffsets = MemoryUtils
            .GetOffsets(memory, Offsets.KeypointPatterns)
            .ToDictionary(x => x.Key.Name!, x => x.Value);

        var gameStatesGlobalSlot = ResolveRipRelativeSlot(memory, staticPatternOffsets[nameof(GameStates)]);
        var staticObject = memory.Read<GameStateStaticOffset>(gameStatesGlobalSlot);

        var tableShapeAddress = Add(memory.BaseAddress, keypointOffsets[Offsets.KeypointNames.GameStateTableShape]);
        var stateTableOffset = memory.Read<byte>(tableShapeAddress);
        var stateEntrySize = memory.Read<int>(Add(tableShapeAddress, 21));
        var inGameEntryOffset = stateTableOffset + (int)GameStateTypes.InGameState * stateEntrySize;
        var inGameEntry = memory.Read<StdTuple2D<IntPtr>>(Add(staticObject.GameState, inGameEntryOffset));

        var areaOffsetAddress = Add(memory.BaseAddress, keypointOffsets[Offsets.KeypointNames.InGameStateAreaInstanceData]);
        var areaInstanceOffset = memory.Read<int>(areaOffsetAddress);
        var areaInstanceAddress = memory.Read<IntPtr>(Add(inGameEntry.X, areaInstanceOffset));

        var localPlayersOffsetAddress = Add(memory.BaseAddress, keypointOffsets[Offsets.KeypointNames.AreaInstanceLocalPlayers]);
        var localPlayersOffset = memory.Read<int>(localPlayersOffsetAddress);
        var localPlayersVector = memory.Read<StdVector>(Add(areaInstanceAddress, localPlayersOffset));
        var localPlayers = ReadStdVector<IntPtr>(memory, localPlayersVector);
        var playerEntityAddress = localPlayers.FirstOrDefault();

        var lookupShapeAddress = Add(memory.BaseAddress, keypointOffsets[Offsets.KeypointNames.EntityComponentLookupShape]);
        var entityDetailsOffset = memory.Read<byte>(lookupShapeAddress);
        var entityDetailsComponentLookupOffset = memory.Read<byte>(Add(lookupShapeAddress, 0x1C));
        var componentNameAndIndexBucketOffset = memory.Read<byte>(Add(lookupShapeAddress, 0x2C));
        var componentListOffset = memory.Read<byte>(Add(lookupShapeAddress, 0x47));

        var entityDetailsAddress = memory.Read<IntPtr>(Add(playerEntityAddress, entityDetailsOffset));
        var componentLookupAddress = memory.Read<IntPtr>(Add(entityDetailsAddress, entityDetailsComponentLookupOffset));
        var componentBucket = memory.Read<StdBucket>(Add(componentLookupAddress, componentNameAndIndexBucketOffset));
        var componentNameIndexes = ReadComponentNameIndexEntriesFromKeypoints(memory, keypointOffsets, lookupShapeAddress, componentBucket);
        var componentListVector = memory.Read<StdVector>(Add(playerEntityAddress, componentListOffset));
        var componentAddresses = ReadStdVector<IntPtr>(memory, componentListVector);

        var componentIndexes = componentNameIndexes
            .Where(x => !string.IsNullOrWhiteSpace(x.Name))
            .GroupBy(x => x.Name)
            .ToDictionary(x => x.Key, x => x.First().Index);

        var playerComponentName = typeof(PlayerComponent).Name;
        var playerIndex = componentIndexes.GetValueOrDefault(playerComponentName, -1);
        playerIndex.ShouldBeInRange(0, componentAddresses.Length - 1);
        var playerComponentAddress = componentAddresses[playerIndex];
        playerComponentAddress.ShouldNotBe(IntPtr.Zero);

        var nameKeypointAddress = Add(memory.BaseAddress, keypointOffsets[Offsets.KeypointNames.PlayerComponentName]);
        var nameOffset = memory.Read<int>(nameKeypointAddress);
        var capacityOffset = memory.Read<byte>(Add(nameKeypointAddress, 0x0C));
        var smallCapacityLimit = memory.Read<byte>(Add(nameKeypointAddress, 0x0D));
        var lengthOffset = memory.Read<byte>(Add(nameKeypointAddress, 0x11));
        var externalBufferOpcode = memory.Read<byte>(Add(nameKeypointAddress, 0x14), 3);

        var recoveredNameStruct = memory.Read<StdWString>(Add(playerComponentAddress, nameOffset));
        var recoveredName = ReadStdWString(memory, recoveredNameStruct);
        var runtimeOffsets = RuntimeGameOffsets.Resolve(memory);
        var providerName = runtimeOffsets.ReadStdWString(memory, Add(playerComponentAddress, runtimeOffsets.PlayerNameOffset));
        var playerData = memory.Read<PlayerOffsets>(playerComponentAddress);
        var structName = ReadStdWString(memory, playerData.Name);
        var playerHeader = memory.Read<ComponentHeader>(playerComponentAddress);

        using var game = new TheGame(Mock.Of<IFluentLog>(), memory);
        game.UpdateData();
        var hasPlayer = game.Player.TryGetComponent<PlayerComponent>(out var playerComponent);

        TestContext.Progress.WriteLine(
            "[poe-keypoint] " +
            $"entity=0x{playerEntityAddress.ToInt64():X}, " +
            $"playerComponent=0x{playerComponentAddress.ToInt64():X}, " +
            $"nameOffset=0x{nameOffset:X}, " +
            $"wstringLengthOffset=0x{lengthOffset:X}, " +
            $"wstringCapacityOffset=0x{capacityOffset:X}, " +
            $"smallCapacityLimit={smallCapacityLimit}, " +
            $"name='{recoveredName}', " +
            $"length={recoveredNameStruct.Length}, " +
            $"capacity={recoveredNameStruct.Capacity}");

        // Then
        process.ProcessId.ShouldBe(targetProcess.Id);
        staticObject.GameState.ShouldNotBe(IntPtr.Zero);
        playerEntityAddress.ShouldBe(game.Player.Address);
        hasPlayer.ShouldBeTrue();
        playerComponent!.Address.ShouldBe(playerComponentAddress);

        nameOffset.ShouldBe(Marshal.OffsetOf<PlayerOffsets>(nameof(PlayerOffsets.Name)).ToInt32());
        ((int)lengthOffset).ShouldBe(Marshal.OffsetOf<StdWString>(nameof(StdWString.Length)).ToInt32());
        ((int)capacityOffset).ShouldBe(Marshal.OffsetOf<StdWString>(nameof(StdWString.Capacity)).ToInt32());
        smallCapacityLimit.ShouldBe((byte)7);
        externalBufferOpcode.ShouldBe(new byte[] { 0x48, 0x8B, 0x1B });

        playerHeader.EntityPtr.ShouldBe(playerEntityAddress);
        recoveredNameStruct.Length.ShouldBeGreaterThan(0);
        recoveredNameStruct.Capacity.ShouldBeGreaterThanOrEqualTo(recoveredNameStruct.Length);
        recoveredName.ShouldNotBeNullOrWhiteSpace();
        providerName.ShouldBe(recoveredName);
        structName.ShouldBe(recoveredName);
        playerComponent.Name.ShouldBe(recoveredName);
    }

    /// <summary>
    /// WHAT: Verifies Life component vital offsets and VitalStruct current/total fields are recovered from Life vtable functions.
    /// HOW: Resolves the player Life component through pattern-backed component lookup, then derives Health/Mana/ES and Current/Total offsets from code.
    /// </summary>
    [Test]
    [Explicit("Requires a running Path Of Exile client with the character loaded in-world.")]
    public void ShouldResolveLifeVitalsFromCodePatterns()
    {
        // Given
        LocalProcessRuntime.IgnoreIfUnavailable();
        using var targetProcess = PathOfExileClientProcess.FindRunningOrIgnore();
        TestContext.Progress.WriteLine($"[poe-client] Target: {PathOfExileClientProcess.Describe(targetProcess)}");

        // When
        using var process = LocalProcessRuntime.OpenProcess(targetProcess.Id);
        var moduleName = Path.GetFileNameWithoutExtension(process.ProcessName) + ".exe";
        using var memory = process.MemoryOfModule(moduleName);

        var staticPatternOffsets = MemoryUtils
            .GetOffsets(memory, Offsets.Patterns)
            .ToDictionary(x => x.Key.Name!, x => x.Value);
        var keypointOffsets = MemoryUtils
            .GetOffsets(memory, Offsets.KeypointPatterns)
            .ToDictionary(x => x.Key.Name!, x => x.Value);

        var gameStatesGlobalSlot = ResolveRipRelativeSlot(memory, staticPatternOffsets[nameof(GameStates)]);
        var staticObject = memory.Read<GameStateStaticOffset>(gameStatesGlobalSlot);

        var tableShapeAddress = Add(memory.BaseAddress, keypointOffsets[Offsets.KeypointNames.GameStateTableShape]);
        var stateTableOffset = memory.Read<byte>(tableShapeAddress);
        var stateEntrySize = memory.Read<int>(Add(tableShapeAddress, 21));
        var inGameEntryOffset = stateTableOffset + (int)GameStateTypes.InGameState * stateEntrySize;
        var inGameEntry = memory.Read<StdTuple2D<IntPtr>>(Add(staticObject.GameState, inGameEntryOffset));

        var areaOffsetAddress = Add(memory.BaseAddress, keypointOffsets[Offsets.KeypointNames.InGameStateAreaInstanceData]);
        var areaInstanceOffset = memory.Read<int>(areaOffsetAddress);
        var areaInstanceAddress = memory.Read<IntPtr>(Add(inGameEntry.X, areaInstanceOffset));

        var localPlayersOffsetAddress = Add(memory.BaseAddress, keypointOffsets[Offsets.KeypointNames.AreaInstanceLocalPlayers]);
        var localPlayersOffset = memory.Read<int>(localPlayersOffsetAddress);
        var localPlayersVector = memory.Read<StdVector>(Add(areaInstanceAddress, localPlayersOffset));
        var localPlayers = ReadStdVector<IntPtr>(memory, localPlayersVector);
        var playerEntityAddress = localPlayers.FirstOrDefault();

        var lookupShapeAddress = Add(memory.BaseAddress, keypointOffsets[Offsets.KeypointNames.EntityComponentLookupShape]);
        var entityDetailsOffset = memory.Read<byte>(lookupShapeAddress);
        var entityDetailsComponentLookupOffset = memory.Read<byte>(Add(lookupShapeAddress, 0x1C));
        var componentNameAndIndexBucketOffset = memory.Read<byte>(Add(lookupShapeAddress, 0x2C));
        var componentListOffset = memory.Read<byte>(Add(lookupShapeAddress, 0x47));

        var entityDetailsAddress = memory.Read<IntPtr>(Add(playerEntityAddress, entityDetailsOffset));
        var componentLookupAddress = memory.Read<IntPtr>(Add(entityDetailsAddress, entityDetailsComponentLookupOffset));
        var componentBucket = memory.Read<StdBucket>(Add(componentLookupAddress, componentNameAndIndexBucketOffset));
        var componentNameIndexes = ReadComponentNameIndexEntriesFromKeypoints(memory, keypointOffsets, lookupShapeAddress, componentBucket);
        var componentListVector = memory.Read<StdVector>(Add(playerEntityAddress, componentListOffset));
        var componentAddresses = ReadStdVector<IntPtr>(memory, componentListVector);

        var componentIndexes = componentNameIndexes
            .Where(x => !string.IsNullOrWhiteSpace(x.Name))
            .GroupBy(x => x.Name)
            .ToDictionary(x => x.Key, x => x.First().Index);

        var lifeComponentName = typeof(Life).Name;
        var lifeIndex = componentIndexes.GetValueOrDefault(lifeComponentName, -1);
        lifeIndex.ShouldBeInRange(0, componentAddresses.Length - 1);
        var lifeComponentAddress = componentAddresses[lifeIndex];
        lifeComponentAddress.ShouldNotBe(IntPtr.Zero);

        var vitalStartsAddress = Add(memory.BaseAddress, keypointOffsets[Offsets.KeypointNames.LifeComponentVitalOffsets]);
        var healthOffset = memory.Read<int>(vitalStartsAddress);
        var manaOffset = memory.Read<int>(Add(vitalStartsAddress, 0x0C));
        var energyShieldOffset = memory.Read<int>(Add(vitalStartsAddress, 0x1B));

        var currentTotalAddress = Add(memory.BaseAddress, keypointOffsets[Offsets.KeypointNames.LifeVitalCurrentTotal]);
        var healthCurrentAbsoluteOffset = memory.Read<int>(currentTotalAddress);
        var healthTotalAbsoluteOffset = memory.Read<int>(Add(currentTotalAddress, 0x1D));
        var energyShieldCurrentAbsoluteOffset = memory.Read<int>(Add(currentTotalAddress, 0x49));
        var energyShieldTotalAbsoluteOffset = memory.Read<int>(Add(currentTotalAddress, 0x66));
        var manaCurrentAbsoluteOffset = memory.Read<int>(Add(currentTotalAddress, 0x92));
        var manaTotalAbsoluteOffset = memory.Read<int>(Add(currentTotalAddress, 0xAF));

        var currentOffsetFromHealth = healthCurrentAbsoluteOffset - healthOffset;
        var totalOffsetFromHealth = healthTotalAbsoluteOffset - healthOffset;
        var currentOffsetFromEnergyShield = energyShieldCurrentAbsoluteOffset - energyShieldOffset;
        var totalOffsetFromEnergyShield = energyShieldTotalAbsoluteOffset - energyShieldOffset;
        var currentOffsetFromMana = manaCurrentAbsoluteOffset - manaOffset;
        var totalOffsetFromMana = manaTotalAbsoluteOffset - manaOffset;

        var reservationAddress = Add(memory.BaseAddress, keypointOffsets[Offsets.KeypointNames.LifeVitalReservationOffsets]);
        var reservedFlatOffset = memory.Read<byte>(reservationAddress);
        var reservedPercentOffset = memory.Read<byte>(Add(reservationAddress, 0x28));

        var healthCurrent = memory.Read<int>(Add(lifeComponentAddress, healthOffset + currentOffsetFromHealth));
        var healthTotal = memory.Read<int>(Add(lifeComponentAddress, healthOffset + totalOffsetFromHealth));
        var healthReservedFlat = memory.Read<int>(Add(lifeComponentAddress, healthOffset + reservedFlatOffset));
        var healthReservedPercent = memory.Read<int>(Add(lifeComponentAddress, healthOffset + reservedPercentOffset));
        var manaCurrent = memory.Read<int>(Add(lifeComponentAddress, manaOffset + currentOffsetFromMana));
        var manaTotal = memory.Read<int>(Add(lifeComponentAddress, manaOffset + totalOffsetFromMana));
        var manaReservedFlat = memory.Read<int>(Add(lifeComponentAddress, manaOffset + reservedFlatOffset));
        var manaReservedPercent = memory.Read<int>(Add(lifeComponentAddress, manaOffset + reservedPercentOffset));
        var energyShieldCurrent = memory.Read<int>(Add(lifeComponentAddress, energyShieldOffset + currentOffsetFromEnergyShield));
        var energyShieldTotal = memory.Read<int>(Add(lifeComponentAddress, energyShieldOffset + totalOffsetFromEnergyShield));
        var energyShieldReservedFlat = memory.Read<int>(Add(lifeComponentAddress, energyShieldOffset + reservedFlatOffset));
        var energyShieldReservedPercent = memory.Read<int>(Add(lifeComponentAddress, energyShieldOffset + reservedPercentOffset));
        var lifeData = memory.Read<LifeOffset>(lifeComponentAddress);

        using var game = new TheGame(Mock.Of<IFluentLog>(), memory);
        game.UpdateData();
        var hasLife = game.Player.TryGetComponent<Life>(out var lifeComponent);

        TestContext.Progress.WriteLine(
            "[poe-keypoint] " +
            $"lifeComponent=0x{lifeComponentAddress.ToInt64():X}, " +
            $"healthOffset=0x{healthOffset:X}, " +
            $"manaOffset=0x{manaOffset:X}, " +
            $"energyShieldOffset=0x{energyShieldOffset:X}, " +
            $"vitalReservedFlatOffset=0x{reservedFlatOffset:X}, " +
            $"vitalReservedPercentOffset=0x{reservedPercentOffset:X}, " +
            $"vitalTotalOffset=0x{totalOffsetFromHealth:X}, " +
            $"vitalCurrentOffset=0x{currentOffsetFromHealth:X}, " +
            $"hp={healthCurrent}/{healthTotal}, " +
            $"hpReserved={healthReservedFlat}+{healthReservedPercent}, " +
            $"mp={manaCurrent}/{manaTotal}, " +
            $"mpReserved={manaReservedFlat}+{manaReservedPercent}, " +
            $"es={energyShieldCurrent}/{energyShieldTotal}");

        // Then
        process.ProcessId.ShouldBe(targetProcess.Id);
        staticObject.GameState.ShouldNotBe(IntPtr.Zero);
        playerEntityAddress.ShouldBe(game.Player.Address);
        hasLife.ShouldBeTrue();
        lifeComponent!.Address.ShouldBe(lifeComponentAddress);

        healthOffset.ShouldBe(Marshal.OffsetOf<LifeOffset>(nameof(LifeOffset.Health)).ToInt32());
        manaOffset.ShouldBe(Marshal.OffsetOf<LifeOffset>(nameof(LifeOffset.Mana)).ToInt32());
        energyShieldOffset.ShouldBe(Marshal.OffsetOf<LifeOffset>(nameof(LifeOffset.EnergyShield)).ToInt32());

        currentOffsetFromHealth.ShouldBe(Marshal.OffsetOf<VitalStruct>(nameof(VitalStruct.Current)).ToInt32());
        currentOffsetFromEnergyShield.ShouldBe(currentOffsetFromHealth);
        currentOffsetFromMana.ShouldBe(currentOffsetFromHealth);
        totalOffsetFromHealth.ShouldBe(Marshal.OffsetOf<VitalStruct>(nameof(VitalStruct.Total)).ToInt32());
        totalOffsetFromEnergyShield.ShouldBe(totalOffsetFromHealth);
        totalOffsetFromMana.ShouldBe(totalOffsetFromHealth);
        ((int)reservedFlatOffset).ShouldBe(Marshal.OffsetOf<VitalStruct>(nameof(VitalStruct.ReservedFlat)).ToInt32());
        ((int)reservedPercentOffset).ShouldBe(Marshal.OffsetOf<VitalStruct>(nameof(VitalStruct.ReservedPercent)).ToInt32());

        healthCurrent.ShouldBe(lifeData.Health.Current);
        healthTotal.ShouldBe(lifeData.Health.Total);
        healthReservedFlat.ShouldBe(lifeData.Health.ReservedFlat);
        healthReservedPercent.ShouldBe(lifeData.Health.ReservedPercent);
        manaCurrent.ShouldBe(lifeData.Mana.Current);
        manaTotal.ShouldBe(lifeData.Mana.Total);
        manaReservedFlat.ShouldBe(lifeData.Mana.ReservedFlat);
        manaReservedPercent.ShouldBe(lifeData.Mana.ReservedPercent);
        energyShieldCurrent.ShouldBe(lifeData.EnergyShield.Current);
        energyShieldTotal.ShouldBe(lifeData.EnergyShield.Total);
        energyShieldReservedFlat.ShouldBe(lifeData.EnergyShield.ReservedFlat);
        energyShieldReservedPercent.ShouldBe(lifeData.EnergyShield.ReservedPercent);

        lifeData.Health.Current.ShouldBe(lifeComponent.Health.Current);
        lifeData.Health.Total.ShouldBe(lifeComponent.Health.Total);
        lifeData.Health.ReservedFlat.ShouldBe(lifeComponent.Health.ReservedFlat);
        lifeData.Health.ReservedPercent.ShouldBe(lifeComponent.Health.ReservedPercent);
        lifeData.Mana.Current.ShouldBe(lifeComponent.Mana.Current);
        lifeData.Mana.Total.ShouldBe(lifeComponent.Mana.Total);
        lifeData.Mana.ReservedFlat.ShouldBe(lifeComponent.Mana.ReservedFlat);
        lifeData.Mana.ReservedPercent.ShouldBe(lifeComponent.Mana.ReservedPercent);
        lifeData.EnergyShield.Current.ShouldBe(lifeComponent.EnergyShield.Current);
        lifeData.EnergyShield.Total.ShouldBe(lifeComponent.EnergyShield.Total);
        lifeData.EnergyShield.ReservedFlat.ShouldBe(lifeComponent.EnergyShield.ReservedFlat);
        lifeData.EnergyShield.ReservedPercent.ShouldBe(lifeComponent.EnergyShield.ReservedPercent);

        lifeData.Health.Total.ShouldBeGreaterThan(0);
        lifeData.Mana.Total.ShouldBeGreaterThan(0);
        lifeData.EnergyShield.Total.ShouldBeGreaterThanOrEqualTo(0);
        lifeData.Health.Current.ShouldBeInRange(0, lifeData.Health.Total);
        lifeData.Mana.Current.ShouldBeInRange(0, lifeData.Mana.Total);
        lifeData.EnergyShield.Current.ShouldBeInRange(0, lifeData.EnergyShield.Total);
    }

    /// <summary>
    /// WHAT: Verifies the Life constructor shape for embedded VitalStruct header/stat-id fields.
    /// HOW: Recovers the constructor stores, then checks live Health/Mana/ES vitals against those constructor-written values.
    /// </summary>
    [Test]
    [Explicit("Requires a running Path Of Exile client with the character loaded in-world.")]
    public void ShouldResolveLifeVitalConstructorShapeFromCodePattern()
    {
        // Given
        LocalProcessRuntime.IgnoreIfUnavailable();
        using var targetProcess = PathOfExileClientProcess.FindRunningOrIgnore();
        TestContext.Progress.WriteLine($"[poe-client] Target: {PathOfExileClientProcess.Describe(targetProcess)}");

        // When
        using var process = LocalProcessRuntime.OpenProcess(targetProcess.Id);
        var moduleName = Path.GetFileNameWithoutExtension(process.ProcessName) + ".exe";
        using var memory = process.MemoryOfModule(moduleName);

        var staticPatternOffsets = MemoryUtils
            .GetOffsets(memory, Offsets.Patterns)
            .ToDictionary(x => x.Key.Name!, x => x.Value);
        var keypointOffsets = MemoryUtils
            .GetOffsets(
                memory,
                SelectKeypointPatterns(
                    Offsets.KeypointNames.GameStateTableShape,
                    Offsets.KeypointNames.InGameStateAreaInstanceData,
                    Offsets.KeypointNames.AreaInstanceLocalPlayers,
                    Offsets.KeypointNames.EntityComponentLookupShape,
                    Offsets.KeypointNames.ComponentNameAndIndexEntryStride,
                    Offsets.KeypointNames.LifeComponentVitalOffsets,
                    Offsets.KeypointNames.LifeVitalConstructorShape,
                    Offsets.KeypointNames.LifeVitalSharedConstructorShape))
            .ToDictionary(x => x.Key.Name!, x => x.Value);

        var gameStatesGlobalSlot = ResolveRipRelativeSlot(memory, staticPatternOffsets[nameof(GameStates)]);
        var staticObject = memory.Read<GameStateStaticOffset>(gameStatesGlobalSlot);

        var tableShapeAddress = Add(memory.BaseAddress, keypointOffsets[Offsets.KeypointNames.GameStateTableShape]);
        var stateTableOffset = memory.Read<byte>(tableShapeAddress);
        var stateEntrySize = memory.Read<int>(Add(tableShapeAddress, 21));
        var inGameEntryOffset = stateTableOffset + (int)GameStateTypes.InGameState * stateEntrySize;
        var inGameEntry = memory.Read<StdTuple2D<IntPtr>>(Add(staticObject.GameState, inGameEntryOffset));

        var areaOffsetAddress = Add(memory.BaseAddress, keypointOffsets[Offsets.KeypointNames.InGameStateAreaInstanceData]);
        var areaInstanceOffset = memory.Read<int>(areaOffsetAddress);
        var areaInstanceAddress = memory.Read<IntPtr>(Add(inGameEntry.X, areaInstanceOffset));

        var localPlayersOffsetAddress = Add(memory.BaseAddress, keypointOffsets[Offsets.KeypointNames.AreaInstanceLocalPlayers]);
        var localPlayersOffset = memory.Read<int>(localPlayersOffsetAddress);
        var localPlayersVector = memory.Read<StdVector>(Add(areaInstanceAddress, localPlayersOffset));
        var localPlayers = ReadStdVector<IntPtr>(memory, localPlayersVector);
        var playerEntityAddress = localPlayers.FirstOrDefault();

        var lookupShapeAddress = Add(memory.BaseAddress, keypointOffsets[Offsets.KeypointNames.EntityComponentLookupShape]);
        var entityDetailsOffset = memory.Read<byte>(lookupShapeAddress);
        var entityDetailsComponentLookupOffset = memory.Read<byte>(Add(lookupShapeAddress, 0x1C));
        var componentNameAndIndexBucketOffset = memory.Read<byte>(Add(lookupShapeAddress, 0x2C));
        var componentListOffset = memory.Read<byte>(Add(lookupShapeAddress, 0x47));

        var entityDetailsAddress = memory.Read<IntPtr>(Add(playerEntityAddress, entityDetailsOffset));
        var componentLookupAddress = memory.Read<IntPtr>(Add(entityDetailsAddress, entityDetailsComponentLookupOffset));
        var componentBucket = memory.Read<StdBucket>(Add(componentLookupAddress, componentNameAndIndexBucketOffset));
        var componentNameIndexes = ReadComponentNameIndexEntriesFromKeypoints(memory, keypointOffsets, lookupShapeAddress, componentBucket);
        var componentListVector = memory.Read<StdVector>(Add(playerEntityAddress, componentListOffset));
        var componentAddresses = ReadStdVector<IntPtr>(memory, componentListVector);

        var componentIndexes = componentNameIndexes
            .Where(x => !string.IsNullOrWhiteSpace(x.Name))
            .GroupBy(x => x.Name)
            .ToDictionary(x => x.Key, x => x.First().Index);

        var lifeComponentName = typeof(Life).Name;
        var lifeIndex = componentIndexes.GetValueOrDefault(lifeComponentName, -1);
        lifeIndex.ShouldBeInRange(0, componentAddresses.Length - 1);
        var lifeComponentAddress = componentAddresses[lifeIndex];
        lifeComponentAddress.ShouldNotBe(IntPtr.Zero);

        var vitalStartsAddress = Add(memory.BaseAddress, keypointOffsets[Offsets.KeypointNames.LifeComponentVitalOffsets]);
        var healthOffset = memory.Read<int>(vitalStartsAddress);
        var manaOffset = memory.Read<int>(Add(vitalStartsAddress, 0x0C));
        var energyShieldOffset = memory.Read<int>(Add(vitalStartsAddress, 0x1B));

        var constructorShapeAddress = Add(memory.BaseAddress, keypointOffsets[Offsets.KeypointNames.LifeVitalConstructorShape]);
        var sharedConstructorShapeAddress = Add(memory.BaseAddress, keypointOffsets[Offsets.KeypointNames.LifeVitalSharedConstructorShape]);
        var healthOffsetFromConstructor = memory.Read<int>(constructorShapeAddress);
        var healthUnknownStatId0Offset = memory.Read<byte>(Add(constructorShapeAddress, 0x07));
        var healthUnknownStatId0FromConstructor = memory.Read<int>(Add(constructorShapeAddress, 0x08));
        var healthUnknownStatId1Offset = memory.Read<byte>(Add(constructorShapeAddress, 0x0F));
        var healthUnknownStatId1FromConstructor = memory.Read<int>(Add(constructorShapeAddress, 0x10));
        var lifeComponentPtrOffset = memory.Read<byte>(Add(constructorShapeAddress, 0x17));
        var totalStatIdOffset = memory.Read<byte>(Add(constructorShapeAddress, 0x29));
        var unknownStatId2Offset = memory.Read<byte>(Add(constructorShapeAddress, 0x31));
        var unknownStatId3Offset = memory.Read<byte>(Add(constructorShapeAddress, 0x39));
        var healthUnknownStatId3FromConstructor = memory.Read<int>(Add(constructorShapeAddress, 0x3A));
        var manaOffsetFromConstructor = FindConstructorVitalOffset(memory, constructorShapeAddress, manaOffset);
        var manaUnknownStatId0Offset = healthUnknownStatId0Offset;
        var manaUnknownStatId0FromConstructor = ReadConstructorImmediateForVitalField(
            memory,
            constructorShapeAddress,
            manaOffset,
            manaUnknownStatId0Offset);
        var manaUnknownStatId1Offset = healthUnknownStatId1Offset;
        var manaUnknownStatId1FromConstructor = ReadConstructorImmediateForVitalField(
            memory,
            constructorShapeAddress,
            manaOffset,
            manaUnknownStatId1Offset);
        var manaUnknownStatId3FromConstructor = ReadConstructorImmediateForVitalField(
            memory,
            constructorShapeAddress,
            manaOffset,
            unknownStatId3Offset);
        var energyShieldOffsetFromConstructor = FindConstructorVitalOffset(memory, constructorShapeAddress, energyShieldOffset);
        var energyShieldUnknownStatId3FromConstructor = ReadConstructorStackArgumentForVitalHelper(
            memory,
            constructorShapeAddress,
            energyShieldOffset,
            stackOffset: 0x20);
        var sharedLifeComponentPtrOffset = memory.Read<byte>(sharedConstructorShapeAddress);
        var sharedUnknownStatId3Offset = memory.Read<byte>(Add(sharedConstructorShapeAddress, 0x16));
        var sharedUnknownStatId0Offset = memory.Read<byte>(Add(sharedConstructorShapeAddress, 0x19));
        var sharedUnknownStatId0FromConstructor = memory.Read<int>(Add(sharedConstructorShapeAddress, 0x1A));
        var sharedUnknownStatId1Offset = memory.Read<byte>(Add(sharedConstructorShapeAddress, 0x20));
        var sharedUnknownStatId1FromConstructor = memory.Read<int>(Add(sharedConstructorShapeAddress, 0x21));
        var sharedTotalStatIdOffset = memory.Read<byte>(Add(sharedConstructorShapeAddress, 0x2C));
        var sharedUnknownStatId2Offset = memory.Read<byte>(Add(sharedConstructorShapeAddress, 0x30));

        var healthLifeComponent = memory.Read<IntPtr>(Add(lifeComponentAddress, healthOffset + lifeComponentPtrOffset));
        var manaLifeComponent = memory.Read<IntPtr>(Add(lifeComponentAddress, manaOffset + lifeComponentPtrOffset));
        var energyShieldLifeComponent = memory.Read<IntPtr>(Add(lifeComponentAddress, energyShieldOffset + lifeComponentPtrOffset));
        var healthUnknownStatId0 = memory.Read<int>(Add(lifeComponentAddress, healthOffset + healthUnknownStatId0Offset));
        var healthUnknownStatId1 = memory.Read<int>(Add(lifeComponentAddress, healthOffset + healthUnknownStatId1Offset));
        var healthUnknownStatId3 = memory.Read<int>(Add(lifeComponentAddress, healthOffset + unknownStatId3Offset));
        var manaUnknownStatId0 = memory.Read<int>(Add(lifeComponentAddress, manaOffset + manaUnknownStatId0Offset));
        var manaUnknownStatId1 = memory.Read<int>(Add(lifeComponentAddress, manaOffset + manaUnknownStatId1Offset));
        var manaUnknownStatId3 = memory.Read<int>(Add(lifeComponentAddress, manaOffset + unknownStatId3Offset));
        var energyShieldUnknownStatId0 = memory.Read<int>(Add(lifeComponentAddress, energyShieldOffset + sharedUnknownStatId0Offset));
        var energyShieldUnknownStatId1 = memory.Read<int>(Add(lifeComponentAddress, energyShieldOffset + sharedUnknownStatId1Offset));
        var energyShieldUnknownStatId3 = memory.Read<int>(Add(lifeComponentAddress, energyShieldOffset + unknownStatId3Offset));
        var lifeData = memory.Read<LifeOffset>(lifeComponentAddress);

        TestContext.Progress.WriteLine(
            "[poe-keypoint] " +
            $"lifeComponent=0x{lifeComponentAddress.ToInt64():X}, " +
            $"healthOffset=0x{healthOffsetFromConstructor:X}, " +
            $"manaOffset=0x{manaOffsetFromConstructor:X}, " +
            $"energyShieldOffset=0x{energyShieldOffsetFromConstructor:X}, " +
            $"lifeComponentPtrOffset=0x{lifeComponentPtrOffset:X}, " +
            $"unknownStatId0Offset=0x{healthUnknownStatId0Offset:X}, " +
            $"unknownStatId1Offset=0x{healthUnknownStatId1Offset:X}, " +
            $"totalStatIdOffset=0x{totalStatIdOffset:X}, " +
            $"unknownStatId2Offset=0x{unknownStatId2Offset:X}, " +
            $"unknownStatId3Offset=0x{unknownStatId3Offset:X}, " +
            $"hpUnknownStatId0=0x{healthUnknownStatId0:X}, " +
            $"hpUnknownStatId1=0x{healthUnknownStatId1:X}, " +
            $"hpUnknownStatId3=0x{healthUnknownStatId3:X}, " +
            $"mpUnknownStatId0=0x{manaUnknownStatId0:X}, " +
            $"mpUnknownStatId1=0x{manaUnknownStatId1:X}, " +
            $"mpUnknownStatId3=0x{manaUnknownStatId3:X}, " +
            $"esUnknownStatId0=0x{energyShieldUnknownStatId0:X}, " +
            $"esUnknownStatId1=0x{energyShieldUnknownStatId1:X}, " +
            $"esUnknownStatId3=0x{energyShieldUnknownStatId3:X}");

        // Then
        process.ProcessId.ShouldBe(targetProcess.Id);
        staticObject.GameState.ShouldNotBe(IntPtr.Zero);
        playerEntityAddress.ShouldNotBe(IntPtr.Zero);

        healthOffsetFromConstructor.ShouldBe(healthOffset);
        manaOffsetFromConstructor.ShouldBe(manaOffset);
        energyShieldOffsetFromConstructor.ShouldBe(energyShieldOffset);
        healthOffsetFromConstructor.ShouldBe(Marshal.OffsetOf<LifeOffset>(nameof(LifeOffset.Health)).ToInt32());
        manaOffsetFromConstructor.ShouldBe(Marshal.OffsetOf<LifeOffset>(nameof(LifeOffset.Mana)).ToInt32());
        energyShieldOffsetFromConstructor.ShouldBe(Marshal.OffsetOf<LifeOffset>(nameof(LifeOffset.EnergyShield)).ToInt32());

        ((int)healthUnknownStatId0Offset).ShouldBe(Marshal.OffsetOf<VitalStruct>(nameof(VitalStruct.UnknownStatId0)).ToInt32());
        ((int)healthUnknownStatId1Offset).ShouldBe(Marshal.OffsetOf<VitalStruct>(nameof(VitalStruct.UnknownStatId1)).ToInt32());
        manaUnknownStatId0Offset.ShouldBe(healthUnknownStatId0Offset);
        manaUnknownStatId1Offset.ShouldBe(healthUnknownStatId1Offset);
        sharedUnknownStatId0Offset.ShouldBe(healthUnknownStatId0Offset);
        sharedUnknownStatId1Offset.ShouldBe(healthUnknownStatId1Offset);
        ((int)lifeComponentPtrOffset).ShouldBe(Marshal.OffsetOf<VitalStruct>(nameof(VitalStruct.LifeComponentPtr)).ToInt32());
        sharedLifeComponentPtrOffset.ShouldBe(lifeComponentPtrOffset);
        ((int)totalStatIdOffset).ShouldBe(Marshal.OffsetOf<VitalStruct>(nameof(VitalStruct.TotalStatId)).ToInt32());
        sharedTotalStatIdOffset.ShouldBe(totalStatIdOffset);
        ((int)unknownStatId2Offset).ShouldBe(Marshal.OffsetOf<VitalStruct>(nameof(VitalStruct.UnknownStatId2)).ToInt32());
        sharedUnknownStatId2Offset.ShouldBe(unknownStatId2Offset);
        ((int)unknownStatId3Offset).ShouldBe(Marshal.OffsetOf<VitalStruct>(nameof(VitalStruct.UnknownStatId3)).ToInt32());
        sharedUnknownStatId3Offset.ShouldBe(unknownStatId3Offset);

        healthLifeComponent.ShouldBe(lifeComponentAddress);
        manaLifeComponent.ShouldBe(lifeComponentAddress);
        energyShieldLifeComponent.ShouldBe(lifeComponentAddress);
        lifeData.Health.LifeComponentPtr.ShouldBe(lifeComponentAddress);
        lifeData.Mana.LifeComponentPtr.ShouldBe(lifeComponentAddress);
        lifeData.EnergyShield.LifeComponentPtr.ShouldBe(lifeComponentAddress);

        healthUnknownStatId0.ShouldBe(healthUnknownStatId0FromConstructor);
        healthUnknownStatId1.ShouldBe(healthUnknownStatId1FromConstructor);
        healthUnknownStatId3.ShouldBe(healthUnknownStatId3FromConstructor);
        manaUnknownStatId0.ShouldBe(manaUnknownStatId0FromConstructor);
        manaUnknownStatId1.ShouldBe(manaUnknownStatId1FromConstructor);
        manaUnknownStatId3.ShouldBe(manaUnknownStatId3FromConstructor);
        energyShieldUnknownStatId0.ShouldBe(sharedUnknownStatId0FromConstructor);
        energyShieldUnknownStatId1.ShouldBe(sharedUnknownStatId1FromConstructor);
        energyShieldUnknownStatId3.ShouldBe(energyShieldUnknownStatId3FromConstructor);
        lifeData.Health.UnknownStatId0.ShouldBe(healthUnknownStatId0);
        lifeData.Health.UnknownStatId1.ShouldBe(healthUnknownStatId1);
        lifeData.Health.UnknownStatId3.ShouldBe(healthUnknownStatId3);
        lifeData.Mana.UnknownStatId0.ShouldBe(manaUnknownStatId0);
        lifeData.Mana.UnknownStatId1.ShouldBe(manaUnknownStatId1);
        lifeData.Mana.UnknownStatId3.ShouldBe(manaUnknownStatId3);
        lifeData.EnergyShield.UnknownStatId0.ShouldBe(energyShieldUnknownStatId0);
        lifeData.EnergyShield.UnknownStatId1.ShouldBe(energyShieldUnknownStatId1);
        lifeData.EnergyShield.UnknownStatId3.ShouldBe(energyShieldUnknownStatId3);
    }

    /// <summary>
    /// WHAT: Verifies code keypoints recover the direct InGameState zone-switch counter and nested state pointer.
    /// HOW: Reads the code displacements, walks GameStates -> InGameState, and prints the direct counter plus nested object address.
    /// </summary>
    [Test]
    [Explicit("Requires a running Path Of Exile client with the character loaded in-world.")]
    public void ShouldResolveZoneSwitchCounterAndStatePointerFromCodePatterns()
    {
        // Given
        LocalProcessRuntime.IgnoreIfUnavailable();
        using var targetProcess = PathOfExileClientProcess.FindRunningOrIgnore();
        TestContext.Progress.WriteLine($"[poe-client] Target: {PathOfExileClientProcess.Describe(targetProcess)}");

        // When
        using var process = LocalProcessRuntime.OpenProcess(targetProcess.Id);
        var moduleName = Path.GetFileNameWithoutExtension(process.ProcessName) + ".exe";
        using var memory = process.MemoryOfModule(moduleName);

        var staticPatternOffsets = MemoryUtils
            .GetOffsets(memory, Offsets.Patterns)
            .ToDictionary(x => x.Key.Name!, x => x.Value);
        var keypointOffsets = MemoryUtils
            .GetOffsets(memory, Offsets.KeypointPatterns)
            .ToDictionary(x => x.Key.Name!, x => x.Value);

        var gameStatesGlobalSlot = ResolveRipRelativeSlot(memory, staticPatternOffsets[nameof(GameStates)]);
        var staticObject = memory.Read<GameStateStaticOffset>(gameStatesGlobalSlot);

        var tableShapeAddress = Add(memory.BaseAddress, keypointOffsets[Offsets.KeypointNames.GameStateTableShape]);
        var stateTableOffset = memory.Read<byte>(tableShapeAddress);
        var stateEntrySize = memory.Read<int>(Add(tableShapeAddress, 21));
        var inGameEntryOffset = stateTableOffset + (int)GameStateTypes.InGameState * stateEntrySize;
        var inGameEntry = memory.Read<StdTuple2D<IntPtr>>(Add(staticObject.GameState, inGameEntryOffset));

        var zoneSwitchStateOffsetAddress = Add(memory.BaseAddress, keypointOffsets[Offsets.KeypointNames.InGameStateZoneSwitchState]);
        var zoneSwitchStateConstructorOffsetAddress = Add(memory.BaseAddress, keypointOffsets[Offsets.KeypointNames.InGameStateZoneSwitchStateConstructor]);
        var directZoneSwitchCounterOffsetAddress = Add(memory.BaseAddress, keypointOffsets[Offsets.KeypointNames.InGameStateZoneSwitchCounter]);
        var zoneSwitchStateOffset = memory.Read<int>(zoneSwitchStateOffsetAddress);
        var zoneSwitchStateConstructorReadOffset = memory.Read<int>(zoneSwitchStateConstructorOffsetAddress);
        var zoneSwitchStateConstructorWriteOffset = memory.Read<int>(Add(zoneSwitchStateConstructorOffsetAddress, 7));
        var directZoneSwitchCounterOffset = memory.Read<int>(directZoneSwitchCounterOffsetAddress);
        var zoneSwitchStateAddress = memory.Read<IntPtr>(Add(inGameEntry.X, zoneSwitchStateOffset));
        var inGameStateVtable = memory.Read<IntPtr>(inGameEntry.X);
        var directZoneSwitchCounterStructOffset = Marshal.OffsetOf<InGameStateOffset>(nameof(InGameStateOffset.ZoneSwitchCounter)).ToInt32();
        var directZoneSwitchCounterAddress = Add(inGameEntry.X, directZoneSwitchCounterOffset);
        var directZoneSwitchCounter = memory.Read<int>(directZoneSwitchCounterAddress);

        TestContext.Progress.WriteLine(
            "[poe-keypoint] " +
            $"inGameState=0x{inGameEntry.X.ToInt64():X}, " +
            $"inGameStateVtable=0x{inGameStateVtable.ToInt64():X}, " +
            $"directZoneSwitchCounterOffset=0x{directZoneSwitchCounterOffset:X}, " +
            $"directZoneSwitchCounterAddress=0x{directZoneSwitchCounterAddress.ToInt64():X}, " +
            $"directZoneSwitchCounter=0x{directZoneSwitchCounter:X}, " +
            $"zoneSwitchStateOffset=0x{zoneSwitchStateOffset:X}, " +
            $"zoneSwitchStateConstructorReadOffset=0x{zoneSwitchStateConstructorReadOffset:X}, " +
            $"zoneSwitchStateConstructorWriteOffset=0x{zoneSwitchStateConstructorWriteOffset:X}, " +
            $"zoneSwitchState=0x{zoneSwitchStateAddress.ToInt64():X}");

        // Then
        process.ProcessId.ShouldBe(targetProcess.Id);
        staticObject.GameState.ShouldNotBe(IntPtr.Zero);
        inGameEntry.X.ShouldNotBe(IntPtr.Zero);

        zoneSwitchStateOffset.ShouldBe(0x368);
        zoneSwitchStateConstructorReadOffset.ShouldBe(zoneSwitchStateOffset);
        zoneSwitchStateConstructorWriteOffset.ShouldBe(zoneSwitchStateOffset);
        directZoneSwitchCounterOffset.ShouldBe(0x56C);
        directZoneSwitchCounterOffset.ShouldBe(directZoneSwitchCounterStructOffset);
        directZoneSwitchCounter.ShouldBeGreaterThanOrEqualTo(0);
        zoneSwitchStateAddress.ShouldNotBe(IntPtr.Zero);
        zoneSwitchStateAddress.ShouldNotBe(inGameEntry.X);
    }

    /// <summary>
    /// WHAT: Verifies reset/increment writer sites for the direct InGameState zone-switch counter.
    /// HOW: Reads writer displacements from static code patterns and verifies they all target the same managed field.
    /// </summary>
    [Test]
    [Explicit("Requires a running Path Of Exile client with the character loaded in-world.")]
    public void ShouldResolveZoneSwitchCounterWriterSitesFromCodePatterns()
    {
        // Given
        LocalProcessRuntime.IgnoreIfUnavailable();
        using var targetProcess = PathOfExileClientProcess.FindRunningOrIgnore();
        TestContext.Progress.WriteLine($"[poe-client] Target: {PathOfExileClientProcess.Describe(targetProcess)}");

        // When
        using var process = LocalProcessRuntime.OpenProcess(targetProcess.Id);
        var moduleName = Path.GetFileNameWithoutExtension(process.ProcessName) + ".exe";
        using var memory = process.MemoryOfModule(moduleName);

        var staticPatternOffsets = MemoryUtils
            .GetOffsets(memory, Offsets.Patterns)
            .ToDictionary(x => x.Key.Name!, x => x.Value);
        var keypointOffsets = MemoryUtils
            .GetOffsets(memory, Offsets.KeypointPatterns)
            .ToDictionary(x => x.Key.Name!, x => x.Value);

        var gameStatesGlobalSlot = ResolveRipRelativeSlot(memory, staticPatternOffsets[nameof(GameStates)]);
        var staticObject = memory.Read<GameStateStaticOffset>(gameStatesGlobalSlot);

        var tableShapeAddress = Add(memory.BaseAddress, keypointOffsets[Offsets.KeypointNames.GameStateTableShape]);
        var stateTableOffset = memory.Read<byte>(tableShapeAddress);
        var stateEntrySize = memory.Read<int>(Add(tableShapeAddress, 21));
        var inGameEntryOffset = stateTableOffset + (int)GameStateTypes.InGameState * stateEntrySize;
        var inGameEntry = memory.Read<StdTuple2D<IntPtr>>(Add(staticObject.GameState, inGameEntryOffset));

        var directReadOffsetAddress = Add(memory.BaseAddress, keypointOffsets[Offsets.KeypointNames.InGameStateZoneSwitchCounter]);
        var resetOffsetAddress = Add(memory.BaseAddress, keypointOffsets[Offsets.KeypointNames.InGameStateZoneSwitchCounterReset]);
        var incrementFirstOffsetAddress = Add(memory.BaseAddress, keypointOffsets[Offsets.KeypointNames.InGameStateZoneSwitchCounterIncrementFirst]);
        var incrementSecondOffsetAddress = Add(memory.BaseAddress, keypointOffsets[Offsets.KeypointNames.InGameStateZoneSwitchCounterIncrementSecond]);

        var directReadOffset = memory.Read<int>(directReadOffsetAddress);
        var resetOffset = memory.Read<int>(resetOffsetAddress);
        var resetAdjacentFlagOffset = memory.Read<int>(Add(resetOffsetAddress, 0x06));
        var incrementFirstOffset = memory.Read<int>(incrementFirstOffsetAddress);
        var incrementSecondOffset = memory.Read<int>(incrementSecondOffsetAddress);

        var directZoneSwitchCounterStructOffset = Marshal.OffsetOf<InGameStateOffset>(nameof(InGameStateOffset.ZoneSwitchCounter)).ToInt32();
        var directZoneSwitchCounterAddress = Add(inGameEntry.X, directReadOffset);
        var directZoneSwitchCounter = memory.Read<int>(directZoneSwitchCounterAddress);

        TestContext.Progress.WriteLine(
            "[poe-keypoint] " +
            $"inGameState=0x{inGameEntry.X.ToInt64():X}, " +
            $"directReadOffset=0x{directReadOffset:X}, " +
            $"resetOffset=0x{resetOffset:X}, " +
            $"resetAdjacentFlagOffset=0x{resetAdjacentFlagOffset:X}, " +
            $"incrementFirstOffset=0x{incrementFirstOffset:X}, " +
            $"incrementSecondOffset=0x{incrementSecondOffset:X}, " +
            $"directZoneSwitchCounter=0x{directZoneSwitchCounter:X}");

        // Then
        process.ProcessId.ShouldBe(targetProcess.Id);
        staticObject.GameState.ShouldNotBe(IntPtr.Zero);
        inGameEntry.X.ShouldNotBe(IntPtr.Zero);

        directReadOffset.ShouldBe(directZoneSwitchCounterStructOffset);
        resetOffset.ShouldBe(directZoneSwitchCounterStructOffset);
        incrementFirstOffset.ShouldBe(directZoneSwitchCounterStructOffset);
        incrementSecondOffset.ShouldBe(directZoneSwitchCounterStructOffset);

        resetAdjacentFlagOffset.ShouldBe(0x568);
        resetAdjacentFlagOffset.ShouldBe(directZoneSwitchCounterStructOffset - 4);
        directZoneSwitchCounter.ShouldBeGreaterThanOrEqualTo(0);
    }

    /// <summary>
    /// WHAT: Verifies the InGameState elapsed timer offset is recovered from the real writer.
    /// HOW: Resolves the FUN_140FAD6E0 code pattern, reads InGameState + recovered offset, and observes the dword increasing.
    /// </summary>
    [Test]
    [Explicit("Requires a running Path Of Exile client with the character loaded in-world.")]
    public void ShouldResolveMsElapsedFromCodePattern()
    {
        // Given
        LocalProcessRuntime.IgnoreIfUnavailable();
        using var targetProcess = PathOfExileClientProcess.FindRunningOrIgnore();
        TestContext.Progress.WriteLine($"[poe-client] Target: {PathOfExileClientProcess.Describe(targetProcess)}");

        // When
        using var process = LocalProcessRuntime.OpenProcess(targetProcess.Id);
        var moduleName = Path.GetFileNameWithoutExtension(process.ProcessName) + ".exe";
        using var memory = process.MemoryOfModule(moduleName);

        var staticPatternOffsets = MemoryUtils
            .GetOffsets(memory, Offsets.Patterns)
            .ToDictionary(x => x.Key.Name!, x => x.Value);
        var keypointOffsets = MemoryUtils
            .GetOffsets(memory, Offsets.KeypointPatterns)
            .ToDictionary(x => x.Key.Name!, x => x.Value);

        var gameStatesGlobalSlot = ResolveRipRelativeSlot(memory, staticPatternOffsets[nameof(GameStates)]);
        var staticObject = memory.Read<GameStateStaticOffset>(gameStatesGlobalSlot);

        var tableShapeAddress = Add(memory.BaseAddress, keypointOffsets[Offsets.KeypointNames.GameStateTableShape]);
        var stateTableOffset = memory.Read<byte>(tableShapeAddress);
        var stateEntrySize = memory.Read<int>(Add(tableShapeAddress, 21));
        var inGameEntryOffset = stateTableOffset + (int)GameStateTypes.InGameState * stateEntrySize;
        var inGameEntry = memory.Read<StdTuple2D<IntPtr>>(Add(staticObject.GameState, inGameEntryOffset));

        var msElapsedOffsetAddress = Add(memory.BaseAddress, keypointOffsets[Offsets.KeypointNames.InGameStateMsElapsed]);
        var msElapsedOffset = memory.Read<int>(msElapsedOffsetAddress);
        var msElapsedStructOffset = Marshal.OffsetOf<InGameStateOffset>(nameof(InGameStateOffset.MsElapsed)).ToInt32();
        var msElapsedAddress = Add(inGameEntry.X, msElapsedOffset);
        var first = memory.Read<int>(msElapsedAddress);

        var second = first;
        for (var attempt = 0; attempt < 5 && second <= first; attempt++)
        {
            Thread.Sleep(250);
            second = memory.Read<int>(msElapsedAddress);
        }

        TestContext.Progress.WriteLine(
            "[poe-keypoint] " +
            $"inGameState=0x{inGameEntry.X.ToInt64():X}, " +
            $"msElapsedOffset=0x{msElapsedOffset:X}, " +
            $"msElapsedAddress=0x{msElapsedAddress.ToInt64():X}, " +
            $"first=0x{first:X}, " +
            $"second=0x{second:X}");

        // Then
        process.ProcessId.ShouldBe(targetProcess.Id);
        staticObject.GameState.ShouldNotBe(IntPtr.Zero);
        inGameEntry.X.ShouldNotBe(IntPtr.Zero);

        msElapsedOffset.ShouldBe(0x400);
        msElapsedOffset.ShouldBe(msElapsedStructOffset);
        first.ShouldBeGreaterThanOrEqualTo(0);
        second.ShouldBeGreaterThan(first, "Expected the InGameState elapsed timer to advance while the live client is loaded in-world.");
    }

    /// <summary>
    /// WHAT: Verifies the elapsed-timer writer is reached from the live InGameState virtual update method.
    /// HOW: Resolves the writer-start keypoint, then checks InGameState vtable slot +0x8 directly branches to it.
    /// </summary>
    [Test]
    [Explicit("Requires a running Path Of Exile client with the character loaded in-world.")]
    public void ShouldResolveMsElapsedWriterFromInGameStateUpdateVtableChain()
    {
        // Given
        LocalProcessRuntime.IgnoreIfUnavailable();
        using var targetProcess = PathOfExileClientProcess.FindRunningOrIgnore();
        TestContext.Progress.WriteLine($"[poe-client] Target: {PathOfExileClientProcess.Describe(targetProcess)}");

        // When
        using var process = LocalProcessRuntime.OpenProcess(targetProcess.Id);
        var moduleName = Path.GetFileNameWithoutExtension(process.ProcessName) + ".exe";
        using var memory = process.MemoryOfModule(moduleName);

        var keypointOffsets = MemoryUtils
            .GetOffsets(memory, Offsets.KeypointPatterns)
            .ToDictionary(x => x.Key.Name!, x => x.Value);
        var runtimeOffsets = RuntimeGameOffsets.Resolve(memory);
        var gameStateOwner = memory.Read<IntPtr>(Add(runtimeOffsets.GameStatesGlobalSlot, runtimeOffsets.GameStateStaticGameStateOffset));
        var inGameEntry = runtimeOffsets.ReadInGameStateEntry(memory, gameStateOwner);
        var enumInGameEntry = runtimeOffsets.ReadGameStateEntry(memory, gameStateOwner, GameStateTypes.InGameState);

        var writerFunctionAddressFromStartPattern = Add(
            memory.BaseAddress,
            keypointOffsets[Offsets.KeypointNames.InGameStateMsElapsedWriterFunctionStart]);
        var writerFunctionAddress = runtimeOffsets.InGameStateMsElapsedWriterFunctionAddress;
        var inGameVtable = memory.Read<IntPtr>(inGameEntry.X);
        var updateSlotFunction = memory.Read<IntPtr>(Add(inGameVtable, runtimeOffsets.GameStateVirtualUpdateSlotOffset));
        var updateSlotBranchesToWriter = runtimeOffsets.StateUpdateSlotBranchesToElapsedWriter(memory, inGameEntry.X);
        var msElapsedOffset = runtimeOffsets.InGameStateMsElapsedOffset;
        var msElapsedAddress = Add(inGameEntry.X, msElapsedOffset);

        var first = memory.Read<int>(msElapsedAddress);
        Thread.Sleep(250);
        var second = memory.Read<int>(msElapsedAddress);

        TestContext.Progress.WriteLine(
            "[poe-keypoint] " +
            $"inGameState=0x{inGameEntry.X.ToInt64():X}, " +
            $"vtable=0x{inGameVtable.ToInt64():X}, " +
            $"slot=0x{runtimeOffsets.GameStateVirtualUpdateSlotOffset:X}, " +
            $"slotFunction=0x{updateSlotFunction.ToInt64():X}, " +
            $"writerFunctionFromStartPattern=0x{writerFunctionAddressFromStartPattern.ToInt64():X}, " +
            $"writerFunction=0x{writerFunctionAddress.ToInt64():X}, " +
            $"slotBranchesToWriter={updateSlotBranchesToWriter}, " +
            $"msElapsedAddress=0x{msElapsedAddress.ToInt64():X}, " +
            $"first=0x{first:X}, " +
            $"second=0x{second:X}");

        // Then
        process.ProcessId.ShouldBe(targetProcess.Id);
        gameStateOwner.ShouldNotBe(IntPtr.Zero);
        inGameEntry.X.ShouldNotBe(IntPtr.Zero);

        msElapsedOffset.ShouldBe(0x400);
        runtimeOffsets.GameStateVirtualUpdateSlotOffset.ShouldBe(0x08);
        writerFunctionAddress.ShouldBe(writerFunctionAddressFromStartPattern);
        inGameEntry.X.ShouldBe(enumInGameEntry.X, "The behavior-derived InGame state entry should match the known enum index for this build.");
        updateSlotBranchesToWriter.ShouldBeTrue("Expected InGameState vtable slot +0x8 to be the update wrapper that directly branches to the MsElapsed writer.");
        second.ShouldBeGreaterThan(first, "Expected the live InGameState update method to keep advancing MsElapsed.");
    }

    private static IntPtr Add(IntPtr address, long offset)
    {
        return new IntPtr(checked((nint)(address.ToInt64() + offset)));
    }

    private static IntPtr Add(long address, long offset)
    {
        return new IntPtr(checked((nint)(address + offset)));
    }

    private static IntPtr ResolveRipRelativeSlot(IMemory memory, long displacementRva)
    {
        var displacementAddress = Add(memory.BaseAddress, displacementRva);
        var relativeOffset = memory.Read<int>(displacementAddress);
        return Add(displacementAddress, relativeOffset + sizeof(int));
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "CheatCartridge.sln")) &&
                Directory.Exists(Path.Combine(directory.FullName, "CheatCartridge")))
            {
                return directory;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find CheatCartridge repository root.");
    }

    private static string RequireMainModuleFileName(Process process)
    {
        try
        {
            var fileName = process.MainModule?.FileName;
            if (!string.IsNullOrWhiteSpace(fileName) && File.Exists(fileName))
            {
                return fileName;
            }
        }
        catch (Exception e) when (e is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            Assert.Ignore($"Could not read target process main module path for versioned FF generation: {e.Message}");
        }

        Assert.Ignore("Could not read target process main module path for versioned FF generation.");
        throw new UnreachableException();
    }

    private static string ComputeSha256(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    private static string NormalizeRelativePath(DirectoryInfo repositoryRoot, string filePath)
    {
        return Path.GetRelativePath(repositoryRoot.FullName, filePath).Replace('\\', '/');
    }

    private static string CreateResolvedLayoutFileName(string buildId)
    {
        return $"poe-game-model.{buildId}.ff.proto";
    }

    private static T[] ReadStdVector<T>(IMemory memory, StdVector nativeContainer)
        where T : unmanaged
    {
        var typeSize = Marshal.SizeOf<T>();
        var length = nativeContainer.Last.ToInt64() - nativeContainer.First.ToInt64();
        if (length <= 0 || length % typeSize != 0)
        {
            return [];
        }

        return memory.Read<T>(nativeContainer.First, (int)length / typeSize);
    }

    private static T[] ReadStdBucket<T>(IMemory memory, StdBucket nativeContainer)
        where T : unmanaged
    {
        if (nativeContainer.Data.First == IntPtr.Zero)
        {
            return [];
        }

        return ReadStdVector<T>(memory, nativeContainer.Data);
    }

    private static RuntimeGameOffsets.ComponentNameIndexEntry[] ReadComponentNameIndexEntriesFromKeypoints(
        IMemory memory,
        IReadOnlyDictionary<string, int> keypointOffsets,
        IntPtr lookupShapeAddress,
        StdBucket nativeContainer)
    {
        var indexOffset = memory.Read<byte>(Add(lookupShapeAddress, 0x3E));
        var strideShiftAddress = Add(memory.BaseAddress, keypointOffsets[Offsets.KeypointNames.ComponentNameAndIndexEntryStride]);
        var namePointerOffset = 0;
        var entrySize = 1 << memory.Read<byte>(strideShiftAddress);
        var namePointerReadOpcode = memory.Read<byte>(Add(strideShiftAddress, 0x04), 3);

        namePointerReadOpcode.ShouldBe(new byte[] { 0x48, 0x8B, 0x0A });
        entrySize.ShouldBeGreaterThanOrEqualTo(indexOffset + sizeof(int));

        return ReadComponentNameIndexEntries(memory, nativeContainer, namePointerOffset, indexOffset, entrySize);
    }

    private static RuntimeGameOffsets.ComponentNameIndexEntry[] ReadComponentNameIndexEntries(
        IMemory memory,
        StdBucket nativeContainer,
        int namePointerOffset,
        int indexOffset,
        int entrySize)
    {
        if (nativeContainer.Data.First == IntPtr.Zero)
        {
            return [];
        }

        var length = nativeContainer.Data.Last.ToInt64() - nativeContainer.Data.First.ToInt64();
        if (length <= 0 ||
            length % entrySize != 0)
        {
            return [];
        }

        var entries = new List<RuntimeGameOffsets.ComponentNameIndexEntry>((int)(length / entrySize));
        for (var i = 0; i < length / entrySize; i++)
        {
            var entryAddress = Add(nativeContainer.Data.First, i * entrySize);
            var namePtr = memory.Read<IntPtr>(Add(entryAddress, namePointerOffset));
            if (namePtr == IntPtr.Zero)
            {
                continue;
            }

            var name = memory.ReadString(namePtr);
            if (string.IsNullOrEmpty(name))
            {
                continue;
            }

            var index = memory.Read<int>(Add(entryAddress, indexOffset));
            entries.Add(new RuntimeGameOffsets.ComponentNameIndexEntry(name, index, namePtr));
        }

        return entries.ToArray();
    }

    private static int FindConstructorVitalOffset(IMemory memory, IntPtr constructorShapeAddress, int expectedVitalOffset)
    {
        var (_, _, _, vitalOffset) = FindConstructorVitalLea(memory, constructorShapeAddress, expectedVitalOffset);
        return vitalOffset;
    }

    private static int ReadConstructorImmediateForVitalField(
        IMemory memory,
        IntPtr constructorShapeAddress,
        int vitalOffset,
        int vitalFieldOffset)
    {
        var (window, leaOffset, targetRegister, _) = FindConstructorVitalLea(memory, constructorShapeAddress, vitalOffset);
        var searchEnd = Math.Min(window.Length, leaOffset + 0x90);
        for (var offset = leaOffset + 7; offset < searchEnd; offset++)
        {
            if (!TryReadMovDwordImmediate(window, offset, out var baseRegister, out var displacement, out var immediate))
            {
                continue;
            }

            if (baseRegister == targetRegister &&
                displacement == vitalFieldOffset)
            {
                return immediate;
            }
        }

        throw new InvalidOperationException(
            $"Could not find constructor immediate for vital 0x{vitalOffset:X} field 0x{vitalFieldOffset:X}.");
    }

    private static int ReadConstructorStackArgumentForVitalHelper(
        IMemory memory,
        IntPtr constructorShapeAddress,
        int vitalOffset,
        int stackOffset)
    {
        var (window, leaOffset, _, _) = FindConstructorVitalLea(memory, constructorShapeAddress, vitalOffset);
        var searchStart = Math.Max(0, leaOffset - 0x60);
        for (var offset = leaOffset; offset >= searchStart; offset--)
        {
            if (!TryReadMovDwordImmediate(window, offset, out var baseRegister, out var displacement, out var immediate))
            {
                continue;
            }

            if (baseRegister == 4 &&
                displacement == stackOffset)
            {
                return immediate;
            }
        }

        throw new InvalidOperationException(
            $"Could not find constructor stack argument at [rsp + 0x{stackOffset:X}] for vital 0x{vitalOffset:X}.");
    }

    private static (byte[] Window, int LeaOffset, int TargetRegister, int VitalOffset) FindConstructorVitalLea(
        IMemory memory,
        IntPtr constructorShapeAddress,
        int expectedVitalOffset)
    {
        var window = memory.Read<byte>(Add(constructorShapeAddress, -3), 0x220);
        for (var offset = 0; offset <= window.Length - 7; offset++)
        {
            var rex = window[offset];
            if ((rex & 0xF0) != 0x40 ||
                window[offset + 1] != 0x8D)
            {
                continue;
            }

            var modRm = window[offset + 2];
            var mode = modRm >> 6;
            var rm = modRm & 0x07;
            if (mode != 2 ||
                rm != 6)
            {
                continue;
            }

            var vitalOffset = BitConverter.ToInt32(window, offset + 3);
            if (vitalOffset != expectedVitalOffset)
            {
                continue;
            }

            var targetRegister = ((modRm >> 3) & 0x07) + ((rex & 0x04) != 0 ? 8 : 0);
            return (window, offset, targetRegister, vitalOffset);
        }

        throw new InvalidOperationException($"Could not find constructor LEA for vital offset 0x{expectedVitalOffset:X}.");
    }

    private static bool TryReadMovDwordImmediate(
        byte[] bytes,
        int offset,
        out int baseRegister,
        out int displacement,
        out int immediate)
    {
        baseRegister = 0;
        displacement = 0;
        immediate = 0;

        var cursor = offset;
        if (cursor >= bytes.Length)
        {
            return false;
        }

        var rex = (byte)0;
        if ((bytes[cursor] & 0xF0) == 0x40)
        {
            rex = bytes[cursor++];
        }

        if (cursor + 2 >= bytes.Length ||
            bytes[cursor++] != 0xC7)
        {
            return false;
        }

        var modRm = bytes[cursor++];
        var mode = modRm >> 6;
        var opcodeExtension = (modRm >> 3) & 0x07;
        var rm = modRm & 0x07;
        if (opcodeExtension != 0)
        {
            return false;
        }

        if (rm == 4)
        {
            if (cursor >= bytes.Length)
            {
                return false;
            }

            var sib = bytes[cursor++];
            baseRegister = (sib & 0x07) + ((rex & 0x01) != 0 ? 8 : 0);
        }
        else
        {
            baseRegister = rm + ((rex & 0x01) != 0 ? 8 : 0);
        }

        switch (mode)
        {
            case 1:
                if (cursor + 1 + sizeof(int) > bytes.Length)
                {
                    return false;
                }

                displacement = unchecked((sbyte)bytes[cursor++]);
                break;
            case 2:
                if (cursor + sizeof(int) + sizeof(int) > bytes.Length)
                {
                    return false;
                }

                displacement = BitConverter.ToInt32(bytes, cursor);
                cursor += sizeof(int);
                break;
            default:
                return false;
        }

        if (cursor + sizeof(int) > bytes.Length)
        {
            return false;
        }

        immediate = BitConverter.ToInt32(bytes, cursor);
        return true;
    }

    private static IEnumerable<int> FindBytePatternMatches(byte[] buffer, string pattern)
    {
        var parsedPattern = ParseBytePattern(pattern);
        if (parsedPattern.Length == 0 ||
            parsedPattern.Length > buffer.Length)
        {
            yield break;
        }

        for (var offset = 0; offset <= buffer.Length - parsedPattern.Length; offset++)
        {
            var isMatch = true;
            for (var i = 0; i < parsedPattern.Length; i++)
            {
                var expected = parsedPattern[i];
                if (expected.HasValue &&
                    buffer[offset + i] != expected.Value)
                {
                    isMatch = false;
                    break;
                }
            }

            if (isMatch)
            {
                yield return offset;
            }
        }
    }

    private static byte?[] ParseBytePattern(string pattern)
    {
        return pattern
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(token => token is "?" or "??"
                ? (byte?)null
                : Convert.ToByte(token, 16))
            .ToArray();
    }

    private static IBytePattern[] SelectKeypointPatterns(params string[] names)
    {
        var requestedNames = new HashSet<string>(names, StringComparer.Ordinal);
        return Offsets.KeypointPatterns
            .Where(pattern => pattern.Name != null && requestedNames.Contains(pattern.Name))
            .ToArray();
    }

    private static ComponentLookupResolverLayout ReadComponentLookupResolverLayout(byte[] moduleBytes, int matchOffset)
    {
        const int returnedOffset = 0x10;
        var returnedAddressOffset = matchOffset + returnedOffset;
        return new ComponentLookupResolverLayout(
            EntityDetailsOffset: moduleBytes[returnedAddressOffset],
            EntityDetailsOffsetSecondRead: moduleBytes[returnedAddressOffset + 0x0E],
            EntityDetailsComponentLookupOffset: moduleBytes[returnedAddressOffset + 0x1C],
            ComponentNameAndIndexBucketOffset: moduleBytes[returnedAddressOffset + 0x2C],
            ComponentNameAndIndexBucketEndOffset: moduleBytes[returnedAddressOffset + 0x38],
            ComponentNameAndIndexIndexOffset: moduleBytes[returnedAddressOffset + 0x3E],
            ComponentListOffset: moduleBytes[returnedAddressOffset + 0x47]);
    }

    private readonly record struct ComponentLookupResolverLayout(
        int EntityDetailsOffset,
        int EntityDetailsOffsetSecondRead,
        int EntityDetailsComponentLookupOffset,
        int ComponentNameAndIndexBucketOffset,
        int ComponentNameAndIndexBucketEndOffset,
        int ComponentNameAndIndexIndexOffset,
        int ComponentListOffset);

    private static string ReadStdWString(IMemory memory, StdWString nativeContainer)
    {
        const int MaxAllowed = 1000;
        if (nativeContainer.Length <= 0 ||
            nativeContainer.Length > MaxAllowed ||
            nativeContainer.Capacity <= 0 ||
            nativeContainer.Capacity > MaxAllowed)
        {
            return string.Empty;
        }

        if (nativeContainer.Capacity <= 7)
        {
            var buffer = BitConverter.GetBytes(nativeContainer.Buffer.ToInt64());
            var value = Encoding.Unicode.GetString(buffer);
            buffer = BitConverter.GetBytes(nativeContainer.ReservedBytes.ToInt64());
            value += Encoding.Unicode.GetString(buffer);
            return nativeContainer.Length < value.Length ? value[..nativeContainer.Length] : string.Empty;
        }

        return Encoding.Unicode.GetString(memory.Read<byte>(nativeContainer.Buffer, nativeContainer.Length * 2));
    }
}
