# Entity Component Lookup

Build: `sha256-c5da3833`
Binary: `PathOfExileSteam.exe`
Slice: `game-states`
Status: `confirmed`

## Finding

The generic named-component lookup path proves the core entity/component chain:

```text
Entity + 0x08  -> EntityDetails
Entity + 0x10  -> component pointer vector
EntityDetails + 0x28 -> ComponentLookUp
ComponentLookUp + 0x28 -> name/index bucket
ComponentLookUp + 0x30 -> bucket end/sentinel
ComponentNameAndIndex + 0x00 -> component name pointer
ComponentNameAndIndex + 0x08 -> component index
ComponentNameAndIndex entry size -> 0x10
```

The anchor is a repeated generic named-component resolver shape. `FUN_1401512E0`
is one concrete instantiation, and the build example resolves the `Animated`
component, but the important part is the shared lookup shape used by named
component resolution.

```text
1401512ED  MOV RCX, qword ptr [RCX + 0x08]
1401512FC  MOV RAX, qword ptr [RDI + 0x08]
140151309  MOV RBX, qword ptr [RAX + 0x28]
140151319  LEA RCX, [RBX + 0x28]
14015131D  CALL FUN_140163120
140151326  CMP RAX, qword ptr [RBX + 0x30]
14015132D  MOVSXD RCX, dword ptr [RAX + 0x08]
140151335  MOV RAX, qword ptr [RDI + 0x10]
140151339  MOV RAX, qword ptr [RAX + RCX*8]
```

Decompile shape:

```text
details = *(Entity + 0x08)
lookup = *(details + 0x28)
entry = find(lookup + 0x28, &componentName)
if (entry != *(lookup + 0x30) && *(int *)(entry + 0x08) != -1)
    return *(IntPtr *)(*(Entity + 0x10) + index * 8)
```

`FUN_140163120` is the bucket lookup helper. It compares the first qword of a
16-byte bucket entry against the requested component-name pointer and returns
the matching entry. `FUN_1401512E0` then reads the dword at `entry + 0x08` as
the component index.

Ghidra decompile of the current build shows the caller shape directly:

```text
undefined8 FUN_1401512e0(longlong param_1)
{
  if (*(longlong *)(param_1 + 8) != 0) {
    FUN_142111a60();
  }
  lookup = *(longlong *)(*(longlong *)(param_1 + 8) + 0x28);
  name = "Animated";
  entryOut = FUN_140163120(lookup + 0x28, local_res10, &name);
  if ((*entryOut != *(longlong *)(lookup + 0x30)) &&
      (index = *(int *)(*entryOut + 8), index != -1)) {
    return *(undefined8 *)(*(longlong *)(param_1 + 0x10) + index * 8);
  }
  return 0;
}
```

Ghidra decompile of `FUN_140163120` confirms that the helper treats its first
argument as a bucket whose first two qwords are the vector `First` and `Last`:

```text
lVar1 = *param_1;
if (lVar1 == param_1[1]) {
  *param_2 = param_1[1];
  return param_2;
}
...
plVar6 = (longlong *)((ulonglong)*(uint *)(lVar2 + 4 + uVar8 * 8) * 0x10 + lVar1);
if (*(longlong *)param_3 == *plVar6) ...
```

The same helper also reads capacity-like metadata from `param_1[4]`, but
CheatCartridge does not need that field to enumerate known component entries.
Production now bounds enumeration by the recovered vector span and recovered
entry size, so `StdBucket.Capacity` is intentionally not part of the required
offset surface.

The required bucket data is therefore treated as the shared `StdVector` header
shape from `StdVector.evidence.md`: first at bucket `+0x00`, last at bucket
`+0x08`, and end at bucket `+0x10`. Runtime enumeration validates that the
header is aligned to the recovered entry size and that capacity is at least
count before walking entries.

The entry-size anchor is also in `FUN_140163120`:

```text
1401631EF  MOV EDX, dword ptr [RSI + RCX*8 + 0x04]
1401631F3  SHL RDX, 0x04
1401631F7  ADD RDX, RDI
1401631FA  MOV RCX, qword ptr [RDX]
1401631FD  CMP qword ptr [RBX], RCX
```

`SHL RDX, 0x04` proves `entryIndex * 0x10`. The following
`MOV RCX, [RDX]` has no displacement, which proves the name pointer is at
`entry +0x00`.

## Pattern

The managed keypoint is `Offsets.KeypointNames.EntityComponentLookupShape`.
Unlike most other keypoints, this pattern is intentionally not unique in the
binary. Ghidra static search on build `sha256-c5da3833` finds 102 resolver
instantiations. This is acceptable only because the live consistency test proves
every match carries the same field operands.

```text
48 89 5C 24 18
57
48 83 EC 20
48 8B F9
48 8B 49 ^ 08
48 85 C9
74 ??
E8 ?? ?? ?? ??
48 8B 47 08
4C 8D 44 24 30
48 8D 54 24 38
48 8B 58 28
48 8D 05 ?? ?? ?? ??
48 89 44 24 30
48 8D 4B 28
E8 ?? ?? ?? ??
48 8B 00
48 3B 43 30
74 ??
48 63 48 08
83 F9 FF
74 ??
48 8B 47 10
48 8B 04 C8
```

The caret lands on the first `Entity + 0x08` displacement. The live integration
test reads the rest of the relevant operands at fixed positions inside this
compact semantic pattern:

```text
+0x00 -> EntityDetailsPtr, first read                         = 0x08
+0x0E -> EntityDetailsPtr, second read                        = 0x08
+0x1C -> EntityDetails.ComponentLookUpPtr                     = 0x28
+0x2C -> ComponentLookUpStruct.ComponentsNameAndIndex         = 0x28
+0x38 -> ComponentLookUpStruct bucket Data.Last sentinel      = 0x30
+0x3E -> ComponentNameAndIndexStruct.Index                    = 0x08
+0x47 -> ItemStruct.ComponentListPtr                          = 0x10
```

This is a field-displacement recovery pattern. It returns offsets, not final
runtime addresses. Runtime code still reads each pointer from the live entity.
When this pattern changes, do not tighten it by adding an unrelated tail just to
force one arbitrary resolver to be unique. Prefer keeping it as a semantic
generic-resolver shape and prove that all matches still agree on the layout.

The entry stride keypoint is
`Offsets.KeypointNames.ComponentNameAndIndexEntryStride`.

```text
3B 04 CE
?? ??
8B 54 CE 04
48 C1 E2 ^ 04
48 03 D7
48 8B 0A
48 39 0B
```

The caret lands on the shift amount. Runtime derives `entrySize = 1 << 4` and
validates the adjacent `MOV RCX, [RDX]` opcode before using the bucket entries.

## Runtime Confirmation

Explicit integration test after adding the keypoint:

```text
ShouldResolveEntityComponentLookupFromCodePattern: passed
entity              = 0x27A939CB980
detailsOffset       = 0x8
componentListOffset = 0x10
detailsLookupOffset = 0x28
lookupBucketOffset  = 0x28
lookupEndOffset     = 0x30
entryNameOffset     = 0x0
entryIndexOffset    = 0x8
entrySize           = 0x10
components          = 12
names               = 12
playerIndex         = 6
lifeIndex           = 4
playerComponent     = 0x27A8D1DC030
lifeComponent       = 0x27B508C8430
```

The test proves:

- the recovered player entity is the same address as `TheGame.Player.Address`;
- the lookup bucket contains `Player` and `Life`;
- both component indexes are in bounds for the component vector;
- the recovered component addresses match the existing object model;
- `ComponentHeader.EntityPtr` for both components points back to the player entity.
- production component enumeration no longer depends on reading
  `ComponentNameAndIndexStruct`; `RuntimeGameOffsets` walks the bucket using
  the recovered name offset, index offset, and entry size.
- production component enumeration no longer depends on `StdBucket.Capacity`;
  it uses the bucket `Data.First..Data.Last` vector span recovered from the
  lookup helper and a hard cap supplied by the caller.
- provider-backed vector validation proves the component-name bucket and entity
  component pointer vector both have valid first/last/end spans before use.

After the client-build rehearsal on 2026-06-13:

```text
ShouldResolveEntityComponentLookupShapeConsistentlyAcrossGenericResolvers: passed
genericResolverMatches = 102
distinctLayouts        = 1
detailsOffset          = 0x08
componentListOffset    = 0x10
detailsLookupOffset    = 0x28
lookupBucketOffset     = 0x28
lookupEndOffset        = 0x30
entryIndexOffset       = 0x08
```

This test scans the live client binary bytes for the resolver shape and asserts
that every match encodes the same recovered component lookup layout. It covers
the non-unique nature of this keypoint explicitly.

## Test

`ShouldResolveEntityComponentLookupFromCodePattern`:

- resolves `GameStates`, `GameStateTableShape`, `InGameStateAreaInstanceData`,
  and `AreaInstanceLocalPlayers`;
- uses those keypoints to find the live player entity;
- resolves `EntityComponentLookupShape`;
- resolves `ComponentNameAndIndexEntryStride`;
- walks the recovered lookup bucket and component vector;
- compares `Player` and `Life` component addresses against the current managed
  object model.

`ShouldResolveEntityComponentLookupShapeConsistentlyAcrossGenericResolvers`:

- scans the live client binary for every generic resolver-shaped byte sequence;
- verifies the pattern is repeated, not unique;
- verifies all matches produce one identical field layout;
- compares that layout with `RuntimeGameOffsets` and the managed comparator
  structs.
