# AreaInstance Local Players

Build: `sha256-c5da3833`
Binary: `PathOfExileSteam.exe`
Intent: recover `AreaInstanceOffsets.LocalPlayers`
Status: `pattern-backed`
Confidence: `confirmed`

## Current Conclusion

`AreaInstanceOffsets.LocalPlayers` is a `StdVector<IntPtr>` at `AreaInstance + 0x588`.

The current keypoint is not a broad `0x588` displacement hit. It comes from the live AreaInstance vtable. The live vtable pointer read from the current AreaInstance was `0x7FF7741F9C48`, which maps to copied-binary static VA `0x1432C9C48` / RVA `0x32C9C48`.

Vtable slots `0x140` and `0x148` both point to `FUN_1420731D0`. That function copies non-null qword entries from `AreaInstance + 0x588..0x590` into a caller-provided output vector.

## Static Evidence

Ghidra function:

```text
FUN_1420731D0
```

Relevant decompile:

```c
count = (*(longlong *)(param_1 + 0x590) -
         *(longlong *)(param_1 + 0x588)) >> 3;

for (entry = *(ulonglong **)(param_1 + 0x588);
     entry != *(ulonglong **)(param_1 + 0x590);
     entry = entry + 1)
{
    value = *entry;
    if (value != 0)
        output.push_back(value);
}
```

Relevant bytes and addresses:

```text
142073235  48 8B B7 90 05 00 00     MOV RSI, [RDI + 0x590]
14207323C  48 8B BF 88 05 00 00     MOV RDI, [RDI + 0x588]
142073243  48 3B FE                 CMP RDI, RSI
142073250  48 8B 07                 MOV RAX, [RDI]
142073253  48 85 C0                 TEST RAX, RAX
142073266  48 89 02                 MOV [RDX], RAX
```

Pattern added to `Offsets.KeypointPatterns`:

```text
48 89 74 24 38
48 8B B7 90 05 00 00
48 8B BF ^ ?? ?? ?? ??
48 3B FE 74 ??
0F 1F 80 00 00 00 00
48 8B 07
48 85 C0 74 ??
48 8B 53 08
```

Caret semantics:

```text
returned RVA -> displacement operand of MOV RDI, [RDI + disp32]
read int32   -> 0x588
```

This returns the field offset, not the final vector address. The live vector address is:

```text
localPlayersVectorAddress = areaInstanceAddress + 0x588
```

The vector itself uses the shared `StdVector` header shape documented in
`StdVector.evidence.md`. This function proves `AreaInstance +0x588` is the
vector `First` and `AreaInstance +0x590` is `Last`; the provider treats
`+0x598` as the adjacent `End` slot and validates `End >= Last` before reading
entries.

## Related AreaInstance Callers

`FUN_141640560` is an AreaInstance-family vtable method that checks the same vector shape using qword indexes:

```c
if (param_1[0xb2] - param_1[0xb1] >> 3 == 0) ...
```

`0xB1 * 8 == 0x588` and `0xB2 * 8 == 0x590`. The same function falls back to virtual slot `0x140` when it needs the current controlled/local entity list.

`FUN_141636C00` also calls virtual slot `0x140` and treats an empty returned list plus no explicit current entity as "not ready".

## Live Test

Test:

```text
CheatCartridge.Tests.Integration.LocalProcessClientIntegrationTests.ShouldResolveAreaInstanceLocalPlayersFromCodePattern
```

Result from the 2026-06-13 live client:

```text
inGameState=0x27ABEE62F10
area=0x27A8D887000
localPlayersOffset=0x588
first=0x27A8D8875A0
last=0x27A8D8875A8
end=0x27A8D8875B0
count=1
player=0x27A939CB980
```

Assertions:

- recovered offset equals `Marshal.OffsetOf<AreaInstanceOffsets>(LocalPlayers)`;
- recovered vector `First/Last/End` equals the current managed struct field;
- vector byte length is qword-aligned;
- provider-derived count/capacity are valid and capacity is at least count;
- recovered vector has at least one entry;
- first recovered entity pointer equals `TheGame.Player.Address`.

## Patch Guidance

If this breaks after a client patch:

1. Re-read the current AreaInstance vtable from the live object.
2. Map that vtable pointer back to the copied binary RVA.
3. Locate the vtable method that appends non-null entity pointers from an owned vector into a caller-provided output vector.
4. Re-anchor on the loop setup that loads `Last`, loads `First`, compares them, reads qword entries, filters zeroes, and appends non-zero pointers.
5. Do not use broad `0x588` searches as evidence unless the containing function is first proven to operate on the current AreaInstance object.
