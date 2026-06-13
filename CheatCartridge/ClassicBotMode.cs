using CheatCartridge.GameHelper;
using CheatCartridge.GameHelper.RemoteObjects.Components;
using EyeAuras.Memory;
using EyeAuras.Memory.Scaffolding;
using EyeAuras.Shared.Statistics;

namespace CheatCartridge;

internal sealed class ClassicBotMode
{
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(5);

    private readonly BotBrain brain;
    private readonly IFactory<TheGame, IMemory> gameFactory;

    public ClassicBotMode(BotBrain brain, IFactory<TheGame, IMemory> gameFactory)
    {
        this.brain = brain ?? throw new ArgumentNullException(nameof(brain));
        this.gameFactory = gameFactory ?? throw new ArgumentNullException(nameof(gameFactory));
    }

    public async Task Run(CancellationToken cancellationToken)
    {
        var disabledRightFromTheStart = brain.IsEnabledAura.IsActive != true;
        brain.Window.Show();

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        using var whenClosedCancel = brain.Window.WhenClosed.Subscribe(_ => linkedCts.Cancel());

        using var closeIfDisabled = brain.IsEnabledAura.IsActive == true
            ? brain.IsEnabledAura.WhenAnyValue(x => x.IsActive).Where(x => x != true).Subscribe(_ => linkedCts.Cancel())
            : Disposable.Empty;

        brain.Log.Info("Starting classic bot loop");
        while (!linkedCts.IsCancellationRequested)
        {
            try
            {
                if (!disabledRightFromTheStart && brain.IsEnabledAura.IsActive != true)
                {
                    brain.Log.Info("Bot is no longer enabled - breaking classic bot loop");
                    break;
                }

                if (!brain.IsEnabled)
                {
                    await Delay(RetryDelay, linkedCts.Token).ConfigureAwait(false);
                    continue;
                }

                var targetProcessId = brain.WinActiveTrigger.ActiveWindow?.ProcessId;
                if (targetProcessId == null)
                {
                    brain.Log.Warn("Path Of Exile 2 client not found");
                    await Delay(RetryDelay, linkedCts.Token).ConfigureAwait(false);
                    continue;
                }

                brain.Log.Info($"Binding to process with Id {targetProcessId}");
                brain.TargetProcessId = targetProcessId;

                using var process = LocalProcess.ByProcessId(targetProcessId.Value);
                brain.Log.Info($"Found Path Of Exile Process: {process}");

                var moduleName = Path.GetFileNameWithoutExtension(process.ProcessName) + ".exe";
                brain.Log.Info($"Process: {process}, process name: {moduleName}, module name: {moduleName}");

                using var memory = process.MemoryOfModule(moduleName);
                await BindToProcess(memory, linkedCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (linkedCts.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                brain.Log.Error("Exception occurred in classic bot loop", ex);
                await Delay(RetryDelay, linkedCts.Token).ConfigureAwait(false);
            }
        }

        brain.IsEnabledHotkeyTrigger.TriggerValue = false;
        brain.Log.Info("Classic bot loop stopped");
    }

    private async Task BindToProcess(IMemory memory, CancellationToken cancellationToken)
    {
        using var game = gameFactory.Create(memory);
        var fpsStats = new ConcurrentMovingStatistics(100);

        var sw = Stopwatch.StartNew();
        while (!cancellationToken.IsCancellationRequested && brain.IsEnabled)
        {
            sw.Restart();
            var targetFrameDelay = 1000 / Math.Max(brain.TargetFps, 1);
            await using (new ForcedDelayBlock(targetFrameDelay))
            {
                game.UpdateData();

                if (game.Player.TryGetComponent<Life>(out var playerLife))
                {
                    brain.HealthCurrent = playerLife.Health.Current;
                    brain.HealthMax = playerLife.Health.Total;
                    brain.HealthPercentage = playerLife.Health.CurrentInPercent();

                    brain.ManaCurrent = playerLife.Mana.Current;
                    brain.ManaMax = playerLife.Mana.Total;
                    brain.ManaPercentage = playerLife.Mana.CurrentInPercent();

                    brain.EnergyShieldCurrent = playerLife.EnergyShield.Current;
                    brain.EnergyShieldMax = playerLife.EnergyShield.Total;
                    brain.EnergyShieldPercentage = playerLife.EnergyShield.CurrentInPercent();
                }

                brain.BehaviorTree.Variables.Edit(cache =>
                {
                    cache.AddOrUpdate(new AuraVariable(nameof(brain.HealthPercentage), brain.HealthPercentage));
                    cache.AddOrUpdate(new AuraVariable(nameof(brain.ManaPercentage), brain.ManaPercentage));
                    cache.AddOrUpdate(new AuraVariable(nameof(brain.EnergyShieldPercentage), brain.EnergyShieldPercentage));
                });

                await brain.BehaviorTree.TickAsync(cancellationToken).ConfigureAwait(false);
            }

            sw.Stop();

            var frameTime = sw.ElapsedMilliseconds < 0 ? 0 : sw.ElapsedMilliseconds;
            fpsStats.Push(frameTime);
            brain.FrameTime = fpsStats.GetValue();
        }
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
