namespace CheatCartridge.GameHelper.GameOffsets.Objects.Components;

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct ComponentHeader
{
    [FieldOffset(0x0000)] public IntPtr StaticPtr;
    [FieldOffset(0x0008)] public IntPtr EntityPtr;
}