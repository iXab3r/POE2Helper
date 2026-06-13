using CheatCartridge.GameHelper.GameOffsets;

namespace CheatCartridge.GameHelper.Natives;

[FrameFormatType("StdVector")]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct StdVector
{
    [FrameFormatField("first")]
    [FrameFormatGenerated("poe-game-model.sha256-c5da3833", "2026-06-13T12:25:29.4645730+00:00", "StdVector.first; First element pointer.")]
    public IntPtr First;

    [FrameFormatField("last")]
    [FrameFormatGenerated("poe-game-model.sha256-c5da3833", "2026-06-13T12:25:29.4645730+00:00", "StdVector.last; One-past-last element pointer.")]
    public IntPtr Last;

    [FrameFormatField("end")]
    [FrameFormatGenerated("poe-game-model.sha256-c5da3833", "2026-06-13T12:25:29.4645730+00:00", "StdVector.end; One-past-capacity pointer.")]
    public IntPtr End;

    /// <summary>
    ///     Counts the number of elements in the StdVector.
    /// </summary>
    /// <param name="elementSize">Number of bytes in 1 element.</param>
    /// <returns></returns>
    public long TotalElements(int elementSize)
    {
        return (Last.ToInt64() - First.ToInt64()) / elementSize;
    }

    public override string ToString()
    {
        return $"First: {First.ToInt64():X} - " +
               $"Last: {Last.ToInt64():X} - " +
               $"Size (bytes): {TotalElements(1)}";
    }
}
