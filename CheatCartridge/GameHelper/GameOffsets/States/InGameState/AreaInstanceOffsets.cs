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
    /// 0xB4 -> 0xBC (+8) -> 0xC4 (+8) -> 0xBC (-8)
    /// </summary>
    [FieldOffset(0x0BC)] public byte CurrentAreaLevel;
    
    /// <summary>
    /// Usually has quite high entrhtropy, e.g. 1494246552
    /// 0xF4 -> 0xFC (+8) -> 0x104 (+8) => 0x11C (+0x18) -> 0x114 (-8)
    /// </summary>
    [FieldOffset(0x114)] public uint CurrentAreaHash;
    
    /// <summary>
    /// The ServerData pointer is immediately before this vector at +0x5B0.
    /// 0x588 -> 0x5A0 (+0x18) -> 0x5A8 (+8) -> 0x5B8 (+0x10)
    /// </summary>
    [FieldOffset(0x5B8)] public StdVector LocalPlayers;
    
    /// <summary>
    /// Some oscillating value, 0.07 - 0.30, ping?
    /// 0xAB8 -> 0xAC0 (+8) -> 0x638 (-0x488)
    /// </summary>
    [FieldOffset(0x638)] public float UnknownNumber1;
    
    /// <summary>
    /// Pointer to some vtable
    /// 0xB38 -> 0xB40 (+8)
    /// </summary>
    [FieldOffset(0xB38)] public IntPtr UnknownVtablePtr; 
    
    /// <summary>
    /// In Clearfell at tp ~30-50
    /// 0xB58 -> 0xB60 (+8) -> 0xB68 (+8) -> 0x6E8 (-0x480) -> 0x6F8 (+0x10)
    /// </summary>
    [FieldOffset(0x6F8)] public uint EntitiesCount;
}
