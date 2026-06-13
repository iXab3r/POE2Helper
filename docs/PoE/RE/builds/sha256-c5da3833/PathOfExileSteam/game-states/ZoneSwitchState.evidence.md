# Zone Switch State Evidence

Build: `sha256-c5da3833`
Binary: `PathOfExileSteam.exe`
Related reconstruction: `none yet`
Confidence: `confirmed`

## Proposed Signatures

| Anchor | Proposed signature | Confidence | Argument evidence |
| --- | --- | --- | --- |
| Label-less block, VA `0x140FB3F60` | `InGameState.ZoneSwitchCounter` direct dword field | `confirmed` | The block compares `dword ptr [RAX + 0x56C]` with `1`; live address arithmetic confirms the direct field is `InGameState + 0x56C`. |
| `FUN_140FAC260`, VA `0x140FAC37A` | `InGameState.ZoneSwitchCounter` reset site | `confirmed` | Reset/lifetime path writes `dword ptr [RBX + 0x56C]` and clears adjacent byte flag `+0x568`. |
| `FUN_140FB24A0`, VA `0x140FB2831` and `0x140FB28E6` | `InGameState.ZoneSwitchCounter` increment sites | `confirmed` | The function has two branches that increment `dword ptr [R14 + 0x56C]` after updating `+0x4B8`. |
| `FUN_140FAF2F0`, VA `0x140FAF3E9` | `InGameState.ZoneSwitchState` pointer field read | `confirmed` | The same function first checks `[InGameState + 0x290]`, then reads `[InGameState + 0x368]` before checking the nested zone-switch byte. |
| `FUN_140FA9D00`, VA `0x140FAA2FF` and `0x140FAA306` | `InGameState.ZoneSwitchState` pointer lifetime assignment | `confirmed` | The constructor path allocates `0x578` bytes, calls `FUN_14012F740(newObject, InGameState, oldObject)`, reads the old object from `[InGameState + 0x368]`, then writes the new object back to `[InGameState + 0x368]`. |

## Stable Byte Keypoints

| Name | Pattern | Immediate recovered |
| --- | --- | --- |
| `InGameStateZoneSwitchCounter` | `48 8B 85 ?? ?? ?? ?? 83 B8 ^ ?? ?? ?? ?? 01 0F 85 ?? ?? ?? ?? 48 8B 98 ?? ?? ?? ?? 49 8B 85 ?? ?? ?? ??` | `0x56C` |
| `InGameStateZoneSwitchCounterReset` | `FF 15 ?? ?? ?? ?? 89 AB ^ 6C 05 00 00 C6 83 68 05 00 00 00 48 8D 8B 50 06 00 00 45 33 C0 33 D2 E8` | `0x56C`; adjacent flag offset is at `+0x06` and recovers `0x568`. |
| `InGameStateZoneSwitchCounterIncrementFirst` | `49 89 9E B8 04 00 00 41 FF 86 ^ 6C 05 00 00 48 8B 5D` | `0x56C` |
| `InGameStateZoneSwitchCounterIncrementSecond` | `4D 89 BE B8 04 00 00 41 FF 86 ^ 6C 05 00 00 49 8B BE D0 04 00 00` | `0x56C` |
| `InGameStateZoneSwitchState` | `49 8B 06 48 8B 98 ^ ?? ?? ?? ?? 80 BB ?? ?? ?? ?? 00 75 ?? 48 8B BB ?? ?? ?? ?? 48 8B 8F ?? ?? ?? ?? 48 85 C9` | `0x368` |
| `InGameStateZoneSwitchStateConstructor` | `B9 78 05 00 00 E8 ?? ?? ?? ?? 48 89 45 48 48 85 C0 74 ?? 4C 8B C3 49 8B D6 48 8B C8 E8 ?? ?? ?? ?? EB ?? 49 8B C7 49 8B 9E ^ ?? ?? ?? ?? 49 89 86 ?? ?? ?? ?? 48 85 DB` | `0x368` at the old-slot read; the new-slot write displacement is at `+7` and is also `0x368`. |

## Observations

- Exact byte search for `49 8B 06 48 8B 98 68 03 00 00 80 BB 6C 05 00 00 00 75 6F 48 8B BB 98 00 00 00` has one hit at VA `0x140FAF3E6`.
- `FUN_140FAF2F0` starts from `param_1[0]`. Ghidra assigns that value to `RSI`.
- At VA `0x140FAF31B`, the function checks `qword ptr [RSI + 0x290]`, matching the already-confirmed `InGameState.AreaInstanceData` offset.
- At VA `0x140FAF3E6`, the function reloads the same state object through `[R14]`.
- At VA `0x140FAF3E9`, it reads `RBX = [RAX + 0x368]`.
- At VA `0x140FAF3F0`, it checks `byte ptr [RBX + 0x56C]` against zero.
- The code then reads `[RBX + 0x98]` and `[RDI + 0xA0]`, and calls `FUN_1423AABC0` on offsets under that object before optionally calling `FUN_140133210`.
- Exact byte search for `48 8B 85 F0 09 00 00 83 B8 6C 05 00 00 01 0F 85 3F 01 00 00 48 8B 98 60 03 00 00` has one hit at VA `0x140FB3F59`.
- The block around VA `0x140FB3F60` is not currently assigned a Ghidra function name, but it has parsed instructions:
  - `140FB3F59  MOV RAX, qword ptr [RBP + 0x9F0]`
  - `140FB3F60  CMP dword ptr [RAX + 0x56C], 0x1`
  - `140FB3F67  JNZ 0x140FB40AC`
  - `140FB3F6D  MOV RBX, qword ptr [RAX + 0x360]`
- Nearby in the same analyzed region, VA `0x140FB4644` reads `RDX = [R15 + 0x368]`, matching the nested zone-switch owner offset seen in `FUN_140FAF2F0`.
- `FUN_140FA9D00` constructs the same object family and initializes `*(undefined8 *)(param_1 + 0x56C) = 0`.
- The same constructor/lifetime function installs the nested transition object:
  - `140FAA2D9  MOV ECX, 0x578`
  - `140FAA2DE  CALL 0x142AE8350`
  - `140FAA2EC  MOV R8, RBX`
  - `140FAA2EF  MOV RDX, R14`
  - `140FAA2F2  MOV RCX, RAX`
  - `140FAA2F5  CALL FUN_14012F740`
  - `140FAA2FF  MOV RBX, qword ptr [R14 + 0x368]`
  - `140FAA306  MOV qword ptr [R14 + 0x368], RAX`
- `FUN_140FAC260` is a reset/lifetime path. It calls `FUN_140FB1490(param_1)`, resets nested transition object fields under `param_1 +0x368`, then writes `*(undefined4 *)(param_1 + 0x56C) = 0` and clears byte `param_1 +0x568`.
- `FUN_140FB24A0` has two direct increment branches for the same owner:
  - `140FB2831  INC dword ptr [R14 + 0x56C]`
  - `140FB28E6  INC dword ptr [R14 + 0x56C]`
- Later in `FUN_140FB24A0`, the current counter value is read and checked as `uVar2 < 2`, matching counter semantics rather than a pure boolean.

## Inferences

- `0x56C` is the direct dword `InGameStateOffset.ZoneSwitchCounter` field. It is initialized/reset to zero, incremented by two branches in `FUN_140FB24A0`, and later used as a small counter threshold.
- `0x368` is a confirmed pointer field inside the InGame state object. The constructor path installs a `0x578`-byte nested object into this slot, and update/reset paths use the same slot as transition/zone-switch state.
- `0x56C` is also observed as a byte field inside that nested object in this function. We do not currently expose it as a separate keypoint because it is the same anchor with a different caret and the nested object is not modeled yet.
- The existing managed field `InGameStateOffset.ZoneSwitchCounter` at direct `InGameState + 0x56C` is runtime-confirmed across read and writer-site tests:
  - live `inGameState = 0x0000033E22E42D10`
  - live direct counter address = `0x0000033E22E4327C`
  - delta = `0x56C`
  - observed value in the checked snapshot = `0xF`
- `ShouldResolveZoneSwitchCounterWriterSitesFromCodePatterns` later confirmed:
  - `directReadOffset=0x56C`
  - `resetOffset=0x56C`
  - `resetAdjacentFlagOffset=0x568`
  - `incrementFirstOffset=0x56C`
  - `incrementSecondOffset=0x56C`
  - live value during the check: `0x1`
- The nested field is currently treated as flag-like by game code, because the proven nested access is `CMP byte ptr [RBX + 0x56C], 0`.

## Runtime Recheck

`ShouldResolveZoneSwitchCounterAndStatePointerFromCodePatterns` after adding the
constructor-backed keypoint:

```text
PathOfExileSteam PID                     = 5592
InGameState                              = 0x0000027ABEE62F10
InGameState vtable                       = 0x00007FF773FBF7C8
direct ZoneSwitchCounter offset          = 0x56C
direct ZoneSwitchCounter address         = 0x0000027ABEE6347C
direct ZoneSwitchCounter value           = 0x1
ZoneSwitchState reader offset            = 0x368
ZoneSwitchState constructor read offset  = 0x368
ZoneSwitchState constructor write offset = 0x368
ZoneSwitchState live object              = 0x0000027ABEE71000
```

`ShouldResolveZoneSwitchCounterWriterSitesFromCodePatterns` in the same client
session:

```text
directReadOffset       = 0x56C
resetOffset            = 0x56C
resetAdjacentFlagOffset= 0x568
incrementFirstOffset   = 0x56C
incrementSecondOffset  = 0x56C
directCounterValue     = 0x1
```

## How To Recheck

- Ghidra MCP: decompile `FUN_140FAF2F0` and inspect VA `0x140FAF31B`, `0x140FAF3E9`, and `0x140FAF3F0`.
- Ghidra MCP: decompile `FUN_140FA9D00` and inspect VA `0x140FAA2D9`, `0x140FAA2F5`, `0x140FAA2FF`, and `0x140FAA306`.
- Ghidra MCP: decompile `FUN_140FAC260` and inspect VA `0x140FAC37A`.
- Ghidra MCP: decompile `FUN_140FB24A0` and inspect VA `0x140FB2831` and `0x140FB28E6`.
- Ghidra MCP: byte-search the exact sequence listed above and confirm it has one hit.
- Runtime: run `dotnet test .\CheatCartridge.Tests\CheatCartridge.Tests.csproj --filter "FullyQualifiedName=CheatCartridge.Tests.Integration.LocalProcessClientIntegrationTests.ShouldResolveZoneSwitchCounterAndStatePointerFromCodePatterns" --logger "console;verbosity=normal"`. This test verifies the reader keypoint and both constructor read/write displacements all resolve `0x368`.
- Runtime: run `dotnet test .\CheatCartridge.Tests\CheatCartridge.Tests.csproj --filter "FullyQualifiedName=CheatCartridge.Tests.Integration.LocalProcessClientIntegrationTests.ShouldResolveZoneSwitchCounterWriterSitesFromCodePatterns" --logger "console;verbosity=normal"`.
- Product: inspect `CheatCartridge/GameHelper/GameOffsets/Offsets.cs`, `CheatCartridge/GameHelper/GameOffsets/States/InGameStateOffset.cs`, and `CheatCartridge.Tests/Integration/LocalProcessClientIntegrationTests.cs`.

## Open Questions

- Which state transition or loading event writes this byte?
- What is the correct managed name for the nested object at `InGameState + 0x368` if we decide to expose it?

## Rejected Ideas

- Treating this nested byte-pattern as proof for the direct `InGameState + 0x56C` counter is rejected. The code-backed access found here reads `*(InGameState + 0x368)` first, then checks `*(nested + 0x56C)`.
