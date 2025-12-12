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
    /// 0xB4 -> 0xBC (+8) -> 0xC4 (+8)
    /// </summary>
    [FieldOffset(0x0C4)] public byte CurrentAreaLevel;
    
    /// <summary>
    /// Usually has quite high entrhtropy, e.g. 1494246552
    /// 0xF4 -> 0xFC (+8) -> 0x104 (+8)
    /// </summary>
    [FieldOffset(0x104)] public uint CurrentAreaHash;
    
    /// <summary>
    /// Before this ptr there are 28 zeroes
    /// 0xA00 -> 0xA08 (+8) -> 0xA10 (+8)
    /// </summary>
    [FieldOffset(0xA10)] public StdVector LocalPlayers;
    
    /// <summary>
    /// Some oscillating value, 0.07 - 0.30, ping?
    /// 0xAB8 -> 0xAC0 (+8)
    /// </summary>
    [FieldOffset(0xAC0)] public float UnknownNumber1;
    
    /// <summary>
    /// Pointer to some vtable
    /// 0xB38 -> 0xB40 (+8)
    /// </summary>
    [FieldOffset(0xB38)] public IntPtr UnknownVtablePtr; 
    
    /// <summary>
    /// In Clearfell at tp ~30-50
    /// 0xB58 -> 0xB60 (+8) -> 0xB68 (+8)
    /// </summary>
    [FieldOffset(0xB68)] public uint EntitiesCount;
}