using CheatCartridge.GameHelper.GameOffsets;

namespace CheatCartridge.GameHelper.GameOffsets.Objects.Components;

[FrameFormatType("Life")]
[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct LifeOffset
{
    [FieldOffset(0x000)] public ComponentHeader Header;

    [FrameFormatField("health")]
    [FrameFormatGenerated("poe-game-model.sha256-1abda874", "2026-06-26T01:51:27.2215998+00:00", "Life.health; Health vital block.")]
    [FieldOffset(0x1A8)] public VitalStruct Health;

    [FrameFormatField("mana")]
    [FrameFormatGenerated("poe-game-model.sha256-1abda874", "2026-06-26T01:51:27.2215998+00:00", "Life.mana; Mana vital block.")]
    [FieldOffset(0x200)] public VitalStruct Mana;

    [FrameFormatField("energy_shield")]
    [FrameFormatGenerated("poe-game-model.sha256-1abda874", "2026-06-26T01:51:27.2215998+00:00", "Life.energy_shield; Energy shield vital block.")]
    [FieldOffset(0x240)] public VitalStruct EnergyShield;
}
