using CheatCartridge.GameHelper.GameOffsets;

namespace CheatCartridge.GameHelper.GameOffsets.States.InGameState;

[FrameFormatType("Entity")]
[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct EntityOffsets
{
    [FieldOffset(0x00)] public ItemStruct ItemBase;
    
    /// <summary>
    /// Area-local entity id.
    ///
    /// Build sha256-c5da3833 evidence:
    ///
    ///     FUN_14163B7A0 walks the AreaInstance entity tree, reads each node's
    ///     entity pointer from node +0x28, then compares:
    ///
    ///         *(uint*)(Entity + 0x88) < 0x40000000
    ///
    ///     Offsets.KeypointNames.EntityIdentityFilter recovers this displacement
    ///     from the same compact filter that also proves the status byte at +0x8C.
    /// </summary>
    [FrameFormatField("id")]
    [FrameFormatGenerated("poe-game-model.sha256-c5da3833", "2026-06-13T12:25:29.4645730+00:00", "Entity.id; Area-local entity id.")]
    [FieldOffset(0x88)] public uint Id;
    
    /// <summary>
    /// Entity status/validity byte.
    ///
    /// Build sha256-c5da3833 evidence:
    ///
    ///     FUN_14163B7A0 walks the AreaInstance entity tree and tests:
    ///
    ///         (*(byte*)(Entity + 0x8C) & 1) == 0
    ///
    ///     before accepting an entity for further processing. The test-only
    ///     runtime resolver recovers this mask from the same instruction and combines it with the
    ///     adjacent id and active-flag predicates instead of treating one exact
    ///     live status byte as the validity rule.
    /// </summary>
    [FrameFormatField("status")]
    [FrameFormatGenerated("poe-game-model.sha256-c5da3833", "2026-06-13T12:25:29.4645730+00:00", "Entity.status; Entity status byte used by the identity filter.")]
    [FieldOffset(0x8C)] public byte IsValid;
}
