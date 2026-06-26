using CheatCartridge.GameHelper.Natives;

namespace CheatCartridge.GameHelper.GameOffsets;

[FrameFormatType("GameStates")]
[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct GameStateOffset
{
    /// <summary>
    /// Source current-state vector owned by the GameStates singleton.
    ///
    /// Build sha256-c5da3833 evidence:
    ///
    ///     FUN_1415B0360 checks the dirty flag at GameStates +0x40, computes
    ///     the source vector count from [GameStates +0x10] - [GameStates +0x08],
    ///     and copies 0x10-byte entries into a dispatch vector at +0x20.
    ///
    ///     the FF/runtime-layout tests reads the latest entry from
    ///     this source vector using the recovered 0x10-byte entry shape, so
    ///     +0x08 is the managed CurrentStatePtr field.
    ///
    /// See:
    ///     docs/PoE/RE/builds/sha256-c5da3833/PathOfExileSteam/game-states/CurrentStateVector.evidence.md
    /// </summary>
    [FrameFormatField("current_state_vector")]
    [FrameFormatGenerated("poe-game-model.sha256-1abda874", "2026-06-26T01:51:27.2215998+00:00", "GameStates.current_state_vector; Source vector for current game-state entries.")]
    [FieldOffset(0x08)] public StdVector CurrentStatePtr;

    [FrameFormatField("state_table")]
    [FrameFormatGenerated("poe-game-model.sha256-1abda874", "2026-06-26T01:51:27.2215998+00:00", "GameStates.state_table; Fixed table indexed by GameStateTypes.")]
    [FieldOffset(0x48)] public GameStateBuffer States;
}
