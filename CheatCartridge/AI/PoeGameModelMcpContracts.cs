namespace CheatCartridge.AI;

public sealed class PoeMcpStatusResult
{
    public bool Success { get; init; }

    public string ErrorMessage { get; init; } = string.Empty;

    public int ProcessId { get; init; }

    public string ProcessName { get; init; } = string.Empty;

    public string ModuleName { get; init; } = string.Empty;

    public string EndpointUrl { get; init; } = string.Empty;

    public IReadOnlyList<string> Plugins { get; init; } = Array.Empty<string>();

    public bool UsesSeparateProcessReader { get; init; }
}

public sealed class PoePlayerSnapshotResult
{
    public bool Success { get; init; }

    public string ErrorMessage { get; init; } = string.Empty;

    public DateTimeOffset CapturedAt { get; init; }

    public int ProcessId { get; init; }

    public string ModuleName { get; init; } = string.Empty;

    public string PlayerName { get; init; } = string.Empty;

    public string EntityAddress { get; init; } = string.Empty;

    public ulong EntityAddressValue { get; init; }

    public uint EntityId { get; init; }

    public string EntityPath { get; init; } = string.Empty;

    public bool IsValid { get; init; }

    public byte CurrentAreaLevel { get; init; }

    public uint CurrentAreaHash { get; init; }

    public uint EntitiesCount { get; init; }

    public PoeVitalSnapshot Health { get; init; } = new();

    public PoeVitalSnapshot Mana { get; init; } = new();

    public PoeVitalSnapshot EnergyShield { get; init; } = new();
}

public sealed class PoeVitalSnapshot
{
    public int Current { get; init; }

    public int Total { get; init; }

    public int CurrentPercent { get; init; }

    public int ReservedTotal { get; init; }

    public int ReservedPercent { get; init; }
}
