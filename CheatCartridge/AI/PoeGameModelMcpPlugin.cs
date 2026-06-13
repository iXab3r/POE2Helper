using System.ComponentModel;
using CheatCartridge.GameHelper;
using CheatCartridge.GameHelper.GameOffsets.Objects.Components;
using CheatCartridge.GameHelper.RemoteObjects.Components;
using EyeAuras.AI.SemanticKernel;
using EyeAuras.Memory;
using EyeAuras.Memory.Scaffolding;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using PlayerComponent = CheatCartridge.GameHelper.RemoteObjects.Components.Player;

namespace CheatCartridge.AI;

internal sealed class PoeGameModelMcpPlugin : AiKernelPlugin
{
    private readonly PoeGameModelMcpRuntime runtime;

    public PoeGameModelMcpPlugin(PoeGameModelMcpRuntime runtime)
    {
        this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
    }

    public override void InitializeChatHistory(in ChatHistory chatHistory)
    {
        const string instruction =
            "This session exposes a bounded Path Of Exile object-model view from a separate debug MCP loop. " +
            "Use poe_debug_status to confirm the attached process and plugin boundary. " +
            "Use poe_read_player_snapshot for current player identity, location facts, and vitals. " +
            "Use re_process for raw memory, module, search, RTTI, disassembly, and artifact-backed reverse-engineering work. " +
            "FF layout generation and source offset updates intentionally live in explicit CheatCartridge.Tests tooling, not in the runtime MCP loop. " +
            "Do not assume these tools share state with the live bot loop; they intentionally use a separate LocalProcess reader.";
        if (!chatHistory.Any(x => x.Role == AuthorRole.System && string.Equals(x.Content, instruction, StringComparison.Ordinal)))
        {
            chatHistory.AddSystemMessage(instruction);
        }
    }

    [KernelFunction("poe_debug_status")]
    [Description("Describe the PoE MCP host and confirm that it uses an isolated LocalProcess reader.")]
    public PoeMcpStatusResult GetStatus()
    {
        return runtime.GetStatus();
    }

    [KernelFunction("poe_read_player_snapshot")]
    [Description("Read the current player identity, location facts, and vitals through the CheatCartridge object model.")]
    public PoePlayerSnapshotResult ReadPlayerSnapshot()
    {
        return runtime.ReadPlayerSnapshot();
    }
}

internal sealed class PoeGameModelMcpRuntime
{
    private readonly object gate = new();
    private readonly int processId;
    private readonly string processName;
    private readonly string moduleName;
    private readonly Func<IMemory, TheGame> gameFactory;
    private readonly Func<IReadOnlyList<string>> pluginNamesProvider;

    public PoeGameModelMcpRuntime(
        int processId,
        string processName,
        string moduleName,
        Func<IMemory, TheGame> gameFactory,
        Func<IReadOnlyList<string>> pluginNamesProvider)
    {
        this.processId = processId;
        this.processName = processName ?? string.Empty;
        this.moduleName = moduleName ?? throw new ArgumentNullException(nameof(moduleName));
        this.gameFactory = gameFactory ?? throw new ArgumentNullException(nameof(gameFactory));
        this.pluginNamesProvider = pluginNamesProvider ?? throw new ArgumentNullException(nameof(pluginNamesProvider));
    }

    public PoeMcpStatusResult GetStatus()
    {
        return new PoeMcpStatusResult
        {
            Success = true,
            ProcessId = processId,
            ProcessName = processName,
            ModuleName = moduleName,
            EndpointUrl = PoeDebugMcpSettings.EndpointUrl,
            Plugins = pluginNamesProvider(),
            UsesSeparateProcessReader = true
        };
    }

    public PoePlayerSnapshotResult ReadPlayerSnapshot()
    {
        lock (gate)
        {
            try
            {
                using var process = LocalProcess.ByProcessId(processId);
                using var memory = process.MemoryOfModule(moduleName);
                using var game = gameFactory(memory);
                game.UpdateData();

                var area = game.States.InGameStateObject.CurrentAreaInstance;
                var player = game.Player;
                var hasPlayerComponent = player.TryGetComponent<PlayerComponent>(out var playerComponent);
                var hasLifeComponent = player.TryGetComponent<Life>(out var life);

                return new PoePlayerSnapshotResult
                {
                    Success = true,
                    CapturedAt = DateTimeOffset.UtcNow,
                    ProcessId = processId,
                    ModuleName = moduleName,
                    PlayerName = hasPlayerComponent ? playerComponent?.Name ?? string.Empty : string.Empty,
                    EntityAddress = player.Address.ToHexadecimal(),
                    EntityAddressValue = (ulong)player.Address.ToInt64(),
                    EntityId = player.Id,
                    EntityPath = player.Path,
                    IsValid = player.IsValid,
                    CurrentAreaLevel = area.CurrentAreaLevel,
                    CurrentAreaHash = area.CurrentAreaHash,
                    EntitiesCount = area.EntitiesCount,
                    Health = hasLifeComponent ? CreateVital(life!.Health) : new PoeVitalSnapshot(),
                    Mana = hasLifeComponent ? CreateVital(life!.Mana) : new PoeVitalSnapshot(),
                    EnergyShield = hasLifeComponent ? CreateVital(life!.EnergyShield) : new PoeVitalSnapshot()
                };
            }
            catch (Exception e)
            {
                return new PoePlayerSnapshotResult
                {
                    Success = false,
                    ErrorMessage = e.Message,
                    CapturedAt = DateTimeOffset.UtcNow,
                    ProcessId = processId,
                    ModuleName = moduleName
                };
            }
        }
    }

    private static PoeVitalSnapshot CreateVital(VitalStruct vital)
    {
        return new PoeVitalSnapshot
        {
            Current = vital.Current,
            Total = vital.Total,
            CurrentPercent = vital.CurrentInPercent(),
            ReservedTotal = vital.ReservedTotal,
            ReservedPercent = vital.ReservedInPercent()
        };
    }
}
