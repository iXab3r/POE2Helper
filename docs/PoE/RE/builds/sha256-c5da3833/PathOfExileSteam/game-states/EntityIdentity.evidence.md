# Entity Identity And Status

Build: `sha256-c5da3833`
Binary: `PathOfExileSteam.exe`
Slice: `game-states`
Status: `confirmed`

## Finding

`EntityOffsets.Id` and the entity status byte are recoverable from the
AreaInstance entity-tree filter in `FUN_14163B7A0`.

The keypoint starts from the already-confirmed AreaInstance entity tree root,
reads the entity pointer from each tree node, and then filters entity fields:

```text
14163B9EB  MOV RBX, qword ptr [RSI + 0x6C0]
14163B9F2  MOV RBX, qword ptr [RAX]
14163B9F5  CMP RBX, RAX
14163BA00  MOV RDI, qword ptr [RBX + 0x28]
14163BA04  TEST byte ptr [RDI + 0x8C], 0x1
14163BA0D  CMP dword ptr [RDI + 0x88], 0x40000000
14163BA19  TEST byte ptr [RDI + 0x8D], 0x4
```

Recovered values:

```text
AreaInstance entity tree root slot = 0x6C0
tree node entity pointer           = 0x28
EntityOffsets.Id                   = 0x88
EntityOffsets.IsValid/status       = 0x8C
adjacent active flag byte          = 0x8D
status invalid mask                = 0x01
id upper bound                     = 0x40000000
required active flag mask          = 0x04
```

The object in `RDI` is an entity because it is loaded from the payload of a node
in the AreaInstance entity tree. This same tree is initialized by
`FUN_14206E780` and cleaned by `FUN_141DC14B0(param_1 + 0xD6)`.

## Pattern

The managed keypoint is `Offsets.KeypointNames.EntityIdentityFilter`.

```text
48 8B 86 C0 06 00 00
48 8B 18
48 3B D8
0F 84 ?? ?? ?? ??
66 90
48 8B 7B 28
F6 87 ^ ?? ?? ?? ?? 01
75 ??
81 BF ?? ?? ?? ?? 00 00 00 40
73 ??
F6 87 ?? ?? ?? ?? 04
```

The caret lands on the `disp32` operand of:

```text
TEST byte ptr [RDI + 0x8C], 1
```

Runtime extraction from the returned operand address:

```text
+0x00 -> Entity status byte displacement             = 0x8C
+0x04 -> status invalid mask                         = 0x01
+0x09 -> Entity id dword displacement                = 0x88
+0x0D -> id upper bound                              = 0x40000000
+0x15 -> adjacent active flag byte displacement      = 0x8D
+0x19 -> required active flag mask                   = 0x04
-0x18 -> AreaInstance entity tree root displacement  = 0x6C0
-0x03 -> tree node entity pointer displacement       = 0x28
```

The pattern is unique in the copied binary at `0x14163B9EB`.

## Runtime Confirmation

`ShouldResolveEntityIdentityFromAreaInstanceTreeFilter` passed against the live
client on 2026-06-13:

```text
PathOfExileSteam PID = 5592
entity               = 0x0000027A939CB980
treeRootOffset       = 0x6C0
nodeEntityPtrOffset  = 0x28
idOffset             = 0x88
statusOffset         = 0x8C
statusMask           = 0x01
idUpperBound         = 0x40000000
activeFlagOffset     = 0x8D
activeMask           = 0x04
id                   = 2858
status               = 0x0C
activeFlags          = 0x0C
objectModelId        = 2858
objectModelIsValid   = True
```

The test verifies:

- recovered `idOffset` equals `Marshal.OffsetOf<EntityOffsets>(Id)`;
- recovered `statusOffset` equals `Marshal.OffsetOf<EntityOffsets>(IsValid)`;
- recovered id/status raw reads match `EntityOffsets` and the managed object model;
- the live player status byte is currently `0x0C`;
- the filter operands from game code are recovered and hold for the live player:
  `(status & 1) == 0`, `id < 0x40000000`, and `(activeFlags & 4) != 0`.

## Nuance

This keypoint proves field ownership, the status-byte offset, and the game-side
filter predicate. It does not fully recover the enum semantics of every status
byte value. The live player currently observes status `0x0C`, but production now
uses the recovered predicate rather than an exact status equality.

## Test

`ShouldResolveEntityIdentityFromAreaInstanceTreeFilter`:

- resolves `GameStates`, `GameStateTableShape`, `InGameStateAreaInstanceData`,
  `AreaInstanceLocalPlayers`, `AreaInstanceEntityTreeRoot`, and
  `EntityIdentityFilter`;
- starts from the local player entity recovered through the pattern-backed
  local-player vector;
- reads `Id`, status, the adjacent active flag, and the predicate masks/bounds
  through recovered operands;
- compares the raw values with `EntityOffsets` and verifies `TheGame.Player`
  uses the recovered predicate.
