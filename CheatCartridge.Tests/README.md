# CheatCartridge.Tests

This project is the placeholder test home for CheatCartridge.

The live-client integration path mirrors the Banka.TL integration style, but uses `LocalProcess` as the primary process backend because CheatCartridge reads Path Of Exile through direct local process memory.

## Default Tests

Run the safe scaffold tests:

```powershell
dotnet test .\CheatCartridge.Tests\CheatCartridge.Tests.csproj --filter "TestCategory!=integration"
```

## Live Client Tests

Run the explicit LocalProcess attach smoke test against a running Path Of Exile client:

```powershell
dotnet test .\CheatCartridge.Tests\CheatCartridge.Tests.csproj --filter "FullyQualifiedName=CheatCartridge.Tests.Integration.LocalProcessClientIntegrationTests.ShouldAttachToRunningPathOfExileClientWithLocalProcess"
```

Run the current player object-model snapshot test against a loaded character at full life, mana, and energy shield:

```powershell
dotnet test .\CheatCartridge.Tests\CheatCartridge.Tests.csproj --filter "FullyQualifiedName=CheatCartridge.Tests.Integration.LocalProcessClientIntegrationTests.ShouldReadCurrentPlayerSnapshotWithFullVitalsFromObjectModel"
```

If process-name discovery is not enough, set the process id directly:

```powershell
$env:CHEATCARTRIDGE_POE_PROCESS_ID = "12345"
```

Integration tests should stay focused on one live capability at a time and should skip with a clear reason when no suitable client or LocalProcess runtime is available.
