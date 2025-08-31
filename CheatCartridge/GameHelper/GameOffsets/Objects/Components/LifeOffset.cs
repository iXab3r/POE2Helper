namespace CheatCartridge.GameHelper.GameOffsets.Objects.Components;

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct LifeOffset
{
    [FieldOffset(0x000)] public ComponentHeader Header;
    [FieldOffset(0x1B0)] public VitalStruct Health;
    [FieldOffset(0x200)] public VitalStruct Mana;
    [FieldOffset(0x238)] public VitalStruct EnergyShield;
}