namespace CheatCartridge.GameHelper.GameOffsets.Objects.Components;

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct VitalStruct
{
    [FieldOffset(0x00)] public IntPtr VtablePtr;
    [FieldOffset(0x08)] public IntPtr PtrToLifeComponent;

    /// <summary>
    ///     e.g. Clarity reserve flat Vital
    /// </summary>
    [FieldOffset(0x10)] public int ReservedFlat;

    /// <summary>
    ///     e.g. Heralds reserve % Vital.
    ///     ReservedFlat does not change this value.
    ///     Note that it's an integer, this is due to 20.23% is stored as 2023
    /// </summary>
    [FieldOffset(0x14)] public int ReservedPercent;

    /// <summary>
    ///     This is greater than zero if Vital is regenerating
    ///     For value = 0 or less than 0, Vital isn't regenerating
    /// </summary>
    [FieldOffset(0x28)] public float Regeneration;
    [FieldOffset(0x2C)] public int Total;
    [FieldOffset(0x30)] public int Current;

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