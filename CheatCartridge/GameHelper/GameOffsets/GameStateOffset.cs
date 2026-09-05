using CheatCartridge.GameHelper.Natives;

namespace CheatCartridge.GameHelper.GameOffsets;

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct GameStateOffset
{
    // 0x08 -> 0x10 (+8)
    [FieldOffset(0x10)] public StdVector CurrentStatePtr; // Used in RemoteObject -> CurrentState
    // 0x48 -> 0x50 (+8)
    [FieldOffset(0x50)] public GameStateBuffer States;
}