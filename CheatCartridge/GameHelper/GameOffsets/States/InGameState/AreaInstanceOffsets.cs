using CheatCartridge.GameHelper.Natives;
using CheatCartridge.GameHelper.GameOffsets;

namespace CheatCartridge.GameHelper.GameOffsets.States.InGameState;

[FrameFormatType("AreaInstance")]
[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct AreaInstanceOffsets
{
    /// <summary>
    /// == Monster Level, Clearfell = 2
    /// 0xB4 -> 0xBC (+8) -> 0xC4 (+8)
    /// </summary>
    [FrameFormatField("current_area_level")]
    [FrameFormatGenerated("poe-game-model.sha256-1abda874", "2026-06-26T01:51:27.2215998+00:00", "AreaInstance.current_area_level; Monster/area level.")]
    [FieldOffset(0x0C4)] public byte CurrentAreaLevel;

    /// <summary>
    /// Usually has quite high entrhtropy, e.g. 1494246552
    /// 0xF4 -> 0xFC (+8) -> 0x104 (+8) => 0x11C (+0x18)
    /// </summary>
    [FrameFormatField("current_area_hash")]
    [FrameFormatGenerated("poe-game-model.sha256-1abda874", "2026-06-26T01:51:27.2215998+00:00", "AreaInstance.current_area_hash; Hash of the currently loaded area instance.")]
    [FieldOffset(0x11C)] public uint CurrentAreaHash;

    /// <summary>
    /// Before this ptr there are 28 zeroes
    /// Latest observed slide:
    /// 0x588 -> 0x5A0 (+0x18) -> 0x5A8 (+8)
    /// </summary>
    [FrameFormatField("local_players")]
    [FrameFormatGenerated("poe-game-model.sha256-1abda874", "2026-06-26T01:51:27.2215998+00:00", "AreaInstance.local_players; Vector of local player entity pointers.")]
    [FieldOffset(0x5A8)] public StdVector LocalPlayers;

    /// <summary>
    /// Count for the AreaInstance-owned entity tree.
    ///
    /// Latest observed slide:
    /// 0x6C8 -> 0x6E0 (+0x18) -> 0x6E8 (+8)
    ///
    /// See docs/PoE/RE/builds/sha256-c5da3833/PathOfExileSteam/game-states/AreaInstanceScalars.evidence.md.
    /// </summary>
    [FrameFormatField("entities_count")]
    [FrameFormatGenerated("poe-game-model.sha256-1abda874", "2026-06-26T01:51:27.2215998+00:00", "AreaInstance.entities_count; Entity-tree count stored after the root pointer.")]
    [FieldOffset(0x6E8)] public uint EntitiesCount;
}
