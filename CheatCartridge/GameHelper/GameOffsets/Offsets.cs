using CheatCartridge.GameHelper.RemoteObjects;
using EyeAuras.Memory.Scaffolding;

namespace CheatCartridge.GameHelper.GameOffsets;

public static class Offsets
{
    public static readonly IBytePattern[] Patterns =
    {
        BytePattern.FromTemplate(
        
            "48 83 EC ?? 48 8B F1 33 ED 48 39 2D ^ ?? ?? ?? ??",
            nameof(GameStates)
        ),
    };
}