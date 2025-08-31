using CheatCartridge.GameHelper.Natives;

namespace CheatCartridge.GameHelper.GameOffsets.States.InGameState;

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct ItemStruct
{
    [FieldOffset(0x00)] public IntPtr VTablePtr;
    [FieldOffset(0x08)] public IntPtr EntityDetailsPtr;
    [FieldOffset(0x10)] public StdVector ComponentListPtr;
}