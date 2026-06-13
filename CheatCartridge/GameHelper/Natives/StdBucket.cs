using CheatCartridge.GameHelper.GameOffsets;

namespace CheatCartridge.GameHelper.Natives;

/// <summary>
///     Observed hash/bucket-like container used by the component lookup path.
///     The statically proven production dependency is the embedded StdVector at
///     +0x00. FUN_140163120 also reads capacity-like metadata, but
///     CheatCartridge does not depend on that field for component enumeration.
/// </summary>
[FrameFormatType("StdBucket")]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct StdBucket
{
    [FrameFormatField("data")]
    [FrameFormatGenerated("poe-game-model.sha256-c5da3833", "2026-06-13T12:25:29.4645730+00:00", "StdBucket.data; Vector backing the bucket payload.")]
    public StdVector Data; // ComponentArrayStructure

    [FrameFormatField("unknown_ptr")]
    [FrameFormatGenerated("poe-game-model.sha256-c5da3833", "2026-06-13T12:25:29.4645730+00:00", "StdBucket.unknown_ptr; Unclassified bucket pointer.")]
    public IntPtr UnknownPtr; // todo: figure out what this pointer store

    [FrameFormatField("capacity_minus_one")]
    [FrameFormatGenerated("poe-game-model.sha256-c5da3833", "2026-06-13T12:25:29.4645730+00:00", "StdBucket.capacity_minus_one; Bucket capacity mask/capacity-minus-one value.")]
    public int Capacity; // actually, it's Capacity number - 1. // given that the Data is StdVector, we don't need this
    public int PAD_0x24; // byte + padd

    [FrameFormatField("unknown1")]
    [FrameFormatGenerated("poe-game-model.sha256-c5da3833", "2026-06-13T12:25:29.4645730+00:00", "StdBucket.unknown1; Unclassified bucket scalar.")]
    public int Unknown1;
    public int PAD_0x2C;

    [FrameFormatField("unknown2")]
    [FrameFormatGenerated("poe-game-model.sha256-c5da3833", "2026-06-13T12:25:29.4645730+00:00", "StdBucket.unknown2; Unclassified bucket scalar.")]
    public int Unknown2;

    [FrameFormatField("unknown3")]
    [FrameFormatGenerated("poe-game-model.sha256-c5da3833", "2026-06-13T12:25:29.4645730+00:00", "StdBucket.unknown3; Unclassified bucket scalar.")]
    public int Unknown3;
}
