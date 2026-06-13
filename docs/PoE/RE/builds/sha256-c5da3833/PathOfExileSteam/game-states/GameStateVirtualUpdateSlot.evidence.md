# GameState Virtual Update Slot

Build: `sha256-c5da3833`
Binary: `PathOfExileSteam.exe`
Intent: `game-states`
Status: `active`
Confidence: `confirmed`

## Finding

`FUN_1415B2890` dispatches game-state updates through a virtual call:

```text
1415B2A48  MOV    RAX, qword ptr [R14]
1415B2A4B  MOVAPS XMM2, XMM6
1415B2A4E  MOV    RDX, R12
1415B2A51  MOV    RCX, R14
1415B2A54  CALL   qword ptr [RAX + 0x8]
1415B2A57  CMP    byte ptr [R14 + 0x20A], 0
```

The virtual update slot is `state.vtable +0x8`.

## Pattern

`Offsets.KeypointNames.GameStateVirtualUpdateSlot` resolves the slot displacement
from the call operand:

```text
4D 85 F6 74 ?? 49 8B 06 0F 28 D6 49 8B D4 49 8B CE FF 50 ^ 08 41 80 BE ?? ?? ?? ?? 00
```

Reading the byte at the caret returns `0x08`.

## Why This Replaces InGameStateEntry

A long pattern for `InGameStateEntry` was rejected because it is brittle and low
value. The table shape is already known from the GameStates constructor:

```text
state table offset = 0x48
entry size         = 0x10
entry count        = 0x0D
```

Production now scans that recovered table and identifies the InGame state by
behavior:

```text
state.vtable[GameStateVirtualUpdateSlot] -> wrapper -> InGameStateMsElapsed writer
```

The enum-derived entry offset remains useful as a comparator:

```text
0x48 + GameStateTypes.InGameState(4) * 0x10 = 0x88
```

It is not the production selector.

## Live Proof, 2026-06-13

After the client restart, focused integration tests resolved the same update
chain from the live process:

```text
client PID             = 5592
module base            = 0x7FF770F30000
GameStates global slot = 0x7FF775419EB8
GameStates owner       = 0x0000027ABEDE39C0
InGameState            = 0x0000027ABEE62F10
vtable                 = 0x00007FF773FBF7C8
virtual update slot    = 0x8
slot function          = 0x00007FF771EDD1A0  ; static VA 0x140FAD1A0
writer function        = 0x00007FF771EDD6E0  ; static VA 0x140FAD6E0
MsElapsed address      = 0x0000027ABEE63310
```

`ShouldResolveMsElapsedWriterFromInGameStateUpdateVtableChain` verifies that the
slot function contains a relative branch to the writer. `ShouldReadRootObjectModelThroughRuntimeGameOffsets`
also verifies that the behavior-selected entry still agrees with the enum-derived
entry for this build.

## Tests

- `ShouldReadRootObjectModelThroughRuntimeGameOffsets`
- `ShouldResolveMsElapsedFromCodePattern`
- `ShouldResolveMsElapsedWriterFromInGameStateUpdateVtableChain`
