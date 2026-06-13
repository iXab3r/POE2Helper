namespace CheatCartridge.Tests.Scaffold.FrameFormat;

public sealed class FrameFormatProtoWriterTests
{
    [Test]
    public void ShouldSerializeGenericFrameFormatProto()
    {
        // Given
        var options = new FrameFormatProtoWriterOptions(
            PackageName: "sample.memory",
            LayoutName: "sample-layout",
            SourceName: "sample.bin",
            BuildId: "sha256-deadbeef",
            SourceSha256: "deadbeef",
            CapturedAt: DateTimeOffset.Parse("2026-06-13T12:00:00Z"),
            Summary: "Generated from a sample runtime.",
            LayoutPointerSize: 8);

        var messages = new[]
        {
            new FrameFormatProtoMessage(
                "SampleState",
                "Sample state block.",
                [
                    new FrameFormatProtoField(
                        TypeName: "int32",
                        Name: "counter",
                        Number: 1,
                        Offset: 0x10,
                        Length: 0x04,
                        Shape: "primitive(int32)")
                    {
                        Comment = "Sample counter value."
                    }
                ])
        };

        // When
        var proto = FrameFormatProtoWriter.Write(options, messages);

        // Then
        proto.ShouldContain("syntax = \"proto3\";");
        proto.ShouldContain("package sample.memory;");
        proto.ShouldContain("// Generated from a sample runtime.");
        proto.ShouldContain("// @ffmeta layout=sample-layout");
        proto.ShouldContain("// @ffmeta source_name=sample.bin");
        proto.ShouldContain("// @ffmeta build_id=sha256-deadbeef");
        proto.ShouldContain("// @ffmeta source_sha256=deadbeef");
        proto.ShouldContain("// @ffmeta captured_at=2026-06-13T12:00:00.0000000+00:00");
        proto.ShouldContain("message FFFileOptions");
        proto.ShouldContain("layout_pointer_size: 8");
        proto.ShouldContain("message SampleState");
        proto.ShouldContain("// @reclass offset=0x10 length=0x4");
        proto.ShouldContain("// @fflayout shape=primitive(int32)");
        proto.ShouldContain("int32 counter = 1; // Sample counter value.");
        proto.ShouldNotContain("@ffanchor");
        proto.ShouldNotContain("@ffkeypoint");
        proto.ShouldNotContain("@ffresolve");
        proto.ShouldNotContain("@ffevidence");
        proto.ShouldNotContain("@reclass size=");
        proto.ShouldNotContain("@fflayout id=");
    }
}
