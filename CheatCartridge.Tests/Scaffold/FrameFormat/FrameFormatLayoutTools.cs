using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.IO;
using CheatCartridge.GameHelper.GameOffsets;

namespace CheatCartridge.Tests.Scaffold.FrameFormat;

internal static class FrameFormatLayoutParser
{
    public static ParsedFfLayout ParseFile(DirectoryInfo repositoryRoot, string relativePath)
    {
        var path = Path.Combine(repositoryRoot.FullName, relativePath.Replace('/', Path.DirectorySeparatorChar));
        return ParseFile(path, relativePath);
    }

    public static ParsedFfLayout ParseFile(string path, string? sourceName = null)
    {
        return Parse(File.ReadAllLines(path), sourceName ?? path);
    }

    public static ParsedFfLayout Parse(IReadOnlyList<string> lines, string sourceName = "inline")
    {
        var messages = new Dictionary<string, ParsedFfMessage>(StringComparer.Ordinal);
        var layoutName = string.Empty;
        var sourceModuleName = string.Empty;
        var buildId = string.Empty;
        var sourceSha256 = string.Empty;
        var capturedAt = string.Empty;

        ParsedFfMessage? currentMessage = null;
        PendingField? pendingField = null;

        foreach (var line in lines)
        {
            var metadata = FrameFormatMetadataRegex.Match(line);
            if (metadata.Success)
            {
                switch (metadata.Groups["key"].Value)
                {
                    case "layout":
                        layoutName = metadata.Groups["value"].Value;
                        break;
                    case "source_name":
                        sourceModuleName = metadata.Groups["value"].Value;
                        break;
                    case "build_id":
                        buildId = metadata.Groups["value"].Value;
                        break;
                    case "source_sha256":
                        sourceSha256 = metadata.Groups["value"].Value;
                        break;
                    case "captured_at":
                        capturedAt = metadata.Groups["value"].Value;
                        break;
                }

                continue;
            }

            var message = MessageRegex.Match(line);
            if (message.Success)
            {
                var name = message.Groups["name"].Value;
                currentMessage = new ParsedFfMessage(name);
                messages[name] = currentMessage;
                continue;
            }

            if (line == "}")
            {
                currentMessage = null;
                pendingField = null;
                continue;
            }

            if (currentMessage == null)
            {
                continue;
            }

            var fieldOffset = FieldOffsetRegex.Match(line);
            if (fieldOffset.Success)
            {
                pendingField = new PendingField(
                    Offset: ParseHex(fieldOffset.Groups["offset"].Value),
                    Length: ParseHex(fieldOffset.Groups["length"].Value),
                    Shape: string.Empty);
                continue;
            }

            var fieldShape = FieldShapeRegex.Match(line);
            if (fieldShape.Success && pendingField != null)
            {
                pendingField = pendingField with { Shape = fieldShape.Groups["shape"].Value };
                continue;
            }

            var field = FieldRegex.Match(line);
            if (field.Success && pendingField != null)
            {
                var shape = pendingField.Shape;
                var inlineComment = field.Groups["comment"].Success
                    ? field.Groups["comment"].Value.Trim()
                    : string.Empty;
                currentMessage.Fields[field.Groups["name"].Value] = new ParsedFfField(
                    field.Groups["type"].Value,
                    field.Groups["name"].Value,
                    int.Parse(field.Groups["number"].Value),
                    pendingField.Offset,
                    pendingField.Length,
                    shape,
                    ParseArrayCount(shape),
                    inlineComment);
                currentMessage.Size = Math.Max(currentMessage.Size, pendingField.Offset + pendingField.Length);
                pendingField = null;
            }
        }

        layoutName = string.IsNullOrWhiteSpace(layoutName)
            ? GetLayoutNameFromSource(sourceName)
            : layoutName;
        sourceModuleName = string.IsNullOrWhiteSpace(sourceModuleName)
            ? GetModuleNameFromSource(sourceName)
            : sourceModuleName;
        return new ParsedFfLayout(
            CreateSourceName(sourceName),
            layoutName,
            sourceModuleName,
            buildId,
            sourceSha256,
            capturedAt,
            messages);
    }

    private static string CreateSourceName(string sourceName)
    {
        var normalized = sourceName.Replace('\\', '/');
        var fileName = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? normalized;
        return RemoveFrameFormatProtoSuffix(fileName);
    }

    private static string GetLayoutNameFromSource(string sourceName)
    {
        return RemoveFrameFormatProtoSuffix(Path.GetFileName(sourceName));
    }

    private static string GetModuleNameFromSource(string sourceName)
    {
        var normalized = sourceName.Replace('\\', '/');
        var parts = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2 ? parts[^2] : string.Empty;
    }

    private static string RemoveFrameFormatProtoSuffix(string value)
    {
        return value.EndsWith(".ff.proto", StringComparison.OrdinalIgnoreCase)
            ? value[..^".ff.proto".Length]
            : Path.GetFileNameWithoutExtension(value);
    }

    private static int ParseHex(string value)
    {
        return Convert.ToInt32(value[2..], 16);
    }

    private static int? ParseArrayCount(string shape)
    {
        var match = ArrayCountRegex.Match(shape);
        return match.Success ? int.Parse(match.Groups["count"].Value) : null;
    }

    private static readonly Regex FrameFormatMetadataRegex = new(
        @"^// @ffmeta (?<key>\w+)=(?<value>.+)$",
        RegexOptions.Compiled);

    private static readonly Regex MessageRegex = new(
        @"^message (?<name>\w+) \{$",
        RegexOptions.Compiled);

    private static readonly Regex FieldOffsetRegex = new(
        @"^  // @reclass offset=(?<offset>0x[0-9A-Fa-f]+) length=(?<length>0x[0-9A-Fa-f]+)$",
        RegexOptions.Compiled);

    private static readonly Regex FieldShapeRegex = new(
        @"^  // @fflayout shape=(?<shape>.+)$",
        RegexOptions.Compiled);

    private static readonly Regex FieldRegex = new(
        @"^  (?<type>\S+) (?<name>\w+) = (?<number>\d+);(?:\s*//\s*(?<comment>.*))?$",
        RegexOptions.Compiled);

    private static readonly Regex ArrayCountRegex = new(
        @"\barray\(count=(?<count>\d+),",
        RegexOptions.Compiled);
}

internal static class FrameFormatReflection
{
    public static IReadOnlyList<FrameFormatMemberMapping> GetMappings(Assembly assembly)
    {
        return assembly
            .GetTypes()
            .SelectMany(GetMappings)
            .OrderBy(x => x.FrameFormatPath, StringComparer.Ordinal)
            .ThenBy(x => x.ClrType.FullName, StringComparer.Ordinal)
            .ThenBy(x => x.MemberName, StringComparer.Ordinal)
            .ToArray();
    }

    private static IEnumerable<FrameFormatMemberMapping> GetMappings(Type type)
    {
        var typeAttribute = type.GetCustomAttribute<FrameFormatTypeAttribute>();
        var defaultTypeName = typeAttribute?.Name;

        foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
        {
            var fieldAttribute = field.GetCustomAttribute<FrameFormatFieldAttribute>();
            if (fieldAttribute == null)
            {
                continue;
            }

            var generatedAttribute = field.GetCustomAttribute<FrameFormatGeneratedAttribute>();
            var ffPath = fieldAttribute.Name.Contains('.', StringComparison.Ordinal)
                ? fieldAttribute.Name
                : $"{defaultTypeName ?? type.Name}.{fieldAttribute.Name}";
            yield return new FrameFormatMemberMapping(
                ClrType: type,
                MemberName: field.Name,
                FrameFormatPath: ffPath,
                Binding: fieldAttribute.Binding,
                RuntimeOffset: field.IsStatic ? null : Marshal.OffsetOf(type, field.Name).ToInt32(),
                ConstantValue: field.IsLiteral ? field.GetRawConstantValue() : null,
                RequiresGeneratedProvenance: true,
                GeneratedSourceName: generatedAttribute?.SourceName,
                GeneratedTimestampUtc: generatedAttribute?.TimestampUtc,
                GeneratedComment: generatedAttribute?.Comment);
        }
    }
}

internal static class FrameFormatSourceUpdater
{
    public static FrameFormatSourceUpdateResult UpdateSourceTree(
        DirectoryInfo repositoryRoot,
        ParsedFfLayout ff,
        bool apply)
    {
        var sourceRoot = Path.Combine(repositoryRoot.FullName, "CheatCartridge");
        var files = Directory
            .EnumerateFiles(sourceRoot, "*.cs", SearchOption.AllDirectories)
            .Where(x => !x.Contains($"{Path.DirectorySeparatorChar}generated{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        var changes = new List<FrameFormatSourceChange>();
        foreach (var file in files)
        {
            var original = File.ReadAllLines(file);
            var updated = UpdateFile(repositoryRoot, file, original, ff, changes);
            if (apply && !original.SequenceEqual(updated))
            {
                File.WriteAllLines(file, updated);
            }
        }

        return new FrameFormatSourceUpdateResult(changes);
    }

    private static string[] UpdateFile(
        DirectoryInfo repositoryRoot,
        string file,
        string[] lines,
        ParsedFfLayout ff,
        List<FrameFormatSourceChange> changes)
    {
        var updated = (string[])lines.Clone();
        var currentLines = updated.ToList();
        string? pendingType = null;
        string? currentType = null;
        PendingSourceField? pendingField = null;
        var typeBraceDepth = 0;
        var hasEnteredTypeBody = false;

        for (var i = 0; i < currentLines.Count; i++)
        {
            var line = currentLines[i];
            var typeAttribute = TypeAttributeRegex.Match(line);
            if (typeAttribute.Success)
            {
                pendingType = typeAttribute.Groups["name"].Value;
                continue;
            }

            if (pendingType != null && TypeDeclarationRegex.IsMatch(line))
            {
                currentType = pendingType;
                pendingType = null;
                typeBraceDepth = CountChar(line, '{') - CountChar(line, '}');
                hasEnteredTypeBody = typeBraceDepth > 0;
                continue;
            }

            if (currentType != null)
            {
                typeBraceDepth += CountChar(line, '{') - CountChar(line, '}');
                hasEnteredTypeBody |= line.Contains('{');
                if (hasEnteredTypeBody && typeBraceDepth <= 0)
                {
                    currentType = null;
                    pendingField = null;
                    continue;
                }
            }

            var fieldAttribute = FieldAttributeRegex.Match(line);
            if (fieldAttribute.Success)
            {
                var binding = fieldAttribute.Groups["binding"].Success
                    ? Enum.Parse<FrameFormatBinding>(fieldAttribute.Groups["binding"].Value)
                    : FrameFormatBinding.Offset;
                pendingField = new PendingSourceField(
                    fieldAttribute.Groups["name"].Value,
                    currentType,
                    binding,
                    GeneratedAttributeLineIndex: null);
                continue;
            }

            if (pendingField != null && GeneratedAttributeRegex.IsMatch(line))
            {
                pendingField = pendingField with { GeneratedAttributeLineIndex = i };
                continue;
            }

            if (pendingField == null)
            {
                continue;
            }

            if (pendingField.Binding == FrameFormatBinding.Offset)
            {
                var offset = FieldOffsetRegex.Match(line);
                if (offset.Success)
                {
                    var ffField = ResolveField(ff, pendingField);
                    EnsureGeneratedAttribute(
                        repositoryRoot,
                        file,
                        currentLines,
                        ref i,
                        pendingField,
                        ff,
                        ffField,
                        changes);
                    line = currentLines[i];
                    offset = FieldOffsetRegex.Match(line);
                    var oldValue = offset.Groups["value"].Value;
                    var newValue = FormatIntegerLiteralLike(oldValue, ffField.Offset);
                    if (ParseIntegerLiteral(oldValue) != ffField.Offset)
                    {
                        currentLines[i] = FieldOffsetRegex.Replace(line, match => match.Value.Replace(oldValue, newValue));
                        changes.Add(CreateChange(repositoryRoot, file, i, pendingField, oldValue, newValue));
                    }

                    pendingField = null;
                    continue;
                }

                if (LooksLikeFieldDeclaration(line))
                {
                    var ffField = ResolveField(ff, pendingField);
                    EnsureGeneratedAttribute(
                        repositoryRoot,
                        file,
                        currentLines,
                        ref i,
                        pendingField,
                        ff,
                        ffField,
                        changes);
                    pendingField = null;
                }
            }
            else if (pendingField.Binding == FrameFormatBinding.ArrayCount)
            {
                var constant = ConstIntRegex.Match(line);
                if (constant.Success)
                {
                    var ffField = ResolveField(ff, pendingField);
                    if (ffField.ArrayCount == null)
                    {
                        throw new InvalidOperationException($"{pendingField.FrameFormatPath} does not have an array count in FF.");
                    }

                    EnsureGeneratedAttribute(
                        repositoryRoot,
                        file,
                        currentLines,
                        ref i,
                        pendingField,
                        ff,
                        ffField,
                        changes);
                    line = currentLines[i];
                    constant = ConstIntRegex.Match(line);
                    var oldValue = constant.Groups["value"].Value;
                    var newValue = ffField.ArrayCount.Value.ToString();
                    if (ParseIntegerLiteral(oldValue) != ffField.ArrayCount.Value)
                    {
                        currentLines[i] = ConstIntRegex.Replace(line, match => match.Value.Replace(oldValue, newValue));
                        changes.Add(CreateChange(repositoryRoot, file, i, pendingField, oldValue, newValue));
                    }

                    pendingField = null;
                }
            }
        }

        return currentLines.ToArray();
    }

    private static void EnsureGeneratedAttribute(
        DirectoryInfo repositoryRoot,
        string file,
        List<string> lines,
        ref int targetLineIndex,
        PendingSourceField pendingField,
        ParsedFfLayout ff,
        ParsedFfField ffField,
        List<FrameFormatSourceChange> changes)
    {
        var indent = LeadingWhitespace(lines[targetLineIndex]);
        var generatedLine = indent + CreateGeneratedAttributeLine(pendingField, ff, ffField);
        if (pendingField.GeneratedAttributeLineIndex.HasValue)
        {
            var generatedLineIndex = pendingField.GeneratedAttributeLineIndex.Value;
            if (!string.Equals(lines[generatedLineIndex], generatedLine, StringComparison.Ordinal))
            {
                var oldValue = lines[generatedLineIndex].Trim();
                lines[generatedLineIndex] = generatedLine;
                changes.Add(CreateChange(repositoryRoot, file, generatedLineIndex, pendingField, oldValue, generatedLine.Trim()));
            }

            return;
        }

        lines.Insert(targetLineIndex, generatedLine);
        changes.Add(CreateChange(repositoryRoot, file, targetLineIndex, pendingField, string.Empty, generatedLine.Trim()));
        targetLineIndex++;
    }

    private static string CreateGeneratedAttributeLine(
        PendingSourceField pendingField,
        ParsedFfLayout ff,
        ParsedFfField ffField)
    {
        var comment = string.IsNullOrWhiteSpace(ffField.Comment)
            ? pendingField.FrameFormatPath
            : $"{pendingField.FrameFormatPath}; {ffField.Comment}";
        return
            "[FrameFormatGenerated(" +
            $"\"{EscapeCSharpString(ff.SourceName)}\", " +
            $"\"{EscapeCSharpString(ff.CapturedAt)}\", " +
            $"\"{EscapeCSharpString(comment)}\")]";
    }

    private static ParsedFfField ResolveField(ParsedFfLayout ff, PendingSourceField pendingField)
    {
        var path = pendingField.FrameFormatPath;
        var separatorIndex = path.IndexOf('.', StringComparison.Ordinal);
        if (separatorIndex <= 0 || separatorIndex >= path.Length - 1)
        {
            throw new InvalidOperationException($"FrameFormatField '{path}' must resolve to Type.field.");
        }

        var typeName = path[..separatorIndex];
        var fieldName = path[(separatorIndex + 1)..];
        if (!ff.Messages.TryGetValue(typeName, out var message))
        {
            throw new InvalidOperationException($"FF type '{typeName}' was not found.");
        }

        if (!message.Fields.TryGetValue(fieldName, out var field))
        {
            throw new InvalidOperationException($"FF field '{path}' was not found.");
        }

        return field;
    }

    private static FrameFormatSourceChange CreateChange(
        DirectoryInfo repositoryRoot,
        string file,
        int lineIndex,
        PendingSourceField pendingField,
        string oldValue,
        string newValue)
    {
        return new FrameFormatSourceChange(
            Path.GetRelativePath(repositoryRoot.FullName, file),
            lineIndex + 1,
            pendingField.FrameFormatPath,
            pendingField.Binding,
            oldValue,
            newValue);
    }

    private static bool LooksLikeFieldDeclaration(string line)
    {
        return line.Contains(';') &&
               !line.TrimStart().StartsWith("[", StringComparison.Ordinal) &&
               !line.TrimStart().StartsWith("//", StringComparison.Ordinal);
    }

    private static int CountChar(string value, char character)
    {
        return value.Count(x => x == character);
    }

    private static string LeadingWhitespace(string value)
    {
        return value[..value.TakeWhile(char.IsWhiteSpace).Count()];
    }

    private static string EscapeCSharpString(string value)
    {
        return value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);
    }

    private static int ParseIntegerLiteral(string value)
    {
        return value.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
            ? Convert.ToInt32(value[2..], 16)
            : int.Parse(value);
    }

    private static string FormatIntegerLiteralLike(string oldValue, int value)
    {
        if (!oldValue.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            return value.ToString();
        }

        var width = oldValue.Length - 2;
        return $"0x{value.ToString($"X{width}")}";
    }

    private sealed record PendingSourceField(
        string Name,
        string? CurrentType,
        FrameFormatBinding Binding,
        int? GeneratedAttributeLineIndex)
    {
        public string FrameFormatPath => Name.Contains('.', StringComparison.Ordinal)
            ? Name
            : $"{CurrentType ?? throw new InvalidOperationException($"FrameFormatField '{Name}' is missing a FrameFormatType context.")}.{Name}";
    }

    private static readonly Regex TypeAttributeRegex = new(
        @"\[FrameFormatType\(""(?<name>[^""]+)""\)\]",
        RegexOptions.Compiled);

    private static readonly Regex TypeDeclarationRegex = new(
        @"\b(struct|class|interface)\s+\w+",
        RegexOptions.Compiled);

    private static readonly Regex FieldAttributeRegex = new(
        @"\[FrameFormatField\(""(?<name>[^""]+)""(?:,\s*Binding\s*=\s*FrameFormatBinding\.(?<binding>\w+))?\)\]",
        RegexOptions.Compiled);

    private static readonly Regex GeneratedAttributeRegex = new(
        @"\[FrameFormatGenerated\(",
        RegexOptions.Compiled);

    private static readonly Regex FieldOffsetRegex = new(
        @"\[FieldOffset\((?<value>0x[0-9A-Fa-f]+|\d+)\)\]",
        RegexOptions.Compiled);

    private static readonly Regex ConstIntRegex = new(
        @"\bconst\s+int\s+\w+\s*=\s*(?<value>\d+)",
        RegexOptions.Compiled);
}

internal sealed record ParsedFfLayout(
    string SourceName,
    string LayoutName,
    string SourceModuleName,
    string BuildId,
    string SourceSha256,
    string CapturedAt,
    IReadOnlyDictionary<string, ParsedFfMessage> Messages);

internal sealed record ParsedFfMessage(string Name)
{
    public int Size { get; set; }

    public Dictionary<string, ParsedFfField> Fields { get; } = new(StringComparer.Ordinal);
}

internal sealed record ParsedFfField(
    string TypeName,
    string Name,
    int Number,
    int Offset,
    int Length,
    string Shape,
    int? ArrayCount,
    string Comment);

internal sealed record FrameFormatMemberMapping(
    Type ClrType,
    string MemberName,
    string FrameFormatPath,
    FrameFormatBinding Binding,
    int? RuntimeOffset,
    object? ConstantValue,
    bool RequiresGeneratedProvenance,
    string? GeneratedSourceName,
    string? GeneratedTimestampUtc,
    string? GeneratedComment);

internal sealed record FrameFormatSourceUpdateResult(IReadOnlyList<FrameFormatSourceChange> Changes);

internal sealed record FrameFormatSourceChange(
    string FilePath,
    int Line,
    string FrameFormatPath,
    FrameFormatBinding Binding,
    string OldValue,
    string NewValue);

internal sealed record PendingField(
    int Offset,
    int Length,
    string Shape);
