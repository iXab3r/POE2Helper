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
    /// 0xA00 -> 0xA08 (+8) -> 0xA10 (+8)
    /// </summary>
    [FrameFormatField("local_players")]
    [FrameFormatGenerated("poe-game-model.sha256-1abda874", "2026-06-26T01:51:27.2215998+00:00", "AreaInstance.local_players; Vector of local player entity pointers.")]
    [FieldOffset(0x5A0)] public StdVector LocalPlayers;

    /// <summary>
    /// Count for the AreaInstance-owned entity tree.
    ///
    /// RE evidence for build sha256-c5da3833:
    /// FUN_14206E780 initializes the first entity tree container at +0x6B0.
    /// The constructor sets the tree root/sentinel pointer at +0x6C0 and clears
    /// the adjacent qword count at +0x6C8. Offsets.KeypointNames.AreaInstanceEntityTreeRoot
    /// recovers +0x6C0 from code, then tests derive this field as +0x6C0 + 8.
    ///
    /// See docs/PoE/RE/builds/sha256-c5da3833/PathOfExileSteam/game-states/AreaInstanceScalars.evidence.md.
    /// </summary>
    [FrameFormatField("entities_count")]
    [FrameFormatGenerated("poe-game-model.sha256-1abda874", "2026-06-26T01:51:27.2215998+00:00", "AreaInstance.entities_count; Entity-tree count stored after the root pointer.")]
    [FieldOffset(0x6E0)] public uint EntitiesCount;
}
