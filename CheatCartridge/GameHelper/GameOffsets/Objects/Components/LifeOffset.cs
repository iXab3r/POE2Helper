namespace CheatCartridge.GameHelper.GameOffsets.Objects.Components;

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct LifeOffset
{
    [FieldOffset(0x000)] public ComponentHeader Header;
    [FieldOffset(0x1A8)] public VitalStruct Health;
    [FieldOffset(0x200)] public VitalStruct Mana;
    [FieldOffset(0x240)] public VitalStruct EnergyShield;
}