// <copyright file="Player.cs" company="None">
// Copyright (c) None. All rights reserved.
// </copyright>

using CheatCartridge.GameHelper.GameOffsets.Objects.Components;
using CheatCartridge.GameHelper.Scaffolding;
using EyeAuras.Memory;

namespace CheatCartridge.GameHelper.RemoteObjects.Components;

public sealed class Player : ComponentBase
{
    public Player(IMemory memory, IntPtr address)
        : base(memory, address) { }

    /// <summary>
    ///     Gets the name of the player.
    /// </summary>
    public string? Name { get; private set; }

    /// <inheritdoc />
    protected override void UpdateData(bool hasAddressChanged)
    {
        var data = Memory.Read<PlayerOffsets>(Address);
        OwnerEntityAddress = data.Header.EntityPtr;

        if (hasAddressChanged)
        {
            Name = Memory.ReadStdWString(data.Name);
        }
    }

    public override string ToString()
    {
        var builder = new ToStringBuilder(this);
        builder.AppendParameterIfNotDefault(nameof(Name), Name);
        return builder.ToString();
    }
}