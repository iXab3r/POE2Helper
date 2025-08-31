using CheatCartridge.GameHelper.Natives;

namespace CheatCartridge.GameHelper.GameOffsets.States.InGameState;

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct EntityDetails
{
    /// <summary>
    /// For Player something like L'Metadata/Characters/Dex/DexFourb'
    /// </summary>
    [FieldOffset(0x08)] public StdWString name;
    
    /// <summary>
    /// The very first 
    /// </summary>
    [FieldOffset(0x28)] public IntPtr ComponentLookUpPtr;
}