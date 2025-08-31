using CheatCartridge.GameHelper.Natives;

namespace CheatCartridge.GameHelper.GameOffsets.States.InGameState;

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct AreaInstanceOffsets
{
    [FieldOffset(0x088)] public byte CurrentAreaLevel;
    [FieldOffset(0x0AC)] public uint CurrentAreaHash;
    [FieldOffset(0x9F8)] public StdVector LocalPlayers;
}