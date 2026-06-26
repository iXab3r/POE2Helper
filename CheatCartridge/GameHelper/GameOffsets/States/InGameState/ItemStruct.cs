using CheatCartridge.GameHelper.Natives;
using CheatCartridge.GameHelper.GameOffsets;

namespace CheatCartridge.GameHelper.GameOffsets.States.InGameState;

[FrameFormatType("Entity")]
[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct ItemStruct
{
    [FrameFormatField("details")]
    [FrameFormatGenerated("poe-game-model.sha256-1abda874", "2026-06-26T01:51:27.2215998+00:00", "Entity.details; Pointer to entity metadata/details.")]
    [FieldOffset(0x08)] public IntPtr EntityDetailsPtr;

    [FrameFormatField("component_list")]
    [FrameFormatGenerated("poe-game-model.sha256-1abda874", "2026-06-26T01:51:27.2215998+00:00", "Entity.component_list; Bucket/vector of component header pointers.")]
    [FieldOffset(0x10)] public StdVector ComponentListPtr;
}
