namespace CheatCartridge.GameHelper.GameOffsets.States.InGameState;

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct EntityOffsets
{
    [FieldOffset(0x00)] public ItemStruct ItemBase;
    [FieldOffset(0x80)] public uint Id;
    [FieldOffset(0x84)] public byte IsValid; // 0x0C = Valid, 0x03 = Invalid
}