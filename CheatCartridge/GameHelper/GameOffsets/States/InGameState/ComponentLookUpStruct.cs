using CheatCartridge.GameHelper.Natives;
using CheatCartridge.GameHelper.GameOffsets;

namespace CheatCartridge.GameHelper.GameOffsets.States.InGameState;

[FrameFormatType("ComponentLookup")]
[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct ComponentLookUpStruct
{
    [FrameFormatField("name_and_index_bucket")]
    [FrameFormatGenerated("poe-game-model.sha256-c5da3833", "2026-06-13T12:25:29.4645730+00:00", "ComponentLookup.name_and_index_bucket; Bucket/vector of component-name/index entries.")]
    [FieldOffset(0x28)] public StdBucket ComponentsNameAndIndex;
}
