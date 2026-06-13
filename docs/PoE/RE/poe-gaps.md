# PoE Offset Gaps

This file tracks the gap between the current CheatCartridge Path Of Exile object model and the target RE-backed model.

The goal is not merely to replace numbers with different numbers. The goal is to make each important pointer chain recoverable from stable binary/runtime evidence, with integration tests that fail when a patch invalidates the model.

Current build evidence starts at:

- `docs/PoE/RE/builds/sha256-c5da3833/PathOfExileSteam/game-states/`
- `CheatCartridge/GameHelper/GameOffsets/Offsets.cs`
- `CheatCartridge/GameHelper/GameOffsets/PoeRuntimeLayouts.cs`
- `CheatCartridge/GameHelper/GameOffsets/RuntimeGameOffsets.cs`
- `CheatCartridge.Tests/Integration/LocalProcessClientIntegrationTests.cs`

## Status Legend

| Status | Meaning |
| --- | --- |
| `pattern-backed` | We have a byte-pattern keypoint and a runtime test proving it reaches the live object chain. |
| `derived` | We intentionally compute it from a stronger nearby fact rather than patterning the exact access. |
| `hardcoded` | The managed struct still owns the value with no durable keypoint. |
| `observed` | The value worked in a live snapshot, but the ownership/function path is not proven. |
| `suspect` | The field may be wrong or may describe a different concept than its name implies. |
| `drop-candidate` | Not currently needed by the object model or debugging goal. Keep only if a real use appears. |

## Target Shape

Target state for a key offset is one of:

- A short, semantic byte pattern that recovers a displacement, immediate, or RIP-relative global slot from game code.
- A derived value from a stronger recovered shape, such as `stateTableOffset + enumIndex * entrySize`.
- A validated runtime invariant when code anchoring is not enough by itself.

Avoid promoting offsets found by broad displacement searches. A hit for `0x588` or `0x6C8` is not evidence unless the containing function is known to operate on the object we think it operates on.

## Bootstrap And Game States

Runtime-provider status:

- Done for the root object graph. `RuntimeGameOffsets.Resolve` now recovers the
  GameStates singleton/static wrapper, current-state vector, state table shape,
  InGame area pointer, InGame elapsed/counter offsets, and AreaInstance
  level/hash/local-player/count offsets from the static keypoints, then exposes
  them as typed `PoeRuntimeLayouts`.
- Done for the required entity/component graph. The resolver also recovers
  entity identity/path/component-lookup offsets, component bucket entry
  name/index/size shape, component owner/name offsets, Life component vital
  starts, and VitalStruct stat/reservation/current fields from static keypoints.
- Production `TheGame`, `GameStates`, `InGameState`, `AreaInstance`, `Entity`,
  `ComponentBase`, `Player`, and `Life` consume `PoeRuntimeLayouts` for the
  current area/player chain and hp/mp/es object model. The flat
  `RuntimeGameOffsets` surface remains a compatibility facade for existing
  integration tests and old evidence names.
- Evidence: `RuntimeOffsetProvider.evidence.md` and
  `OffsetCoverage.audit.md`, plus
  `ShouldReadRootObjectModelThroughRuntimeGameOffsets` /
  `ShouldReadEntityComponentsThroughRuntimeGameOffsets`.
- Production hardcoded-read audit, 2026-06-13: the active object-model readers
  (`TheGame`, `GameStates`, `InGameState`, `AreaInstance`, `Entity`,
  `ComponentBase`, `Player`, `Life`, and headless zone-counter diagnostics)
  consume `PoeRuntimeLayouts`. The remaining numeric offsets in production are
  pattern-local operand extraction offsets inside the resolver, not direct
  game-object field offsets. Layout structs remain as documentation and test
  comparators.
- Shared `StdVector` shape is now documented in `StdVector.evidence.md` and
  provider-validated before enumeration. Runtime tests exercise first/last/end
  count/capacity invariants across current-state, local-player, component-list,
  and component-bucket vectors.
- Scaffolding audit guard, 2026-06-13: `RuntimeOffsetArchitectureTests`
  source-scans production object-model readers and rejects legacy
  `Memory.Read<LayoutStruct>` usage, old `GameHelper.Scaffolding` vector/string
  helpers, direct raw hex `Add(..., 0x...)` layout reads, and
  `Marshal.OffsetOf` in that layer. The old scaffolding helpers are marked
  obsolete with `error: true`; layout structs remain allowed as documentation
  and integration-test comparators.

| Need | Current state | Target state | Next action |
| --- | --- | --- | --- |
| `GameStates` static/global slot | `pattern-backed/runtime-consumed`. `Offsets.Patterns[nameof(GameStates)]` lands on a RIP-relative rel32 operand in `FUN_14010c350`; the resolver exposes the live global slot as `PoeRuntimeLayouts.GameStates.GlobalSlot`, and `TheGame.UpdateData` assigns it to `GameStates.Address`. | Keep pattern-backed. No hardcoded static RVA. | Recheck this pattern on the next client build and keep `ShouldResolveGameStatesSingletonFromSignatureKeypoint` plus `ShouldReadRootObjectModelThroughRuntimeGameOffsets` green. |
| `GameStateStaticOffset.GameState` at `+0x00` | `pattern-backed/runtime-consumed`. `GameStateStaticWrapper` recovers the getter tail in `FUN_14010C350`, which writes `DAT_1444E9EB8` into `[out +0x00]` and `DAT_1444E9EC0` into `[out +0x08]`. Runtime verifies the first global slot matches the `GameStates` singleton slot and resolves the live state owner. | Keep pattern-backed. The sidecar/control pointer at `+0x08` is documented and resolved, but production only follows the owner pointer. | Keep `ShouldResolveGameStateStaticWrapperFromGetterPattern` and `ShouldReadRootObjectModelThroughRuntimeGameOffsets` green. |
| `GameStateOffset.CurrentStatePtr` at `+0x08` | `pattern-backed/runtime-consumed`. `GameStateCurrentStateVector` recovers the source current-state vector from `FUN_1415B0360`, which checks dirty flag `+0x40`, computes the source count from `[GameStates +0x10] - [GameStates +0x08]`, and copies 16-byte entries into dispatch vector `+0x20..+0x28`; the same function proves dispatch vector end/capacity at `+0x30`, anchoring the shared `StdVector` `First/Last/End` shape. `GameStates.UpdateData` now calls `PoeRuntimeLayouts.ReadCurrentStateAddress` instead of open-coding `Last - entrySize`. | Runtime-derived current-state vector offset, dispatch vector offsets, dirty flag offset, and validated vector header shape. | Keep `ShouldResolveCurrentStateVectorFromDirtyCopyPattern` and provider test green. |
| `GameStateOffset.States` table at `+0x48` | `pattern-backed/runtime-consumed`. `GameStateTableShape` recovers table offset `0x48`, entry size `0x10`, and count `0x0D`; `GameStates.UpdateData` now reads state entries through `PoeRuntimeLayouts`. | Runtime-derived table shape. | Keep provider test green; later remove/replace direct production references to `GameStateBuffer` when no longer needed as test comparator/documentation. |
| `GameStateBuffer.TOTAL_STATES = 13` | `pattern-backed/runtime-consumed`. Recovered from constructor immediate `MOV R8D, 0x0D`; production state traversal now uses `PoeRuntimeLayouts.GameStates.Count`. | Runtime-derived count. | Keep test coverage that compares recovered count to the managed buffer count until the buffer is generated/configured. |
| InGame state entry offset `0x88` | `behavior-backed/runtime-consumed`. We deliberately do not keep a brittle `InGameStateEntry` pattern. `GameStatesLayout.GetEntryOffset(GameStateTypes.InGameState)` still computes `0x48 + enumIndex * 0x10` as a comparator, but production `GameStates.UpdateData` now uses `PoeRuntimeLayouts.ReadInGameStateEntry`: scan the recovered state table and pick the state whose virtual update slot branches to the pattern-backed `InGameState.MsElapsed` writer. | Keep production selection behavior-backed from recovered table shape, recovered entry size, `GameStateVirtualUpdateSlot`, and the elapsed-writer keypoint. Enum order remains a useful sanity check, not the primary production selector. | Keep `ShouldReadRootObjectModelThroughRuntimeGameOffsets` and `ShouldResolveMsElapsedWriterFromInGameStateUpdateVtableChain` green. Confirm enum ordering only if a future patch breaks comparator tests. |

## InGameState

| Need | Current state | Target state | Next action |
| --- | --- | --- | --- |
| `InGameStateOffset.AreaInstanceData` at `+0x290` | `pattern-backed/runtime-consumed`. `InGameStateAreaInstanceData` recovers the `MOV RBX, [InGameState + 0x290]` displacement. This is an offset, not the final `AreaInstance` address. `InGameState.UpdateData` now reads the final pointer through `PoeRuntimeLayouts.InGameState.AreaInstanceDataOffset`. | Runtime-derived field offset. The final address remains `*(IntPtr*)(inGameState + offset)`. | Keep `ShouldResolveInGameAreaKeypointsFromCodePatterns` and provider test green. |
| InGame zone-switch state pointer at `+0x368` | `pattern-backed/confirmed`. `InGameStateZoneSwitchState` recovers a nested pointer read from `FUN_140FAF2F0`. `InGameStateZoneSwitchStateConstructor` independently recovers the constructor/lifetime path in `FUN_140FA9D00`, which installs a `0x578`-byte object into `[InGameState + 0x368]`. | Introduce a small nested managed model only if zone-switch/debugging logic needs it. | Keep `ShouldResolveZoneSwitchCounterAndStatePointerFromCodePatterns` green; do not expose the nested byte until needed. |
| `InGameStateOffset.MsElapsed` at `+0x400` | `pattern-backed/provider-resolved`. `InGameStateMsElapsedWriterFunctionStart` resolves the writer entry at `FUN_140FAD6E0`, and `InGameStateMsElapsed` separately recovers the `MOV dword ptr [InGameState +0x400], EAX` displacement. The function accumulates a double at `+0x408`, scales it, then writes a 32-bit value to `+0x400`. Static/live vtable-chain evidence shows the live `InGameState` virtual update slot `+0x8` points at wrapper `FUN_140FAD1A0`, which tail-branches to the writer. | Runtime-derived writer function address and field offset. Keep the field typed as `int`, not `IntPtr`. | Keep `ShouldResolveMsElapsedFromCodePattern` and `ShouldResolveMsElapsedWriterFromInGameStateUpdateVtableChain` green. Avoid hardware watchpoints on this hot field; use static analysis and LocalProcess-backed vtable/keypoint tests for call-site ownership. |
| `InGameStateOffset.ZoneSwitchCounter` at `+0x56C` | `pattern-backed/provider-resolved`. `InGameStateZoneSwitchCounter` recovers the direct `CMP dword ptr [InGameState + 0x56C], 1` displacement at VA `0x140FB3F60`. Writer-site patterns recover the reset in `FUN_140FAC260` and both increment branches in `FUN_140FB24A0`, all targeting the same `+0x56C` dword. Runtime also confirmed live `0x0000033E22E4327C - 0x0000033E22E42D10 = 0x56C`. | Runtime-derived direct counter offset plus writer/lifetime semantics. Keep separate from the nested `+0x56C` byte below. | Keep `ShouldResolveZoneSwitchCounterAndStatePointerFromCodePatterns` and `ShouldResolveZoneSwitchCounterWriterSitesFromCodePatterns` green. |

## AreaInstance

| Need | Current state | Target state | Next action |
| --- | --- | --- | --- |
| `AreaInstanceOffsets.CurrentAreaLevel` at `+0x0C4` | `pattern-backed/runtime-consumed`. `AreaInstanceCurrentAreaLevel` recovers a byte read from the current area object after loading that object through the already-proven `InGameState.AreaInstanceData` slot. `AreaInstance.UpdateData` now reads it through `PoeRuntimeLayouts.AreaInstance.CurrentAreaLevelOffset`. | Runtime-derived field offset. | Keep `ShouldResolveAreaInstanceCurrentAreaLevelFromCodePattern` and provider test green. |
| `AreaInstanceOffsets.CurrentAreaHash` at `+0x11C` | `pattern-backed/runtime-consumed`. `AreaInstanceCurrentAreaHash` recovers the dword read from the current-area holder path. Holder ownership is backed by `FUN_14205D6D0`, which stores `AreaInstance` at holder `+0x2170` and stores the holder back at `AreaInstance +0x580`. `AreaInstance.UpdateData` now reads it through `PoeRuntimeLayouts.AreaInstance.CurrentAreaHashOffset`. | Runtime-derived field offset. | Keep `ShouldResolveAreaInstanceCurrentAreaHashFromCodePattern` and provider test green. |
| `AreaInstanceOffsets.LocalPlayers` at `+0x588` | `pattern-backed/runtime-consumed`. `AreaInstanceLocalPlayers` recovers the displacement from AreaInstance vtable accessor `FUN_1420731D0`, which copies non-null qword entries from `AreaInstance + 0x588..0x590` into an output vector. Production now reads the `StdVector<IntPtr>` through `PoeRuntimeLayouts.ReadStdVector`, and provider validation checks `End >= Last` plus qword alignment before enumeration. | Runtime-derived field offset plus shared vector header validation. Keep using the current `StdVector<IntPtr>` shape. | Use this as the trusted player entity chain anchor and keep provider tests green. |
| `AreaInstanceOffsets.EntitiesCount` at `+0x6C8` | `pattern-backed/runtime-consumed`. `AreaInstanceEntityTreeRoot` recovers the entity tree root slot `+0x6C0` from `FUN_14206E780`; count is the adjacent qword at `+0x6C8`. `FUN_14163B7A0` corroborates ownership by iterating the same tree as an entity set. Production now derives count as `entityTreeRoot + IntPtr.Size`. | Runtime-derived from constructor-backed container shape. | Keep `ShouldResolveAreaInstanceEntitiesCountFromCodePattern` and provider test green. |

## Entity And Component Lookup

| Need | Current state | Target state | Next action |
| --- | --- | --- | --- |
| `ItemStruct.EntityDetailsPtr` at `+0x08` | `pattern-backed/runtime-consumed`. `EntityComponentLookupShape` recovers two reads of `Entity + 0x08` inside `FUN_1401512E0`; `Entity.UpdateData` now reads this through `PoeRuntimeLayouts.Entity.DetailsPtrOffset`. | Runtime-derived field offset. | Keep `ShouldResolveEntityComponentLookupFromCodePattern` and `ShouldReadEntityComponentsThroughRuntimeGameOffsets` green. |
| `ItemStruct.ComponentListPtr` at `+0x10` | `pattern-backed/runtime-consumed`. `FUN_1401512E0` reads `Entity + 0x10`, indexes it by the recovered component index, and returns the component pointer. `Entity.UpdateData` now reads the component vector through `PoeRuntimeLayouts.Entity.ComponentListOffset`. | Runtime-derived field offset. | Keep the provider-backed Player/Life component lookup test green. |
| `EntityOffsets.Id` at `+0x88` | `pattern-backed/runtime-consumed`. `EntityIdentityFilter` recovers this from `FUN_14163B7A0`, which walks the AreaInstance entity tree, reads each node's entity pointer from node `+0x28`, and compares `dword ptr [Entity + 0x88]` with `0x40000000`. | Runtime-derived entity id offset. | Keep `ShouldResolveEntityIdentityFromAreaInstanceTreeFilter` and provider-backed entity test green. |
| `EntityOffsets.IsValid` at `+0x8C` | `pattern-backed/predicate-backed/runtime-consumed`. `EntityIdentityFilter` recovers the entity status byte, invalid-status mask `0x01`, id upper bound `0x40000000`, adjacent active-flag byte `+0x8D`, and required active mask `0x04` from the AreaInstance entity-tree filter. `Entity.UpdateData` now uses the recovered predicate instead of hardcoding `status == 0x0C`; the live player still observes status `0x0C`. | Runtime-derived status-byte offset and runtime-derived predicate operands. | Find fuller enum semantics only if we need to distinguish additional invalid/transient states beyond the static game filter. |
| `EntityDetails.name` at `+0x08` | `pattern-backed/runtime-consumed`. `EntityDetailsName` recovers this from the live player Entity vtable accessor at VA `0x141C7A6D0`, which returns `*(Entity + 0x08) + 0x08`. `Entity.UpdateData` now reads the metadata path through `PoeRuntimeLayouts.ReadStdWString`, including the provider-derived `StdWString` buffer/inline/length/capacity shape. | Runtime-derived path/name offset plus shared string layout. | Keep `ShouldResolveEntityDetailsNameFromVtableAccessor` and provider-backed entity test green. |
| `EntityDetails.ComponentLookUpPtr` at `+0x28` | `pattern-backed/runtime-consumed`. `FUN_1401512E0` reads `EntityDetails + 0x28` before using the component lookup bucket. | Runtime-derived field offset. | Keep this as the component lookup pointer anchor. |
| `ComponentLookUpStruct.ComponentsNameAndIndex` at `+0x28` | `pattern-backed/runtime-consumed`. `FUN_1401512E0` passes `ComponentLookUp + 0x28` into `FUN_140163120`, the bucket lookup helper. | Runtime-derived field offset. | Keep the recovered bucket shape covered by the lookup and provider-backed component tests. |
| `ComponentNameAndIndexStruct` layout | `pattern-backed/runtime-consumed`. `FUN_140163120` compares the first qword of each bucket entry with the requested component-name pointer and computes entries as `first + index * 0x10`; the repeated generic named-component resolver shape reads the dword at `entry + 0x08` as the component index. Ghidra finds 102 resolver-shaped matches in build `sha256-c5da3833`; `ShouldResolveEntityComponentLookupShapeConsistentlyAcrossGenericResolvers` proves they all encode one layout. `RuntimeGameOffsets` exposes name pointer offset `+0x00`, index offset `+0x08`, and entry size `0x10`; `Entity.UpdateData` now enumerates the bucket through the provider instead of reading the managed struct. Downstream component tests also resolve `Player`/`Life` through the recovered bucket shape. `StdBucket.Capacity` is pruned from the required surface; production bounds enumeration from `Data.First..Data.Last` plus a caller cap. | Runtime-derived name/index/entry-size shape; generic resolver keypoint is non-unique but consistency-proven. | Keep `ShouldResolveEntityComponentLookupFromCodePattern`, `ShouldResolveEntityComponentLookupShapeConsistentlyAcrossGenericResolvers`, `ShouldReadEntityComponentsThroughRuntimeGameOffsets`, and the component-internal tests green. |

## Components

| Need | Current state | Target state | Next action |
| --- | --- | --- | --- |
| `ComponentHeader.StaticPtr` at `+0x00` | `pattern-backed/invariant-backed`. `PlayerComponentHeaderOwnerEntity` starts from Player vtable slot `+0x68`; the live test reads `PlayerComponent +0x00` as the vtable and verifies slot `+0x68` points to `FUN_141D267F0`. | Keep structural vtable field at component start. | Keep `ShouldResolveComponentHeaderFromPlayerFormatter` green; production does not currently need to read this field. |
| `ComponentHeader.EntityPtr` at `+0x08` | `pattern-backed/runtime-consumed`. `FUN_141D267F0` reads `[PlayerComponent +0x08]` before deriving owner-backed class/ascendancy data. `ComponentBase`, `Player`, and `Life` now read the owner through `PoeRuntimeLayouts.ComponentHeader.OwnerEntityOffset`. | Runtime-derived owner pointer offset. | Keep `ShouldResolveComponentHeaderFromPlayerFormatter` and provider-backed component test green. |
| `PlayerOffsets.Name` at `+0x1B0` | `pattern-backed/runtime-consumed`. `PlayerComponentName` recovers this from Player vtable slot `0x68` -> `FUN_141D267F0`, which prints `"Character Name: "` and reads the embedded `StdWString` at `Player + 0x1B0`. The same formatter proves `StdWString.Buffer=+0x00`, inline buffer `+0x00`, `Length=+0x10`, `Capacity=+0x18`, and SSO limit `7`. `Player.UpdateData` now reads the name through `PoeRuntimeLayouts.ReadStdWString`. | Runtime-derived field offset plus shared string layout. | Keep `ShouldResolvePlayerNameFromCodePattern` and provider-backed component test green. |
| `LifeOffset.Health` at `+0x1A8` | `pattern-backed/runtime-consumed`. `LifeComponentVitalOffsets` recovers this from Life vtable slot `0x98` -> `FUN_141CE1C00`, which passes `Life + 0x1A8` to a vital helper. `Life.UpdateData` now reads the vital through `PoeRuntimeLayouts.ReadVitalStruct`. | Runtime-derived field offset. | Keep `ShouldResolveLifeVitalsFromCodePatterns` and provider-backed component test green. |
| `LifeOffset.Mana` at `+0x200` | `pattern-backed/runtime-consumed`. The same Life vtable function passes `Life + 0x200` to a vital helper. | Runtime-derived field offset. | Keep covered with health/energy shield in the same tests. |
| `LifeOffset.EnergyShield` at `+0x240` | `pattern-backed/runtime-consumed`. The same Life vtable function passes `Life + 0x240` to a vital helper. | Runtime-derived field offset. | Keep covered with health/mana in the same tests; allow zero total on characters without shield. |
| `VitalStruct.UnknownStatId0` at `+0x08` | `pattern-backed/unknown semantics/runtime-consumed`. `LifeVitalConstructorShape` recovers direct Health/Mana constructor stores; `LifeVitalSharedConstructorShape` recovers the shared helper default used by Energy Shield. `Life.UpdateData` reconstructs `VitalStruct` through provider-derived offsets. | Runtime-derived field offset with conservative name. | Keep `ShouldResolveLifeVitalConstructorShapeFromCodePattern` green; rename only when stat semantics are proven. |
| `VitalStruct.UnknownStatId1` at `+0x0C` | `pattern-backed/unknown semantics/runtime-consumed`. `LifeVitalConstructorShape` recovers direct Health/Mana constructor stores; `LifeVitalSharedConstructorShape` recovers the shared helper default used by Energy Shield. | Runtime-derived field offset with conservative name. | Keep constructor tests green; rename only when stat semantics are proven. |
| `VitalStruct.LifeComponentPtr` at `+0x10` | `pattern-backed/runtime-consumed`. `LifeVitalConstructorShape` recovers constructor stores that write the owning Life component pointer into each embedded vital object. | Runtime-derived owner pointer offset. | Keep constructor and provider-backed component tests green. |
| `VitalStruct.TotalStatId` at `+0x20` | `pattern-backed/runtime-consumed`. `LifeVitalConstructorShape` recovers the constructor field offset and `FUN_141CDDFB0` uses this field to refresh `VitalStruct.Total` from the stats container. | Runtime-derived stat-id field offset. | Keep constructor test green; rename only if fuller stat semantics are mapped. |
| `VitalStruct.UnknownStatId2` at `+0x24` | `pattern-backed/unknown semantics/runtime-consumed`. Constructor writes stat-like ids here for each vital. Some callback paths use this value when current reaches zero. | Runtime-derived field offset with conservative name. | Keep name conservative until callback/stat semantics are proven. |
| `VitalStruct.UnknownStatId3` at `+0x28` | `pattern-backed/reclassified/runtime-consumed`. This was previously `Regeneration`; constructor evidence proves it is a dword stat-like id (`Health=0x759`, `Mana=0x1C9C`, `EnergyShield=0x4DE5`), not a float regeneration value. | Runtime-derived field offset with conservative name. | Keep constructor test green; do not use this as regeneration without separate writer/reader proof. |
| `VitalStruct.Total` at `+0x34` | `pattern-backed/runtime-consumed`. `LifeVitalCurrentTotal` recovers absolute total reads from Life vtable slot `0x68` -> `FUN_141CE2300`; subtracting the recovered vital starts gives `+0x34`. | Runtime-derived field offset. | Keep raw reads compared against object-model vitals. |
| `VitalStruct.Current` at `+0x38` | `pattern-backed/runtime-consumed`. `LifeVitalCurrentTotal` recovers absolute current reads from the same formatter; subtracting the recovered vital starts gives `+0x38`. | Runtime-derived field offset. | Keep sane range checks, plus full HP/MP/ES checks in the explicit safe-state snapshot test. |
| `VitalStruct.ReservedFlat` at `+0x18` | `pattern-backed/runtime-consumed`. `LifeVitalReservationOffsets` recovers this from `FUN_141CE13A0`, the common vital reservation deserializer reached from the Life vital deserialize/update path. | Runtime-derived field offset. | Keep covered by `ShouldResolveLifeVitalsFromCodePatterns`; this field is used by `VitalStruct.ReservedTotal` and the game-model MCP snapshot. |
| `VitalStruct.ReservedPercent` at `+0x1C` | `pattern-backed/runtime-consumed`. `FUN_141CE13A0` reads a signed 16-bit percent value from the stream, sign-extends it, and writes it to `VitalStruct +0x1C`. | Runtime-derived field offset. Keep typed as `int`; the stream source is short but the object field is dword. | Keep covered by `ShouldResolveLifeVitalsFromCodePatterns`; this field is used by `VitalStruct.ReservedInPercent()` and the game-model MCP snapshot. |

## Integration Test Gaps

| Test need | Current state | Target state |
| --- | --- | --- |
| GameStates singleton keypoint | Done. `ShouldResolveGameStatesSingletonFromSignatureKeypoint`. | Keep as smoke test for every client build. |
| GameStates shared wrapper shape | Done. `ShouldResolveGameStateStaticWrapperFromGetterPattern`. | Keep as a regression for `GameStateStaticOffset.GameState +0x00` and the sidecar pointer at `+0x08`. |
| GameStates current-state vector | Done. `ShouldResolveCurrentStateVectorFromDirtyCopyPattern`. | Keep as a regression for `GameStateOffset.CurrentStatePtr`, dispatch vector offsets, dirty flag offset, and provider-owned latest-current-entry reading. |
| GameStates table shape and InGame area offset | Done. `ShouldResolveInGameAreaKeypointsFromCodePatterns`. | Extend provider plumbing after more offsets are recovered. |
| Runtime root offset provider | Done. `ShouldReadRootObjectModelThroughRuntimeGameOffsets`. | Keep as a regression that production root traversal consumes provider-derived offsets for GameStates, InGameState, and AreaInstance. |
| Runtime entity/component provider | Done. `ShouldReadEntityComponentsThroughRuntimeGameOffsets`. | Keep as a regression that production player identity, component lookup, Player name, Life vitals, and VitalStruct reads consume provider-derived offsets. |
| Provider-only production architecture | Done. `RuntimeOffsetArchitectureTests.ProductionObjectModelShouldReadThroughRuntimeOffsetProvider`. | Keep as a fast guard that active production readers do not drift back to layout-struct or scaffolding-helper reads. |
| InGame elapsed timer | Done. `ShouldResolveMsElapsedFromCodePattern` and `ShouldResolveMsElapsedWriterFromInGameStateUpdateVtableChain`. | Keep as regressions for the timing/update writer keypoint and its live vtable update-chain ownership. |
| Direct zone-switch counter writer sites | Done. `ShouldResolveZoneSwitchCounterWriterSitesFromCodePatterns`. | Keep as a regression that reset/increment writer sites still target the same direct counter field. |
| AreaInstance local player vector | Done. `ShouldResolveAreaInstanceLocalPlayersFromCodePattern`. | Keep as a regression; extend downstream tests to prove `Player` and `Life` component lookup from the recovered entity chain. |
| AreaInstance level/hash/count | Done. `CurrentAreaLevel`, `CurrentAreaHash`, and `EntitiesCount` are covered by explicit code-pattern tests. | Keep these as AreaInstance scalar regressions for every client build. |
| Entity id/status offsets and validity predicate | Done. `ShouldResolveEntityIdentityFromAreaInstanceTreeFilter`. | Keep as a regression for `EntityOffsets.Id`, `EntityOffsets.IsValid`, active flag offset, invalid-status mask, id upper bound, required-active mask, and production `Entity.IsValid` following the recovered game predicate. |
| Entity metadata path | Done. `ShouldResolveEntityDetailsNameFromVtableAccessor`. | Keep as a regression for `EntityDetails.name` and `ItemStruct.EntityDetailsPtr` from the entity vtable accessor. |
| Entity component lookup | Done. `ShouldResolveEntityComponentLookupFromCodePattern` and `ShouldResolveEntityComponentLookupShapeConsistentlyAcrossGenericResolvers`. | Keep as regressions for the component discovery chain and for the repeated generic resolver shape producing one consistent field layout; use it as the starting point for component-internal offset tests. |
| Component header owner/vtable | Done. `ShouldResolveComponentHeaderFromPlayerFormatter`; component address resolution uses recovered bucket entry shape. | Keep as a regression for `ComponentHeader.StaticPtr` and `ComponentHeader.EntityPtr`; generic constructor proof is optional unless future component types diverge. |
| Player name offset | Done. `ShouldResolvePlayerNameFromCodePattern`; component address resolution uses recovered bucket entry shape. | Keep as a regression for character identity and `StdWString` layout. |
| Life/vitals offsets | Done. `ShouldResolveLifeVitalsFromCodePatterns`; component address resolution uses recovered bucket entry shape. | Keep as a regression for `LifeOffset` and `VitalStruct`; full HP/MP/ES remains covered by `ShouldReadCurrentPlayerSnapshotWithFullVitalsFromObjectModel`. |
| Life vital constructor/stat-id fields | Done. `ShouldResolveLifeVitalConstructorShapeFromCodePattern`; component address resolution uses recovered bucket entry shape. | Keep as a regression for `VitalStruct.UnknownStatId0`, `UnknownStatId1`, `LifeComponentPtr`, `TotalStatId`, `UnknownStatId2`, and `UnknownStatId3`; do not reintroduce `Regeneration` without new evidence. |

## Priority Order

1. Next client-build rehearsal: copy the new binary, re-run Ghidra/static keypoint searches, re-check `OffsetCoverage.audit.md` against any model changes, then run the explicit LocalProcess tests against the live client.
2. Generated metadata follow-up: decide whether layout structs should be generated from provider metadata or stay as hand-maintained documentation/test comparators.

## Deferred Observed Facts

These facts are useful RE context, but they are not part of the current required object-model offset surface:

- Nested zone-switch/transition byte at `*(InGameState +0x368) +0x56C`: `FUN_140FAF2F0` reads `[InGameState +0x368]`, then checks `byte ptr [nested +0x56C]`. The parent pointer is confirmed and pattern-backed; the nested byte should only become a modeled/keypointed field if debugging or product logic needs that nested object.

## Pruned Non-Required Offsets

These offsets are intentionally no longer part of the required target surface:

- `InGameStateOffset.LoginServerHostPtr +0x530`: not present in the current managed struct and not needed for character/state reads.
- `AreaInstanceOffsets.UnknownNumber1 +0x638`: definition-only oscillating value with no object-model consumer.
- `AreaInstanceOffsets.UnknownVtablePtr +0xB38`: removed as suspect; existing Ghidra evidence shows `vtable +0xB38` is a virtual method call, not an `AreaInstance +0xB38` field read.
- `AreaInstanceOffsets.Vtable +0x00`: removed as a definition-only structural placeholder. Vtable functions remain useful RE anchors, but the object model does not consume the field.
- `ComponentLookUpStruct.Unknown0/Unknown1/Unknown2`: removed as padding-only placeholders. The required bucket remains `ComponentsNameAndIndex +0x28`.
- `StdBucket.Capacity`: `FUN_140163120` reads capacity-like metadata, but the required component enumeration path is recoverable from the embedded vector `First/Last` and the recovered 16-byte entry shape. Production no longer gates component lookup on this field.
- `ItemStruct.VTablePtr +0x00`: removed as a definition-only structural placeholder. Entity field keypoints start at `EntityDetailsPtr +0x08`.
- `PlayerOffsets.UnknownNumber1 +0x98`: definition-only oscillating value with no object-model consumer.
- `VitalStruct.VtablePtr +0x00` and `VitalStruct.FractionalCurrent +0x30`: removed as definition-only placeholders. Required vital reads are covered by constructor/stat-id/current/total/reservation keypoints.

## Promotion Criteria

An offset can move from `hardcoded` to target-ready only when all of the following are true:

1. We know which object owns the field.
2. We know whether the recovered value is an offset, an RVA, a final address, or an immediate.
3. The evidence note names the function/address/pattern that proves it.
4. A LocalProcess integration test proves the recovered fact against the live object model.
5. The code comments point back to the evidence file.
