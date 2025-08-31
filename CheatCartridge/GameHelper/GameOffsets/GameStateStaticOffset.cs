namespace CheatCartridge.GameHelper.GameOffsets;

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct GameStateStaticOffset
{
    [FieldOffset(0x00)] public IntPtr GameState;
}