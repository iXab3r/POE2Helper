Log.Info("Starting CheatCartridge");
cancellationToken.Register(() => Log.Warn("Run has been cancelled"));

var useHeadlessMode = true;

if (useHeadlessMode)
{
    Log.Info("CheatCartridge selected headless debug mode");
    var headlessBot = GetService<HeadlessBotMode, IFluentLog>(Log)
        .AddTo(ExecutionAnchors);
    await headlessBot.Run(cancellationToken);
}
else
{
    Log.Info("CheatCartridge selected classic EyeAuras mode");
    var classicBot = GetService<BotBrain, IFluentLog>(Log)
        .AddTo(ExecutionAnchors);
    await classicBot.Run(cancellationToken);
}

Log.Info("CheatCartridge has completed its run");
