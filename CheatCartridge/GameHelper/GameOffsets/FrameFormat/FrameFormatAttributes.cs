namespace CheatCartridge.GameHelper.GameOffsets;

public enum FrameFormatBinding
{
    Offset,
    ArrayCount
}

[AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class | AttributeTargets.Interface)]
public sealed class FrameFormatTypeAttribute : Attribute
{
    public FrameFormatTypeAttribute(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }

    public string Name { get; }
}

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class FrameFormatFieldAttribute : Attribute
{
    public FrameFormatFieldAttribute(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }

    public string Name { get; }

    public FrameFormatBinding Binding { get; init; } = FrameFormatBinding.Offset;
}

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class FrameFormatGeneratedAttribute : Attribute
{
    public FrameFormatGeneratedAttribute(string sourceName, string timestampUtc, string comment)
    {
        SourceName = sourceName ?? throw new ArgumentNullException(nameof(sourceName));
        TimestampUtc = timestampUtc ?? throw new ArgumentNullException(nameof(timestampUtc));
        Comment = comment ?? throw new ArgumentNullException(nameof(comment));
    }

    public string SourceName { get; }

    public string TimestampUtc { get; }

    public string Comment { get; }
}
