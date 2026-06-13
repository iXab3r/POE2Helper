using System.Globalization;
using System.Text;

namespace CheatCartridge.Tests.Scaffold.FrameFormat;

internal sealed record FrameFormatProtoWriterOptions(
    string PackageName,
    string LayoutName,
    string SourceName,
    string BuildId,
    string SourceSha256,
    DateTimeOffset? CapturedAt,
    string Summary = "Generated FrameFormat layout.",
    int LayoutPointerSize = 8);

internal sealed record FrameFormatProtoMessage(
    string Name,
    string Summary,
    IReadOnlyList<FrameFormatProtoField> Fields);

internal sealed record FrameFormatProtoField(
    string TypeName,
    string Name,
    int Number,
    int Offset,
    int Length,
    string Shape)
{
    public string Comment { get; init; } = string.Empty;
}

internal static class FrameFormatProtoWriter
{
    public static string Write(
        FrameFormatProtoWriterOptions options,
        IReadOnlyList<FrameFormatProtoMessage> messages)
    {
        var writer = new Writer(options, messages);
        return writer.Write();
    }

    public static string Hex(int value)
    {
        return value < 0
            ? "-0x" + (-value).ToString("X", CultureInfo.InvariantCulture)
            : "0x" + value.ToString("X", CultureInfo.InvariantCulture);
    }

    private sealed class Writer
    {
        private readonly FrameFormatProtoWriterOptions options;
        private readonly IReadOnlyList<FrameFormatProtoMessage> messages;
        private readonly StringBuilder builder = new();

        public Writer(
            FrameFormatProtoWriterOptions options,
            IReadOnlyList<FrameFormatProtoMessage> messages)
        {
            this.options = options;
            this.messages = messages;
        }

        public string Write()
        {
            WriteHeader();
            WriteInfrastructure();
            foreach (var message in messages)
            {
                WriteMessage(message);
            }

            return builder.ToString().TrimEnd() + Environment.NewLine;
        }

        private void WriteHeader()
        {
            builder.AppendLine("syntax = \"proto3\";");
            builder.AppendLine();
            builder.Append("package ");
            builder.Append(SanitizePackage(options.PackageName));
            builder.AppendLine(";");
            builder.AppendLine();
            builder.AppendLine("import \"google/protobuf/descriptor.proto\";");
            builder.AppendLine();
            WriteComment(options.Summary);
            WriteComment($"@ffmeta layout={options.LayoutName}");
            WriteComment($"@ffmeta source_name={options.SourceName}");
            if (!string.IsNullOrWhiteSpace(options.BuildId))
            {
                WriteComment($"@ffmeta build_id={options.BuildId}");
            }

            if (!string.IsNullOrWhiteSpace(options.SourceSha256))
            {
                WriteComment($"@ffmeta source_sha256={options.SourceSha256}");
            }

            if (options.CapturedAt != null)
            {
                WriteComment($"@ffmeta captured_at={options.CapturedAt.Value:O}");
            }

            builder.AppendLine();
        }

        private void WriteInfrastructure()
        {
            builder.AppendLine("message FFFileOptions {");
            builder.AppendLine("  int32 layout_pointer_size = 1;");
            builder.AppendLine("}");
            builder.AppendLine();
            builder.AppendLine("message FFPacketOptions {");
            builder.AppendLine("}");
            builder.AppendLine();
            builder.AppendLine("extend google.protobuf.FileOptions {");
            builder.AppendLine("  FFFileOptions ff_file = 51000;");
            builder.AppendLine("}");
            builder.AppendLine();
            builder.AppendLine("extend google.protobuf.MessageOptions {");
            builder.AppendLine("  FFPacketOptions ff_packet = 51001;");
            builder.AppendLine("}");
            builder.AppendLine();
            builder.AppendLine("option (ff_file) = {");
            builder.Append("  layout_pointer_size: ");
            builder.AppendLine(options.LayoutPointerSize.ToString(CultureInfo.InvariantCulture));
            builder.AppendLine("};");
            builder.AppendLine();
        }

        private void WriteMessage(FrameFormatProtoMessage message)
        {
            WriteComment(message.Summary);
            builder.Append("message ");
            builder.Append(message.Name);
            builder.AppendLine(" {");

            foreach (var field in message.Fields.OrderBy(x => x.Number))
            {
                builder.Append("  // @reclass offset=");
                builder.Append(Hex(field.Offset));
                builder.Append(" length=");
                builder.Append(Hex(field.Length));
                builder.AppendLine();
                builder.Append("  // @fflayout shape=");
                builder.AppendLine(field.Shape);

                builder.Append("  ");
                builder.Append(field.TypeName);
                builder.Append(' ');
                builder.Append(field.Name);
                builder.Append(" = ");
                builder.Append(field.Number);
                builder.Append(';');
                if (!string.IsNullOrWhiteSpace(field.Comment))
                {
                    builder.Append(" // ");
                    builder.Append(SanitizeInlineComment(field.Comment));
                }

                builder.AppendLine();
                builder.AppendLine();
            }

            builder.AppendLine("}");
            builder.AppendLine();
        }

        private void WriteComment(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            foreach (var line in text.Replace("\r\n", "\n").Split('\n'))
            {
                builder.Append("// ");
                builder.AppendLine(line);
            }
        }
    }

    private static string SanitizePackage(string packageName)
    {
        return string.IsNullOrWhiteSpace(packageName)
            ? "frameformat.memory"
            : packageName.Trim();
    }

    private static string SanitizeInlineComment(string comment)
    {
        return comment
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal)
            .Trim();
    }
}
