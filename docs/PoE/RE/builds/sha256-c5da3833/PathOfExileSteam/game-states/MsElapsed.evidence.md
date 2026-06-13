# InGameState MsElapsed Evidence

Build: `sha256-c5da3833`
Binary: `PathOfExileSteam.exe`
Intent: `game-states`
Status: `stable`
Confidence: `confirmed`

## Current Conclusion

`InGameStateOffset.MsElapsed` is a 32-bit elapsed/tick value at
`InGameState +0x400`.

The durable keypoints are:

- `InGameStateMsElapsedWriterFunctionStart`: writer entry at `FUN_140FAD6E0`.
- `InGameStateMsElapsed`: field displacement recovered from
  `MOV dword ptr [InGameState +0x400], EAX`.
- `GameStateVirtualUpdateSlot`: `state.vtable +0x8`, used to prove the live
  InGame state update chain reaches the same writer.

## Static Writer Shape

`FUN_140FAD6E0` accumulates a double at `InGameState +0x408`, scales it, then
writes the integer projection to `InGameState +0x400`:

```text
140FAD708  CVTSS2SD   XMM0, XMM6
140FAD70F  ADDSD      XMM0, qword ptr [RCX + 0x408]
140FAD717  MOVSD      qword ptr [RCX + 0x408], XMM0
140FAD71F  MULSD      XMM0, qword ptr [0x14352CB18]
140FAD727  CVTTSD2SI  RAX, XMM0
140FAD72C  MOV        dword ptr [RCX + 0x400], EAX
```

The field pattern intentionally keys on the nearby `+0x408` double accumulation
and the final `+0x400` dword store. That is stronger than searching the whole
binary for displacement `0x400`.

## Virtual Update Ownership

`FUN_1415B2890` dispatches game-state updates through vtable slot `+0x8`:

```text
1415B2A48  MOV    RAX, qword ptr [R14]
1415B2A4B  MOVAPS XMM2, XMM6
1415B2A4E  MOV    RDX, R12
1415B2A51  MOV    RCX, R14
1415B2A54  CALL   qword ptr [RAX + 0x8]
1415B2A57  CMP    byte ptr [R14 + 0x20A], 0
```

The live InGame state slot does not point directly at `FUN_140FAD6E0`. It points
at wrapper `FUN_140FAD1A0`, whose live code tail-branches to the writer:

```text
140FAD279  JMP 0x140FAD6E0
```

The wrapper-tail keypoint is unique enough for the current build and gives a
stable way to confirm that the behavior-selected state-table entry is the InGame
state, without relying only on enum ordering.

## Runtime Confirmation

The latest LocalProcess-backed test run resolved the same chain from the live
client:

```text
PID                 = 5592
InGameState         = 0x0000027ABEE62F10
msElapsedOffset     = 0x400
msElapsedAddress    = 0x0000027ABEE63310
first               = 0x01A32B17
second              = 0x01A32C16
vtable              = 0x00007FF773FBF7C8
slotFunction        = 0x00007FF771EDD1A0
writerFunction      = 0x00007FF771EDD6E0
slotBranchesToWriter= true
```

`second > first`, the writer displacement resolves to `0x400`, and the live
vtable slot function branches to the pattern-resolved writer.

## Why This Is The Preferred Keypoint

This field is hot and changes continuously. Hardware watchpoints on it have
historically made the client unstable. The stable path is therefore:

1. Resolve the writer function and field displacement from static bytes.
2. Resolve the live InGame state through the recovered GameStates table.
3. Confirm the live InGame state virtual update slot reaches the writer.
4. Read the field through `RuntimeGameOffsets`, not through a hardcoded layout.

## Product References

- `CheatCartridge/GameHelper/GameOffsets/Offsets.cs`
- `CheatCartridge/GameHelper/GameOffsets/RuntimeGameOffsets.cs`
- `CheatCartridge/GameHelper/GameOffsets/States/InGameStateOffset.cs`
- `CheatCartridge/GameHelper/RemoteObjects/States/InGameState.cs`

## Tests

- `ShouldResolveMsElapsedFromCodePattern`
- `ShouldResolveMsElapsedWriterFromInGameStateUpdateVtableChain`
- `ShouldReadRootObjectModelThroughRuntimeGameOffsets`

## How To Recheck

1. Open `FUN_140FAD6E0` in IDA/Ghidra and confirm the `+0x408` double
   accumulation followed by the `+0x400` dword store.
2. Open `FUN_1415B2890` and confirm the virtual call through `[vtable +0x8]`.
3. Run the LocalProcess integration tests listed above against the live client.
4. If a patch changes the wrapper, recover the new branch from the live slot
   function to the static writer before trusting the state-table selector.
