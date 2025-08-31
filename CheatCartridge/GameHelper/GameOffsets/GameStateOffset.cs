using CheatCartridge.GameHelper.Natives;

namespace CheatCartridge.GameHelper.GameOffsets;

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct GameStateOffset
{
    [FieldOffset(0x08)] public StdVector CurrentStatePtr; // Used in RemoteObject -> CurrentState
    [FieldOffset(0x48)] public GameStateBuffer States;
}