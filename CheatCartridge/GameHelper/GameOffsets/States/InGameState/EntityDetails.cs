using CheatCartridge.GameHelper.Natives;
using CheatCartridge.GameHelper.GameOffsets;

namespace CheatCartridge.GameHelper.GameOffsets.States.InGameState;

[FrameFormatType("EntityDetails")]
[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct EntityDetails
{
    /// <summary>
    /// Metadata path/name string.
    ///
    /// For the player this is something like:
    ///
    ///     Metadata/Characters/Dex/DexFourb
    ///
    /// Build sha256-c5da3833 evidence:
    ///
    ///     The live player Entity vtable slot +0x10 points at VA 0x141C7A6D0:
    ///
    ///         MOV RAX, qword ptr [RCX + 0x08]
    ///         ADD RAX, 0x08
    ///         RET
    ///
    ///     Offsets.KeypointNames.EntityDetailsName recovers the second 0x08,
    ///     proving this StdWString starts at EntityDetails +0x08.
    /// </summary>
    [FrameFormatField("name")]
    [FrameFormatGenerated("poe-game-model.sha256-c5da3833", "2026-06-13T12:25:29.4645730+00:00", "EntityDetails.name; Entity metadata path/name.")]
    [FieldOffset(0x08)] public StdWString name;
    
    /// <summary>
    /// The very first 
    /// </summary>
    [FrameFormatField("component_lookup")]
    [FrameFormatGenerated("poe-game-model.sha256-c5da3833", "2026-06-13T12:25:29.4645730+00:00", "EntityDetails.component_lookup; Lookup table mapping component names to component indices.")]
    [FieldOffset(0x28)] public IntPtr ComponentLookUpPtr;
}
