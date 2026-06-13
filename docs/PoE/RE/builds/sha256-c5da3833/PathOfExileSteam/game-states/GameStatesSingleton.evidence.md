# GameStates Singleton Evidence

Build: `sha256-c5da3833`
Binary: `PathOfExileSteam.exe`
Related reconstruction: `none yet`
Confidence: `confirmed`

## Build And Binary

- Live process: `PathOfExileSteam`, PID `27532`.
- Source binary: `D:\SteamLibrary\steamapps\common\Path of Exile 2\PathOfExileSteam.exe`.
- Copied binary: `docs\PoE\RE\builds\sha256-c5da3833\PathOfExileSteam\binary\PathOfExileSteam.exe`.
- SHA256: `C5DA38334762A28CEB77A65E16E741A7FDDB19CA38C4F414566E1024D4E4637D`.
- Ghidra import path: `/PoE/sha256-c5da3833/PathOfExileSteam.exe`.
- Ghidra image base: `0x140000000`.

## Proposed Signatures

| Anchor | Proposed signature | Confidence | Argument evidence |
| --- | --- | --- | --- |
| `FUN_14010c350`, VA `0x14010C350` | `GameStatesSharedPtr* GetOrCreateGameStates(GameStatesSharedPtr* out)` | `probable` | Ghidra decompiler shows `param_1[0] = DAT_1444e9eb8` and `param_1[1] = DAT_1444e9ec0`; call sites pass an output pointer. |
| Global slot VA `0x1444E9EB8`, RVA `0x44E9EB8` | `GameStateStaticOffset.GameState` | `confirmed` | `GameStateStaticWrapper` proves the getter copies this slot into wrapper offset `+0x00`; runtime reads this slot and reaches a coherent state owner, InGame state, and area pointer. |
| Global slot VA `0x1444E9EC0`, RVA `0x44E9EC0` | shared sidecar/control pointer | `confirmed` | `GameStateStaticWrapper` proves the getter copies this adjacent slot into wrapper offset `+0x08`. Production code does not currently model this field. |
| Pattern displacement VA `0x14010C371`, RVA `0x10C371` | `Offsets.Patterns[nameof(GameStates)]` points at the RIP-relative rel32 displacement | `confirmed` | Runtime `MemoryUtils.GetOffsets` returns `0x10C371`; reading rel32 from that address resolves the live global slot. |

## Observations

- Existing pattern in `Offsets.cs`: `48 83 EC ?? 48 8B F1 33 ED 48 39 2D ^ ?? ?? ?? ??`.
- Ghidra byte search for `48 83 EC ?? 48 8B F1 33 ED 48 39 2D ?? ?? ?? ??` has one hit at VA `0x14010C365`.
- The caret points to the rel32 displacement bytes at VA `0x14010C371`.
- Static displacement bytes are `43 DB 3D 04`, resolving to global slot VA `0x1444E9EB8`.
- At VA `0x14010C36E`, Ghidra disassembles a comparison against `qword ptr [0x1444e9eb8]`.
- `FUN_14010c350` lazily allocates an object of size `0x140`, writes vtable-like pointer `0x142E9FC50`, initializes state data, calls `FUN_142AD9C6C(plVar4 + 9, 0x10, 0xD, FUN_14010C550, FUN_140107E50)`, then stores the object wrapper into `DAT_1444e9eb8`.
- The same getter returns a two-pointer wrapper:

```text
14010C491  MOV qword ptr [RSI], RBP
14010C494  MOV qword ptr [RSI + 0x08], RBP
14010C498  MOV RAX, qword ptr [0x1444E9EC0]
14010C49F  TEST RAX, RAX
14010C4A4  INC.LOCK dword ptr [RAX + 0x08]
14010C4A8  MOV RAX, qword ptr [0x1444E9EB8]
14010C4AF  MOV qword ptr [RSI], RAX
14010C4B2  MOV RAX, qword ptr [0x1444E9EC0]
14010C4B9  MOV qword ptr [RSI + 0x08], RAX
```

`GameStateStaticOffset.GameState` is therefore the first pointer in that
wrapper. The second pointer is the shared sidecar/control object used for
reference-counting/lifetime management.

- The unique wrapper-copy keypoint is:

```text
48 89 2E
48 89 6E 08
48 8B 05 ?? ?? ?? ??
48 85 C0
74 ??
F0 FF 40 08
48 8B 05 ?? ?? ?? ??
^ 48 89 06
48 8B 05 ?? ?? ?? ??
48 89 46 08
```

The caret marks the owner store `MOV [RSI], RAX`. The first wrapper field has
implicit offset `+0x00`; the sidecar store has explicit displacement `+0x08`.

- Ghidra reports function count `106954` for the imported copied program and analysis was completed before collecting this evidence.
- Runtime MCP status confirmed separate LocalProcess reader mode, endpoint `http://127.0.0.1:41338/mcp`, process `PathOfExileSteam`, PID `27532`, module `PathOfExileSteam.exe`.
- Runtime object model snapshot reached player `Xaber`, entity `0x33E98694680`, area level `65`, area hash `208683925`, and full life, mana, and energy shield values.
- New explicit integration test reached `patternRva=0x10C371`, `globalSlot=0x7FF775419EB8`, `gameStates=0x33E22DF3A00`, `inGameState=0x33E22E42D10`, `area=0x33E98D2C800`.
- `ShouldResolveGameStateStaticWrapperFromGetterPattern` reached the getter wrapper-copy keypoint, resolved owner and sidecar global slots, verified the owner global slot equals the primary `GameStates` singleton slot, verified wrapper offsets `+0x00/+0x08`, and reached the live InGame state through the recovered first field.

## Inferences

- `FUN_14010c350` is very likely the lazy getter or initializer for the game-state owner singleton.
- `DAT_1444e9eb8` is the first pointer-sized field of the static/shared object that CheatCartridge models as `GameStateStaticOffset.GameState`.
- `DAT_1444e9ec0` is an adjacent lifetime/control pointer for the same shared object and is returned at wrapper offset `+0x08`.
- The current hardcoded `GameStates` static offset can be replaced by pattern resolution plus RIP-relative target calculation.
- The next offset-replacement step should not start by guessing field offsets. It should trace accessors and loops from the confirmed state owner and state table.

## How To Recheck

- Ghidra MCP: import the copied binary and run a byte-pattern search for `48 83 EC ?? 48 8B F1 33 ED 48 39 2D ?? ?? ?? ??`.
- Ghidra MCP: decompile `FUN_14010c350` and confirm writes to `DAT_1444e9eb8` and output writes through `param_1`.
- Runtime: run `dotnet test .\CheatCartridge.Tests\CheatCartridge.Tests.csproj --filter "FullyQualifiedName=CheatCartridge.Tests.Integration.LocalProcessClientIntegrationTests.ShouldResolveGameStatesSingletonFromSignatureKeypoint" --logger "console;verbosity=normal"`.
- Runtime: run `dotnet test .\CheatCartridge.Tests\CheatCartridge.Tests.csproj --filter "FullyQualifiedName=CheatCartridge.Tests.Integration.LocalProcessClientIntegrationTests.ShouldResolveGameStateStaticWrapperFromGetterPattern" --logger "console;verbosity=normal"`.
- Product: inspect `CheatCartridge/GameHelper/GameOffsets/Offsets.cs` and `CheatCartridge.Tests/Integration/LocalProcessClientIntegrationTests.cs`.
- MCP: call `poe_debug_status` and `poe_read_player_snapshot` to confirm the same live process and object model are being read.

## Open Questions

- What stable instruction pattern reaches `InGameState.AreaInstanceData`?
- Which static access sites prove `GameStateOffset.CurrentStatePtr` and the state table layout?
- Is `FUN_142AD9C6C` a state registration helper, and can its arguments identify all state entries without relying on fixed array indexes?
- Does the singleton getter shape remain stable across the next Path Of Exile client update?

## Rejected Ideas

- Treating `0x44E9EB8` as a durable hardcoded RVA is rejected. It is confirmed for this build only.
- Promoting the local player snapshot as proof of every nested offset is rejected. It proves the current chain is coherent, but each nested field still needs its own keypoint or accessor evidence.
