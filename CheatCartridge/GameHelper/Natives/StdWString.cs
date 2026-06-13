using CheatCartridge.GameHelper.GameOffsets;

namespace CheatCartridge.GameHelper.Natives;

[FrameFormatType("StdWString")]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct StdWString
{
    [FrameFormatField("buffer_or_inline0")]
    [FrameFormatGenerated("poe-game-model.sha256-c5da3833", "2026-06-13T12:25:29.4645730+00:00", "StdWString.buffer_or_inline0; External buffer pointer or first inline UTF-16 characters; inline capacity limit is 7.")]
    public IntPtr Buffer;

    //// PlayerComponentName proves the current SSO shape:
    //// capacity <= 7 stores UTF-16 bytes inline starting at +0x00;
    //// capacity > 7 stores an external buffer pointer at +0x00.
    //// The production object model reads this struct directly; FF tooling can
    //// refresh these offsets when the native string layout changes.
    [FrameFormatField("inline1")]
    [FrameFormatGenerated("poe-game-model.sha256-c5da3833", "2026-06-13T12:25:29.4645730+00:00", "StdWString.inline1; Tail of inline UTF-16 storage.")]
    public IntPtr ReservedBytes;

    [FrameFormatField("length")]
    [FrameFormatGenerated("poe-game-model.sha256-c5da3833", "2026-06-13T12:25:29.4645730+00:00", "StdWString.length; UTF-16 character length.")]
    public int Length; // according to debugger this is long but for now int is working fine.
    public int PAD_14;

    [FrameFormatField("capacity")]
    [FrameFormatGenerated("poe-game-model.sha256-c5da3833", "2026-06-13T12:25:29.4645730+00:00", "StdWString.capacity; UTF-16 character capacity.")]
    public int Capacity; // according to debugger this is long but for now int is working fine.
    public int PAD_1C;

    public override string ToString()
    {
        return $"Buffer: {Buffer.ToInt64():X}, ReservedBytes: {ReservedBytes.ToInt64():X}, " +
               $"Length: {Length}, Capacity: {Capacity}";
    }
}
