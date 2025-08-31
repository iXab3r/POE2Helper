using CheatCartridge.GameHelper.Natives;

namespace CheatCartridge.GameHelper.GameOffsets;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct GameStateBuffer
{
    public const int TOTAL_STATES = 12;

    private fixed byte _data[TOTAL_STATES * 16];

    /// <summary>
    /// Accesses the elements of the buffer by index.
    /// </summary>
    /// <param name="index">The index of the element to access.</param>
    /// <returns>A reference to the element at the specified index.</returns>
    public ref StdTuple2D<IntPtr> this[int index]
    {
        get
        {
            if (index < 0 || index >= TOTAL_STATES)
            {
                throw new IndexOutOfRangeException($"Index must be between 0 and {TOTAL_STATES - 1}.");
            }

            fixed (byte* ptr = _data)
            {
                return ref ((StdTuple2D<IntPtr>*)ptr)[index];
            }
        }
    }
}