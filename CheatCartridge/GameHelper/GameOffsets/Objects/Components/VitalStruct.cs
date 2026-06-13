using CheatCartridge.GameHelper.GameOffsets;

namespace CheatCartridge.GameHelper.GameOffsets.Objects.Components;

[FrameFormatType("Vital")]
[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct VitalStruct
{
    /// <summary>
    ///     Constructor-written stat id. Exact runtime meaning is not proven yet.
    /// </summary>
    [FrameFormatField("unknown_stat_id0")]
    [FrameFormatGenerated("poe-game-model.sha256-c5da3833", "2026-06-13T12:25:29.4645730+00:00", "Vital.unknown_stat_id0; Constructor-written stat id; exact meaning is still unknown.")]
    [FieldOffset(0x08)] public int UnknownStatId0;

    /// <summary>
    ///     Constructor-written stat id. Exact runtime meaning is not proven yet.
    /// </summary>
    [FrameFormatField("unknown_stat_id1")]
    [FrameFormatGenerated("poe-game-model.sha256-c5da3833", "2026-06-13T12:25:29.4645730+00:00", "Vital.unknown_stat_id1; Constructor-written stat id; exact meaning is still unknown.")]
    [FieldOffset(0x0C)] public int UnknownStatId1;

    [FrameFormatField("life_component")]
    [FrameFormatGenerated("poe-game-model.sha256-c5da3833", "2026-06-13T12:25:29.4645730+00:00", "Vital.life_component; Back pointer to owning Life component.")]
    [FieldOffset(0x10)] public IntPtr LifeComponentPtr;

    /// <summary>
    ///     e.g. Clarity reserve flat Vital
    /// </summary>
    [FrameFormatField("reserved_flat")]
    [FrameFormatGenerated("poe-game-model.sha256-c5da3833", "2026-06-13T12:25:29.4645730+00:00", "Vital.reserved_flat; Flat reserved amount.")]
    [FieldOffset(0x18)] public int ReservedFlat;

    /// <summary>
    ///     e.g. Heralds reserve % Vital.
    ///     ReservedFlat does not change this value.
    ///     Note that it's an integer, this is due to 20.23% is stored as 2023
    /// </summary>
    [FrameFormatField("reserved_percent")]
    [FrameFormatGenerated("poe-game-model.sha256-c5da3833", "2026-06-13T12:25:29.4645730+00:00", "Vital.reserved_percent; Percent reserved amount stored as basis points.")]
    [FieldOffset(0x1C)] public int ReservedPercent;

    /// <summary>
    ///     Stat id used by the game when refreshing this vital's total.
    /// </summary>
    [FrameFormatField("total_stat_id")]
    [FrameFormatGenerated("poe-game-model.sha256-c5da3833", "2026-06-13T12:25:29.4645730+00:00", "Vital.total_stat_id; Stat id used when refreshing this vital's total.")]
    [FieldOffset(0x20)] public int TotalStatId;

    /// <summary>
    ///     Constructor-written stat id. Exact runtime meaning is not proven yet.
    /// </summary>
    [FrameFormatField("unknown_stat_id2")]
    [FrameFormatGenerated("poe-game-model.sha256-c5da3833", "2026-06-13T12:25:29.4645730+00:00", "Vital.unknown_stat_id2; Constructor-written stat id; exact meaning is still unknown.")]
    [FieldOffset(0x24)] public int UnknownStatId2;

    /// <summary>
    ///     Constructor-written stat id. This used to be interpreted as a float
    ///     regeneration value, but the Life constructor writes integer ids here.
    /// </summary>
    [FrameFormatField("unknown_stat_id3")]
    [FrameFormatGenerated("poe-game-model.sha256-c5da3833", "2026-06-13T12:25:29.4645730+00:00", "Vital.unknown_stat_id3; Constructor-written stat id; exact meaning is still unknown.")]
    [FieldOffset(0x28)] public int UnknownStatId3;

    [FrameFormatField("total")]
    [FrameFormatGenerated("poe-game-model.sha256-c5da3833", "2026-06-13T12:25:29.4645730+00:00", "Vital.total; Current maximum value for this vital.")]
    [FieldOffset(0x34)] public int Total;

    [FrameFormatField("current")]
    [FrameFormatGenerated("poe-game-model.sha256-c5da3833", "2026-06-13T12:25:29.4645730+00:00", "Vital.current; Current value for this vital.")]
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
