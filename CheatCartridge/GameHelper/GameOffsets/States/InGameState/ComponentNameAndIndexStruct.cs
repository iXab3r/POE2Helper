namespace CheatCartridge.GameHelper.GameOffsets.States.InGameState;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct ComponentNameAndIndexStruct
{
    public IntPtr NamePtr;
    public int Index;
    public int PAD_0xC;
}