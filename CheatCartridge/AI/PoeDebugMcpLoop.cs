using CheatCartridge.GameHelper;
using EyeAuras.AI;
using EyeAuras.AI.Desktop.RE;
using EyeAuras.AI.Mcp;
using EyeAuras.AI.PluginHost;
using EyeAuras.Memory;
using EyeAuras.Memory.Scaffolding;
using PoeShared.Logging;
using PoeShared.Prism;

namespace CheatCartridge.AI;

internal sealed class PoeDebugMcpLoop : IDisposable
{
    private static readonly TimeSpan DisposeWaitTimeout = TimeSpan.FromSeconds(3);

    private readonly IFluentLog log;
    private readonly int processId;
    private readonly string processName;
    private readonly string moduleName;
    private readonly IFactory<TheGame, IMemory> gameFactory;
    private readonly CancellationTokenSource cancellationTokenSource;
    private readonly Task worker;

    private PoeDebugMcpLoop(
        IFluentLog log,
        int processId,
        string processName,
        string moduleName,
        IFactory<TheGame, IMemory> gameFactory,
        CancellationToken cancellationToken)
    {
        this.log = log ?? throw new ArgumentNullException(nameof(log));
        this.processId = processId;
        this.processName = processName ?? string.Empty;
        this.moduleName = moduleName ?? throw new ArgumentNullException(nameof(moduleName));
        this.gameFactory = gameFactory ?? throw new ArgumentNullException(nameof(gameFactory));
        cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        worker = Task.Run(RunAsync, CancellationToken.None);
    }

    public static IDisposable StartIfEnabled(
        IFluentLog log,
        int processId,
        string processName,
        string moduleName,
        IFactory<TheGame, IMemory> gameFactory,
        CancellationToken cancellationToken)
    {
        if (!PoeDebugMcpSettings.IsEnabled)
        {
            return Disposable.Empty;
        }

        log.Info($"Starting PoE debug MCP at {PoeDebugMcpSettings.EndpointUrl} for PID {processId}");
        return new PoeDebugMcpLoop(log, processId, processName, moduleName, gameFactory, cancellationToken);
    }

    public void Dispose()
    {
        cancellationTokenSource.Cancel();
        try
        {
            worker.Wait(DisposeWaitTimeout);
        }
        catch (AggregateException e) when (e.InnerExceptions.All(x => x is OperationCanceledException))
        {
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            cancellationTokenSource.Dispose();
        }
    }

    private async Task RunAsync()
    {
        var cancellationToken = cancellationTokenSource.Token;
        AiMcpSession? session = null;
        AiMcpServerHost? host = null;

        try
        {
            using var process = LocalProcess.ByProcessId(processId);
            session = new AiMcpSession($"poe-debug-{processId}-{Guid.NewGuid():N}", AiBudgets.Default, new AiPluginHost());

            session.AddOrUpdatePlugin(
                "poe.debug.game",
                new PoeGameModelMcpPlugin(
                    new PoeGameModelMcpRuntime(
                        processId,
                        processName,
                        moduleName,
                        gameFactory.Create,
                        () => session.Plugins.Select(x => x.Name).ToArray())));
            session.AddOrUpdatePlugin(
                "re_process",
                new ReToolsPlugin(new ReFacade(process, session.ArtifactStore, session.Budgets)));

            host = new AiMcpServerHost(session, PoeDebugMcpSettings.Port);
            await host.StartAsync(cancellationToken).ConfigureAwait(false);
            log.Info(
                $"Started PoE debug MCP at {host.EndpointUrl} for PID {processId}. " +
                $"Plugins: {string.Join(", ", session.Plugins.Select(x => x.Name))}");

            while (!cancellationToken.IsCancellationRequested && process.IsValid)
            {
                await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception e)
        {
            log.Warn($"PoE debug MCP loop failed for PID {processId}", e);
        }
        finally
        {
            if (host != null)
            {
                try
                {
                    await host.StopAsync(CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    log.Warn($"Failed to stop PoE debug MCP host for PID {processId}", e);
                }
                finally
                {
                    host.Dispose();
                }
            }

            session?.Dispose();
            log.Info($"PoE debug MCP loop stopped for PID {processId}");
        }
    }
}
