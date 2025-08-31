// <copyright file="Life.cs" company="None">
// Copyright (c) None. All rights reserved.
// </copyright>

using CheatCartridge.GameHelper.GameOffsets.Objects.Components;
using EyeAuras.Memory;

namespace CheatCartridge.GameHelper.RemoteObjects.Components;

/// <summary>
///     The <see cref="Life" /> component in the entity.
/// </summary>
public sealed class Life : ComponentBase
{
    public Life(IMemory memory, IntPtr address)
        : base(memory, address) { }

    /// <summary>
    ///     Gets the health related information of the entity.
    /// </summary>
    public VitalStruct Health { get; private set; }

    /// <summary>
    ///     Gets the energyshield related information of the entity.
    /// </summary>
    public VitalStruct EnergyShield { get; private set; }

    /// <summary>
    ///     Gets the mana related information of the entity.
    /// </summary>
    public VitalStruct Mana { get; private set; }

    /// <inheritdoc />
    protected override void UpdateData(bool hasAddressChanged)
    {
        var data = Memory.Read<LifeOffset>(Address);
        OwnerEntityAddress = data.Header.EntityPtr;
        Health = data.Health;
        EnergyShield = data.EnergyShield;
        Mana = data.Mana;
    }
    
    public override string ToString()
    {
        var builder = new ToStringBuilder(this);
        builder.AppendParameterIfNotDefault(nameof(Health), Health);
        builder.AppendParameterIfNotDefault(nameof(Mana), Mana);
        builder.AppendParameterIfNotDefault(nameof(EnergyShield), EnergyShield);
        return builder.ToString();
    }
}