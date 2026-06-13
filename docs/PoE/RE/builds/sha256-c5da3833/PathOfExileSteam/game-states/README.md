# Game States

Build: `sha256-c5da3833`
Binary: `PathOfExileSteam.exe`
Intent: `game-states`
Status: `active`
Confidence: `confirmed`

## Current Conclusion

The existing `Offsets.Patterns` `GameStates` signature resolves a stable RIP-relative global slot for the live `GameStates` owner. The constructor and InGame accessors also provide stable keypoints for state table shape, the shared `StdVector` header shape, the current-state vector, behavior-backed InGame-state selection through the virtual update slot, `InGameState.AreaInstanceData`, `InGameState.MsElapsed`, the direct `InGameState.ZoneSwitchCounter`, the constructor-backed nested zone-switch state pointer, `AreaInstance.CurrentAreaLevel`, `AreaInstance.CurrentAreaHash`, `AreaInstance.LocalPlayers`, `AreaInstance.EntitiesCount`, entity id/status fields, `EntityDetails.name`, the generic entity component lookup chain including component bucket entry stride, the component header owner pointer, the player `Player.Name` string, and the player `Life` vital layout.

`RuntimeGameOffsets` now consumes the root/static and entity/component keypoints
in production for `TheGame`, `GameStates`, `InGameState`, `AreaInstance`,
`Entity`, `ComponentBase`, `Player`, and `Life`.

`OffsetCoverage.audit.md` maps the current required production offset surface to
the provider properties, evidence notes, and live integration tests. It also
lists structural/test-only fields that are intentionally not production
dependencies.

## Anchors

- Ghidra: `FUN_14010c350`, hit VA `0x14010c365`, displacement VA `0x14010c371`, global slot VA `0x1444E9EB8`.
- Ghidra: `FUN_14010c350`, wrapper-copy tail at VA `0x14010C4A8`; owner global `DAT_1444E9EB8` is returned at wrapper offset `+0x00`, sidecar global `DAT_1444E9EC0` at `+0x08`.
- Ghidra: `FUN_14010c350`, state table constructor at VA `0x14010C3C0`, table offset `0x48`, entry size `0x10`, entry count `0xD`.
- Ghidra: `FUN_1415B0360`, dirty-copy path at VA `0x1415B03D9`; source current-state vector is `GameStates +0x08/+0x10`, dispatch vector is `+0x20/+0x28/+0x30`, dirty flag is `+0x40`, entries are `0x10` bytes, and first/last/end are consecutive pointer fields.
- Ghidra: `FUN_1415B2890`, state update dispatch at VA `0x1415B2A54`; calls `state.vtable +0x08`, giving `GameStateVirtualUpdateSlot=0x08`.
- Ghidra: `FUN_140233C10`, InGame entry read at VA `0x140233C47`, area pointer read at VA `0x140233CB0`.
- Ghidra: AreaInstance vtable static VA `0x1432C9C48`, slots `0x140`/`0x148` -> `FUN_1420731D0`; loop reads `AreaInstance + 0x588..0x590` and appends non-null qword entries.
- Ghidra: `FUN_140FAD6E0`, elapsed timer writer at VA `0x140FAD72C`, `MOV dword ptr [InGameState + 0x400], EAX`; live `InGameState` vtable slot `+0x8` points at wrapper `FUN_140FAD1A0`, which tail-branches to the writer.
- Ghidra: label-less analyzed block at VA `0x140FB3F60`, direct counter compare `CMP dword ptr [InGameState + 0x56C], 1`.
- Ghidra: `FUN_140FAC260`, direct counter reset at VA `0x140FAC37A`, `MOV dword ptr [InGameState +0x56C], EBP`, adjacent flag clear at `+0x568`.
- Ghidra: `FUN_140FB24A0`, direct counter increments at VA `0x140FB2831` and `0x140FB28E6`, both `INC dword ptr [InGameState +0x56C]`.
- Ghidra: `FUN_140FAF2F0`, InGame-shaped state check at VA `0x140FAF31B`, nested zone-switch state pointer read at VA `0x140FAF3E9`, nested zone-switch byte check at VA `0x140FAF3F0`.
- Ghidra: `FUN_140FA9D00`, nested transition object allocation/assignment at VA `0x140FAA2D9` through `0x140FAA306`, old/new slot displacements both `InGameState +0x368`.
- Ghidra: label-less analyzed block at VA `0x140FB33F0`, `MOV RAX, [InGameState + 0x290]`, then `MOVZX ECX, byte ptr [AreaInstance + 0x0C4]`.
- Ghidra: `FUN_142061AD0`, current-area holder hash read at VA `0x1420607C2`, `MOV EAX, dword ptr [AreaInstance + 0x11C]`.
- Ghidra: `FUN_14205D6D0`, holder constructor stores `AreaInstance` at holder `+0x2170` and stores the holder back at `AreaInstance +0x580`.
- Ghidra: `FUN_14206E780`, base AreaInstance constructor initializes the entity tree at `AreaInstance +0x6B0`, root/sentinel slot at `+0x6C0`, and count at `+0x6C8`.
- Ghidra: `FUN_14163B7A0`, AreaInstance destructor iterates `param_1[0xD8]`, reads entity pointers from tree nodes, filters entity fields, and cleans the same container through `FUN_141DC14B0(param_1 + 0xD6)`.
- Ghidra: `FUN_14163B7A0`, entity-tree filter at VA `0x14163BA04` tests `Entity +0x8C` with invalid mask `0x01`, compares `Entity +0x88` against `0x40000000`, and checks adjacent flag byte `Entity +0x8D` with required mask `0x04`.
- Ghidra: live player Entity vtable static VA `0x1433FE2F8`; slot `+0x10` -> `0x141C7A6D0`, which returns `*(Entity +0x08) + 0x08`.
- Ghidra: `FUN_1401512E0`, named component resolver; reads `Entity + 0x08`, `Entity + 0x10`, `EntityDetails + 0x28`, `ComponentLookUp + 0x28`, `ComponentLookUp + 0x30`, and `ComponentNameAndIndex + 0x08`.
- Ghidra: `FUN_140163120`, component name/index bucket lookup helper; returns a 16-byte entry whose first qword is the component-name pointer and whose dword at `+0x08` is the component index. The helper computes entry addresses with `SHL RDX, 0x04`, proving the `0x10` entry size.
- Ghidra: Player component vtable slot `0x68` -> `FUN_141D267F0`; entry reads `ComponentHeader.EntityPtr` at `PlayerComponent +0x08`, and the live test verifies `ComponentHeader.StaticPtr +0x00` is the vtable pointer.
- Ghidra: live Player component vtable static VA `0x1432C4758`; slot `0x68` -> `FUN_141D267F0`, which prints `"Character Name: "` and reads the `StdWString` at `Player + 0x1B0`.
- Ghidra: live Life component vtable static VA `0x1433EE948`; slot `0x98` -> `FUN_141CE1C00`, which walks `Life + 0x1A8`, `Life + 0x200`, and `Life + 0x240`.
- Ghidra: `FUN_141CE13A0`, common vital reservation deserializer; writes `VitalStruct +0x18` and `VitalStruct +0x1C`.
- Ghidra: live Life component vtable slot `0x68` -> `FUN_141CE2300`, which prints current/total pairs from `Life + 0x1E0/0x1DC`, `Life + 0x278/0x274`, and `Life + 0x238/0x234`.
- Ghidra: `FUN_141CDC130`, Life constructor; initializes embedded vital owner pointers and stat-id fields, including `VitalStruct +0x28` as a dword id rather than a float regeneration value.
- Runtime: `ShouldResolveGameStatesSingletonFromSignatureKeypoint` prints `patternRva=0x10C371`, `globalSlot=0x7FF775419EB8`, `gameStates=0x33E22DF3A00`, `inGameState=0x33E22E42D10`, `area=0x33E98D2C800`.
- Runtime: `ShouldResolveGameStateStaticWrapperFromGetterPattern` recovers wrapper offsets `+0x00/+0x08`, verifies the owner global slot is the `GameStates` singleton slot, and reaches the live InGame state through `GameStateStaticOffset.GameState`.
- Runtime: `ShouldResolveCurrentStateVectorFromDirtyCopyPattern` recovers `currentVectorFirstOffset=0x08`, `currentVectorLastOffset=0x10`, `dispatchVectorFirstOffset=0x20`, `dispatchVectorLastOffset=0x28`, `dispatchVectorEndOffset=0x30`, `dirtyFlagOffset=0x40`, and verifies `RuntimeGameOffsets.ReadCurrentStateEntry` returns the InGame state while loaded in-world.
- Runtime: `ShouldResolveInGameAreaKeypointsFromCodePatterns` prints `stateTableOffset=0x48`, `stateEntrySize=0x10`, `stateCount=0xD`, `inGameEntryOffset=0x88`, `inGameStateIndex=4`, `areaInstanceOffset=0x290`.
- Runtime: `ShouldResolveAreaInstanceLocalPlayersFromCodePattern` recovers `localPlayersOffset=0x588`, reads one vector entry, and verifies it equals `TheGame.Player.Address`.
- Runtime: `ShouldResolveAreaInstanceCurrentAreaLevelFromCodePattern` recovers `currentAreaLevelOffset=0x0C4`, verifies the preceding owner displacement equals `InGameState.AreaInstanceData`, and compares the raw byte with the managed object model.
- Runtime: `ShouldResolveAreaInstanceCurrentAreaHashFromCodePattern` recovers `currentAreaHashOffset=0x11C` and compares the raw dword with the managed object model.
- Runtime: `ShouldResolveAreaInstanceEntitiesCountFromCodePattern` recovers `entityTreeRootOffset=0x6C0`, derives `entitiesCountOffset=0x6C8`, and verifies raw count `105` against the managed struct and object model.
- Runtime: `ShouldResolveEntityIdentityFromAreaInstanceTreeFilter` recovers `idOffset=0x88`, `statusOffset=0x8C`, `statusMask=0x01`, `idUpperBound=0x40000000`, `activeFlagOffset=0x8D`, and `activeMask=0x04`, then verifies raw player id/status and the production validity predicate against `EntityOffsets` and the object model.
- Runtime: `ShouldResolveEntityDetailsNameFromVtableAccessor` recovers `EntityDetailsPtr=0x08` and `EntityDetails.name=0x08`, then verifies the raw metadata path equals `TheGame.Player.Path`.
- Runtime: `ShouldResolveMsElapsedFromCodePattern` recovers `msElapsedOffset=0x400` and verifies the live dword advances; `ShouldResolveMsElapsedWriterFromInGameStateUpdateVtableChain` verifies `GameStateVirtualUpdateSlot=0x08` and the live vtable update wrapper branches to the writer.
- Runtime: `ShouldResolveZoneSwitchCounterAndStatePointerFromCodePatterns` recovers direct `directZoneSwitchCounterOffset=0x56C` and separately recovers `zoneSwitchStateOffset=0x368`; constructor read/write keypoints also resolve `0x368`.
- Runtime: `ShouldResolveZoneSwitchCounterWriterSitesFromCodePatterns` recovers `resetOffset=0x56C`, `incrementFirstOffset=0x56C`, `incrementSecondOffset=0x56C`, and adjacent reset flag offset `0x568`.
- Runtime: `ShouldResolveEntityComponentLookupFromCodePattern` recovers the entity/component lookup offsets plus bucket entry shape `name +0x00`, `index +0x08`, `size 0x10`; resolves `Player` and `Life`; and verifies both component headers point back to the player entity. `ShouldResolveEntityComponentLookupShapeConsistentlyAcrossGenericResolvers` covers the non-unique generic resolver keypoint by scanning all 102 resolver-shaped matches in the live binary and proving they encode one layout.
- Runtime: `ShouldResolveComponentHeaderFromPlayerFormatter` recovers `ComponentHeader.EntityPtr=0x08`, verifies `ComponentHeader.StaticPtr=0x00`, checks Player vtable slot `+0x68` reaches the formatter, and verifies Player/Life owners equal the live player entity. Component address resolution uses the recovered bucket entry shape.
- Runtime: `ShouldResolvePlayerNameFromCodePattern` recovers `PlayerOffsets.Name` plus the shared `StdWString` buffer/inline/length/capacity/SSO shape, then verifies the provider-read string equals `PlayerComponent.Name`. Component address resolution uses the recovered bucket entry shape.
- Runtime: `ShouldResolveLifeVitalsFromCodePatterns` recovers `LifeOffset.Health/Mana/EnergyShield`, `VitalStruct.ReservedFlat/ReservedPercent`, and `VitalStruct.Current/Total`, then verifies raw HP/MP/ES/reserve reads against the managed object model. Component address resolution uses the recovered bucket entry shape.
- Runtime: `ShouldResolveLifeVitalConstructorShapeFromCodePattern` recovers `VitalStruct.UnknownStatId0`, `UnknownStatId1`, `LifeComponentPtr`, `TotalStatId`, `UnknownStatId2`, and `UnknownStatId3`, then verifies live Health/Mana/ES owner pointers and constructor-written dword ids. Component address resolution uses the recovered bucket entry shape.
- Runtime: `ShouldReadRootObjectModelThroughRuntimeGameOffsets` proves production root traversal consumes provider-derived offsets for `GameStates`, `InGameState`, and `AreaInstance`.
- Runtime: `ShouldReadEntityComponentsThroughRuntimeGameOffsets` proves production entity/component traversal consumes provider-derived offsets for player identity, provider-backed component lookup enumeration, Player name, Life vitals, and `VitalStruct` fields.
- Runtime: the provider tests validate shared `StdVector` first/last/end headers against current-state, local-player, component-list, and component-bucket vectors before enumeration.
- Product: `CheatCartridge/GameHelper/GameOffsets/Offsets.cs`, `CheatCartridge/GameHelper/GameOffsets/GameStateStaticOffset.cs`, `CheatCartridge/GameHelper/GameOffsets/GameStateOffset.cs`.

## Reconstructed Files

- `GameStatesSingleton.evidence.md`
- `GameStateVirtualUpdateSlot.evidence.md`
- `CurrentStateVector.evidence.md`
- `GameStateTableAndInGameArea.evidence.md`
- `AreaInstanceLocalPlayers.evidence.md`
- `AreaInstanceScalars.evidence.md`
- `EntityComponentLookup.evidence.md`
- `ComponentHeader.evidence.md`
- `EntityDetailsName.evidence.md`
- `EntityIdentity.evidence.md`
- `LifeVitals.evidence.md`
- `MsElapsed.evidence.md`
- `OffsetCoverage.audit.md`
- `PlayerName.evidence.md`
- `RuntimeOffsetProvider.evidence.md`
- `StdVector.evidence.md`
- `ZoneSwitchState.evidence.md`

## Related Slices

- `../binary/PathOfExileSteam.exe`

## Open Questions

- Confirm any remaining entity metadata/status enum semantics only if product logic needs more than the current path/id/valid invariants.
- Confirm any remaining `AreaInstance` fields from vtable/function-family anchors rather than broad offset searches.
- Follow state owner update and registration loops around `FUN_142ad9c6c`.
- Decide whether the constructor-backed nested object at `InGameState +0x368` needs a managed model; currently only the parent pointer is required.

## Promotion Notes

Promote this slice from active to stable after one more client build confirms the same function shape or an equivalent singleton initialization pattern.
