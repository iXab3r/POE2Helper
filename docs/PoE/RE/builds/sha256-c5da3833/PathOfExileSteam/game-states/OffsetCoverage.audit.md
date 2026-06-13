# Offset Coverage Audit

Build: `sha256-c5da3833`
Binary: `PathOfExileSteam.exe`
Slice: `game-states`
Status: `current-build audit`

## Scope

This audit defines the current required offset surface as the fields consumed by
the production object model and headless diagnostics:

```text
TheGame
GameStates
InGameState
AreaInstance
Entity
ComponentBase
Player
Life
HeadlessBotMode zone-switch diagnostic
```

The target state is:

- production readers consume `RuntimeGameOffsets`, not managed layout structs;
- every required offset is recovered from a static pattern, derived from a
  stronger recovered shape, or validated as a structural invariant;
- live integration tests prove the recovered values against the actual client.

## Production Guard

`RuntimeOffsetArchitectureTests.ProductionObjectModelShouldReadThroughRuntimeOffsetProvider`
source-scans the production reader surface and rejects:

```text
Memory.Read<layout-struct>
GameHelper.Scaffolding vector/string helpers
Marshal.OffsetOf in production object-model readers
direct raw hex Add(..., 0x...) layout reads
```

This keeps layout structs as documentation/test comparators only.

## Root And State Coverage

| Required fact | Runtime source | Evidence | Live proof |
| --- | --- | --- | --- |
| `GameStates` global slot | `Offsets.Patterns[nameof(GameStates)]` RIP-relative rel32 | `GameStatesSingleton.evidence.md` | `ShouldResolveGameStatesSingletonFromSignatureKeypoint`; `ShouldReadRootObjectModelThroughRuntimeGameOffsets` |
| `GameStateStaticOffset.GameState +0x00` | `GameStateStaticWrapper`; wrapper first slot | `GameStatesSingleton.evidence.md` | `ShouldResolveGameStateStaticWrapperFromGetterPattern`; provider test |
| wrapper sidecar `+0x08` | `GameStateStaticWrapper`; wrapper second slot | `GameStatesSingleton.evidence.md` | `ShouldResolveGameStateStaticWrapperFromGetterPattern` |
| current-state vector `+0x08/+0x10` | `GameStateCurrentStateVector` | `CurrentStateVector.evidence.md`; `StdVector.evidence.md` | `ShouldResolveCurrentStateVectorFromDirtyCopyPattern`; provider test |
| dispatch vector `+0x20/+0x28/+0x30` | same dirty-copy keypoint | `CurrentStateVector.evidence.md`; `StdVector.evidence.md` | `ShouldResolveCurrentStateVectorFromDirtyCopyPattern`; provider test |
| dirty flag `+0x40` | same dirty-copy keypoint | `CurrentStateVector.evidence.md` | `ShouldResolveCurrentStateVectorFromDirtyCopyPattern`; provider test |
| state table `+0x48`, entry size `0x10`, count `0x0D` | `GameStateTableShape` | `GameStateTableAndInGameArea.evidence.md` | `ShouldResolveInGameAreaKeypointsFromCodePatterns`; provider test |
| InGame state selection | recovered table scan plus vtable slot calling elapsed writer | `GameStateVirtualUpdateSlot.evidence.md`; `MsElapsed.evidence.md` | `ShouldResolveMsElapsedWriterFromInGameStateUpdateVtableChain`; provider test |
| virtual update slot `+0x08` | `GameStateVirtualUpdateSlot` | `GameStateVirtualUpdateSlot.evidence.md` | `ShouldResolveMsElapsedWriterFromInGameStateUpdateVtableChain`; provider test |

## InGameState And AreaInstance Coverage

| Required fact | Runtime source | Evidence | Live proof |
| --- | --- | --- | --- |
| `InGameState.AreaInstanceData +0x290` | `InGameStateAreaInstanceData` displacement | `GameStateTableAndInGameArea.evidence.md` | `ShouldResolveInGameAreaKeypointsFromCodePatterns`; provider test |
| `InGameState.MsElapsed +0x400` | `InGameStateMsElapsed` displacement | `MsElapsed.evidence.md` | `ShouldResolveMsElapsedFromCodePattern`; `ShouldResolveMsElapsedWriterFromInGameStateUpdateVtableChain`; provider test |
| elapsed writer function | `InGameStateMsElapsedWriterFunctionStart` | `MsElapsed.evidence.md` | `ShouldResolveMsElapsedWriterFromInGameStateUpdateVtableChain`; provider test |
| direct `ZoneSwitchCounter +0x56C` | `InGameStateZoneSwitchCounter` plus writer-site patterns | `ZoneSwitchState.evidence.md` | `ShouldResolveZoneSwitchCounterAndStatePointerFromCodePatterns`; `ShouldResolveZoneSwitchCounterWriterSitesFromCodePatterns`; provider test |
| nested zone-switch state pointer `+0x368` | `InGameStateZoneSwitchState`; constructor keypoint | `ZoneSwitchState.evidence.md` | `ShouldResolveZoneSwitchCounterAndStatePointerFromCodePatterns` |
| `AreaInstance.CurrentAreaLevel +0x0C4` | `AreaInstanceCurrentAreaLevel` | `AreaInstanceScalars.evidence.md` | `ShouldResolveAreaInstanceCurrentAreaLevelFromCodePattern`; provider test |
| `AreaInstance.CurrentAreaHash +0x11C` | `AreaInstanceCurrentAreaHash` | `AreaInstanceScalars.evidence.md` | `ShouldResolveAreaInstanceCurrentAreaHashFromCodePattern`; provider test |
| `AreaInstance.LocalPlayers +0x588` | `AreaInstanceLocalPlayers` vtable accessor | `AreaInstanceLocalPlayers.evidence.md` | `ShouldResolveAreaInstanceLocalPlayersFromCodePattern`; provider test |
| entity tree root `+0x6C0` and count `+0x6C8` | `AreaInstanceEntityTreeRoot`; count derived as root + pointer size | `AreaInstanceScalars.evidence.md`; `EntityIdentity.evidence.md` | `ShouldResolveAreaInstanceEntitiesCountFromCodePattern`; provider test |

## Entity And Component Coverage

| Required fact | Runtime source | Evidence | Live proof |
| --- | --- | --- | --- |
| `EntityDetailsPtr +0x08` | generic component resolver plus entity name accessor cross-check | `EntityComponentLookup.evidence.md`; `EntityDetailsName.evidence.md` | `ShouldResolveEntityDetailsNameFromVtableAccessor`; `ShouldResolveEntityComponentLookupFromCodePattern`; provider test |
| component list vector `+0x10` | generic component resolver | `EntityComponentLookup.evidence.md` | `ShouldResolveEntityComponentLookupFromCodePattern`; provider test |
| entity id `+0x88` | `EntityIdentityFilter` | `EntityIdentity.evidence.md` | `ShouldResolveEntityIdentityFromAreaInstanceTreeFilter`; provider test |
| status byte `+0x8C` and active flag `+0x8D` | `EntityIdentityFilter` | `EntityIdentity.evidence.md` | `ShouldResolveEntityIdentityFromAreaInstanceTreeFilter`; provider test |
| invalid-status mask `0x01`, id upper bound `0x40000000`, active mask `0x04` | `EntityIdentityFilter` operands | `EntityIdentity.evidence.md` | `ShouldResolveEntityIdentityFromAreaInstanceTreeFilter`; provider test |
| `EntityDetails.name +0x08` | entity vtable accessor | `EntityDetailsName.evidence.md` | `ShouldResolveEntityDetailsNameFromVtableAccessor`; provider test |
| `EntityDetails.ComponentLookUpPtr +0x28` | generic component resolver | `EntityComponentLookup.evidence.md` | `ShouldResolveEntityComponentLookupFromCodePattern`; provider test |
| component name/index bucket `+0x28` | generic component resolver and bucket helper | `EntityComponentLookup.evidence.md` | `ShouldResolveEntityComponentLookupFromCodePattern`; provider test |
| component bucket entry: name `+0x00`, index `+0x08`, size `0x10` | bucket helper plus repeated generic resolver consistency | `EntityComponentLookup.evidence.md` | `ShouldResolveEntityComponentLookupShapeConsistentlyAcrossGenericResolvers`; provider test |
| component owner pointer `+0x08` | Player formatter owner read | `ComponentHeader.evidence.md` | `ShouldResolveComponentHeaderFromPlayerFormatter`; provider test |

## Player, String, And Life Coverage

| Required fact | Runtime source | Evidence | Live proof |
| --- | --- | --- | --- |
| `Player.Name +0x1B0` | Player formatter string read | `PlayerName.evidence.md` | `ShouldResolvePlayerNameFromCodePattern`; provider test |
| `StdWString` buffer/inline `+0x00`, length `+0x10`, capacity `+0x18`, SSO limit `7` | same Player formatter branch | `PlayerName.evidence.md` | `ShouldResolvePlayerNameFromCodePattern`; provider test |
| `Life.Health +0x1A8`, `Mana +0x200`, `EnergyShield +0x240` | Life vtable vital formatter plus constructor | `LifeVitals.evidence.md` | `ShouldResolveLifeVitalsFromCodePatterns`; `ShouldResolveLifeVitalConstructorShapeFromCodePattern`; provider test |
| `VitalStruct.Current +0x38`, `Total +0x34` | Life current/total formatter | `LifeVitals.evidence.md` | `ShouldResolveLifeVitalsFromCodePatterns`; provider test |
| reservation fields `+0x18/+0x1C` | common vital reservation deserializer | `LifeVitals.evidence.md` | `ShouldResolveLifeVitalsFromCodePatterns`; provider test |
| constructor/stat-id fields `+0x08/+0x0C/+0x10/+0x20/+0x24/+0x28` | Life direct constructor plus shared helper | `LifeVitals.evidence.md` | `ShouldResolveLifeVitalConstructorShapeFromCodePattern`; provider test |

## Non-Required Or Structural Fields

These facts are intentionally not production dependencies:

| Fact | Status |
| --- | --- |
| `ComponentHeader.StaticPtr +0x00` | Structural vtable pointer. Verified by `ShouldResolveComponentHeaderFromPlayerFormatter`, but production does not read it. |
| `StdBucket.Capacity` and unknown fields | Pruned from production. Component enumeration uses the embedded vector span and recovered entry size. |
| nested zone-switch byte at `*(InGameState +0x368) +0x56C` | Observed/debug context only. The direct production diagnostic is `InGameState +0x56C`. |
| `GameStateBuffer` enum order | Comparator only. Production InGame selection uses behavior-backed vtable/writer ownership. |

## Current Audit Result

For build `sha256-c5da3833`, all currently required production object-model and
headless diagnostic offsets are either:

- resolved directly from `RuntimeGameOffsets`;
- derived from a stronger recovered shape, such as `EntitiesCount = entity tree
  root + pointer size`;
- or kept out of production as structural/test-only facts.

The full explicit live regression surface is
`CheatCartridge.Tests.Integration.LocalProcessClientIntegrationTests`, and the
fast production-reader architecture guard is
`CheatCartridge.Tests.Scaffold.RuntimeOffsetArchitectureTests`.

Static Ghidra sanity check on the copied program during this audit:

```text
open program             = /PoE/sha256-c5da3833/PathOfExileSteam.exe
wrapper-tail pattern     = 0x140FAD261, unique
elapsed writer field     = 0x140FAD72C, MOV dword ptr [RCX +0x400], EAX
dispatch-callsite bytes  = 0x1415B2A51, setup before CALL qword ptr [RAX +0x8]
```

Live verification during this audit:

```text
command      = dotnet test .\CheatCartridge.Tests\CheatCartridge.Tests.csproj --no-build --filter "FullyQualifiedName~CheatCartridge.Tests.Integration.LocalProcessClientIntegrationTests" -- NUnit.Explicit=true
result       = Passed: 24, Failed: 0, Skipped: 0
duration     = 3m35s
client PID   = 5592
player       = Xaber
entity       = 0x27A939CB980
InGameState  = 0x27ABEE62F10
AreaInstance = 0x27A8D887000
vitals       = HP 1427/1427, MP 349/349, ES 115/115
```

Fast production-reader guard:

```text
RuntimeOffsetArchitectureTests = Passed: 1, Failed: 0
```
