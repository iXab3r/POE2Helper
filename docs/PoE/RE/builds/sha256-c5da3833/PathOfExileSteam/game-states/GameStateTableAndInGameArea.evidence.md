# GameState Table And InGame Area Evidence

Build: `sha256-c5da3833`
Binary: `PathOfExileSteam.exe`
Related reconstruction: `none yet`
Confidence: `confirmed`

## Proposed Signatures

| Anchor | Proposed signature | Confidence | Argument evidence |
| --- | --- | --- | --- |
| `FUN_14010c350`, VA `0x14010C3C0` | `GameStateOffset.States` table constructor | `confirmed` | `LEA RCX, [RDI + 0x48]`, `MOV EDX, 0x10`, `MOV R8D, 0xD`, then calls the array-construction helper. |
| `FUN_1415B0360`, VA `0x1415B03E8` | `GameStateOffset.CurrentStatePtr` source vector | `confirmed` | Dirty-copy helper computes source count from `[GameStates + 0x10] - [GameStates + 0x08]`, then copies 16-byte entries into dispatch vector `+0x20..+0x28`. |
| `FUN_140233C10`, VA `0x140233CB0` | `InGameState.AreaInstanceData` accessor | `confirmed` | Same code path checks the InGame state pointer and reads `[InGameState + 0x290]` before using the area object. |

## Stable Byte Keypoints

| Name | Pattern | Immediate recovered |
| --- | --- | --- |
| `GameStateTableShape` | `48 8D 4F ^ 48 48 8D 05 ?? ?? ?? ?? 48 89 44 24 20 4C 8D 0D ?? ?? ?? ?? BA 10 00 00 00 41 B8 0D 00 00 00 E8` | `0x48`; element size is at `+21`, count at `+27`. |
| `GameStateCurrentStateVector` | `41 80 7F 40 00 0F 84 ?? ?? ?? ?? 49 8B 7F 10 49 2B 7F ^ 08 48 C1 FF 04 49 8B 5F 28 49 8B 57 20 48 8B CB 48 2B CA 48 C1 F9 04 48 3B F9` | `0x08`; source Last is at `-0x04`, dispatch Last at `+0x08`, dispatch First at `+0x0C`, dirty flag at `-0x0F`. |
| `InGameStateAreaInstanceData` | `4D 85 F6 0F 84 ?? ?? ?? ?? 49 8B 9E ^ ?? ?? ?? ?? 48 85 DB` | `0x290`. |

## Observations

- `FUN_14010c350` initializes the state owner with object size `0x140`.
- The table constructor starts at `GameStates + 0x48`, constructs entries of size `0x10`, and constructs `0xD` entries.
- The previous managed buffer size of `12` entries was inconsistent with constructor evidence. It should be `13`.
- The table ends at `0x48 + 0x10 * 0xD == 0x118`, matching the next initialization at `LEA RBX, [RDI + 0x118]`.
- `FUN_1415B0360` proves the source current-state vector starts at `GameStates +0x08`; it computes `(Last - First) >> 4` from `+0x10` and `+0x08`.
- The same dirty-copy helper proves a separate dispatch vector at `+0x20..+0x28` and dirty flag at `+0x40`.
- `FUN_140233C10` calls `FUN_14010C350`, reads the InGame state entry from `GameStates + 0x88`, preserves the shared reference sidecar at `+0x90`, then reads the area instance from `InGameState + 0x290`.
- The `GameStates + 0x88` accessor is useful corroboration, but the byte pattern required to make it unique was too long and fragile to keep as an active keypoint.
- Broad searches for `AreaInstance` field offsets such as `0x588` and `0x6C8` are noisy. Those fields need anchors from the live area object's vtable/function family, not standalone offset searches.

## Inferences

- `GameStateBuffer.TOTAL_STATES` should be `13` for this build.
- `GameStateOffset.CurrentStatePtr` should remain `0x08` for this build and should be recovered from `GameStateCurrentStateVector`.
- The current InGame entry offset can be derived without its own pattern: `0x48 + GameStateTypes.InGameState * 0x10 == 0x88`.
- `InGameStateOffset.AreaInstanceData = 0x290` is backed by a concrete accessor that first resolves the InGame state entry from the state owner.
- Future runtime resolution can recover table shape and area-instance offset from code patterns, while the specific InGame table entry is inferred from the managed enum and recovered table stride.

## How To Recheck

- Ghidra MCP: decompile `FUN_14010c350` and inspect VA `0x14010C3C0` through `0x14010C3E2`.
- Ghidra MCP: decompile `FUN_1415B0360` and inspect VA `0x1415B03D9` through `0x1415B03F4`.
- Ghidra MCP: decompile `FUN_140233C10` and inspect VA `0x140233CB0` for the area-instance displacement. VA `0x140233C47` is corroboration only, not an active pattern.
- Runtime: run `dotnet test .\CheatCartridge.Tests\CheatCartridge.Tests.csproj --filter "FullyQualifiedName=CheatCartridge.Tests.Integration.LocalProcessClientIntegrationTests.ShouldResolveInGameAreaKeypointsFromCodePatterns" --logger "console;verbosity=normal"`.
- Runtime: run `dotnet test .\CheatCartridge.Tests\CheatCartridge.Tests.csproj --filter "FullyQualifiedName=CheatCartridge.Tests.Integration.LocalProcessClientIntegrationTests.ShouldResolveCurrentStateVectorFromDirtyCopyPattern" --logger "console;verbosity=normal"`.
- Product: inspect `CheatCartridge/GameHelper/GameOffsets/Offsets.cs`, `CheatCartridge/GameHelper/GameOffsets/GameStateBuffer.cs`, and `CheatCartridge.Tests/Integration/LocalProcessClientIntegrationTests.cs`.

## Open Questions

- Which area object vtable method or constructor gives the best stable anchor for any future area fields not already covered by `AreaInstanceScalars.evidence.md`?

## Rejected AreaInstance Field

The previous `AreaInstanceOffsets.UnknownVtablePtr` candidate at `+0xB38`
was removed from the managed struct. `FUN_1402332D0` proves `vtable + 0xB38`
is a virtual method call slot, not an `AreaInstance +0xB38` object-field read.

## Rejected Ideas

- Using a global search for `0x588` or `0x6C8` as proof of `AreaInstance` fields is rejected. Those displacements appear in many unrelated functions.
- Keeping the long `InGameStateEntry` pattern is rejected. It is brittle and low value because the entry offset is cleaner to derive from table shape plus `GameStateTypes.InGameState`.
