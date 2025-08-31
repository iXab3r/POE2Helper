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

        var data = Memory.Read<AreaInstanceOffsets>(Address);
        if (hasAddressChanged)
        {
            Log.Info($"Current area: {new { data.CurrentAreaLevel, data.CurrentAreaHash, data.EntitiesCount }}");
        }
        var playerVector = Memory.ReadStdVector<IntPtr>(data.LocalPlayers);
        Player.Address = playerVector[0];
    }

    private void Cleanup(bool isAreaChange)
    {
        if (!isAreaChange)
        {
            Player.Address = IntPtr.Zero;
        }
    }
}