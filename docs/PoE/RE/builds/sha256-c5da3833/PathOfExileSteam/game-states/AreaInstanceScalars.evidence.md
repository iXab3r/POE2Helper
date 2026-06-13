# AreaInstance Scalars

Build: `sha256-c5da3833`
Binary: `PathOfExileSteam.exe`
Slice: `game-states`
Status: `confirmed`

## CurrentAreaLevel

`AreaInstance + 0x0C4` is the current area monster level byte.

The durable keypoint is a short block that first loads the current
`AreaInstance` pointer from the already-proven `InGameState.AreaInstanceData`
slot, then reads the level byte from that area object:

```text
140FB33F0  MOV RAX, qword ptr [R12 + 0x290]
140FB33F8  MOVZX ECX, byte ptr [RAX + 0xC4]
140FB33FF  MOV dword ptr [0x14434BAC8], ECX
```

The matching pattern is unique in the copied binary even when the
`InGameState.AreaInstanceData` displacement is wildcarded:

```text
49 8B 84 24 ?? ?? ?? ??
0F B6 88 ^ ?? ?? ?? ??
89 0D ?? ?? ?? ??
```

The caret marks the `disp32` operand of:

```text
MOVZX ECX, byte ptr [RAX + disp32]
```

Reading that operand returns `0x0C4`.

The preceding `MOV RAX, [R12 + disp32]` displacement starts seven bytes before
the caret. The integration test reads it too and requires it to equal the
independently recovered `InGameState.AreaInstanceData` offset:

```text
areaInstanceDataOffset = *(int32 *)(caret - 7) == 0x290
currentAreaLevelOffset = *(int32 *)(caret)     == 0x0C4
```

This is important because it proves the owner transition. The second
instruction reads a byte from the object loaded through the current
`InGameState` area pointer, not from an unrelated object that happens to have a
`+0xC4` byte.

## Related Current-Area Holder Evidence

There are other level reads through a larger current-area holder at
`holder + 0x2170`:

```text
14205FB0E  MOVZX EAX, byte ptr [RCX + 0xC4]
142062671  MOVZX EDX, byte ptr [RCX + 0xC4]
```

Those are useful orientation, but they are not the primary keypoint because the
`0x2170` holder still needs its own ownership model. The `0x140FB33F0` block is
stronger for the CheatCartridge object model because it starts from the
already-modeled `InGameState`.

## CurrentAreaHash

`AreaInstance + 0x11C` is the current area hash dword.

The durable keypoint is a current-area holder read in `FUN_142061AD0`:

```text
1420607BB  MOV RCX, qword ptr [RDI + 0x2170]
1420607C2  MOV EAX, dword ptr [RCX + 0x11C]
1420607C8  MOV dword ptr [RSP + 0x58], EAX
1420607CC  MOV EAX, dword ptr [RCX + 0x120]
1420607D2  MOV dword ptr [RSP + 0x5C], EAX
1420607D6  LEA RDX, [RSP + 0x40]
1420607DB  MOV RCX, RDI
1420607DE  CALL FUN_14205F360
```

The matching pattern is unique in the copied binary:

```text
48 8B 8F ?? ?? ?? ??
8B 81 ^ ?? ?? ?? ??
89 44 24 ??
8B 81 ?? ?? ?? ??
89 44 24 ??
48 8D 54 24 ??
48 8B CF
E8 ?? ?? ?? ??
```

The caret marks the `disp32` operand of:

```text
MOV EAX, dword ptr [RCX + disp32]
```

Reading that operand returns `0x011C`.

The preceding `MOV RCX, [RDI + disp32]` holder displacement starts six bytes
before the caret. Reading it returns `0x2170`.

### Holder Ownership

This is not a broad `+0x11C` search. The current-area holder constructor
`FUN_14205D6D0` links the holder to the same AreaInstance object family used by
the object model:

```text
holder + 0x2170      = AreaInstance
AreaInstance + 0x580 = holder
```

Relevant decompile from `FUN_14205D6D0`:

```text
param_1[0x42E] = param_2;
...
param_2[0xB0] = (longlong)param_1;
```

`0x42E * 8 = 0x2170`, and `0xB0 * 8 = 0x580`.
`AreaInstance + 0x580` is immediately before the already-proven
`AreaInstance.LocalPlayers` vector at `+0x588`, which ties this holder to the
same current area object.

The packet/update handler also contains the unique string
`"Server sent area hash for area that doesn't exist"`. That string remains
useful orientation for the broader area-load flow, but the promoted hash
keypoint is the compact current-area holder read above.

## EntitiesCount

`AreaInstance + 0x6C8` is the count field for the first AreaInstance-owned
entity tree.

The durable keypoint is the base AreaInstance constructor, not a broad direct
`+0x6C8` search. `FUN_14206E780` initializes the relevant container:

```text
14206EBD2  MOV qword ptr [RSI + 0x6B0], RAX
14206EBD9  MOV qword ptr [RSI + 0x6B8], RBP
14206EBE0  LEA RBX, [RSI + 0x6C0]
14206EBE7  MOV qword ptr [RSP + 0x68], RBX
14206EBEC  MOV qword ptr [RBX], RBP
14206EBEF  MOV qword ptr [RBX + 0x8], RBP
14206EBF3  MOV ECX, 0x30
14206EBF8  CALL FUN_140155B20
14206EBFD  MOV qword ptr [RAX], RAX
14206EC00  MOV qword ptr [RAX + 0x8], RAX
14206EC04  MOV qword ptr [RAX + 0x10], RAX
14206EC08  MOV word ptr [RAX + 0x18], 0x101
14206EC0E  MOV qword ptr [RBX], RAX
```

The matching tightened pattern is unique in the copied binary:

```text
48 8D 05 ?? ?? ?? ??
48 89 86 B0 06 00 00
48 89 AE B8 06 00 00
48 8D 9E ^ ?? ?? ?? ??
48 89 5C 24 ??
48 89 2B
48 89 6B 08
B9 30 00 00 00
E8 ?? ?? ?? ??
48 89 00
48 89 40 08
48 89 40 10
66 C7 40 18 01 01
48 89 03
```

The caret marks the displacement operand of:

```text
LEA RBX, [RSI + disp32]
```

Reading that operand returns `0x6C0`, the entity-tree root/sentinel pointer
slot. The entity count is derived from the adjacent qword that the constructor
clears through `MOV qword ptr [RBX + 0x8], RBP`:

```text
entityTreeRootOffset = 0x6C0
entitiesCountOffset  = entityTreeRootOffset + sizeof(void*) = 0x6C8
```

The decompiler shows the same structure explicitly:

```text
param_1[0xD6] = &PTR_FUN_1433FE1B8;  // +0x6B0
param_1[0xD7] = 0;                   // +0x6B8
param_1[0xD8] = 0;                   // +0x6C0
param_1[0xD9] = 0;                   // +0x6C8
...
param_1[0xD8] = sentinel;
```

Ownership is tied back to `AreaInstance` by two independent paths:

- `FUN_14163CB80` calls `FUN_14206E780` first, then installs the AreaInstance
  vtable `0x1432C9C48`.
- `FUN_14163B7A0` later iterates `param_1[0xD8]`, reads each node's entity
  pointer from node `+0x28`, filters entity fields including
  `+0x88/+0x8C/+0x8D/+0x98`, and cleans the same container with
  `FUN_141DC14B0(param_1 + 0xD6)`.

This rejects the earlier broad-search result as the promoted evidence. The
accepted keypoint finds the owning constructor and derives the count from the
container shape.

## Test

`ShouldResolveAreaInstanceCurrentAreaLevelFromCodePattern`:

- resolves `GameStates`, `GameStateTableShape`, and `InGameStateAreaInstanceData`;
- resolves `AreaInstanceCurrentAreaLevel`;
- reads the preceding owner displacement at `caret - 7`;
- verifies it equals the recovered `InGameState.AreaInstanceData` offset;
- reads `AreaInstance + recoveredLevelOffset`;
- verifies the byte equals both `AreaInstanceOffsets.CurrentAreaLevel` and the
  `TheGame` object model.

Runtime result on 2026-06-13:

```text
PathOfExileSteam PID              = 5592
InGameState                       = 0x0000027ABEE62F10
AreaInstanceData offset           = 0x290
AreaInstanceData offset from block = 0x290
AreaInstance                      = 0x0000027A8D887000
AreaInstance from block           = 0x0000027A8D887000
CurrentAreaLevel offset           = 0x0C4
CurrentAreaLevel                  = 38
```

`ShouldResolveAreaInstanceCurrentAreaHashFromCodePattern`:

- resolves `GameStates`, `GameStateTableShape`, and `InGameStateAreaInstanceData`;
- resolves `AreaInstanceCurrentAreaHash`;
- reads the current-area holder displacement at `caret - 6`;
- reads `AreaInstance + recoveredHashOffset`;
- verifies the dword equals both `AreaInstanceOffsets.CurrentAreaHash` and the
  `TheGame` object model.

Runtime result on 2026-06-13:

```text
PathOfExileSteam PID        = 5592
InGameState                 = 0x0000027ABEE62F10
AreaInstance                = 0x0000027A8D887000
Holder AreaInstance offset  = 0x2170
CurrentAreaHash offset      = 0x011C
Sibling hash-related offset = 0x0120
CurrentAreaHash             = 0x69A58CE3
```

`ShouldResolveAreaInstanceEntitiesCountFromCodePattern`:

- resolves `GameStates`, `GameStateTableShape`, and `InGameStateAreaInstanceData`;
- resolves `AreaInstanceEntityTreeRoot`;
- reads the tree-root slot offset from the constructor keypoint;
- derives `EntitiesCount` as `treeRootOffset + IntPtr.Size`;
- verifies the derived offset equals `AreaInstanceOffsets.EntitiesCount`;
- reads the raw count from live AreaInstance memory;
- verifies the raw count equals both `AreaInstanceOffsets.EntitiesCount` and the
  `TheGame` object model.

Runtime result on 2026-06-13:

```text
PathOfExileSteam PID     = 5592
InGameState              = 0x0000027ABEE62F10
AreaInstance             = 0x0000027A8D887000
EntityTreeRoot offset    = 0x6C0
EntityTreeRoot pointer   = 0x0000027B23166B80
EntitiesCount offset     = 0x6C8
EntitiesCount            = 105
```
