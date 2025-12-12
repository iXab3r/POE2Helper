// <copyright file="AreaInstance.cs" company="None">
// Copyright (c) None. All rights reserved.
// </copyright>

using CheatCartridge.GameHelper.GameOffsets.States.InGameState;
using CheatCartridge.GameHelper.Scaffolding;
using EyeAuras.Memory;

namespace CheatCartridge.GameHelper.RemoteObjects.States.InGameStateObjects;

/// <summary>
///     Points to the InGameState -> AreaInstanceData Object.
/// </summary>
public class AreaInstance : MemoryObjectBase
{
    public IFluentLog Log { get; }

    public AreaInstance(IMemory memory, IFluentLog log)
        : base(memory)
    {
        Log = log;
        Player = new Entity(memory, log);
    }

    public Entity Player { get; }

    protected override void CleanUpData()
    {
        Cleanup(false);
    }

    protected override void UpdateData(bool hasAddressChanged)
    {
        if (hasAddressChanged)
        {
            Cleanup(true);
            Log.Info($"Area Instance Address changed to: {Address.ToHexadecimal()}");
        }

        AreaInstanceOffsets data;
        try
        {
            data = Memory.Read<AreaInstanceOffsets>(Address);
            if (hasAddressChanged)
            {
                Log.Info($"Current area: {new {data.CurrentAreaLevel, data.CurrentAreaHash, data.EntitiesCount}}");
            }
        }
        catch (Exception e)
        {
            Log.Error($"Failed to read area instance offsets @ {Address.ToHexadecimal()}");
            throw;
        }

        try
        {
            var playerVector = Memory.ReadStdVector<IntPtr>(data.LocalPlayers);
            Player.Address = playerVector[0];
        }
        catch (Exception e)
        {
            Log.Error($"Failed to read local players array @ {new { First = data.LocalPlayers.First.ToHexadecimal(), Last = data.LocalPlayers.Last.ToHexadecimal(), End = data.LocalPlayers.End.ToHexadecimal() }}");
            throw;
        }
       
    }

    private void Cleanup(bool isAreaChange)
    {
        if (!isAreaChange)
        {
            Player.Address = IntPtr.Zero;
        }
    }
}