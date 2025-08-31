namespace CheatCartridge;
public partial class PoeEyeComponent : BlazorReactiveComponent<BotBrain>
{
    public PoeEyeComponent()
    {
        ChangeTrackers.Add(this.WhenAnyValue(x => x.DataContext.AutoHealth).Skip(1));
        ChangeTrackers.Add(this.WhenAnyValue(x => x.DataContext.IsEnabled).Skip(1));
        ChangeTrackers.Add(this.WhenAnyValue(x => x.DataContext.IsExpanded).Skip(1));
        ChangeTrackers.Add(this.WhenAnyValue(x => x.DataContext.TargetFps).Skip(1));
        ChangeTrackers.Add(this.WhenAnyValue(x => x.ShowFpsEditor).Skip(1));
    }

    public bool ShowFpsEditor { get; set; }
}