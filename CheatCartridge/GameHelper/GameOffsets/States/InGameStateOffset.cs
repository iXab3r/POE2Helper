namespace CheatCartridge.GameHelper.GameOffsets.States;

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct InGameStateOffset
{
    [FieldOffset(0x298)] public IntPtr AreaInstanceData;
    [FieldOffset(0x2F8)] public IntPtr WorldData;
    [FieldOffset(0x648)] public IntPtr UiRootPtr;
    [FieldOffset(0xC20)] public IntPtr IngameUi;
}