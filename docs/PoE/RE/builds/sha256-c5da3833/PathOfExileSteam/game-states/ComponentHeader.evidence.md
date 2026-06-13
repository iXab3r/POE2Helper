# Component Header

Build: `sha256-c5da3833`
Binary: `PathOfExileSteam.exe`
Slice: `game-states`
Status: `confirmed`

## Finding

The managed `ComponentHeader` used by `PlayerOffsets` and `LifeOffset` is:

```text
0x00 -> component vtable pointer
+0x08 -> owner entity pointer
```

The strongest current static anchor is the Player component debug/info formatter:

```text
FUN_141D267F0
```

This function is reached through the live Player component vtable at slot `+0x68`.
At entry it preserves the component pointer in `R14`, then immediately reads
the owner pointer from `PlayerComponent + 0x08`:

```text
141D267F0  MOV qword ptr [RSP + 0x08], RBX
141D267F5  MOV qword ptr [RSP + 0x10], RBP
141D267FA  MOV qword ptr [RSP + 0x18], RSI
141D267FF  MOV qword ptr [RSP + 0x20], RDI
141D26804  PUSH R14
141D26806  SUB RSP, 0x20
141D2680A  MOV R14, RCX
141D2680D  MOV RDI, RDX
141D26810  MOV RCX, qword ptr [RCX + 0x08]
141D26814  CALL FUN_140E60A90
141D26819  LEA RDX, [0x1432C9080]
141D26820  MOVZX R8D, byte ptr [RAX + 0x158]
```

The decompile shape is:

```text
ownerInfo = FUN_140E60A90(*(undefined8 *)(PlayerComponent + 0x08))
classBits = *(byte *)(ownerInfo + 0x158) & 0x1F
```

Later in the same function, the preserved component pointer in `R14` is used to
read `Player + 0x1B0` for the character name. That ties the function to the
same Player component object used by the existing `PlayerComponentName`
keypoint.

## Pattern

The managed keypoint is `Offsets.KeypointNames.PlayerComponentHeaderOwnerEntity`.

```text
48 89 5C 24 08
48 89 6C 24 10
48 89 74 24 18
48 89 7C 24 20
41 56
48 83 EC 20
4C 8B F1
48 8B FA
48 8B 49 ^ 08
E8 ?? ?? ?? ??
48 8D 15 ?? ?? ?? ??
44 0F B6 80 58 01 00 00
```

Search result in Ghidra:

```text
0x141D267F0
```

The caret lands on the one-byte displacement in:

```text
MOV RCX, qword ptr [RCX + 0x08]
```

Runtime extraction:

```text
componentOwnerOffset = *(byte *)(moduleBase + returnedRva) // 0x08
formatterAddress     = moduleBase + returnedRva - 0x23
```

The function-start subtraction is intentionally local to this compact pattern.
The test verifies the instruction bytes at `returnedRva - 0x03` are still
`48 8B 49 08` before trusting the recovered offset.

## Runtime Confirmation

Explicit integration test:

```text
ShouldResolveComponentHeaderFromPlayerFormatter
```

The test:

- resolves `GameStates`, `GameStateTableShape`, `InGameStateAreaInstanceData`,
  `AreaInstanceLocalPlayers`, and `EntityComponentLookupShape`;
- uses those keypoints to find the live player entity;
- resolves `Player` and `Life` component addresses through the recovered
  component lookup bucket;
- reads `ComponentHeader` from both component addresses;
- verifies `ComponentHeader.StaticPtr` is at `+0x00`;
- verifies the Player header vtable slot `+0x68` points to
  `FUN_141D267F0`;
- verifies the recovered `+0x08` owner pointer on both Player and Life
  components equals the live player entity.

This is deliberately stronger than the previous indirect check in
`ShouldResolveEntityComponentLookupFromCodePattern`, which only verified that
`ComponentHeader.EntityPtr` happened to point back to the player.

## Test

Run:

```powershell
dotnet test .\CheatCartridge.Tests\CheatCartridge.Tests.csproj --filter "FullyQualifiedName=CheatCartridge.Tests.Integration.LocalProcessClientIntegrationTests.ShouldResolveComponentHeaderFromPlayerFormatter" --logger "console;verbosity=normal"
```
