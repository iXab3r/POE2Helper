using CheatCartridge.GameHelper.Natives;

namespace CheatCartridge.GameHelper.GameOffsets.States.InGameState;

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct AreaInstanceOffsets
{
    /// <summary>
    /// == Monster Level, Clearfell = 2
    /// </summary>
    [FieldOffset(0x0B4)] public byte CurrentAreaLevel;
    /// <summary>
    /// Usually has quite high entrhtropy, e.g. 1494246552
    /// </summary>
    [FieldOffset(0x0F4)] public uint CurrentAreaHash;
    
    /// <summary>
    /// In Clearfell at tp ~30-50
    /// </summary>
    [FieldOffset(0xB58)] public uint EntitiesCount;
    
    /// <summary>
    /// Before this ptr there are 2-3 zeroes
    /// </summary>
    [FieldOffset(0xA00)] public StdVector LocalPlayers;
}