using CheatCartridge.GameHelper.GameOffsets;

namespace CheatCartridge.GameHelper.GameOffsets.States.InGameState;

[FrameFormatType("ComponentNameAndIndex")]
[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct ComponentNameAndIndexStruct
{
    [FrameFormatField("name")]
    [FrameFormatGenerated("poe-game-model.sha256-1abda874", "2026-06-26T01:51:27.2215998+00:00", "ComponentNameAndIndex.name; Component name C-string pointer.")]
    [FieldOffset(0x00)] public IntPtr NamePtr;

    [FrameFormatField("index")]
    [FrameFormatGenerated("poe-game-model.sha256-1abda874", "2026-06-26T01:51:27.2215998+00:00", "ComponentNameAndIndex.index; Index into the owning entity component list.")]
    [FieldOffset(0x08)] public int Index;

    [FieldOffset(0x0C)] public int PAD_0xC;
}
