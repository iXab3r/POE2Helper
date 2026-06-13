using CheatCartridge.GameHelper;
using CheatCartridge.GameHelper.GameOffsets.States;
using CheatCartridge.GameHelper.RemoteObjects.Components;
using EyeAuras.Memory;
using EyeAuras.Memory.Scaffolding;

namespace CheatCartridge;

public sealed class HeadlessBotMode : DisposableReactiveObject
{
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan TickDelay = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan StatsLogInterval = TimeSpan.FromSeconds(5);

    private readonly IFluentLog log;
    private readonly IFactory<TheGame, IMemory> gameFactory;

    public HeadlessBotMode(
        IFluentLog log,
        IFactory<TheGame, IMemory> gameFactory)
    {
        this.log = log ?? throw new ArgumentNullException(nameof(log));
        this.gameFactory = gameFactory ?? throw new ArgumentNullException(nameof(gameFactory));
    }

    public async Task Run(CancellationToken cancellationToken)
    {
        log.Info("Starting headless PoE debug loop");
        log.Info(
            AI.PoeDebugMcpSettings.IsEnabled
                ? $"PoE debug MCP endpoint: {AI.PoeDebugMcpSettings.EndpointUrl}"
                : $"PoE debug MCP is disabled");

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                using var targetProcess = PoeClientProcessDiscovery.TryFindRunningProcess();
                if (targetProcess == null)
                {
                    log.Warn("Path Of Exile client not found");
                    await Delay(RetryDelay, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                log.Info($"Headless mode attaching to {PoeClientProcessDiscovery.Describe(targetProcess)}");
                using var process = LocalProcess.ByProcessId(targetProcess.Id);

                var moduleName = Path.GetFileNameWithoutExtension(process.ProcessName) + ".exe";
                log.Info($"Headless mode attached to {process}; module name: {moduleName}");

                using var debugMcpLoop = AI.PoeDebugMcpLoop.StartIfEnabled(
                    log,
                    targetProcess.Id,
                    process.ProcessName ?? string.Empty,
                    moduleName,
                    gameFactory,
                    cancellationToken);

                using var memory = process.MemoryOfModule(moduleName);
                using var game = gameFactory.Create(memory);
                var lastStatsLog = Stopwatch.StartNew();

                while (!cancellationToken.IsCancellationRequested && process.IsValid)
                {
                    game.UpdateData();

                    if (lastStatsLog.Elapsed >= StatsLogInterval)
                    {
                        var zoneSwitchCounter = ReadZoneSwitchCounter(game);
                        var areaStats = FormatAreaStats(game);
                        if (game.Player.TryGetComponent<Life>(out var playerLife))
                        {
                            log.Info(
                                "Headless character stats: " +
                                $"HP {playerLife.Health.Current}/{playerLife.Health.Total}, " +
                                $"MP {playerLife.Mana.Current}/{playerLife.Mana.Total}, " +
                                $"ES {playerLife.EnergyShield.Current}/{playerLife.EnergyShield.Total}, " +
                                $"ZoneSwitchCounter {FormatCounter(zoneSwitchCounter)}, " +
                                areaStats);
                        }
                        else
                        {
                            log.Info(
                                "Headless character stats: " +
                                $"Life component unavailable, " +
                                $"ZoneSwitchCounter {FormatCounter(zoneSwitchCounter)}, " +
                                areaStats);
                        }

                        lastStatsLog.Restart();
                    }

                    await Delay(TickDelay, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                log.Warn("Exception occurred in headless PoE debug loop", ex);
                await Delay(RetryDelay, cancellationToken).ConfigureAwait(false);
            }
        }

        log.Info("Headless PoE debug loop stopped");
    }

    private static int? ReadZoneSwitchCounter(TheGame game)
    {
        var inGameStateAddress = game.States.InGameStateObject.Address;
        if (inGameStateAddress == IntPtr.Zero)
        {
            return null;
        }

        var inGameState = game.Memory.Read<InGameStateOffset>(inGameStateAddress);
        return inGameState.ZoneSwitchCounter;
    }

    private static string FormatCounter(int? value)
    {
        return value.HasValue ? $"{value.Value} (0x{value.Value:X})" : "n/a";
    }

    private static string FormatAreaStats(TheGame game)
    {
        var area = game.States.InGameStateObject.CurrentAreaInstance;
        if (area.Address == IntPtr.Zero)
        {
            return "Area n/a";
        }

        return
            $"Area 0x{area.Address.ToInt64():X}, " +
            $"Level {area.CurrentAreaLevel}, " +
            $"Hash {area.CurrentAreaHash} (0x{area.CurrentAreaHash:X8}), " +
            $"Entities {area.EntitiesCount}";
    }

    private static async Task Delay(TimeSpan delay, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }
}
