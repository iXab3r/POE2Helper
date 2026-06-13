# Life Vitals

Build: `sha256-c5da3833`
Binary: `PathOfExileSteam.exe`
Slice: `game-states`
Status: `confirmed`

## Finding

The `Life` component owns embedded vital objects for health, mana, and energy
shield:

```text
Life + 0x1A8 -> Health VitalStruct
Life + 0x200 -> Mana VitalStruct
Life + 0x240 -> EnergyShield VitalStruct
```

The vital object layout used by CheatCartridge is also confirmed:

```text
VitalStruct + 0x08 -> UnknownStatId0
VitalStruct + 0x0C -> UnknownStatId1
VitalStruct + 0x10 -> LifeComponentPtr
VitalStruct + 0x18 -> ReservedFlat
VitalStruct + 0x1C -> ReservedPercent
VitalStruct + 0x20 -> TotalStatId
VitalStruct + 0x24 -> UnknownStatId2
VitalStruct + 0x28 -> UnknownStatId3
VitalStruct + 0x34 -> Total
VitalStruct + 0x38 -> Current
```

Important correction: `VitalStruct +0x28` is not a float regeneration
amount. The Life constructor writes integer stat-like ids there:

```text
Health       +0x28 = 0x0759
Mana         +0x28 = 0x1C9C
EnergyShield +0x28 = 0x4DE5
```

## Vtable Route

The live player `Life` component was resolved through the already pattern-backed
entity component lookup chain. A read-only runtime check showed:

```text
lifeComponent = 0x27B508C8430
life vtable   = 0x7FF77431E948
module base   = 0x7FF770F30000
vtable RVA    = 0x33EE948
static VA     = 0x1433EE948
owner entity  = 0x27A939CB980
```

From that vtable:

```text
slot 0x68 -> FUN_141CE2300
slot 0x98 -> FUN_141CE1C00
```

## Vital Object Starts

`FUN_141CE1C00` walks embedded vital-like objects on the `Life` component:

```text
141CE1C1A  ADD RCX, 0x1A8
141CE1C21  CALL FUN_141CE1860
141CE1C26  LEA RCX, [RBP + 0x200]
141CE1C30  CALL FUN_141CDEB60
141CE1C35  LEA RCX, [RBP + 0x240]
141CE1C3F  CALL FUN_141CE1860
```

The unique keypoint pattern is `Offsets.KeypointNames.LifeComponentVitalOffsets`.
The caret marks the immediate operand in `ADD RCX, 0x1A8`.

```text
48 8B E9
48 8B FA
48 81 C1 ^ ?? ?? ?? ??
E8 ?? ?? ?? ??
48 8D 8D ?? ?? ?? ??
48 8B D7
E8 ?? ?? ?? ??
48 8D 8D ?? ?? ?? ??
48 8B D7
E8
```

The integration test reads:

```text
+0x00 -> 0x1A8  Health
+0x0C -> 0x200  Mana
+0x1B -> 0x240  EnergyShield
```

This keypoint is vtable-derived. It should be preferred over broad searches for
`0x1A8`, which appears in many unrelated object layouts.

## Constructor Header And Stat Ids

`FUN_141CDC130` is the Life component constructor. It starts from the component
base in `RSI` and directly initializes the embedded vital objects.

Health:

```text
141CDC248  LEA R14, [RSI + 0x1A8]
141CDC24F  MOV dword ptr [R14 + 0x08], 0x3334
141CDC257  MOV dword ptr [R14 + 0x0C], 0x333B
141CDC25F  MOV qword ptr [R14 + 0x10], RSI
141CDC271  MOV dword ptr [R14 + 0x20], 0xEF
141CDC279  MOV dword ptr [R14 + 0x24], 0x584
141CDC281  MOV dword ptr [R14 + 0x28], 0x759
141CDC2A9  MOV dword ptr [R14 + 0x34], EAX
141CDC2AD  MOV dword ptr [R14 + 0x38], EAX
```

Mana:

```text
141CDC2D8  LEA R15, [RSI + 0x200]
141CDC2DF  MOV dword ptr [R15 + 0x08], 0x6986
141CDC2E7  MOV dword ptr [R15 + 0x0C], 0x6986
141CDC2EF  MOV qword ptr [R15 + 0x10], RSI
141CDC2FA  MOV dword ptr [R15 + 0x20], 0xF0
141CDC302  MOV dword ptr [R15 + 0x24], 0x585
141CDC30A  MOV dword ptr [R15 + 0x28], 0x1C9C
141CDC332  MOV dword ptr [R15 + 0x34], EAX
141CDC336  MOV dword ptr [R15 + 0x38], EAX
```

Energy Shield is initialized through shared helper `FUN_141CD0640`:

```text
141CDC344  LEA R12, [RSI + 0x240]
141CDC35B  MOV dword ptr [RSP + 0x20], 0x4DE5
141CDC363  MOV R9D, 0x586
141CDC369  MOV R8D, 0xF1
141CDC375  CALL FUN_141CD0640

FUN_141CD0640:
  param_1[2] = LifeComponent             ; Vital +0x10
  *(dword *)(Vital +0x08) = 0x6986
  *(dword *)(Vital +0x0C) = 0x6986
  *(dword *)(Vital +0x20) = param_3
  *(dword *)(Vital +0x24) = param_4
  *(dword *)(Vital +0x28) = param_5
```

The unique keypoint pattern is `Offsets.KeypointNames.LifeVitalConstructorShape`.
The caret marks the displacement in `LEA R14, [RSI + 0x1A8]`. The integration
test uses nearby instruction displacements and immediates to recover the owner
pointer field offset and constructor-written stat ids.

Energy Shield uses the shared helper for its `+0x08/+0x0C` defaults, so the
helper has its own unique keypoint:
`Offsets.KeypointNames.LifeVitalSharedConstructorShape`.

This constructor evidence rejects the previous `Regeneration` name for
`VitalStruct +0x28`: the value is written as a dword immediate during object
construction and remains a stat-like integer in live memory.

## Reservation Fields

`FUN_141CE13A0` is the common vital reservation deserializer helper. The caller
chain ties it back to the same Life-owned vital objects:

```text
FUN_141CDEB60
  writes Vital +0x38
  calls FUN_141CE13A0
  writes Vital +0x30

FUN_141CE1860
  calls FUN_141CDEB60
  writes Vital +0x48

FUN_141CE1C00
  calls FUN_141CE1860(Life +0x1A8)
  calls FUN_141CDEB60(Life +0x200)
  calls FUN_141CE1860(Life +0x240)
```

The reservation helper itself writes the CheatCartridge reserve fields:

```text
141CE13BE  MOV dword ptr [R8 + 0x18], ECX
141CE13DE  MOVSX ECX, word ptr [RCX + RAX]
141CE13E6  MOV dword ptr [R8 + 0x1C], ECX
```

Important nuance: the stream stores `ReservedPercent` as a signed 16-bit value,
then the helper sign-extends it into the 32-bit field at `VitalStruct +0x1C`.
The managed `int` field is therefore correct.

The unique keypoint pattern is `Offsets.KeypointNames.LifeVitalReservationOffsets`:

```text
4C 8B 52 08
4C 8B C1
48 8B 4A 10
48 8D 41 04
49 3B C2
77 ??
48 8B 02
8B 0C 01
41 89 48 ^ 18
48 8B 4A 10
48 8B 42 08
48 83 C1 04
48 89 4A 10
4C 8D 49 02
4C 3B C8
77 ??
48 8B 02
0F BF 0C 01
4C 89 4A 10
41 89 48 1C
```

The caret marks the one-byte displacement for `ReservedFlat`. The
`ReservedPercent` displacement is at `returnedRva +0x28`.

## Current And Total

`FUN_141CE2300` is a Life debug/info formatter. It prints `Life`, `ES`, `Mana`,
`Ward`, and `Divinity` current/total pairs. The first three pairs prove the
fields used by CheatCartridge:

```text
141CE236A  MOV EDX, dword ptr [RBX + 0x1E0]  ; health current
141CE2387  MOV EDX, dword ptr [RBX + 0x1DC]  ; health total
141CE23B3  MOV EDX, dword ptr [RBX + 0x278]  ; energy shield current
141CE23D0  MOV EDX, dword ptr [RBX + 0x274]  ; energy shield total
141CE23FC  MOV EDX, dword ptr [RBX + 0x238]  ; mana current
141CE2419  MOV EDX, dword ptr [RBX + 0x234]  ; mana total
```

The unique keypoint pattern is `Offsets.KeypointNames.LifeVitalCurrentTotal`.
It is anchored at the start of the formatter and the caret marks the displacement
of the first `MOV EDX, [RBX + disp32]` current-value read.

The integration test reads:

```text
+0x00 -> 0x1E0  health current absolute offset
+0x1D -> 0x1DC  health total absolute offset
+0x49 -> 0x278  energy shield current absolute offset
+0x66 -> 0x274  energy shield total absolute offset
+0x92 -> 0x238  mana current absolute offset
+0xAF -> 0x234  mana total absolute offset
```

Subtracting the vital starts gives the struct offsets:

```text
0x1E0 - 0x1A8 = 0x38  Health.Current
0x1DC - 0x1A8 = 0x34  Health.Total
0x278 - 0x240 = 0x38  EnergyShield.Current
0x274 - 0x240 = 0x34  EnergyShield.Total
0x238 - 0x200 = 0x38  Mana.Current
0x234 - 0x200 = 0x34  Mana.Total
```

## Runtime Confirmation

Explicit integration test:

```text
ShouldResolveLifeVitalsFromCodePatterns: passed
lifeComponent       = 0x27B508C8430
healthOffset        = 0x1A8
manaOffset          = 0x200
energyShieldOffset  = 0x240
vitalReservedFlatOffset    = 0x18
vitalReservedPercentOffset = 0x1C
vitalTotalOffset    = 0x34
vitalCurrentOffset  = 0x38
hp                  = 1427/1427
mp                  = 349/349
es                  = 115/115
```

The test proves:

- `Life` is resolved from the pattern-backed entity component lookup chain;
- recovered `Health`, `Mana`, and `EnergyShield` offsets match `LifeOffset`;
- recovered `ReservedFlat` and `ReservedPercent` offsets match `VitalStruct`;
- recovered `Current` and `Total` offsets match `VitalStruct`;
- raw reads at `lifeComponent + recovered offsets` match the managed object model;
- current values are within `[0, Total]`;
- the current live safe-state snapshot had full health, mana, and energy shield.

Constructor-shape integration test:

```text
ShouldResolveLifeVitalConstructorShapeFromCodePattern: passed
lifeComponent           = 0x27B508C8430
healthOffset            = 0x1A8
manaOffset              = 0x200
energyShieldOffset      = 0x240
lifeComponentPtrOffset  = 0x10
unknownStatId0Offset    = 0x08
unknownStatId1Offset    = 0x0C
totalStatIdOffset       = 0x20
unknownStatId2Offset    = 0x24
unknownStatId3Offset    = 0x28
hpUnknownStatId0        = 0x3334
hpUnknownStatId1        = 0x333B
hpUnknownStatId3        = 0x759
mpUnknownStatId0        = 0x6986
mpUnknownStatId1        = 0x6986
mpUnknownStatId3        = 0x1C9C
esUnknownStatId0        = 0x6986
esUnknownStatId1        = 0x6986
esUnknownStatId3        = 0x4DE5
```

The test proves:

- the Life constructor keypoint recovers `Health`, `Mana`, and `EnergyShield`
  starts;
- `VitalStruct.UnknownStatId0` is `+0x08` and live Health/Mana/ES values match
  constructor-written dword ids;
- `VitalStruct.UnknownStatId1` is `+0x0C` and live Health/Mana/ES values match
  constructor-written dword ids;
- `VitalStruct.LifeComponentPtr` is `+0x10` and points back to the live Life
  component for all three vitals;
- `VitalStruct.UnknownStatId3` is `+0x28`;
- live `Health/Mana/EnergyShield +0x28` values match constructor-written dword
  ids.

## Test

`ShouldResolveLifeVitalsFromCodePatterns`:

- walks `GameStates -> InGameState -> AreaInstance -> LocalPlayers`;
- resolves the player `Life` component through `EntityComponentLookupShape`;
- derives Life vital starts from `LifeComponentVitalOffsets`;
- derives `VitalStruct.ReservedFlat` and `VitalStruct.ReservedPercent` from
  `LifeVitalReservationOffsets`;
- derives `VitalStruct.Current` and `VitalStruct.Total` from
  `LifeVitalCurrentTotal`;
- validates raw memory values against `LifeOffset`, `VitalStruct`, and
  `TheGame.Player.TryGetComponent<Life>()`.

`ShouldResolveLifeVitalConstructorShapeFromCodePattern`:

- walks `GameStates -> InGameState -> AreaInstance -> LocalPlayers`;
- resolves the player `Life` component through `EntityComponentLookupShape`;
- derives vital starts and constructor-written stat-id fields from
  `LifeVitalConstructorShape` and `LifeVitalSharedConstructorShape`;
- validates live vital owner pointers and constructor-written dword values
  against `LifeOffset`.
