using CheatCartridge.GameHelper.Natives;
using CheatCartridge.GameHelper.GameOffsets;

namespace CheatCartridge.GameHelper.GameOffsets.States.InGameState;

[FrameFormatType("ComponentLookup")]
[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct ComponentLookUpStruct
{
    [FrameFormatField("name_and_index_bucket")]
    [FrameFormatGenerated("poe-game-model.sha256-1abda874", "2026-06-26T01:51:27.2215998+00:00", "ComponentLookup.name_and_index_bucket; Bucket/vector of component-name/index entries.")]
    [FieldOffset(0x28)] public StdBucket ComponentsNameAndIndex;
}
