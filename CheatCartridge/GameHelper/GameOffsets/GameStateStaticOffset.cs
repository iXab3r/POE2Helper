namespace CheatCartridge.GameHelper.GameOffsets;

[FrameFormatType("GameStateStaticWrapper")]
[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct GameStateStaticOffset
{
    /// <summary>
    /// First pointer in the shared wrapper returned by FUN_14010C350.
    ///
    /// Build sha256-c5da3833 evidence:
    ///
    ///     FUN_14010C350 copies DAT_1444E9EB8 into wrapper +0x00 and
    ///     DAT_1444E9EC0 into wrapper +0x08.
    ///
    /// The +0x08 sidecar/control pointer is not currently modeled by
    /// production code.
    ///
    /// See:
    ///     docs/PoE/RE/builds/sha256-c5da3833/PathOfExileSteam/game-states/GameStatesSingleton.evidence.md
    /// </summary>
    [FrameFormatField("owner")]
    [FrameFormatGenerated("poe-game-model.sha256-c5da3833", "2026-06-13T12:25:29.4645730+00:00", "GameStateStaticWrapper.owner; Pointer to the GameStates owner object.")]
    [FieldOffset(0x00)] public IntPtr GameState;
}
