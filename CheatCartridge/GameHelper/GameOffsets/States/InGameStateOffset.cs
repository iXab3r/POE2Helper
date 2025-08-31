namespace CheatCartridge.GameHelper.GameOffsets.States;

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct InGameStateOffset
{
    [FieldOffset(0x46C)] public int ZoneSwitchCounter;
    
    /// <summary>
    /// Right after this ptr there is some "ticking" number (not increasing, just oscillating 5000-15000)
    /// </summary>
    [FieldOffset(0x290)] public IntPtr AreaInstanceData;
}