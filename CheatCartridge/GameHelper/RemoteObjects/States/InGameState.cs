using CheatCartridge.GameHelper.GameOffsets.States;
using CheatCartridge.GameHelper.RemoteObjects.States.InGameStateObjects;
using EyeAuras.Memory;

namespace CheatCartridge.GameHelper.RemoteObjects.States;

public class InGameState : MemoryObjectBase
{
    public InGameState(IMemory memory)
        : base(memory)
    {
        CurrentAreaInstance = new AreaInstance(memory);
    }
    
    public AreaInstance CurrentAreaInstance { get; }

    protected override void CleanUpData()
    {
        CurrentAreaInstance.Address = IntPtr.Zero;
    }

    protected override void UpdateData(bool hasAddressChanged)
    {
        var data = Memory.Read<InGameStateOffset>(Address);
        CurrentAreaInstance.Address = data.AreaInstanceData;
    }
}