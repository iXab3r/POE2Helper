using CheatCartridge.GameHelper.GameOffsets;

namespace CheatCartridge.GameHelper.GameOffsets.Objects.Components;

[FrameFormatType("ComponentHeader")]
[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct ComponentHeader
{
    /// <summary>
    /// Component vtable pointer.
    ///
    /// Build sha256-c5da3833 evidence:
    ///     Player component vtable slot +0x68 points to FUN_141D267F0.
    ///
    /// See:
    ///     docs/PoE/RE/builds/sha256-c5da3833/PathOfExileSteam/game-states/ComponentHeader.evidence.md
    /// </summary>
    [FieldOffset(0x0000)] public IntPtr StaticPtr;

    /// <summary>
    /// Owner entity pointer.
    ///
    /// Build sha256-c5da3833 evidence:
    ///     FUN_141D267F0 reads [PlayerComponent +0x08] before deriving
    ///     owner-backed character class/ascendancy data.
    ///
    /// See:
    ///     docs/PoE/RE/builds/sha256-c5da3833/PathOfExileSteam/game-states/ComponentHeader.evidence.md
    /// </summary>
    [FrameFormatField("owner_entity")]
    [FrameFormatGenerated("poe-game-model.sha256-c5da3833", "2026-06-13T12:25:29.4645730+00:00", "ComponentHeader.owner_entity; Owning entity pointer.")]
    [FieldOffset(0x0008)] public IntPtr EntityPtr;
}
