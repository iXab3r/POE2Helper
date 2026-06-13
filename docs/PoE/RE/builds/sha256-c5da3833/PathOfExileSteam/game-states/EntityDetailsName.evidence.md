# EntityDetails Name

Build: `sha256-c5da3833`
Binary: `PathOfExileSteam.exe`
Slice: `game-states`
Status: `confirmed`

## Finding

`EntityDetails.name` at `+0x08` is recoverable from a live Entity vtable
accessor. The accessor returns the address of the embedded metadata path
`StdWString`:

```text
141C7A6D0  MOV RAX, qword ptr [RCX + 0x08]
141C7A6D4  ADD RAX, 0x08
141C7A6D8  RET
```

Recovered values:

```text
ItemStruct.EntityDetailsPtr = 0x08
EntityDetails.name          = 0x08
```

The live player entity vtable supported the ownership chain:

```text
player entity runtime vtable = 0x7FF77432E2F8
copied-binary vtable VA      = 0x1433FE2F8
vtable slot +0x10            = 0x141C7A6D0
```

The decompiled shape is effectively:

```text
return *(Entity + 0x08) + 0x08;
```

That is exactly the managed object model's:

```text
Entity -> EntityDetails -> EntityDetails.name
```

## Pattern

The managed keypoint is `Offsets.KeypointNames.EntityDetailsName`.

```text
48 8B 41 ?? 
48 83 C0 ^ ??
C3
CC CC CC CC CC CC CC
48 89 5C 24 08
48 89 7C 24 18
55
48 8D 6C 24 A0
48 81 EC 60 01 00 00
```

The caret lands on the one-byte immediate in:

```text
ADD RAX, 0x08
```

Runtime extraction from the returned operand address:

```text
+0x00 -> EntityDetails.name offset          = 0x08
-0x04 -> Entity.EntityDetailsPtr offset     = 0x08
```

The tiny accessor shape alone is not unique, so the pattern includes the next
function boundary/prologue. With that context, it is unique in the copied binary
at `0x141C7A6D0`.

## Runtime Confirmation

`ShouldResolveEntityDetailsNameFromVtableAccessor` passed against the live client
on 2026-06-13:

```text
PathOfExileSteam PID = 5592
entity               = 0x0000027A939CB980
detailsPtrOffset     = 0x08
details              = 0x0000027A8E012110
nameOffset           = 0x08
path                 = Metadata/Characters/StrDex/StrDexFourb
length               = 38
capacity             = 39
objectModelPath      = Metadata/Characters/StrDex/StrDexFourb
```

The test verifies:

- recovered `EntityDetailsPtr` equals `Marshal.OffsetOf<ItemStruct>(EntityDetailsPtr)`;
- recovered `EntityDetails.name` equals `Marshal.OffsetOf<EntityDetails>(name)`;
- the raw `StdWString` starts with `Metadata/`;
- `RuntimeGameOffsets.ReadStdWString` returns the same metadata path through
  the provider-derived `StdWString` layout;
- the raw path equals both the struct comparator and `TheGame.Player.Path`.

## Nuance

This keypoint proves the field offset and the entity-to-details owner transition.
The shared `StdWString` internals come from `PlayerName.evidence.md`: buffer
`+0x00`, inline buffer `+0x00`, length `+0x10`, capacity `+0x18`, and SSO limit
`7`. Production path reading uses those provider-derived fields for both
`Player.Name` and `EntityDetails.name`.

## Test

`ShouldResolveEntityDetailsNameFromVtableAccessor`:

- resolves `GameStates`, `GameStateTableShape`, `InGameStateAreaInstanceData`,
  `AreaInstanceLocalPlayers`, and `EntityDetailsName`;
- starts from the pattern-backed local player entity;
- reads `EntityDetails + recoveredNameOffset` through
  `RuntimeGameOffsets.ReadStdWString`;
- compares the raw metadata path with `TheGame.Player.Path`.
