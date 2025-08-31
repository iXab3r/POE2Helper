using CheatCartridge.GameHelper.GameOffsets;
using CheatCartridge.GameHelper.RemoteEnums;
using CheatCartridge.GameHelper.RemoteObjects.States;
using EyeAuras.Memory;

namespace CheatCartridge.GameHelper.RemoteObjects;

/// <summary>
///     Reads and stores the global states of the game.
/// </summary>
public class GameStates : MemoryObjectBase
{
    private readonly ConcurrentDictionary<IntPtr, GameStateTypes> allStates = new();
    private IntPtr currentStateAddress = IntPtr.Zero;
    private GameStateStaticOffset myStaticObj;

    public GameStates(IMemory memory, IFluentLog log)
        : base(memory)
    {
        Log = log;
        InGameStateObject = new InGameState(Memory, log);
    }
    
    public IFluentLog Log { get; }

    public IReadOnlyDictionary<IntPtr, GameStateTypes> AllStates => allStates;

    public InGameState InGameStateObject { get; } 

    protected override void UpdateData(bool hasAddressChanged)
    {
        if (hasAddressChanged)
        {
            myStaticObj = Memory.Read<GameStateStaticOffset>(Address);
            Log.Info($"Loaded state offsets {Address.ToHexadecimal()}, current state struct is @ {myStaticObj.GameState.ToHexadecimal()}");
            
            var data = Memory.Read<GameStateOffset>(myStaticObj.GameState);
            for (var i = 0; i < GameStateBuffer.TOTAL_STATES; i++)
            {
                allStates[data.States[i].X] = (GameStateTypes)i;
            }
            Log.Info($"Loaded game states");
            RefreshCurrentState(data);
            Log.Info($"CurrentState @ {currentStateAddress.ToHexadecimal()}: {data.States.GetStateType(currentStateAddress)}");

            InGameStateObject.Address = data.States[(int)GameStateTypes.InGameState].X;
            Log.Info($"Assigned game states ptrs");
        }
        else
        {
            var data = Memory.Read<GameStateOffset>(myStaticObj.GameState);
            RefreshCurrentState(data);
        }

        return;

        void RefreshCurrentState(GameStateOffset data)
        {
            var cStateAddr = Memory.Read<IntPtr>(data.CurrentStatePtr.Last - 0x10); // Get 2nd-last ptr.
            if (cStateAddr != IntPtr.Zero && cStateAddr != currentStateAddress)
            {
                currentStateAddress = cStateAddr;
            }
        }
    }

    protected override void CleanUpData()
    {
        myStaticObj = default;
        currentStateAddress = IntPtr.Zero;
        InGameStateObject.Address = IntPtr.Zero;
        allStates.Clear();
    }
}