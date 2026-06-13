# Current State Vector Evidence

Build: `sha256-c5da3833`
Binary: `PathOfExileSteam.exe`
Related reconstruction: `GameStateOffset.CurrentStatePtr`
Confidence: `confirmed`

## Conclusion

`GameStateOffset.CurrentStatePtr` is the source current-state vector at
`GameStates + 0x08`. The active dispatch vector is a separate vector at
`GameStates + 0x20`, refreshed by `FUN_1415B0360` when the dirty flag at
`GameStates + 0x40` is set.

This matches the managed model:

```csharp
[FieldOffset(0x08)] public StdVector CurrentStatePtr;
```

and the current-state read is now centralized in `RuntimeGameOffsets`:

```csharp
RuntimeGameOffsets.ReadCurrentStateEntry(memory, gameStateOwner)
```

## Static Evidence

`FUN_1415B0360` resolves the GameStates owner through `FUN_14010C350`, then
uses the owner in `R15`.

Relevant disassembly:

```text
1415B03D9  CMP byte ptr [R15 + 0x40], 0x0
1415B03DE  JZ  0x1415B04E0
1415B03E4  MOV RDI, qword ptr [R15 + 0x10]
1415B03E8  SUB RDI, qword ptr [R15 + 0x08]
1415B03EC  SAR RDI, 0x4
1415B03F0  MOV RBX, qword ptr [R15 + 0x28]
1415B03F4  MOV RDX, qword ptr [R15 + 0x20]
```

Interpretation:

- `R15 + 0x08` is source vector `First`.
- `R15 + 0x10` is source vector `Last`.
- `(Last - First) >> 4` proves each entry is `0x10` bytes.
- `R15 + 0x20` is dispatch vector `First`.
- `R15 + 0x28` is dispatch vector `Last`.
- `R15 + 0x30` is dispatch vector `End`, used for capacity checks before
  reallocation.
- `R15 + 0x40` is the dirty flag.

This is also one of the static anchors for the shared `StdVector` header shape:
first, last, and end are consecutive pointer-sized fields at `+0x00`, `+0x08`,
and `+0x10` relative to a vector object. See `StdVector.evidence.md`.

Representative callers around `FUN_1415B2890`, `FUN_1415B2B00`, and
`FUN_1415B2C70` iterate `param_1 +0x20..+0x28` as the dispatch vector, then
call `FUN_1415B0360` to refresh it from the source vector.

## Stable Byte Keypoint

```text
41 80 7F 40 00
0F 84 ?? ?? ?? ??
49 8B 7F 10
49 2B 7F ^ 08
48 C1 FF 04
49 8B 5F 28
49 8B 57 20
48 8B CB
48 2B CA
48 C1 F9 04
48 3B F9
```

Ghidra search confirmed this pattern is unique at `0x1415B03D9`.

The caret lands on the one-byte displacement in:

```text
SUB RDI, qword ptr [R15 + 0x08]
```

Runtime extraction:

```text
currentVectorFirstOffset  = read<byte>(returned)
currentVectorLastOffset   = read<byte>(returned - 0x04)
dispatchVectorLastOffset  = read<byte>(returned + 0x08)
dispatchVectorFirstOffset = read<byte>(returned + 0x0C)
dispatchVectorEndOffset   = dispatchVectorLastOffset + pointer size
dirtyFlagOffset           = read<byte>(returned - 0x0F)
```

## Runtime Proof

`ShouldResolveCurrentStateVectorFromDirtyCopyPattern` passed against the live
client:

```text
PathOfExileSteam PID       = 5592
GameStates owner           = 0x0000027ABEDE39C0
currentVectorFirstOffset   = 0x08
currentVectorLastOffset    = 0x10
dispatchVectorFirstOffset  = 0x20
dispatchVectorLastOffset   = 0x28
dispatchVectorEndOffset    = 0x30
dirtyFlagOffset            = 0x40
currentVector              = 0x27B23168320..0x27B23168330
dispatchVector             = 0x27B231696D0..0x27B231696E0
currentState               = 0x0000027ABEE62F10
inGameState                = 0x0000027ABEE62F10
```

The test verifies:

- recovered `CurrentStatePtr` offset equals the managed `FieldOffset`;
- the recovered vector bytes equal `GameStateOffset.CurrentStatePtr`;
- `RuntimeGameOffsets.ReadCurrentStateEntry` reads the current entry using the
  recovered source vector and recovered 16-byte entry size;
- the provider-read current entry is the table's InGame state entry while the
  client is loaded in-world;
- the dispatch vector and dirty flag offsets are consistent with the static
  copy function shape.

## Recheck

- Ghidra MCP: decompile `FUN_1415B0360`.
- Ghidra MCP: confirm the stable byte keypoint still matches exactly once.
- Runtime:

```powershell
dotnet test .\CheatCartridge.Tests\CheatCartridge.Tests.csproj --filter "FullyQualifiedName=CheatCartridge.Tests.Integration.LocalProcessClientIntegrationTests.ShouldResolveCurrentStateVectorFromDirtyCopyPattern" --logger "console;verbosity=normal"
```
