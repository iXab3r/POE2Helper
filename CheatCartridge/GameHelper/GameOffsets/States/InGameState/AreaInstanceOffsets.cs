using CheatCartridge.GameHelper.Natives;

namespace CheatCartridge.GameHelper.GameOffsets.States.InGameState;

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct AreaInstanceOffsets
{
    /// <summary>
    /// Right after VTable there are also 3 another pointers  
    /// </summary>
    [FieldOffset(0x000)] public IntPtr Vtable;
    
    /// <summary>
    /// == Monster Level, Clearfell = 2
    /// 0xB4 -> 0xBC (+8)
    /// </summary>
    [FieldOffset(0x0BC)] public byte CurrentAreaLevel;
    
    /// <summary>
    /// Usually has quite high entrhtropy, e.g. 1494246552
    /// 0xF4 -> 0xFC (+8)
    /// </summary>
    [FieldOffset(0x0FC)] public uint CurrentAreaHash;
    
    /// <summary>
    /// Before this ptr there are 28 zeroes
    /// 0xA00 -> 0xA08 (+8)
    /// </summary>
    [FieldOffset(0xA08)] public StdVector LocalPlayers;
    
    /// <summary>
    /// Some oscillating value, 0.07 - 0.30, ping?
    /// </summary>
    [FieldOffset(0xAB8)] public float UnknownNumber1;
    
    /// <summary>
    /// Pointer to some vtable
    /// </summary>
    [FieldOffset(0xB38)] public IntPtr UnknownVtablePtr; 
    
    /// <summary>
    /// In Clearfell at tp ~30-50
    /// 0xB58 -> 0xB60 (+8)
    /// </summary>
    [FieldOffset(0xB60)] public uint EntitiesCount;
}