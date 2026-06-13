# Runtime Offset Provider

`RuntimeGameOffsets` consumes the static RE keypoints instead of letting
production reads depend directly on managed `[FieldOffset]` structs for the
required root, entity, component, and vital object-model surface.

## Static Basis

The provider resolves these values from the existing code patterns:

```text
GameStates global slot
GameStateStatic wrapper owner offset
GameStateStatic wrapper sidecar offset
GameStates current-state vector offset
GameStates dispatch-state vector offset
GameStates dirty flag offset
StdVector first/last/end offsets
GameStates state table offset
GameStates state entry size/count
GameState virtual update slot offset
InGameState.MsElapsed writer function address from the writer-start keypoint
InGameState.AreaInstanceData offset
InGameState.MsElapsed offset
InGameState.ZoneSwitchCounter offset
AreaInstance.CurrentAreaLevel offset
AreaInstance.CurrentAreaHash offset
AreaInstance.LocalPlayers offset
AreaInstance entity tree root offset
AreaInstance.EntitiesCount offset = entity tree root + pointer size
Entity.EntityDetailsPtr offset
Entity.ComponentList offset
Entity.Id / status / active-flag offsets
Entity identity filter masks/bounds
EntityDetails.name offset
EntityDetails.ComponentLookUpPtr offset
ComponentLookUp.ComponentsNameAndIndex bucket offset
ComponentNameAndIndex.NamePtr offset
ComponentNameAndIndex.Index offset
ComponentNameAndIndex entry size
ComponentHeader.EntityPtr offset
Player.Name offset
StdWString buffer/inline/length/capacity offsets and SSO limit
Life.Health/Mana/EnergyShield offsets
VitalStruct owner/stat-id/reservation/current/total offsets
```

The static Ghidra basis remains the same as the individual evidence notes:

- `FUN_14010C350`: GameStates singleton/static wrapper and state table shape.
- `FUN_1415B0360`: current-state source vector.
- `FUN_1415B2890`: game-state update dispatch through
  `state.vtable +0x08`.
- `StdVector.evidence.md`: shared first/last/end vector header shape used by
  current-state, local-player, component-list, and component-bucket reads.
- `FUN_140233C10`: InGameState to AreaInstance pointer.
- `FUN_140FAD6E0`: InGameState elapsed dword writer. The provider now uses a
  start-anchored writer keypoint for the function address and a separate
  displacement keypoint for the `+0x400` field offset; it does not derive the
  function address by subtracting a fixed byte count from the write operand.
- `FUN_140FB3F60` block plus writer sites: direct zone-switch counter.
- `FUN_1420731D0`: AreaInstance local player vector.
- `FUN_14206E780`: AreaInstance entity tree root/count container.
- `FUN_14163B7A0`: entity-tree filter proving Entity id/status/active fields.
- `FUN_141C7A6D0`: EntityDetails path/name accessor.
- `FUN_1401512E0` and `FUN_140163120`: generic named-component lookup.
- `FUN_141D267F0`: Player component owner and name formatter.
- `FUN_141CE1C00`: Life component vital starts.
- `FUN_141CE2300`: Life vital current/total formatter.
- `FUN_141CE13A0`: Vital reservation deserializer.
- `FUN_141CDC130` and `FUN_141CD0640`: Vital constructor/stat-id fields.

## Production Use

The production object-model root now uses `RuntimeGameOffsets` for:

```text
TheGame.UpdateData
GameStates.UpdateData
InGameState.UpdateData
AreaInstance.UpdateData
Entity.UpdateData
ComponentBase.UpdateData
Player.UpdateData
Life.UpdateData
```

`GameStates.UpdateData` no longer selects the InGame state only by enum-derived
entry offset. It scans the recovered state table and picks the state whose
virtual update slot branches to the pattern-backed `InGameState.MsElapsed`
writer. The enum-derived `0x88` entry remains covered as a comparator.

The static current-build chain behind that behavior is:

```text
FUN_1415B2890  state dispatch loop
1415B2A54      CALL qword ptr [RAX + 0x8]

live InGameState vtable slot +0x8 -> FUN_140FAD1A0
140FAD279      JMP 0x140FAD6E0

FUN_140FAD6E0  elapsed writer
140FAD72C      MOV dword ptr [RCX + 0x400], EAX
```

The wrapper tail is also statically searchable as a unique byte shape in the
copied binary:

```text
0F 28 D6 48 8B D7 48 8B CB 48 8B 5C 24 40
0F 28 74 24 20 48 83 C4 30 5F E9 ?? ?? ?? ?? CC CC
-> 0x140FAD261
```

`GameStates.UpdateData` also no longer open-codes the latest current-state
vector arithmetic. It calls `RuntimeGameOffsets.ReadCurrentStateAddress`, which
uses the recovered source current-state vector offset and recovered 16-byte
state entry size. Dispatch-vector offsets and the dirty flag are recovered from
the same static dirty-copy keypoint and verified by tests.

This removes direct production traversal dependence on these hardcoded struct
reads:

```text
GameStateStaticOffset.GameState
GameStateOffset.CurrentStatePtr
GameStateOffset.States
InGameStateOffset.AreaInstanceData
AreaInstanceOffsets.CurrentAreaLevel
AreaInstanceOffsets.CurrentAreaHash
AreaInstanceOffsets.LocalPlayers
AreaInstanceOffsets.EntitiesCount
EntityOffsets.Id
EntityOffsets.IsValid
ItemStruct.EntityDetailsPtr
ItemStruct.ComponentListPtr
EntityDetails.name
EntityDetails.ComponentLookUpPtr
ComponentLookUpStruct.ComponentsNameAndIndex
ComponentNameAndIndexStruct name pointer/index/entry-size shape
StdBucket.Capacity
ComponentHeader.EntityPtr
PlayerOffsets.Name
StdWString buffer/inline/length/capacity/SSO shape
LifeOffset.Health
LifeOffset.Mana
LifeOffset.EnergyShield
VitalStruct current/total/reservation/stat-id fields
```

The structs remain in the codebase as test comparators and compact layout
documentation.

## Production Hardcoded-Read Audit, 2026-06-13

The current production object-model surface was audited for direct live reads
from hardcoded layout structs after `RuntimeGameOffsets` was introduced.

The following production readers consume provider-derived offsets:

```text
TheGame.UpdateData
GameStates.UpdateData
InGameState.UpdateData
AreaInstance.UpdateData
Entity.UpdateData
ComponentBase.UpdateData
Player.UpdateData
Life.UpdateData
HeadlessBotMode.ReadZoneSwitchCounter
```

The remaining numeric offsets in production code are not direct game-object
field offsets. They are offsets inside the matched instruction patterns used by
`RuntimeGameOffsets.Resolve` to read adjacent operands from the same verified
byte sequence, for example:

```text
GameStateStaticWrapper: returnedRva +0x0D reads the wrapper sidecar store offset.
GameStateCurrentStateVector: returnedRva -0x04 reads the vector Last field operand.
EntityComponentLookupShape: returnedRva +0x1C/+0x2C/+0x3E/+0x47 reads operands in one resolver block.
LifeVitalConstructorShape: returnedRva +0x90/+0xFC reads Mana/EnergyShield constructor operands.
```

Those numbers are pattern-local extraction offsets, not object-model layout
constants. If a pattern moves or changes shape, `MemoryUtils.GetOffsets` or the
provider's cross-checks fail before the object model uses the derived offsets.

The `[FieldOffset]` structs remain as compact layout documentation and test
comparators. The live production path no longer depends on reading those structs
wholesale for the required current area, local player, player name, component
lookup, hp/mp/es, elapsed timer, or zone-switch counter surface.

The production string path also no longer reads `StdWString` wholesale before
decoding. `FUN_141D267F0` proves the shared string shape used by `Player.Name`
and `EntityDetails.name`: capacity at `+0x18`, length at `+0x10`, inline buffer
at `+0x00`, external buffer pointer at `+0x00`, and SSO limit `7`.
`RuntimeGameOffsets.ReadStdWString` consumes those recovered fields from a
string address.

The component lookup path also no longer depends on `StdBucket.Capacity`.
Ghidra decompile of `FUN_140163120` proves the helper uses the bucket's first
two qwords as the entry vector `First` and `Last`, and the separate
`ComponentNameAndIndexEntryStride` keypoint recovers the 16-byte entry shape.
Production now bounds the component-name walk from that recovered vector span
plus a caller cap instead of trusting the capacity-like field.

The production entity validity path no longer checks one exact live status byte.
`RuntimeGameOffsets` recovers the full AreaInstance entity-tree filter predicate
from `FUN_14163B7A0`: status invalid mask `0x01`, id upper bound
`0x40000000`, and required active-flag mask `0x04`. `Entity.UpdateData` uses
that recovered predicate through `PassesEntityIdentityFilter`.

The vector path now validates the provider-derived `StdVector` header before
enumeration. `FUN_1415B0360` proves `First/Last/End` as consecutive pointer
slots through the current-state and dispatch vectors: source `+0x08/+0x10`,
dispatch `+0x20/+0x28/+0x30`. Runtime enumeration rejects headers where
`Last < First`, `End < Last`, or count/capacity are not aligned to the element
size. The live provider tests verify this shape against current-state,
local-player, component-list, and component-bucket vectors.

## Live Proof, 2026-06-13

`ShouldReadRootObjectModelThroughRuntimeGameOffsets` resolved the provider from
the live module, walked the root object graph, and compared it to `TheGame`:

```text
client PID            = 5592
GameStates global     = 0x7FF775419EB8
GameStates owner      = 0x0000027ABEDE39C0
current state         = 0x0000027ABEE62F10
InGameState           = 0x0000027ABEE62F10
current vector        = +0x08/+0x10
dispatch vector       = +0x20/+0x28/+0x30
dirty flag            = +0x40
virtual update slot   = 0x08
slot function         = 0x00007FF771EDD1A0
writer function       = 0x00007FF771EDD6E0
AreaInstance          = 0x0000027A8D887000
area level            = 38
area hash             = 0x69A58CE3
entities              = 105
local player          = 0x0000027A939CB980
```

The same run also executed
`ShouldReadCurrentPlayerSnapshotWithFullVitalsFromObjectModel` after production
root traversal was moved to the provider:

```text
player name           = Xaber
entity                = 0x0000027A939CB980
path                  = Metadata/Characters/StrDex/StrDexFourb
hp                    = 1427 / 1427
mp                    = 349 / 349
energy shield         = 115 / 115
area level/hash       = 38 / 1772457187
entities              = 105
```

After entity/component plumbing was moved to the provider,
`ShouldReadEntityComponentsThroughRuntimeGameOffsets` resolved the live player
entity, Player component, Life component, and vitals through runtime-derived
offsets and compared them to `TheGame`:

```text
client PID            = 5592
entity                = 0x0000027A939CB980
entity id             = 2858
status / active flags = 0x0C / 0x0C
status/id/active masks= 0x01 / 0x40000000 / 0x04
path                  = Metadata/Characters/StrDex/StrDexFourb
Player component      = 0x0000027A8D1DC030
Life component        = 0x0000027B508C8430
component entry shape = name +0x00, index +0x08, size 0x10
player name           = Xaber
hp                    = 1427 / 1427
mp                    = 349 / 349
energy shield         = 115 / 115
```

This is the current proof that production entity/component reads are using the
provider-derived offset surface and still agree with raw live memory. The final
production dependency on `ComponentNameAndIndexStruct` enumeration was removed:
`Entity.UpdateData` now walks the lookup bucket through
`RuntimeGameOffsets.ReadComponentNameIndexEntries`.

The downstream component-internal keypoint tests now resolve the `Player` and
`Life` component addresses through the same recovered bucket entry shape instead
of `ReadStdBucket<ComponentNameAndIndexStruct>`:

```text
ShouldResolveComponentHeaderFromPlayerFormatter: passed
ShouldResolvePlayerNameFromCodePattern: passed
ShouldResolveLifeVitalsFromCodePatterns: passed
ShouldResolveLifeVitalConstructorShapeFromCodePattern: passed
```

This keeps the component header/name/vital offset proofs dependent on the
static `EntityComponentLookupShape` and `ComponentNameAndIndexEntryStride`
keypoints rather than the managed bucket-entry struct.

## Current-Build Rehearsal, 2026-06-13

The current live client was restarted and rechecked against the archived build
copy used by this evidence pack:

```text
live PID              = 5592
live process          = PathOfExileSteam
live title            = Path of Exile 2
live binary           = D:\SteamLibrary\steamapps\common\Path of Exile 2\PathOfExileSteam.exe
archived binary       = docs\PoE\RE\builds\sha256-c5da3833\PathOfExileSteam\binary\PathOfExileSteam.exe
sha256                = c5da38334762a28ceb77a65e16e741a7fddb19ca38c4f414566e1024d4e4637d
```

Static keypoint spot-checks in the aligned Ghidra project still resolve the
expected current-build anchors:

```text
GameStates singleton/root pattern               -> 0x14010C365, unique
GameState table shape                           -> 0x14010C3C0, unique
InGameState.MsElapsed writer function start     -> 0x140FAD6E0, unique
InGameState.ZoneSwitchCounter direct counter    -> 0x140FB3F59, unique
AreaInstance.CurrentAreaLevel keypoint          -> 0x140FB33F0, unique
AreaInstance.LocalPlayers keypoint              -> 0x142073231, unique
AreaInstance entity tree constructor keypoint   -> 0x14206EBCB, unique
Entity identity filter keypoint                 -> 0x14163B9EB, unique
Life shared constructor keypoint                -> 0x141CD064A, unique
Life direct constructor shape                   -> 0x141CDC248, unique
```

The `EntityComponentLookupShape` pattern is intentionally not treated as a
unique function anchor. Ghidra finds 102 resolver-shaped matches in this build.
`ShouldResolveEntityComponentLookupShapeConsistentlyAcrossGenericResolvers`
scans those matches in the live binary and proves they all encode the same
layout:

```text
genericResolverMatches = 102
distinctLayouts        = 1
detailsOffset          = 0x08
componentListOffset    = 0x10
detailsLookupOffset    = 0x28
lookupBucketOffset     = 0x28
lookupEndOffset        = 0x30
entryIndexOffset       = 0x08
```

The full explicit `LocalProcessClientIntegrationTests` class passed against
the restarted client:

```text
command  = dotnet test .\CheatCartridge.Tests\CheatCartridge.Tests.csproj --no-restore --filter "FullyQualifiedName~CheatCartridge.Tests.Integration.LocalProcessClientIntegrationTests" -- NUnit.Explicit=true
result   = Passed: 24, Failed: 0, Skipped: 0
duration = 3m42s
```

The MsElapsed-specific checks prove the current live target being tracked:

```text
InGameState           = 0x0000027ABEE62F10
MsElapsed offset      = 0x400
MsElapsed address     = InGameState + 0x400 = 0x0000027ABEE63310
writer function       = 0x00007FF771EDD6E0
vtable slot wrapper   = 0x00007FF771EDD1A0
virtual update slot   = 0x08
slot branches writer  = true
```

The same run confirmed the provider-owned object model still reaches the player
and area data:

```text
player name           = Xaber
entity                = 0x0000027A939CB980
entity id             = 2858
path                  = Metadata/Characters/StrDex/StrDexFourb
hp                    = 1427 / 1427
mp                    = 349 / 349
energy shield         = 115 / 115
AreaInstance          = 0x0000027A8D887000
area level/hash       = 38 / 0x69A58CE3
entities              = 105
ZoneSwitchCounter     = InGameState + 0x56C = 0x0000027ABEE6347C
```

Client health after the run:

```text
PathOfExileSteam PID 5592, Responding = true
```
