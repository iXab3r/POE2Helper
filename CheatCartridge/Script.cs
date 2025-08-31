Log.Info("Hello, world! I will be reading Current/Max HP values and using HP/Mana potions");
var botBrain = GetService<BotBrain, IFluentLog>(Log) //create bot
    .AddTo(ExecutionAnchors); //dispose once script is completed
cancellationToken.Register(() => Log.Warn("Run has been cancelled"));
await botBrain.Run(cancellationToken); //cancellationToken allows EA to stop the script
Log.Info("Bot has completed its run");