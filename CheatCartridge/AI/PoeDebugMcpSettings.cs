namespace CheatCartridge.AI;

/// <summary>
/// Runtime switches for the local AI/MCP workbench.
/// </summary>
public static class PoeDebugMcpSettings
{
    public static bool IsEnabled { get; set; } = true;

    public static int Port { get; set; } = 41338;

    public static string EndpointUrl => Port > 0 ? $"http://127.0.0.1:{Port}/mcp" : string.Empty;
}
