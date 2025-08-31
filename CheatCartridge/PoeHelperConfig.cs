namespace CheatCartridge;

public sealed record PoeHelperConfig
{
    public string HealthPotionKey { get; set; } = "D1";

    public string ManaPotionKey { get; set; } = "D2";
    
    public string EnergyShieldPotionKey { get; set; } = "D3";
    
    public double MinHealthPotionPercentage { get; set; } = 60;

    public double MinManaPotionPercentage { get; set; } = 60;
    
    public double MinEnergyShieldPotionPercentage { get; set; } = 60;

    public double TargetFps { get; set; } = 100;

    public bool IsExpanded { get; set; } = true;

    public Rectangle WindowBounds { get; set; } = new Rectangle(0, 0, 400, 250);
}