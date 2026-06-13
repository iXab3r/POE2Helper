# Player Component Name

Build: `sha256-c5da3833`
Binary: `PathOfExileSteam.exe`
Slice: `game-states`
Status: `confirmed`

## Finding

`PlayerOffsets.Name` is a `StdWString` embedded at `Player + 0x1B0`.

The durable keypoint is the Player component debug/info formatter reached from
the live Player component vtable, slot `0x68`:

```text
FUN_141D267F0
141D26865  LEA RDX, [0x1433F5AC0]      ; "Character Name: "
141D2686C  MOV RCX, RDI
141D2686F  LEA RBX, [R14 + 0x1B0]      ; Player.Name
141D26876  CALL FUN_14014FA40
141D2687B  CMP qword ptr [RBX + 0x18], 0x7
141D26880  MOV R8, qword ptr [RBX + 0x10]
141D26884  JBE 0x141D26889
141D26886  MOV RBX, qword ptr [RBX]
141D26889  MOV RDX, RBX
141D2688C  MOV RCX, RAX
141D2688F  CALL FUN_14015A460
```

The unique string anchor is:

```text
0x1433F5AC0  "Character Name: "
```

Ghidra decompile:

```text
plVar5 = (longlong *)(param_1 + 0x1b0);
uVar3 = FUN_14014fa40(param_2,L"Character Name: ");
if (7 < *(ulonglong *)(param_1 + 0x1c8)) {
    plVar5 = (longlong *)*plVar5;
}
uVar3 = FUN_14015a460(uVar3,plVar5,*(undefined8 *)(param_1 + 0x1c0));
FUN_140fc92a0(uVar3);
```

This proves:

```text
PlayerOffsets.Name       = 0x1B0
StdWString.Buffer        = 0x00
StdWString.InlineBuffer  = 0x00
StdWString.Length        = 0x10
StdWString.Capacity      = 0x18
StdWString.SSO limit     = 7
```

`0x1C0 - 0x1B0 = 0x10` and `0x1C8 - 0x1B0 = 0x18`.
The `Capacity > 7` branch is the normal small-string optimization check. When
capacity is greater than `7`, the code executes `MOV RBX, [RBX]`, proving the
external buffer pointer is stored at `StdWString +0x00`. Otherwise the same
`RBX` pointer is passed directly to the append call, proving the inline buffer
also starts at `StdWString +0x00`.

## Pattern

The matching pattern is unique in the copied binary:

```text
48 8D 15 ?? ?? ?? ??
48 8B CF
49 8D 9E ^ ?? ?? ?? ??
E8 ?? ?? ?? ??
48 83 7B 18 07
4C 8B 43 10
76 ??
48 8B 1B
48 8B D3
48 8B C8
E8 ?? ?? ?? ??
48 8B C8
E8
```

The caret marks the `disp32` operand of `LEA RBX, [R14 + disp32]`.
Reading that operand returns `0x1B0`.

The integration test also reads two byte operands inside the same compact
formatter keypoint:

```text
returnedRva + 0x0C -> StdWString.Capacity = 0x18
returnedRva + 0x0D -> small capacity limit = 7
returnedRva + 0x11 -> StdWString.Length   = 0x10
returnedRva + 0x14 -> MOV RBX, [RBX]       = external buffer at +0x00
```

These are pattern-internal offsets, not game structure offsets. They are valid
only for this formatter keypoint and are verified by the test.

## Runtime Confirmation

Explicit integration test on 2026-06-13:

```text
ShouldResolvePlayerNameFromCodePattern: passed
PathOfExileSteam PID = 5592
entity                = 0x0000027A939CB980
playerComponent       = 0x0000027A8D1DC030
nameOffset            = 0x1B0
wstringLengthOffset   = 0x10
wstringCapacityOffset = 0x18
wstringSmallLimit     = 7
name                  = Xaber
length                = 5
capacity              = 7
```

The test walks only pattern-backed anchors before reading the name:

```text
GameStates -> InGameState -> AreaInstance -> LocalPlayers -> Entity
Entity -> EntityDetails -> ComponentLookUp -> Player component
Player component + recovered name offset -> StdWString
```

It verifies:

- recovered `PlayerOffsets.Name` equals the managed `FieldOffset`;
- recovered `StdWString` buffer, inline buffer, length, capacity, and SSO limit
  match the formatter shape;
- the raw string read through the recovered offset is non-empty;
- the provider string reader returns the same value as the raw struct
  comparator;
- the recovered string equals `PlayerComponent.Name` from the object model.

## Notes

This is a component-internal field keypoint. It relies on the already-confirmed
generic entity component lookup chain to find the live Player component. It does
not attempt to prove unrelated Player fields such as `UnknownNumber1`.

If a future build removes or rewrites the formatter, search for the same
semantic shape:

1. a Player component function reached from the live Player vtable;
2. the unique `"Character Name: "` string or equivalent UI/debug label;
3. a `StdWString` small-string check using capacity `> 7`;
4. a string append call that takes the recovered buffer pointer and length.
