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
    public AreaInstance(IMemory memory)
        : base(memory)
    {
        Player = new Entity(memory);
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
        }

        var data = Memory.Read<AreaInstanceOffsets>(Address);
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