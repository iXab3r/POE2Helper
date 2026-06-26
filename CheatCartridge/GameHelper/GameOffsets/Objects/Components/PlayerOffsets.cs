using CheatCartridge.GameHelper.Natives;
using CheatCartridge.GameHelper.GameOffsets;

namespace CheatCartridge.GameHelper.GameOffsets.Objects.Components;

[FrameFormatType("Player")]
[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct PlayerOffsets
{
    [FieldOffset(0x000)] public ComponentHeader Header;

    [FrameFormatField("name")]
    [FrameFormatGenerated("poe-game-model.sha256-1abda874", "2026-06-26T01:51:27.2215998+00:00", "Player.name; Character display name.")]
    [FieldOffset(0x1B0)] public StdWString Name;
}
