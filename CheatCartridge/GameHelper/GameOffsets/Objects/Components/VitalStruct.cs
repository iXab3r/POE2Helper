namespace CheatCartridge.GameHelper.GameOffsets.Objects.Components;

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct VitalStruct
{
    [FieldOffset(0x00)] public IntPtr VtablePtr;

    /// <summary>
    ///     Constructor-written stat id. Exact runtime meaning is not proven yet.
    /// </summary>
    [FieldOffset(0x08)] public int UnknownStatId0;

    /// <summary>
    ///     Constructor-written stat id. Exact runtime meaning is not proven yet.
    /// </summary>
    [FieldOffset(0x0C)] public int UnknownStatId1;

    /// <summary>
    ///     Back pointer to the owning Life component.
    /// </summary>
    [FieldOffset(0x10)] public IntPtr LifeComponentPtr;

    /// <summary>
    ///     e.g. Clarity reserve flat Vital
    /// </summary>
    [FieldOffset(0x18)] public int ReservedFlat;

    /// <summary>
    ///     e.g. Heralds reserve % Vital.
    ///     ReservedFlat does not change this value.
    ///     Note that it's an integer, this is due to 20.23% is stored as 2023
    /// </summary>
    [FieldOffset(0x1C)] public int ReservedPercent;

    /// <summary>
    ///     Stat id used by the game when refreshing this vital's total.
    /// </summary>
    [FieldOffset(0x20)] public int TotalStatId;

    /// <summary>
    ///     Constructor-written stat id. Exact runtime meaning is not proven yet.
    /// </summary>
    [FieldOffset(0x24)] public int UnknownStatId2;

    /// <summary>
    ///     Constructor-written stat id. This used to be interpreted as a float
    ///     regeneration value, but static RE showed integer ids written here.
    /// </summary>
    [FieldOffset(0x28)] public int UnknownStatId3;

    [FieldOffset(0x34)] public int Total;
    [FieldOffset(0x38)] public int Current;

    /// <summary>
    ///     Final Reserved amount of Vital after all the calculations.
    /// </summary>
    public int ReservedTotal => (int)Math.Ceiling(ReservedPercent / 10000f * Total) + ReservedFlat;

    /// <summary>
    ///     Final un-reserved amount of Vital after all the calculations.
    /// </summary>
    public int Unreserved => Total - ReservedTotal;

    /// <summary>
    ///     Returns current Vital in percentage (excluding the reserved vital) or returns zero in case the Vital
    ///     doesn't exists.
    /// </summary>
    /// <returns></returns>
    public int CurrentInPercent()
    {
        if (Total == 0)
        {
            return 0;
        }

        return (int)Math.Round(100d * Current / Unreserved);
    }

    /// <summary>
    ///     Returns reserved Vital in percentage or returns zero in case the Vital doesn't exists.
    /// </summary>
    /// <returns></returns>
    public int ReservedInPercent()
    {
        if (Total == 0)
        {
            return 0;
        }

        return (int)Math.Round(100d * ReservedTotal / Total);
    }
    
    public override string ToString()
    {
        var result = new ToStringBuilder(this);
        result.AppendParameterIfNotDefault(nameof(Current), Current);
        result.AppendParameterIfNotDefault(nameof(Total), Total);
        return result.ToString();
    }
}
