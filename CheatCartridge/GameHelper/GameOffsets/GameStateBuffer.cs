using CheatCartridge.GameHelper.Natives;
using CheatCartridge.GameHelper.RemoteEnums;

namespace CheatCartridge.GameHelper.GameOffsets;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct GameStateBuffer
{
    public const int TOTAL_STATES = 12;

    private fixed byte _data[TOTAL_STATES * 16];

    public GameStateTypes GetStateType(IntPtr statePtr)
    {
        foreach (var (stateType, statePtr2) in Enumerate())
        {
            if (statePtr2.X == statePtr)
            {
                return stateType;
            }
        }
        return GameStateTypes.AreaLoadingState;
    }

    public IEnumerable<(GameStateTypes StateType, StdTuple2D<IntPtr> StatePtr)> Enumerate()
    {
        for (var i = 0; i < TOTAL_STATES; i++)
        {
            yield return ((GameStateTypes) i, this[i]);
        }
    }
        
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