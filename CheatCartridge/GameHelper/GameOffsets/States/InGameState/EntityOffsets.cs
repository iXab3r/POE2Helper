namespace CheatCartridge.GameHelper.GameOffsets.States.InGameState;

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct EntityOffsets
{
    [FieldOffset(0x00)] public ItemStruct ItemBase;
    
    /// <summary>
    /// For player - quite small value, something around 200-1000
    /// </summary>
    [FieldOffset(0x80)] public uint Id;
    
    /// <summary>
    /// Still works for 0.3
    /// </summary>
    [FieldOffset(0x84)] public byte IsValid; // 0x0C = Valid, 0x03 = Invalid
}