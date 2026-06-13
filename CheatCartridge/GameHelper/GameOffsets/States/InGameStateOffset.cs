using CheatCartridge.GameHelper.GameOffsets;

namespace CheatCartridge.GameHelper.GameOffsets.States;

[FrameFormatType("InGameState")]
[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct InGameStateOffset
{
    /// <summary>
    /// Direct InGameState zone-switch counter.
    ///
    /// Runtime confirmation for build sha256-c5da3833:
    ///
    ///     inGameState               = 0x0000033E22E42D10
    ///     ZoneSwitchCounter address = 0x0000033E22E4327C
    ///     delta                     = 0x56C
    ///
    /// The direct field is reset in FUN_140FAC260 and incremented in two
    /// FUN_140FB24A0 branches, confirming this is counter-like and not just a
    /// boolean flag.
    ///
    /// The FUN_140FAF2F0 pattern is a different fact: it reads a nested pointer
    /// at InGameState + 0x368 and then checks a byte at nested + 0x56C. Same
    /// displacement, different owner. Keep those separate.
    /// </summary>
    [FrameFormatField("zone_switch_counter")]
    [FrameFormatGenerated("poe-game-model.sha256-c5da3833", "2026-06-13T12:25:29.4645730+00:00", "InGameState.zone_switch_counter; Zone-transition counter.")]
    [FieldOffset(0x56C)] public int ZoneSwitchCounter;

    /// <summary>
    /// Right after this ptr there is some "ticking" number (not increasing, just oscillating 5000-15000)
    /// </summary>
    [FrameFormatField("area_instance_data")]
    [FrameFormatGenerated("poe-game-model.sha256-c5da3833", "2026-06-13T12:25:29.4645730+00:00", "InGameState.area_instance_data; Current area-instance data pointer.")]
    [FieldOffset(0x290)] public IntPtr AreaInstanceData;

    /// <summary>
    /// Increasing integer timer maintained by the InGameState tick/update path.
    ///
    /// Build sha256-c5da3833 evidence:
    ///
    ///     FUN_140FAD6E0 accumulates frame delta as a double at +0x408,
    ///     multiplies it by a timing constant, then writes:
    ///
    ///         *(int*)(InGameState + 0x400) = convertedElapsedValue
    ///
    /// Runtime confirmation after a client restart:
    ///
    ///     InGameState  = 0x0000043C14E62A10
    ///     MsElapsed    = 0x0000043C14E62E10
    ///     delta        = 0x400
    ///
    ///     Two reads changed the first dword from 0x00023A61 to 0x00024267.
    ///
    /// This used to be typed as IntPtr, but both live bytes and static writer
    /// evidence show it is a 32-bit value.
    /// </summary>
    [FrameFormatField("ms_elapsed")]
    [FrameFormatGenerated("poe-game-model.sha256-c5da3833", "2026-06-13T12:25:29.4645730+00:00", "InGameState.ms_elapsed; Increasing in-world elapsed timer.")]
    [FieldOffset(0x400)] public int MsElapsed;
}
