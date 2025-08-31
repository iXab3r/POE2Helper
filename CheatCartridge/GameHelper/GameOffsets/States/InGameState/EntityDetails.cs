using CheatCartridge.GameHelper.Natives;

namespace CheatCartridge.GameHelper.GameOffsets.States.InGameState;

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct EntityDetails
{
    [FieldOffset(0x08)] public StdWString name;
    [FieldOffset(0x30)] public IntPtr ComponentLookUpPtr;
}