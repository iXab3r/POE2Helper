using System.Windows;
using CheatCartridge.GameHelper;
using CheatCartridge.GameHelper.RemoteObjects.Components;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;

namespace CheatCartridge;

using EyeAuras.Memory;
using EyeAuras.Memory.Scaffolding;
using StackExchange.Profiling;
using EyeAuras.Shared.Statistics;
using System.Reactive.Threading.Tasks;

/// <summary>
///   This is the primary class which holds all the logic your bot has.
///   Note that it inherits from DisposableReactiveObject
///   which by itself provides automated NotifyPropertyChanged notifications plus few utility
///   properties such as Anchors, which are Disposed with the class
/// </summary>
public sealed class BotBrain : DisposableReactiveObject
{
    //Binder is a mechanism which allows to wire multiple properties together
    //whenever one of those properties gets updated (by anything)
    //all dependent properties will be recalculated
    private static readonly Binder<BotBrain> Binder = new();
    private static readonly TimeSpan ConfigSaveSamplingTimeout = TimeSpan.FromSeconds(3);

    static BotBrain()
    {
        Binder.BindIf(x => x.IsExpanded && x.Window.Width > 0 && x.Window.Height > 0, x => new Size(x.Window.Width, x.Window.Height)).To((x, v) =>
        {
            x.Log.Info($"Window size changed to {v}");
            x.ExpandedWindowSize = v;
        });
        
        Binder.Bind(x => !x.IsExpanded).To(x => x.Window.NoActivate);
    }

    private readonly IConfigSerializer configSerializer;
    private readonly IFactory<TheGame, IMemory> gameFactory;
    private readonly IHotkeyConverter hotkeyConverter;

    public BotBrain(
        IFluentLog log,
        IAuraTreeScriptingApi auraTree, //used to access auras/scripts/macroses/behavior trees in EA
        IAppArguments appArguments,
        IHotkeyConverter hotkeyConverter, //used to convert hotkeys to strings and vice-versa
        IWindowListProvider windowListProvider, //used to get list of windows + provides API to control them on top of it
        IConfigSerializer configSerializer, //used to write/read configurations
        IDialogWindowUnstableScriptingApi dialogApi, //used to create windows
        IFactory<TheGame, IMemory> gameFactory)
    {
        Log = log;
        AuraTree = auraTree;
        BehaviorTree = auraTree.GetBehaviorTreeByPath("./Tree");
        WinActiveTrigger = auraTree.GetTriggerByPath<IWinActiveTrigger>("./Conditions/WindowIsActive");
        IsEnabledAura = auraTree.GetAuraByPath("./IsEnabled");
        IsEnabledHotkeyTrigger = auraTree.GetTriggerByPath<IHotkeyIsActiveTrigger>("./IsEnabled");
        this.configSerializer = configSerializer;
        this.gameFactory = gameFactory;
        this.hotkeyConverter = hotkeyConverter;

        Window = dialogApi
            .CreateWindow<PoeEyeComponent>()
            .AddTo(Anchors);
        Window.ShowInTaskbar = false;
        Window.Topmost = true;
        Window.ShowMaxButton = false;
        Window.ShowMinButton = false;
        Window.Padding = new Thickness(5);
        Window.BackgroundColor = System.Windows.Media.Color.FromArgb(1, 0, 0, 0);
        Window.ViewDataContext = this;
        Window.WindowStartupLocation = WindowStartupLocation.Manual;
        Window.NoActivate = true;

        var configFilePathCandidates = new[]
            {
                new OSPath(Path.Combine(appArguments.AppDataDirectory, "userconfigs", "poe2helper.cfg")), //user in older versions
                new OSPath(Path.Combine(Environment.ExpandEnvironmentVariables("%appdata%/poe2helper"), "userconfigs", "poe2helper.cfg"))
            }.Select(x => new {FilePath = x, Exists = File.Exists(x.FullName)})
            .ToArray();
        Log.Info($"Configuration files matrix:\n{configFilePathCandidates.DumpToTable()}");

        ConfigFilePath = configFilePathCandidates.FirstOrDefault(x => x.Exists)?.FilePath ?? configFilePathCandidates.Last().FilePath;

        // loading config from file, will throw if something is wrong
        LoadConfigFromFile();
        
        EnsureWindowIsInsideScreenBounds();

        this.WhenAnyValue(x => x.IsExpanded)
            .Subscribe(isExpanded =>
            {
                if (isExpanded)
                {
                    Log.Info("Entering config mode");
                    Window.TitleBarDisplayMode = TitleBarDisplayMode.Custom;
                    Window.ResizeMode = System.Windows.ResizeMode.CanResizeWithGrip;
                    Window.SetWindowSize(ExpandedWindowSize);
                }
                else
                {
                    Log.Info("Exiting config mode");
                    Window.TitleBarDisplayMode = TitleBarDisplayMode.None;
                    Window.ResizeMode = System.Windows.ResizeMode.NoResize;
                    Window.SetWindowSize(new Size(130, 90));
                }
            })
            .AddTo(Anchors);

        // propagate properties to BT for analysis
        this.WhenAnyValue(x => x.AutoMana)
            .Subscribe(x => BehaviorTree[nameof(AutoMana)] = x)
            .AddTo(Anchors);

        this.WhenAnyValue(x => x.AutoHealth)
            .Subscribe(x => BehaviorTree[nameof(AutoHealth)] = x)
            .AddTo(Anchors);
        
        this.WhenAnyValue(x => x.AutoEnergyShield)
            .Subscribe(x => BehaviorTree[nameof(AutoEnergyShield)] = x)
            .AddTo(Anchors);

        this.WhenAnyValue(x => x.HealthPotionKey)
            .Subscribe(x => BehaviorTree[nameof(HealthPotionKey)] = x?.ToString())
            .AddTo(Anchors);

        this.WhenAnyValue(x => x.ManaPotionKey)
            .Subscribe(x => BehaviorTree[nameof(ManaPotionKey)] = x?.ToString())
            .AddTo(Anchors);
        
        this.WhenAnyValue(x => x.EnergyShieldPotionKey)
            .Subscribe(x => BehaviorTree[nameof(EnergyShieldPotionKey)] = x?.ToString())
            .AddTo(Anchors);

        this.WhenAnyValue(x => x.MinHealthPotionPercentage)
            .Subscribe(x => BehaviorTree[nameof(MinHealthPotionPercentage)] = x)
            .AddTo(Anchors);

        this.WhenAnyValue(x => x.MinManaPotionPercentage)
            .Subscribe(x => BehaviorTree[nameof(MinManaPotionPercentage)] = x)
            .AddTo(Anchors);
        
        this.WhenAnyValue(x => x.MinEnergyShieldPotionPercentage)
            .Subscribe(x => BehaviorTree[nameof(MinEnergyShieldPotionPercentage)] = x)
            .AddTo(Anchors);

        // hide overlay window if neither POE nor Overlay window are active
        WinActiveTrigger
            .WhenAnyValue(x => x.IsActive)
            .Subscribe(x =>
            {
                var foregroundWindow = windowListProvider.GetForegroundWindow();
                Window.IsVisible = foregroundWindow.ProcessId
                    == Environment.ProcessId || foregroundWindow.ProcessId == WinActiveTrigger.ActiveWindow?.ProcessId;
            })
            .AddTo(Anchors);

        this.WhenAnyValue(x => x.TargetProcessId)
            .Subscribe(x => Window.Title = "POE2 Eye Helper" + (x == null ? " :: NOT FOUND" : $" :: PID {x}"))
            .AddTo(Anchors);

        // tracking changes in bot config and periodically save them to file
        Observable.Merge(
                this.WhenAnyValue(x => x.MinHealthPotionPercentage).ToUnit(),
                this.WhenAnyValue(x => x.MinManaPotionPercentage).ToUnit(),
                this.WhenAnyValue(x => x.MinEnergyShieldPotionPercentage).ToUnit(),
                this.WhenAnyValue(x => x.IsExpanded).ToUnit(),
                this.WhenAnyValue(x => x.TargetFps).ToUnit(),
                this.WhenAnyValue(x => x.HealthPotionKey).ToUnit(),
                this.WhenAnyValue(x => x.ManaPotionKey).ToUnit(),
                this.WhenAnyValue(x => x.EnergyShieldPotionKey).ToUnit(),
                this.WhenAnyValue(x => x.Window.Left).ToUnit(),
                this.WhenAnyValue(x => x.Window.Top).ToUnit(),
                this.WhenAnyValue(x => x.ExpandedWindowSize).ToUnit()
            )
            .Buffer(ConfigSaveSamplingTimeout)
            .Where(x => x.Count > 0)
            .Skip(1)
            .Subscribe(SaveConfigToFileSafe)
            .AddTo(Anchors);

        Log.Info("Bot is ready to run");
        Binder.Attach(this).AddTo(Anchors);
    }

    public IFluentLog Log { get; }

    public IBlazorWindow Window { get; }

    public IAuraTreeScriptingApi AuraTree { get; }

    public IWinActiveTrigger WinActiveTrigger { get; }

    public IBehaviorTreeAccessor BehaviorTree { get; }

    public IAuraAccessor IsEnabledAura { get; }

    public IHotkeyIsActiveTrigger IsEnabledHotkeyTrigger { get; }

    public OSPath ConfigFilePath { get; }

    public bool IsExpanded { get; set; }

    public bool AutoHealth { get; set; }
    
    public bool AutoEnergyShield { get; set; }

    public bool AutoMana { get; set; }

    public double MinHealthPotionPercentage { get; set; }

    public double MinManaPotionPercentage { get; set; }
    
    public double MinEnergyShieldPotionPercentage { get; set; }

    public double TargetFps { get; set; }

    public HotkeyGesture HealthPotionKey { get; set; }

    public HotkeyGesture ManaPotionKey { get; set; }
    
    public HotkeyGesture EnergyShieldPotionKey { get; set; }

    public bool IsEnabled { get; [UsedImplicitly] private set; } = true;

    public double HealthPercentage { get; private set; }

    public double HealthCurrent { get; private set; }

    public double HealthMax { get; private set; }

    public double ManaPercentage { get; private set; }

    public double ManaCurrent { get; private set; }

    public double ManaMax { get; private set; }
    
    public double EnergyShieldPercentage { get; private set; }

    public double EnergyShieldCurrent { get; private set; }

    public double EnergyShieldMax { get; private set; }

    public MovingStatisticsValue<double> FrameTime { get; private set; }

    public int? TargetProcessId { get; private set; }

    public Size ExpandedWindowSize { get; [UsedImplicitly] private set; }

    public async Task Run(CancellationToken cancellationToken)
    {
        //this flag shows that the bot has been started manually
        var disabledRightFromTheStart = IsEnabledAura.IsActive != true;
        Window.Show();

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        using var whenClosedCancel = Window.WhenClosed.Subscribe(x => linkedCts.Cancel());

        using var closeIfDisabled = IsEnabledAura.IsActive == true
            ? IsEnabledAura.WhenAnyValue(x => x.IsActive).Where(x => x != true).Subscribe(() => linkedCts.Cancel())
            : Disposable.Empty;

        Log.Info("Starting bot loop");
        while (!linkedCts.IsCancellationRequested)
        {
            try
            {
                if (!disabledRightFromTheStart && IsEnabledAura.IsActive != true)
                {
                    Log.Info("Bot is no longer enabled - breaking bot loop");
                    break;
                }

                if (!IsEnabled)
                {
                    linkedCts.Token.Sleep(5000);
                    continue;
                }

                var targetProcessId = WinActiveTrigger.ActiveWindow?.ProcessId;
                if (targetProcessId == null)
                {
                    Log.Warn("Path Of Exile 2 client not found");
                    linkedCts.Token.Sleep(5000);
                    continue;
                }

                Log.Info($"Binding to process with Id {targetProcessId}");
                TargetProcessId = targetProcessId;
                
                using var process = 
                    EyeAuras.Memory.LocalProcess.ByProcessId(targetProcessId.Value); //uses naive RPM under the hood
                Log.Info($"Found Path Of Exile Process: {process}");
                
                var moduleName = Path.GetFileNameWithoutExtension(process.ProcessName) + ".exe";
                Log.Info($"Process: {process}, process name: {moduleName}, module name: {moduleName}");

                using var memory = process.MemoryOfModule(moduleName); //get specific module by name
                await BindToProcess(memory, linkedCts.Token);
            }
            catch (Exception ex)
            {
                Log.Error("Exception occurred", ex);
                linkedCts.Token.Sleep(5000);
            }
        }

        // reset hotkey state
        IsEnabledHotkeyTrigger.TriggerValue = false;
        Log.Info("Bot is no longer running");
    }

    private void EnsureWindowIsInsideScreenBounds()
    {
        var windowRect = new Rectangle(Window.Left, Window.Top, Window.Width, Window.Height);
        var screenSize = System.Windows.Forms.Screen.PrimaryScreen!.WorkingArea;

        // Calculate the minimum visible area required (50% of window size)
        var minVisibleWidth = Window.Width / 2;
        var minVisibleHeight = Window.Height / 2;

        // Check if the visible portion of the window is less than 50%
        var isOutOfBounds =
            (windowRect.Left + minVisibleWidth < 0) || // Too far left
            (windowRect.Top + minVisibleHeight < 0) || // Too far up
            (windowRect.Right - minVisibleWidth > screenSize.Width) || // Too far right
            (windowRect.Bottom - minVisibleHeight > screenSize.Height); // Too far down
        if (isOutOfBounds)
        {
            Log.Warn($"Window is considered out of screen bounds, window position: {windowRect}, screen: {screenSize}");
        }

        var positionNotSet = Window.Left == 0 && Window.Top == 0;
        if (isOutOfBounds)
        {
            Log.Warn($"Window position is not set, resetting, screen: {screenSize}");
        }
        
        if (positionNotSet || isOutOfBounds)
        {
            var centered = windowRect.Size.CenterInsideBounds(screenSize);
            Log.Info($"Setting window position to {centered}");
            Window.SetWindowRect(centered);
        }
    }

    private void SaveConfigToFileSafe()
    {
        try
        {
            var configFile = new FileInfo(ConfigFilePath.ToString());
            var config = new PoeHelperConfig
            {
                HealthPotionKey = hotkeyConverter.ConvertToString(HealthPotionKey ?? HotkeyGesture.Empty),
                ManaPotionKey = hotkeyConverter.ConvertToString(ManaPotionKey ?? HotkeyGesture.Empty),
                EnergyShieldPotionKey = hotkeyConverter.ConvertToString(EnergyShieldPotionKey ?? HotkeyGesture.Empty),
                MinHealthPotionPercentage = MinHealthPotionPercentage,
                MinManaPotionPercentage = MinManaPotionPercentage,
                MinEnergyShieldPotionPercentage = MinEnergyShieldPotionPercentage,
                TargetFps = TargetFps,
                IsExpanded = IsExpanded,
                WindowBounds = new Rectangle(new Point(Window.Left, Window.Top), ExpandedWindowSize)
            };
            Log.Info($"Saving config @ {ConfigFilePath}\n{config}");
            configSerializer.Serialize(config, configFile);
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to save config @ {ConfigFilePath}", ex);
        }
    }

    private void LoadConfig(PoeHelperConfig config)
    {
        Log.Info($"Deserialized config:\n{config}");

        MinHealthPotionPercentage = config.MinHealthPotionPercentage;
        MinManaPotionPercentage = config.MinManaPotionPercentage;
        MinEnergyShieldPotionPercentage = config.MinEnergyShieldPotionPercentage;
        HealthPotionKey = hotkeyConverter.ConvertFromString(config.HealthPotionKey ?? string.Empty);
        ManaPotionKey = hotkeyConverter.ConvertFromString(config.ManaPotionKey ?? string.Empty);
        EnergyShieldPotionKey = hotkeyConverter.ConvertFromString(config.EnergyShieldPotionKey ?? string.Empty);
        TargetFps = config.TargetFps;

        Window.Left = config.WindowBounds.Left;
        Window.Top = config.WindowBounds.Top;
        ExpandedWindowSize = config.WindowBounds.Size;
        
        IsExpanded = config.IsExpanded;
        if (IsExpanded)
        {
            Window.Width = config.WindowBounds.Width;
            Window.Height = config.WindowBounds.Height;
        }
    }

    private void LoadConfigFromFile()
    {
        var configFile = new FileInfo(ConfigFilePath.ToString());
        Log.Info($"Loading config @ {ConfigFilePath} (exists: {configFile.Exists})");

        PoeHelperConfig config;
        if (configFile.Exists)
        {
            Log.Info($"Deserializing config {ByteSizeLib.ByteSize.FromBytes(configFile.Length)}");
            config = configSerializer.Deserialize<PoeHelperConfig>(configFile);
        }
        else
        {
            Log.Info($"Config does not exist, using defaults");
            config = new PoeHelperConfig();
        }

        LoadConfig(config);
    }

    private async Task BindToProcess(IMemory memory, CancellationToken cancellationToken)
    {
        var game = gameFactory.Create(memory);
        var fpsStats = new ConcurrentMovingStatistics(100);

        var sw = Stopwatch.StartNew();
        while (!cancellationToken.IsCancellationRequested && IsEnabled)
        {
            sw.Restart();
            var targetFrameDelay = 1000 / Math.Max(TargetFps, 1);
            await using (new ForcedDelayBlock(targetFrameDelay))
            {
                game.UpdateData();

                if (game.Player.TryGetComponent<Life>(out var playerLife))
                {
                    HealthCurrent = playerLife.Health.Current;
                    HealthMax = playerLife.Health.Total;
                    HealthPercentage = playerLife.Health.CurrentInPercent();

                    ManaCurrent = playerLife.Mana.Current;
                    ManaMax = playerLife.Mana.Total;
                    ManaPercentage = playerLife.Mana.CurrentInPercent();
                    
                    EnergyShieldCurrent = playerLife.EnergyShield.Current;
                    EnergyShieldMax = playerLife.EnergyShield.Total;
                    EnergyShieldPercentage = playerLife.EnergyShield.CurrentInPercent();
                }

                BehaviorTree.Variables.Edit(cache =>
                {
                    cache.AddOrUpdate(new AuraVariable(nameof(HealthPercentage), HealthPercentage));
                    cache.AddOrUpdate(new AuraVariable(nameof(ManaPercentage), ManaPercentage));
                    cache.AddOrUpdate(new AuraVariable(nameof(EnergyShieldPercentage), EnergyShieldPercentage));
                });

                await BehaviorTree.TickAsync(cancellationToken);
            }

            sw.Stop();

            var frameTime = sw.ElapsedMilliseconds < 0 ? 0 : sw.ElapsedMilliseconds;
            fpsStats.Push(frameTime);
            FrameTime = fpsStats.GetValue();
        }
    }
}