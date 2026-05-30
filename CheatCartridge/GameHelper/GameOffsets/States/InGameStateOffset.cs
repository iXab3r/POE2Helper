namespace CheatCartridge.GameHelper.GameOffsets.States;

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct InGameStateOffset
{
    /// <summary>
    /// 0x46C => 0x56C (+100)
    /// </summary>
    [FieldOffset(0x56C)] public int ZoneSwitchCounter;
    
    /// <summary>
    /// Right after this ptr there is some "ticking" number (not increasing, just oscillating 5000-15000)
    /// </summary>
    [FieldOffset(0x290)] public IntPtr AreaInstanceData;
    
    /// <summary>
    /// Increasing number, probably count of ms elapsed since computer/client started
    /// 0x328 => 0x400 (+D8)
    /// </summary>
    [FieldOffset(0x400)] public IntPtr MsElapsed;
    
    /// <summary>
    /// Contains ptr to Unicode string containing login server host.
    /// e.g. L'fra.login.pathofexile2.com'
    ///
    /// Right afterwards there a s SECOND ptr to the same string (by ptr)
    /// </summary>
    [FieldOffset(0x530)] public IntPtr LoginServerHostPtr;
}