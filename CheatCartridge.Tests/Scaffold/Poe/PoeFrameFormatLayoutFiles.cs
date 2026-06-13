using System.IO;
using System.Text.Json;
using CheatCartridge.Tests.Scaffold.FrameFormat;

namespace CheatCartridge.Tests.Scaffold.Poe;

internal static class PoeFrameFormatLayoutFiles
{
    public static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "CheatCartridge.sln")) &&
                Directory.Exists(Path.Combine(directory.FullName, "CheatCartridge")))
            {
                return directory;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find CheatCartridge repository root.");
    }

    public static PoeFrameFormatLayoutCurrentManifest LoadCurrentManifest(DirectoryInfo repositoryRoot)
    {
        var path = Path.Combine(repositoryRoot.FullName, "docs", "PoE", "RE", "layouts", "resolved", "current.json");
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<PoeFrameFormatLayoutCurrentManifest>(json)
               ?? throw new InvalidOperationException($"Could not deserialize {path}.");
    }

    public static ParsedFfLayout LoadCurrentLayout(DirectoryInfo repositoryRoot)
    {
        var manifest = LoadCurrentManifest(repositoryRoot);
        return FrameFormatLayoutParser.ParseFile(repositoryRoot, manifest.LayoutPath);
    }
}

internal sealed record PoeFrameFormatLayoutCurrentManifest(
    string BuildId,
    string SourceSha256,
    string ModuleName,
    string ModuleKey,
    string LayoutPath,
    DateTimeOffset GeneratedAtUtc);
