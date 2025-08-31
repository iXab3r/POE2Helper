using CheatCartridge.GameHelper.GameOffsets;
using CheatCartridge.GameHelper.RemoteObjects;
using CheatCartridge.GameHelper.RemoteObjects.Components;
using CheatCartridge.GameHelper.RemoteObjects.States.InGameStateObjects;
using EyeAuras.Memory;
using EyeAuras.Memory.Scaffolding;

namespace CheatCartridge.GameHelper;

public class TheGame : DisposableReactiveObject
{
    public TheGame(IFluentLog log, IMemory memory)
    {
        Log = log;
        Memory = memory;
        States = new GameStates(memory, Log);
    }
    
    public IFluentLog Log { get; }
    public IMemory Memory { get; }

    public GameStates States { get; }

    public Entity Player => States.InGameStateObject.CurrentAreaInstance.Player;

    public void UpdateData()
    {
        if (States.Address == IntPtr.Zero)
        {
            var offsetsByPattern = MemoryUtils
                .GetOffsets(Memory, Offsets.Patterns)
                .ToDictionary(x => x.Key.Name!, x => x.Value);

            var staticAddresses = new Dictionary<string, IntPtr>();
            foreach (var patternInfo in offsetsByPattern)
            {
                var offsetDataValue = Memory.Read<int>(Memory.BaseAddress + patternInfo.Value);
                var address = Memory.BaseAddress + patternInfo.Value + offsetDataValue + 0x04;
                staticAddresses[patternInfo.Key] = new IntPtr(address);
            }
        
            Log.Info($"Static Addresses:\n{staticAddresses.Select(x => new { x.Key, Value = x.Value.ToHexadecimal() }).DumpToTable()}");
            States.Address = staticAddresses[nameof(GameStates)];
            Log.Info($"State:\n{States.AllStates.Select(x => new { Value = x.Value, Key = x.Key.ToHexadecimal() }).DumpToTable()}");
            Log.Info($@"InGame State Address: {States.InGameStateObject.Address.ToHexadecimal()}");
            Log.Info($@"CurrentArea State Address: {States.InGameStateObject.CurrentAreaInstance.Address.ToHexadecimal()}");

            var player = States.InGameStateObject.CurrentAreaInstance.Player;
            Log.Info($"Player: {new { Address = player.Address.ToHexadecimal(), player.Id, player.Path }}");
            if (player.TryGetComponent<Player>(out var playerComponent))
            {
                Log.Info($"Player name: {playerComponent.Name}");
            }
     
            if (player.TryGetComponent<Life>(out var playerLife))
            {
                Log.Info($"Player life: {new { playerLife }}");
            }
        }
        else
        {
            States.InGameStateObject.UpdateData();
        }
    }
}