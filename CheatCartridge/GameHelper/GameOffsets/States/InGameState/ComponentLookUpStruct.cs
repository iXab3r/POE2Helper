using CheatCartridge.GameHelper.Natives;

namespace CheatCartridge.GameHelper.GameOffsets.States.InGameState;

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct ComponentLookUpStruct
{
    [FieldOffset(0x00)] public IntPtr Unknown0;
    [FieldOffset(0x08)] public IntPtr Unknown1;
    [FieldOffset(0x10)] public StdVector Unknown2;
    [FieldOffset(0x28)] public StdBucket ComponentsNameAndIndex;
}