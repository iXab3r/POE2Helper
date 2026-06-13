# StdVector Header Shape

Build: `sha256-c5da3833`
Binary: `PathOfExileSteam.exe`
Slice: `game-states`
Status: `confirmed`

## Finding

The game uses the standard three-pointer vector header shape:

```text
+0x00 -> First
+0x08 -> Last
+0x10 -> End / capacity end
```

`RuntimeGameOffsets` derives this once from code shape and then uses it for
current-state entries, AreaInstance local players, entity component vectors,
and component-name lookup buckets.

## Static Evidence

`FUN_1415B0360` proves two adjacent vector headers inside the GameStates owner.
The source/current-state vector starts at owner `+0x08`; the dispatch vector
starts at owner `+0x20`.

```text
1415B03E4  MOV RDI, qword ptr [R15 + 0x10]
1415B03E8  SUB RDI, qword ptr [R15 + 0x08]
1415B03EC  SAR RDI, 0x4
1415B03F0  MOV RBX, qword ptr [R15 + 0x28]
1415B03F4  MOV RDX, qword ptr [R15 + 0x20]
1415B03FB  SUB RCX, RDX
1415B03FE  SAR RCX, 0x4
1415B0421  MOV RAX, qword ptr [R15 + 0x30]
1415B0425  SUB RAX, RDX
1415B0428  SAR RAX, 0x4
1415B0434  LEA RCX, [R15 + 0x20]
```

Interpretation:

- source vector first/last are owner `+0x08/+0x10`;
- dispatch vector first/last/end are owner `+0x20/+0x28/+0x30`;
- both vectors use `Last - First` for count;
- `End - First` is used for capacity before reallocation;
- the current-state entry stride is `0x10`, independent from the vector header
  shape.

`FUN_1420731D0` proves the same first/last convention for the AreaInstance
local-player vector:

```text
count = (*(longlong *)(AreaInstance + 0x590) -
         *(longlong *)(AreaInstance + 0x588)) >> 3;

for (entry = *(ulonglong **)(AreaInstance + 0x588);
     entry != *(ulonglong **)(AreaInstance + 0x590);
     entry = entry + 1)
```

The output argument to this same function is also a vector-like header:
`param_2[0]` is first, `param_2[1]` is last, and `param_2[2]` is end/capacity.

`FUN_140163120` proves the component lookup bucket also starts with first/last:

```text
lVar1 = *param_1;
if (lVar1 == param_1[1]) {
  *param_2 = param_1[1];
  return param_2;
}
```

The helper uses additional bucket metadata for hashing, but the required entry
enumeration path is the vector span `First..Last` plus the independently
recovered entry size.

## Runtime Provider

`RuntimeGameOffsets.Resolve` recovers:

```text
StdVector.First = 0x00
StdVector.Last  = recoveredCurrentStateLast - recoveredCurrentStateFirst = 0x08
StdVector.End   = StdVector.Last + pointer size = 0x10
```

The provider now validates vector headers before enumeration:

- `First != 0`;
- `Last >= First`;
- `End >= Last`;
- both `Last - First` and `End - First` are aligned to the element size.

Malformed vectors produce an empty result rather than driving arbitrary reads.

## Runtime Proof

`ShouldReadRootObjectModelThroughRuntimeGameOffsets` verifies the provider
shape against the live current-state vector and AreaInstance local-player
vector:

```text
client PID          = 5592
currentStateVector  = 1 / 3
localPlayersVector  = 1 / 2
```

`ShouldReadEntityComponentsThroughRuntimeGameOffsets` verifies the same header
shape against the live component-name lookup bucket and entity component vector:

```text
componentBucket     = 12 decoded entries / 13 capacity
componentVector     = 12 component pointers / 12 capacity
```

These tests prove the shared vector header is not just a managed struct
assumption; it is consumed through the static provider and agrees with multiple
live vectors owned by different game objects.
