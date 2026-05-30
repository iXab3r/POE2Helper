using CheatCartridge.GameHelper.Natives;

namespace CheatCartridge.GameHelper.GameOffsets.Objects.Components;

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct PlayerOffsets
{
    [FieldOffset(0x000)] public ComponentHeader Header;
    
    /// <summary>
    /// Some oscillating value, 0.07 - 0.30, ping?
    /// </summary>
    [FieldOffset(0x98)] public float UnknownNumber1;
    
    [FieldOffset(0x1B0)] public StdWString Name;
}