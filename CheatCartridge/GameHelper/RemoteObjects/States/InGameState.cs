using CheatCartridge.GameHelper.GameOffsets.States;
using CheatCartridge.GameHelper.RemoteObjects.States.InGameStateObjects;
using EyeAuras.Memory;

namespace CheatCartridge.GameHelper.RemoteObjects.States;

public class InGameState : MemoryObjectBase
{
    public IFluentLog Log { get; }

    public InGameState(IMemory memory, IFluentLog log)
        : base(memory)
    {
        Log = log;
        CurrentAreaInstance = new AreaInstance(memory, log);
    }
    
    public AreaInstance CurrentAreaInstance { get; }

    protected override void CleanUpData()
    {
        CurrentAreaInstance.Address = IntPtr.Zero;
    }

    protected override void UpdateData(bool hasAddressChanged)
    {
        if (hasAddressChanged)
        {
            Log.Info($"InGameState Address changed to: {Address.ToHexadecimal()}");
        }
        var data = Memory.Read<InGameStateOffset>(Address);
        CurrentAreaInstance.Address = data.AreaInstanceData;
    }
}