using System.Reflection;
using System.IO;
using CheatCartridge.GameHelper;
using CheatCartridge.GameHelper.GameOffsets;
using CheatCartridge.Tests.Scaffold.FrameFormat;

namespace CheatCartridge.Tests.Scaffold.Poe;

public sealed class PoeFrameFormatLayoutMappingTests
{
    [Test]
    public void ShouldLoadCurrentManifestAndResolvedFfShape()
    {
        // Given
        var repositoryRoot = PoeFrameFormatLayoutFiles.FindRepositoryRoot();

        // When
        var manifest = PoeFrameFormatLayoutFiles.LoadCurrentManifest(repositoryRoot);
        var ff = FrameFormatLayoutParser.ParseFile(repositoryRoot, manifest.LayoutPath);

        // Then
        manifest.BuildId.ShouldStartWith("sha256-");
        manifest.SourceSha256.ShouldNotBeNullOrWhiteSpace();
        manifest.SourceSha256.Length.ShouldBe(64);
        manifest.ModuleName.ShouldNotBeNullOrWhiteSpace();
        manifest.ModuleKey.ShouldNotBeNullOrWhiteSpace();
        manifest.LayoutPath.ShouldContain($"/{manifest.BuildId}/");
        manifest.LayoutPath.ShouldContain($"/{manifest.ModuleKey}/");
        manifest.LayoutPath.ShouldEndWith($".{manifest.BuildId}.ff.proto");
        File.Exists(Path.Combine(repositoryRoot.FullName, manifest.LayoutPath.Replace('/', Path.DirectorySeparatorChar))).ShouldBeTrue();

        ff.SourceName.ShouldBe(Path.GetFileName(manifest.LayoutPath)[..^".ff.proto".Length]);
        ff.LayoutName.ShouldBe("poe-game-model");
        ff.SourceModuleName.ShouldBe(manifest.ModuleName);
        ff.BuildId.ShouldBe(manifest.BuildId);
        ff.SourceSha256.ShouldBe(manifest.SourceSha256);

        var stateTable = RequireField(ff, "GameStates", "state_table");
        stateTable.ArrayCount.ShouldNotBeNull();
        stateTable.ArrayCount.Value.ShouldBeGreaterThan(0);
        RequireField(ff, "InGameState", "area_instance_data").Shape.ShouldContain("AreaInstance");
        RequireField(ff, "InGameState", "ms_elapsed").Shape.ShouldContain("int32");
        RequireField(ff, "InGameState", "zone_switch_counter").Shape.ShouldContain("int32");
        RequireField(ff, "AreaInstance", "local_players").Shape.ShouldContain("StdVector");
        RequireField(ff, "AreaInstance", "entities_count").Shape.ShouldContain("uint32");
        RequireField(ff, "Entity", "component_list").Shape.ShouldContain("StdBucket");
        RequireField(ff, "Life", "health").Shape.ShouldContain("Vital");
        RequireField(ff, "Life", "mana").Shape.ShouldContain("Vital");
        RequireField(ff, "Life", "energy_shield").Shape.ShouldContain("Vital");
        RequireField(ff, "Vital", "current").Shape.ShouldContain("int32");
        RequireField(ff, "Vital", "total").Shape.ShouldContain("int32");
    }

    [Test]
    public void FrameFormatAnnotatedRuntimeFieldsShouldMatchCurrentFf()
    {
        // Given
        var repositoryRoot = PoeFrameFormatLayoutFiles.FindRepositoryRoot();
        var ff = PoeFrameFormatLayoutFiles.LoadCurrentLayout(repositoryRoot);
        var mappings = FrameFormatReflection.GetMappings(typeof(TheGame).Assembly);

        // When
        var violations = mappings
            .Select(x => ValidateMapping(ff, x))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToArray();

        // Then
        mappings.Count.ShouldBeGreaterThan(30, "Expected the core ABI structs to be annotated with FrameFormatField.");
        violations.ShouldBeEmpty();
    }

    [Test]
    public void FrameFormatSourceUpdaterShouldBeStableAgainstCurrentFf()
    {
        // Given
        var repositoryRoot = PoeFrameFormatLayoutFiles.FindRepositoryRoot();
        var ff = PoeFrameFormatLayoutFiles.LoadCurrentLayout(repositoryRoot);

        // When
        var result = FrameFormatSourceUpdater.UpdateSourceTree(repositoryRoot, ff, apply: false);

        // Then
        result.Changes.ShouldBeEmpty("Current source offsets should already match docs/PoE/RE/layouts/resolved/current.json.");
    }

    [Test]
    [Explicit("Updates CheatCartridge [FieldOffset] constants from docs/PoE/RE/layouts/resolved/current.json.")]
    public void ShouldUpdateFrameFormatAnnotatedSourceOffsetsFromCurrentFf()
    {
        // Given
        var repositoryRoot = PoeFrameFormatLayoutFiles.FindRepositoryRoot();
        var ff = PoeFrameFormatLayoutFiles.LoadCurrentLayout(repositoryRoot);

        // When
        var result = FrameFormatSourceUpdater.UpdateSourceTree(repositoryRoot, ff, apply: true);

        // Then
        foreach (var change in result.Changes)
        {
            TestContext.Progress.WriteLine(
                $"{change.FilePath}:{change.Line} {change.FrameFormatPath} {change.OldValue} -> {change.NewValue}");
        }
    }

    [Test]
    public void ProductionProjectShouldNotCompileFfResolverOrGeneratedLayoutProvider()
    {
        // Given
        var repositoryRoot = PoeFrameFormatLayoutFiles.FindRepositoryRoot();
        var productionRoot = Path.Combine(repositoryRoot.FullName, "CheatCartridge");
        var files = Directory.EnumerateFiles(productionRoot, "*.cs", SearchOption.AllDirectories);

        // When
        var violations = files
            .SelectMany(file =>
            {
                var text = File.ReadAllText(file);
                var relativePath = Path.GetRelativePath(repositoryRoot.FullName, file);
                return new[]
                    {
                        text.Contains("PoeFfLayoutProtoWriter", StringComparison.Ordinal)
                            ? $"{relativePath}: contains PoeFfLayoutProtoWriter"
                            : string.Empty,
                        text.Contains("PoeGeneratedRuntimeLayouts", StringComparison.Ordinal)
                            ? $"{relativePath}: contains PoeGeneratedRuntimeLayouts"
                            : string.Empty
                    }
                    .Where(x => !string.IsNullOrWhiteSpace(x));
            })
            .ToArray();

        // Then
        violations.ShouldBeEmpty();
    }

    private static string ValidateMapping(ParsedFfLayout ff, FrameFormatMemberMapping mapping)
    {
        var ffField = ResolveField(ff, mapping.FrameFormatPath);
        return mapping.Binding switch
        {
            FrameFormatBinding.Offset when mapping.RuntimeOffset != ffField.Offset =>
                $"{FormatMember(mapping)} offset 0x{mapping.RuntimeOffset:X} != FF 0x{ffField.Offset:X}",
            FrameFormatBinding.ArrayCount when ffField.ArrayCount == null =>
                $"{FormatMember(mapping)} maps to {mapping.FrameFormatPath}, but FF field has no array count",
            FrameFormatBinding.ArrayCount when Convert.ToInt32(mapping.ConstantValue) != ffField.ArrayCount.Value =>
                $"{FormatMember(mapping)} count {mapping.ConstantValue} != FF {ffField.ArrayCount.Value}",
            _ when mapping.RequiresGeneratedProvenance && mapping.GeneratedSourceName != ff.SourceName =>
                $"{FormatMember(mapping)} source '{mapping.GeneratedSourceName}' != FF source '{ff.SourceName}'",
            _ when mapping.RequiresGeneratedProvenance && mapping.GeneratedTimestampUtc != ff.CapturedAt =>
                $"{FormatMember(mapping)} timestamp '{mapping.GeneratedTimestampUtc}' != FF captured_at '{ff.CapturedAt}'",
            _ when mapping.RequiresGeneratedProvenance &&
                   (string.IsNullOrWhiteSpace(mapping.GeneratedComment) ||
                    !mapping.GeneratedComment.Contains(mapping.FrameFormatPath, StringComparison.Ordinal)) =>
                $"{FormatMember(mapping)} generated comment must mention {mapping.FrameFormatPath}",
            _ => string.Empty
        };
    }

    private static ParsedFfField ResolveField(ParsedFfLayout ff, string path)
    {
        var separatorIndex = path.IndexOf('.', StringComparison.Ordinal);
        separatorIndex.ShouldBeGreaterThan(0, $"FrameFormat path '{path}' must be Type.field.");

        var typeName = path[..separatorIndex];
        var fieldName = path[(separatorIndex + 1)..];
        ff.Messages.ContainsKey(typeName).ShouldBeTrue($"FF type '{typeName}' must exist.");
        ff.Messages[typeName].Fields.ContainsKey(fieldName).ShouldBeTrue($"FF field '{path}' must exist.");
        return ff.Messages[typeName].Fields[fieldName];
    }

    private static ParsedFfField RequireField(ParsedFfLayout ff, string typeName, string fieldName)
    {
        ff.Messages.ContainsKey(typeName).ShouldBeTrue($"FF type '{typeName}' must exist.");
        ff.Messages[typeName].Fields.ContainsKey(fieldName).ShouldBeTrue($"FF field '{typeName}.{fieldName}' must exist.");
        var field = ff.Messages[typeName].Fields[fieldName];
        field.Offset.ShouldBeGreaterThanOrEqualTo(0);
        field.Length.ShouldBeGreaterThan(0);
        return field;
    }

    private static string FormatMember(FrameFormatMemberMapping mapping)
    {
        return $"{mapping.ClrType.FullName}.{mapping.MemberName}";
    }
}
