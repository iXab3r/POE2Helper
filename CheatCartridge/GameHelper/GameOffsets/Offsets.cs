using CheatCartridge.GameHelper.RemoteObjects;
using EyeAuras.Memory.Scaffolding;

namespace CheatCartridge.GameHelper.GameOffsets;

/// <summary>
/// Central registry for byte-pattern based RE keypoints.
///
/// IMPORTANT TERMINOLOGY
/// ---------------------
/// "Offset" in this file does not always mean the same thing:
///
/// 1. Static global slot resolution
///    Example: <see cref="GameStates"/>.
///    The pattern lands on a RIP-relative rel32 operand inside game code.
///    Runtime code reads that rel32 and computes the actual module-local global
///    address. This replaces a hardcoded static RVA such as 0x44E9EB8.
///
/// 2. Struct/table shape recovery
///    Example: <see cref="KeypointNames.GameStateTableShape"/>.
///    The pattern lands on immediate operands used by the game's own constructor
///    code. Runtime code reads those immediates to recover field offset, element
///    size, and element count from the binary.
///
/// 3. Field displacement recovery from accessor code
///    Example: <see cref="KeypointNames.InGameStateAreaInstanceData"/>.
///    The pattern lands on the displacement operand of an instruction like
///    MOV RBX, [R14 + 0x290]. Runtime code reads the displacement instead of
///    trusting a hardcoded FieldOffset.
///
/// CARET SEMANTICS
/// ---------------
/// BytePattern.FromTemplate uses ^ to mark the byte address we want returned by
/// MemoryUtils.GetOffsets. In practice we place ^ immediately before the operand
/// bytes we want to read at runtime.
///
/// For example:
///
///     48 39 2D ^ ?? ?? ?? ??
///
/// means "return the address of the rel32 displacement in CMP [RIP + rel32], RBP".
/// The caller can then read int32 at that returned address and apply normal x64
/// RIP-relative math:
///
///     target = displacementAddress + *(int*)displacementAddress + 4
///
/// EVIDENCE
/// --------
/// The current evidence notes for this file live under:
///
///     docs/PoE/RE/builds/sha256-c5da3833/PathOfExileSteam/game-states/
///
/// Start with:
///
///     GameStatesSingleton.evidence.md
///     CurrentStateVector.evidence.md
///     GameStateTableAndInGameArea.evidence.md
///     AreaInstanceLocalPlayers.evidence.md
///     AreaInstanceScalars.evidence.md
///     EntityComponentLookup.evidence.md
///     ComponentHeader.evidence.md
///     EntityDetailsName.evidence.md
///     EntityIdentity.evidence.md
///     LifeVitals.evidence.md
///     MsElapsed.evidence.md
///     PlayerName.evidence.md
///     ZoneSwitchState.evidence.md
///
/// The live regression tests are in:
///
///     CheatCartridge.Tests/Integration/LocalProcessClientIntegrationTests.cs
///
/// The most important explicit tests are:
///
///     ShouldResolveGameStatesSingletonFromSignatureKeypoint
///     ShouldResolveGameStateStaticWrapperFromGetterPattern
///     ShouldResolveCurrentStateVectorFromDirtyCopyPattern
///     ShouldResolveInGameAreaKeypointsFromCodePatterns
///     ShouldResolveAreaInstanceCurrentAreaLevelFromCodePattern
///     ShouldResolveAreaInstanceEntitiesCountFromCodePattern
///     ShouldResolveAreaInstanceLocalPlayersFromCodePattern
///     ShouldResolveEntityDetailsNameFromVtableAccessor
///     ShouldResolveEntityIdentityFromAreaInstanceTreeFilter
///     ShouldResolveEntityComponentLookupFromCodePattern
///     ShouldResolveComponentHeaderFromPlayerFormatter
///     ShouldResolveLifeVitalsFromCodePatterns
///     ShouldResolveMsElapsedFromCodePattern
///     ShouldResolvePlayerNameFromCodePattern
///     ShouldResolveZoneSwitchCounterAndStatePointerFromCodePatterns
///     ShouldResolveZoneSwitchCounterWriterSitesFromCodePatterns
///
/// HOW TO UPDATE AFTER A PATCH
/// ---------------------------
/// 1. Copy the new PathOfExileSteam.exe into a new build folder under docs/PoE/RE.
/// 2. Import that copy into Ghidra, not the live mutable binary.
/// 3. Re-run the byte searches described below.
/// 4. Re-run the explicit LocalProcess integration tests against the live client.
/// 5. If a pattern moved but still represents the same code shape, update this
///    file and the build-specific evidence note together.
/// 6. If a code shape changed, do not blindly adjust bytes. Find the new
///    semantic keypoint first, then add/update tests.
/// </summary>
public static class Offsets
{
    /// <summary>
    /// Names for secondary RE keypoint patterns.
    ///
    /// These are intentionally separate from the main <see cref="Patterns"/> list.
    /// The main list currently feeds production object-model bootstrapping in
    /// <c>TheGame.UpdateData()</c>. The keypoint list is for proving and gradually
    /// replacing struct FieldOffset constants with offsets recovered from code.
    ///
    /// As we mature this, some entries from <see cref="KeypointPatterns"/> can
    /// graduate into runtime offset providers. Until then, tests consume them to
    /// keep the RE conclusions executable.
    ///
    /// Deliberate omission:
    ///
    ///     We do not keep a pattern for "InGameStateEntry". The candidate pattern
    ///     was long, fragile, and low value. <see cref="GameStateTableShape"/>
    ///     gives us table offset/stride/count. The runtime provider can either
    ///     derive the entry from the known enum index or identify the live InGame
    ///     entry by scanning the table for the state whose virtual update slot
    ///     branches to the pattern-backed MsElapsed writer:
    ///
    ///         state.vtable[GameStateVirtualUpdateSlot] -> wrapper -> InGameStateMsElapsed writer
    /// </summary>
    public static class KeypointNames
    {
        /// <summary>
        /// Finds the singleton getter code that copies the game-state shared wrapper into an output buffer.
        ///
        /// Recovered values for build sha256-c5da3833:
        ///
        ///     GameStateStaticOffset.GameState: 0x00
        ///     shared-sidecar pointer:          0x08
        ///
        /// Ghidra location:
        ///
        ///     FUN_14010C350
        ///     wrapper copy starts at VA 0x14010C4A8
        ///     owner store is at VA 0x14010C4AF
        ///
        /// Relevant disassembly:
        ///
        ///     14010C4A8  MOV RAX, qword ptr [0x1444E9EB8]
        ///     14010C4AF  MOV qword ptr [RSI], RAX
        ///     14010C4B2  MOV RAX, qword ptr [0x1444E9EC0]
        ///     14010C4B9  MOV qword ptr [RSI + 0x08], RAX
        ///
        /// Why it matters:
        ///
        ///     The production object model reads the global slot as
        ///     GameStateStaticOffset and then follows GameStateStaticOffset.GameState.
        ///     This keypoint proves that the first pointer-sized field in the
        ///     shared wrapper is the GameStates owner pointer and that the second
        ///     pointer at +0x08 is the reference-counted sidecar/control pointer.
        /// </summary>
        public const string GameStateStaticWrapper = nameof(GameStateStaticWrapper);

        /// <summary>
        /// Finds the state-owner constructor code which creates the fixed game-state table.
        ///
        /// Recovered values for build sha256-c5da3833:
        ///
        ///     state table offset: 0x48
        ///     state entry size:   0x10
        ///     state entry count:  0x0D
        ///
        /// Ghidra location:
        ///
        ///     PathOfExileSteam.exe
        ///     FUN_14010c350
        ///     VA 0x14010C3C0
        ///
        /// Relevant disassembly:
        ///
        ///     14010C3C0  LEA RCX, [RDI + 0x48]
        ///     14010C3C4  LEA RAX, [0x140107E50]
        ///     14010C3CB  MOV [RSP + 0x20], RAX
        ///     14010C3D0  LEA R9, [0x14010C550]
        ///     14010C3D7  MOV EDX, 0x10
        ///     14010C3DC  MOV R8D, 0x0D
        ///     14010C3E2  CALL 0x142AD9C6C
        ///
        /// Why it matters:
        ///
        ///     This proves GameStateOffset.States starts at +0x48 and that the
        ///     table has 13 entries of 16 bytes. This is why GameStateBuffer now
        ///     uses TOTAL_STATES = 13. The prior value, 12, was inconsistent with
        ///     the game constructor.
        /// </summary>
        public const string GameStateTableShape = nameof(GameStateTableShape);

        /// <summary>
        /// Finds the state-owner dirty-copy path that refreshes the current-state vector.
        ///
        /// Recovered values for build sha256-c5da3833:
        ///
        ///     source/current vector: 0x08..0x10
        ///     copied/dispatch vector: 0x20..0x30
        ///     dirty flag:            0x40
        ///     state entry size:      0x10
        ///
        /// Ghidra location:
        ///
        ///     FUN_1415B0360
        ///     unique pattern starts at VA 0x1415B03D9
        ///     source vector first displacement is at VA 0x1415B03EB
        ///
        /// Relevant disassembly:
        ///
        ///     1415B03D9  CMP byte ptr [R15 + 0x40], 0x0
        ///     1415B03E4  MOV RDI, qword ptr [R15 + 0x10]
        ///     1415B03E8  SUB RDI, qword ptr [R15 + 0x08]
        ///     1415B03EC  SAR RDI, 0x4
        ///     1415B03F0  MOV RBX, qword ptr [R15 + 0x28]
        ///     1415B03F4  MOV RDX, qword ptr [R15 + 0x20]
        ///
        /// Why it matters:
        ///
        ///     the FF/runtime-layout tests reads the latest entry
        ///     from the source vector using the recovered vector offset and
        ///     recovered 0x10-byte entry shape. The same function copies entries
        ///     into a second vector at +0x20..+0x30 when the dirty flag at +0x40
        ///     is set.
        /// </summary>
        public const string GameStateCurrentStateVector = nameof(GameStateCurrentStateVector);

        /// <summary>
        /// Finds the virtual update slot used by the state dispatch loop.
        ///
        /// Recovered value for build sha256-c5da3833:
        ///
        ///     update vtable slot: 0x08
        ///
        /// Ghidra location:
        ///
        ///     FUN_1415B2890
        ///     VA 0x1415B2A54 calls qword ptr [RAX + 0x08]
        ///
        /// Why it matters:
        ///
        ///     This lets the runtime provider identify the InGame state entry
        ///     by state behavior rather than only by enum index: the live InGame
        ///     object's update slot points at wrapper FUN_140FAD1A0, which
        ///     branches to the pattern-backed MsElapsed writer FUN_140FAD6E0.
        /// </summary>
        public const string GameStateVirtualUpdateSlot = nameof(GameStateVirtualUpdateSlot);

        /// <summary>
        /// Finds code that reads the area instance pointer from the InGame state object.
        ///
        /// Recovered value for build sha256-c5da3833:
        ///
        ///     InGameStateOffset.AreaInstanceData: 0x290
        ///
        /// Ghidra location:
        ///
        ///     FUN_140233C10
        ///     VA 0x140233CA7 starts the null-check/read sequence
        ///     VA 0x140233CB0 reads [InGameState + 0x290]
        ///
        /// Relevant disassembly:
        ///
        ///     140233CA7  TEST R14, R14
        ///     140233CAA  JZ ...
        ///     140233CB0  MOV RBX, [R14 + 0x290]
        ///     140233CB7  TEST RBX, RBX
        ///
        /// Why it matters:
        ///
        ///     This is a code-backed replacement path for the hardcoded
        ///     InGameStateOffset.AreaInstanceData FieldOffset. The live integration
        ///     test reads this displacement from the binary, then reads the actual
        ///     area pointer from the live client and compares it against the struct
        ///     model.
        ///
        /// Important: this returns an offset, not the final area address.
        ///
        ///     MemoryUtils.GetOffsets returns the module RVA of the displacement
        ///     operand inside the MOV instruction. Reading int32 at that operand
        ///     gives 0x290. The final live pointer is a separate memory read:
        ///
        ///         fieldOffset  = 0x290
        ///         fieldAddress = inGameStateAddress + fieldOffset
        ///         areaAddress  = *(IntPtr*)fieldAddress
        ///
        /// Nuance:
        ///
        ///     Do not confuse this with AreaInstance fields such as LocalPlayers
        ///     or EntitiesCount. Those are one level deeper and still need their
        ///     own keypoints. Broad searches for displacements like 0x588 and
        ///     0x6C8 were noisy and are documented as rejected evidence.
        /// </summary>
        public const string InGameStateAreaInstanceData = nameof(InGameStateAreaInstanceData);

        /// <summary>
        /// Finds a direct read of the InGameState zone-switch counter.
        ///
        /// Recovered value for build sha256-c5da3833:
        ///
        ///     InGameStateOffset.ZoneSwitchCounter: 0x56C
        ///
        /// Ghidra location:
        ///
        ///     Label-less analyzed block around VA 0x140FB3F59
        ///     VA 0x140FB3F60 compares dword ptr [InGameState + 0x56C] with 1.
        ///
        /// Relevant disassembly:
        ///
        ///     140FB3F59  MOV RAX, [RBP + 0x9F0]
        ///     140FB3F60  CMP dword ptr [RAX + 0x56C], 0x1
        ///     140FB3F67  JNZ ...
        ///     140FB3F6D  MOV RBX, [RAX + 0x360]
        ///
        /// Why this is separate from the nested +0x56C byte check:
        ///
        ///     This keypoint reads +0x56C directly from the InGameState-shaped
        ///     object held in RAX. The nested check in FUN_140FAF2F0 first
        ///     follows InGameState + 0x368 and then checks +0x56C on that nested
        ///     object.
        /// </summary>
        public const string InGameStateZoneSwitchCounter = nameof(InGameStateZoneSwitchCounter);

        /// <summary>
        /// Finds the reset path that clears the direct InGameState zone-switch counter.
        ///
        /// Recovered value for build sha256-c5da3833:
        ///
        ///     InGameStateOffset.ZoneSwitchCounter: 0x56C
        ///
        /// Ghidra location:
        ///
        ///     FUN_140FAC260
        ///     VA 0x140FAC37A writes dword ptr [InGameState + 0x56C].
        ///
        /// Relevant disassembly:
        ///
        ///     140FAC37A  MOV dword ptr [RBX + 0x56C], EBP
        ///     140FAC380  MOV byte ptr [RBX + 0x568], 0x0
        ///     140FAC387  LEA RCX, [RBX + 0x650]
        ///
        /// Decompile shows this is a reset/lifetime path for the same owner
        /// family as the direct counter read.
        /// </summary>
        public const string InGameStateZoneSwitchCounterReset = nameof(InGameStateZoneSwitchCounterReset);

        /// <summary>
        /// Finds the first increment branch for the direct InGameState zone-switch counter.
        ///
        /// Recovered value for build sha256-c5da3833:
        ///
        ///     InGameStateOffset.ZoneSwitchCounter: 0x56C
        ///
        /// Ghidra location:
        ///
        ///     FUN_140FB24A0
        ///     VA 0x140FB2831 increments dword ptr [InGameState + 0x56C].
        ///
        /// The same function has a second increment branch at VA 0x140FB28E6,
        /// covered by <see cref="InGameStateZoneSwitchCounterIncrementSecond"/>.
        /// </summary>
        public const string InGameStateZoneSwitchCounterIncrementFirst = nameof(InGameStateZoneSwitchCounterIncrementFirst);

        /// <summary>
        /// Finds the second increment branch for the direct InGameState zone-switch counter.
        ///
        /// Recovered value for build sha256-c5da3833:
        ///
        ///     InGameStateOffset.ZoneSwitchCounter: 0x56C
        ///
        /// Ghidra location:
        ///
        ///     FUN_140FB24A0
        ///     VA 0x140FB28E6 increments dword ptr [InGameState + 0x56C].
        /// </summary>
        public const string InGameStateZoneSwitchCounterIncrementSecond = nameof(InGameStateZoneSwitchCounterIncrementSecond);

        /// <summary>
        /// Finds the InGameState field that points at the nested zone-switch state/controller object.
        ///
        /// Recovered value for build sha256-c5da3833:
        ///     InGameState zone-switch state pointer: 0x368
        ///
        /// Important ownership nuance:
        ///
        ///     This keypoint proves only the owner transition at +0x368. The
        ///     direct InGameState.ZoneSwitchCounter at +0x56C is a different
        ///     field and has its own <see cref="InGameStateZoneSwitchCounter"/>
        ///     pattern.
        ///
        ///     The code first reads:
        ///
        ///         zoneSwitchState = *(IntPtr*)(InGameState + 0x368)
        ///
        ///     and only then checks:
        ///
        ///         *(byte*)(zoneSwitchState + 0x56C)
        ///
        /// Do not conflate that nested +0x56C byte with the direct
        /// InGameStateOffset.ZoneSwitchCounter field, even though the displacement
        /// value is numerically the same.
        /// </summary>
        public const string InGameStateZoneSwitchState = nameof(InGameStateZoneSwitchState);

        /// <summary>
        /// Finds the InGameState constructor/lifetime assignment for the nested zone-switch state object.
        ///
        /// Recovered value for build sha256-c5da3833:
        ///     InGameState zone-switch state pointer: 0x368
        ///
        /// Ghidra location:
        ///
        ///     FUN_140FA9D00
        ///     VA 0x140FAA2D9 allocates 0x578 bytes.
        ///     VA 0x140FAA2F5 calls FUN_14012F740(newObject, InGameState, oldObject).
        ///     VA 0x140FAA2FF reads the old object from [InGameState + 0x368].
        ///     VA 0x140FAA306 writes the new object to [InGameState + 0x368].
        ///
        /// This is stronger ownership evidence than the reader-only
        /// <see cref="InGameStateZoneSwitchState"/> keypoint because it proves
        /// the field is the slot where the nested transition object is installed.
        /// </summary>
        public const string InGameStateZoneSwitchStateConstructor = nameof(InGameStateZoneSwitchStateConstructor);

        /// <summary>
        /// Finds the start of the InGameState elapsed-time writer function.
        ///
        /// Recovered value for build sha256-c5da3833:
        ///     writer function: FUN_140FAD6E0
        ///
        /// Why this exists separately from <see cref="InGameStateMsElapsed"/>:
        ///
        ///     The field-offset keypoint intentionally lands on the write
        ///     displacement in MOV dword ptr [InGameState + 0x400], EAX. Earlier
        ///     provider code recovered the writer function by subtracting a fixed
        ///     distance from that operand address. This keypoint removes that
        ///     brittle function-start derivation and lets the runtime provider
        ///     resolve the function address directly from a start-anchored
        ///     signature.
        /// </summary>
        public const string InGameStateMsElapsedWriterFunctionStart = nameof(InGameStateMsElapsedWriterFunctionStart);

        /// <summary>
        /// Finds the InGameState elapsed-time field written by the state tick/update path.
        ///
        /// Recovered value for build sha256-c5da3833:
        ///     InGameStateOffset.MsElapsed: 0x400
        ///
        /// Ghidra location:
        ///     FUN_140FAD6E0
        ///     unique pattern starts at VA 0x140FAD708
        ///     field write at VA 0x140FAD72C
        ///
        /// Relevant decompile:
        ///
        ///     elapsedDouble = (double)deltaSeconds + *(double *)(InGameState + 0x408)
        ///     *(double *)(InGameState + 0x408) = elapsedDouble
        ///     elapsedDouble = elapsedDouble * DAT_14352CB18
        ///     *(int *)(InGameState + 0x400) = (int)(longlong)elapsedDouble
        ///
        /// This proves +0x400 is a 32-bit elapsed/tick value owned directly by
        /// InGameState. It is not a pointer.
        /// </summary>
        public const string InGameStateMsElapsed = nameof(InGameStateMsElapsed);

        /// <summary>
        /// Finds code that publishes the current area monster level from the current AreaInstance.
        ///
        /// Recovered values for build sha256-c5da3833:
        ///     InGameStateOffset.AreaInstanceData:       0x290
        ///     AreaInstanceOffsets.CurrentAreaLevel:     0x0C4
        ///
        /// Ghidra location:
        ///     Label-less analyzed block around VA 0x140FB33F0
        ///
        /// Relevant disassembly:
        ///     140FB33F0  MOV RAX, qword ptr [R12 + 0x290]
        ///     140FB33F8  MOVZX ECX, byte ptr [RAX + 0xC4]
        ///     140FB33FF  MOV dword ptr [0x14434BAC8], ECX
        ///
        /// Why this is a strong keypoint:
        ///     The owner pointer is loaded from the already-proven
        ///     InGameState.AreaInstanceData slot and immediately used as an
        ///     AreaInstance-shaped object. The second instruction reads a byte
        ///     from that object and publishes it as a dword global, which matches
        ///     the managed CurrentAreaLevel semantics.
        ///
        /// Runtime extraction:
        ///     areaInstanceDataOffset = memory.Read<int>(returnedAddress - 7) // 0x290
        ///     currentAreaLevelOffset = memory.Read<int>(returnedAddress)     // 0x0C4
        /// </summary>
        public const string AreaInstanceCurrentAreaLevel = nameof(AreaInstanceCurrentAreaLevel);

        /// <summary>
        /// Finds code that reads the current AreaInstance hash through the current-area holder.
        ///
        /// Recovered values for build sha256-c5da3833:
        ///     current-area holder AreaInstance pointer: 0x2170
        ///     AreaInstanceOffsets.CurrentAreaHash:      0x011C
        ///
        /// Ghidra location:
        ///     FUN_142061AD0
        ///     unique pattern starts at VA 0x1420607BB
        ///     hash displacement is at VA 0x1420607C4
        ///
        /// Relevant disassembly:
        ///     1420607BB  MOV RCX, qword ptr [RDI + 0x2170]
        ///     1420607C2  MOV EAX, dword ptr [RCX + 0x11C]
        ///     1420607C8  MOV dword ptr [RSP + 0x58], EAX
        ///     1420607CC  MOV EAX, dword ptr [RCX + 0x120]
        ///     1420607D2  MOV dword ptr [RSP + 0x5C], EAX
        ///     1420607D6  LEA RDX, [RSP + 0x40]
        ///     1420607DB  MOV RCX, RDI
        ///     1420607DE  CALL FUN_14205F360
        ///
        /// Ownership proof:
        ///     This keypoint is not just a broad +0x11C search. The holder
        ///     constructor FUN_14205D6D0 stores the area object in holder +0x2170
        ///     and stores the holder back into area +0x580:
        ///
        ///         param_1[0x42E] = param_2  // holder + 0x2170 = AreaInstance
        ///         param_2[0x0B0] = param_1  // AreaInstance + 0x580 = holder
        ///
        ///     AreaInstance +0x580 is immediately before the already-proven
        ///     LocalPlayers vector at +0x588, so this ties the holder path to
        ///     the same current AreaInstance object family.
        ///
        /// Runtime extraction:
        ///     holderAreaInstanceOffset = memory.Read<int>(returnedAddress - 6) // 0x2170
        ///     currentAreaHashOffset    = memory.Read<int>(returnedAddress)     // 0x011C
        /// </summary>
        public const string AreaInstanceCurrentAreaHash = nameof(AreaInstanceCurrentAreaHash);

        /// <summary>
        /// Finds the AreaInstance vector that the game accessor copies into an output local-player list.
        ///
        /// Recovered value for build sha256-c5da3833:
        ///     AreaInstanceOffsets.LocalPlayers: 0x588
        ///
        /// Ghidra location:
        ///     FUN_1420731D0
        ///     AreaInstance vtable slots 0x140 and 0x148 both point at this function.
        ///
        /// Relevant decompile:
        ///     count = (*(longlong *)(AreaInstance + 0x590) -
        ///              *(longlong *)(AreaInstance + 0x588)) >> 3
        ///
        ///     for (entry = *(ulonglong **)(AreaInstance + 0x588);
        ///          entry != *(ulonglong **)(AreaInstance + 0x590);
        ///          entry++)
        ///     {
        ///         if (*entry != 0)
        ///             output.push_back(*entry);
        ///     }
        ///
        /// Why this is a strong keypoint:
        ///
        ///     The function is not selected from a broad displacement search.
        ///     It is reached from the live AreaInstance vtable, and several
        ///     AreaInstance-family callers use virtual slot 0x140/0x148 when they
        ///     need the current controlled/local entity list. The accessor copies
        ///     only non-null qword entries out of the vector at +0x588, which
        ///     matches the managed object model's StdVector<IntPtr> shape.
        ///
        /// Runtime extraction:
        ///     localPlayersOffset = memory.Read<int>(moduleBase + returnedRva) // 0x588
        /// </summary>
        public const string AreaInstanceLocalPlayers = nameof(AreaInstanceLocalPlayers);

        /// <summary>
        /// Finds the AreaInstance-owned entity tree root initialized by the base AreaInstance constructor.
        ///
        /// Recovered values for build sha256-c5da3833:
        ///     AreaInstance entity tree object:     0x6B0
        ///     AreaInstance entity tree root slot:  0x6C0
        ///     AreaInstanceOffsets.EntitiesCount:  0x6C8
        ///
        /// Ghidra location:
        ///     FUN_14206E780
        ///     unique tightened pattern starts at VA 0x14206EBCB
        ///     tree-root displacement is at VA 0x14206EBE3
        ///
        /// Relevant disassembly:
        ///     14206EBD2  MOV qword ptr [RSI + 0x6B0], RAX
        ///     14206EBD9  MOV qword ptr [RSI + 0x6B8], RBP
        ///     14206EBE0  LEA RBX, [RSI + 0x6C0]
        ///     14206EBEC  MOV qword ptr [RBX], RBP
        ///     14206EBEF  MOV qword ptr [RBX + 0x8], RBP
        ///     14206EBF3  MOV ECX, 0x30
        ///     14206EBF8  CALL FUN_140155B20
        ///     14206EBFD  MOV qword ptr [RAX], RAX
        ///     14206EC00  MOV qword ptr [RAX + 0x8], RAX
        ///     14206EC04  MOV qword ptr [RAX + 0x10], RAX
        ///     14206EC08  MOV word ptr [RAX + 0x18], 0x101
        ///     14206EC0E  MOV qword ptr [RBX], RAX
        ///
        /// What the pattern returns:
        ///     This returns the displacement operand from LEA RBX, [RSI + 0x6C0].
        ///     That is the root/sentinel pointer slot of the first tree, not the
        ///     count field itself.
        ///
        /// How EntitiesCount is derived:
        ///     The decompiler shows the same constructor writes:
        ///
        ///         param_1[0xD8] = 0;  // +0x6C0, root/sentinel pointer
        ///         param_1[0xD9] = 0;  // +0x6C8, tree count
        ///
        ///     The instruction MOV qword ptr [RBX + 0x8], RBP clears the count
        ///     after RBX is set to AreaInstance + 0x6C0. Therefore the managed
        ///     EntitiesCount offset is:
        ///
        ///         recoveredTreeRootOffset + IntPtr.Size == 0x6C0 + 8 == 0x6C8
        ///
        /// Ownership proof:
        ///     This constructor is called by FUN_14163CB80 before that function
        ///     installs the AreaInstance vtable at 0x1432C9C48. The AreaInstance
        ///     destructor FUN_14163B7A0 later iterates param_1[0xD8], reads each
        ///     node's entity pointer from node +0x28, filters entity fields such
        ///     as +0x88/+0x8C/+0x8D/+0x98, and finally cleans the same container
        ///     through FUN_141DC14B0(param_1 + 0xD6). That ties this tree to the
        ///     AreaInstance entity set, not to an unrelated tree-shaped member.
        /// </summary>
        public const string AreaInstanceEntityTreeRoot = nameof(AreaInstanceEntityTreeRoot);

        /// <summary>
        /// Finds the AreaInstance destructor filter that walks the entity tree and reads entity identity/status fields.
        ///
        /// Recovered values for build sha256-c5da3833:
        ///     entity tree root slot:        0x6C0
        ///     tree node entity pointer:     0x28
        ///     EntityOffsets.Id:             0x88
        ///     EntityOffsets.IsValid/status: 0x8C
        ///     adjacent active flag byte:    0x8D
        ///     status invalid mask:          0x01
        ///     id upper bound:               0x40000000
        ///     required active flag mask:    0x04
        ///
        /// Ghidra location:
        ///     FUN_14163B7A0
        ///     unique pattern starts at VA 0x14163B9EB
        ///     status-byte displacement is at VA 0x14163BA06
        ///
        /// Relevant disassembly:
        ///     14163B9EB  MOV RBX, qword ptr [RSI + 0x6C0]
        ///     14163B9F2  MOV RBX, qword ptr [RAX]
        ///     14163BA00  MOV RDI, qword ptr [RBX + 0x28]
        ///     14163BA04  TEST byte ptr [RDI + 0x8C], 0x1
        ///     14163BA0D  CMP dword ptr [RDI + 0x88], 0x40000000
        ///     14163BA19  TEST byte ptr [RDI + 0x8D], 0x4
        ///
        /// What the pattern returns:
        ///     This returns the displacement operand from TEST byte ptr
        ///     [RDI + 0x8C], 1. The object in RDI came from a tree node payload
        ///     at node +0x28 while iterating the AreaInstance entity tree, so this
        ///     is an Entity-owned status/validity byte.
        ///
        /// How Id is derived:
        ///     The next instruction in the same compact filter compares
        ///     dword ptr [RDI + 0x88] with 0x40000000. The integration test reads
        ///     that adjacent displacement from the same pattern and verifies the
        ///     live player id matches the managed object model.
        ///
        /// Nuance:
        ///     This code proves the field ownership and the actual game-side
        ///     predicate used by this filter:
        ///
        ///         (status & 0x01) == 0
        ///         id < 0x40000000
        ///         (activeFlags & 0x04) != 0
        ///
        ///     It still does not name every enum value of the status byte. The
        ///     live player currently observes status 0x0C, but production uses
        ///     the recovered predicate rather than an exact status equality.
        /// </summary>
        public const string EntityIdentityFilter = nameof(EntityIdentityFilter);

        /// <summary>
        /// Finds the Entity virtual accessor that returns the embedded EntityDetails name/path string.
        ///
        /// Recovered values for build sha256-c5da3833:
        ///     ItemStruct.EntityDetailsPtr: 0x08
        ///     EntityDetails.name:          0x08
        ///
        /// Ghidra location:
        ///     live player Entity vtable static VA 0x1433FE2F8
        ///     vtable slot +0x10 -> VA 0x141C7A6D0
        ///
        /// Relevant disassembly:
        ///     141C7A6D0  MOV RAX, qword ptr [RCX + 0x08]
        ///     141C7A6D4  ADD RAX, 0x08
        ///     141C7A6D8  RET
        ///
        /// What the pattern returns:
        ///     This returns the immediate operand from ADD RAX, 0x08. That is
        ///     the offset of the StdWString path/name field inside EntityDetails.
        ///
        /// Why this is a strong keypoint:
        ///     The function is reached from the live player entity vtable and
        ///     has exactly the accessor shape expected for:
        ///
        ///         return &(*(Entity + EntityDetailsPtr)).name;
        ///
        ///     The integration test starts from the pattern-backed local player
        ///     entity, reads the recovered StdWString, and compares it with
        ///     TheGame.Player.Path.
        /// </summary>
        public const string EntityDetailsName = nameof(EntityDetailsName);

        /// <summary>
        /// Finds the generic entity component resolver shape.
        ///
        /// Recovered values for build sha256-c5da3833:
        ///     ItemStruct.EntityDetailsPtr:                         0x08
        ///     ItemStruct.ComponentListPtr:                         0x10
        ///     EntityDetails.ComponentLookUpPtr:                    0x28
        ///     ComponentLookUpStruct.ComponentsNameAndIndex:        0x28
        ///     ComponentLookUpStruct.ComponentsNameAndIndex.Data.Last sentinel: 0x30
        ///     ComponentNameAndIndexStruct.Index:                   0x08
        ///
        /// Ghidra location:
        ///     FUN_1401512E0
        ///
        /// Relevant decompile:
        ///     details = *(Entity + 0x08)
        ///     lookup = *(details + 0x28)
        ///     entry = find(lookup + 0x28, &componentName)
        ///     if (entry != *(lookup + 0x30) && *(int *)(entry + 0x08) != -1)
        ///         return *(IntPtr *)(*(Entity + 0x10) + index * 8)
        ///
        /// Why this is a strong keypoint:
        ///
        ///     This is the game's own named-component resolver. It proves the
        ///     entity details pointer, component vector, lookup pointer, lookup
        ///     bucket, bucket end sentinel, and name/index element layout in a
        ///     single semantic function.
        /// </summary>
        public const string EntityComponentLookupShape = nameof(EntityComponentLookupShape);

        /// <summary>
        /// Finds the entry stride for the component-name/index bucket entries.
        ///
        /// Recovered values for build sha256-c5da3833:
        ///     ComponentNameAndIndexStruct.NamePtr: 0x00
        ///     ComponentNameAndIndexStruct size:    0x10
        ///
        /// Ghidra location:
        ///     FUN_140163120
        ///
        /// Relevant decompile:
        ///     entry = firstEntry + hashSlotEntryIndex * 0x10
        ///     if (*(longlong *)componentNameParam == *(longlong *)entry)
        ///         return entry
        ///
        /// Relevant disassembly:
        ///     1401631EF  MOV EDX, dword ptr [RSI + RCX*8 + 0x04]
        ///     1401631F3  SHL RDX, 0x04
        ///     1401631F7  ADD RDX, RDI
        ///     1401631FA  MOV RCX, qword ptr [RDX]
        ///     1401631FD  CMP qword ptr [RBX], RCX
        ///
        /// What the pattern returns:
        ///     The caret lands on the shift amount in SHL RDX, 0x04. Runtime
        ///     converts it to entrySize = 1 &lt;&lt; 4 = 0x10 and verifies the
        ///     following MOV RCX, [RDX] has no displacement, proving the name
        ///     pointer is at entry +0x00.
        ///
        /// Why this is a strong keypoint:
        ///     This is the bucket helper called by the named-component resolver.
        ///     It computes the actual address returned to the resolver, so it is
        ///     the right place to recover the entry stride instead of depending
        ///     on the managed ComponentNameAndIndexStruct size.
        /// </summary>
        public const string ComponentNameAndIndexEntryStride = nameof(ComponentNameAndIndexEntryStride);

        /// <summary>
        /// Finds the Player component formatter path that reads the component
        /// owner entity through the generic component header.
        ///
        /// Recovered values for build sha256-c5da3833:
        ///     ComponentHeader.StaticPtr: 0x00
        ///     ComponentHeader.EntityPtr: 0x08
        ///
        /// Ghidra location:
        ///     Player component vtable slot +0x68 -> FUN_141D267F0
        ///     owner read at VA 0x141D26810
        ///
        /// Relevant disassembly:
        ///     141D2680A  MOV R14, RCX
        ///     141D2680D  MOV RDI, RDX
        ///     141D26810  MOV RCX, qword ptr [RCX + 0x08]
        ///     141D26814  CALL FUN_140E60A90
        ///
        /// Why it matters:
        ///     The managed ComponentHeader is embedded at the start of Player
        ///     and Life components. The keypoint proves that a concrete Player
        ///     component virtual method treats the field at +0x08 as the owner
        ///     entity pointer. The integration test also reads the qword at
        ///     component +0x00 as the vtable and verifies vtable slot +0x68
        ///     points back to this formatter.
        /// </summary>
        public const string PlayerComponentHeaderOwnerEntity = nameof(PlayerComponentHeaderOwnerEntity);

        /// <summary>
        /// Finds the Player component debug/info formatter that prints the character name.
        ///
        /// Recovered values for build sha256-c5da3833:
        ///     PlayerOffsets.Name:       0x1B0
        ///     StdWString.Buffer:        0x00
        ///     StdWString.InlineBuffer:  0x00
        ///     StdWString.Length:        0x10
        ///     StdWString.Capacity:      0x18
        ///     StdWString.SSO limit:     7
        ///
        /// Ghidra location:
        ///     Live Player component vtable RVA: 0x32C4758
        ///     Vtable slot 0x68 -> FUN_141D267F0
        ///
        /// Relevant decompile:
        ///     name = (StdWString *)(Player + 0x1B0)
        ///     print(L"Character Name: ")
        ///     if (7 < *(ulonglong *)(Player + 0x1C8))
        ///         name = *(StdWString.Buffer *)(Player + 0x1B0)
        ///     append(name, *(undefined8 *)(Player + 0x1C0))
        ///
        /// Why this is a strong keypoint:
        ///     The function was reached from the live Player component vtable,
        ///     and the immediate string label is unique: "Character Name: ".
        ///     The code uses the normal small-string optimization guard. The
        ///     external branch executes MOV RBX, [RBX], proving buffer +0x00.
        ///     The inline branch passes RBX directly, proving inline buffer
        ///     +0x00. The same block proves length/capacity and the limit 7.
        /// </summary>
        public const string PlayerComponentName = nameof(PlayerComponentName);

        /// <summary>
        /// Finds the Life component function that walks the component-owned vital objects.
        ///
        /// Recovered values for build sha256-c5da3833:
        ///     LifeOffset.Health:       0x1A8
        ///     LifeOffset.Mana:         0x200
        ///     LifeOffset.EnergyShield: 0x240
        ///
        /// Ghidra location:
        ///     Live Life component vtable RVA: 0x33EE948
        ///     Vtable slot 0x98 -> FUN_141CE1C00
        ///
        /// Relevant decompile:
        ///     FUN_141ce1860(Life + 0x1A8, ...)
        ///     FUN_141cdeb60(Life + 0x200, ...)
        ///     FUN_141ce1860(Life + 0x240, ...)
        ///
        /// Why this is a strong keypoint:
        ///     The function was reached from the live Life component vtable, not
        ///     from a broad search for common displacements. It treats these
        ///     offsets as embedded vital-like objects in the Life component.
        /// </summary>
        public const string LifeComponentVitalOffsets = nameof(LifeComponentVitalOffsets);

        /// <summary>
        /// Finds the Life debug/info formatter that prints current/total values
        /// for Life, Energy Shield, and Mana.
        ///
        /// Recovered values for build sha256-c5da3833:
        ///     VitalStruct.Total:   0x34
        ///     VitalStruct.Current: 0x38
        ///
        /// Ghidra location:
        ///     Live Life component vtable RVA: 0x33EE948
        ///     Vtable slot 0x68 -> FUN_141CE2300
        ///
        /// Relevant decompile:
        ///     "Life: " reads *(Life + 0x1E0) / *(Life + 0x1DC)
        ///     "ES: "   reads *(Life + 0x278) / *(Life + 0x274)
        ///     "Mana: " reads *(Life + 0x238) / *(Life + 0x234)
        ///
        /// Subtracting the vital-object starts recovered by
        /// <see cref="LifeComponentVitalOffsets"/> gives:
        ///     Current = absoluteCurrent - vitalStart = 0x38
        ///     Total   = absoluteTotal   - vitalStart = 0x34
        /// </summary>
        public const string LifeVitalCurrentTotal = nameof(LifeVitalCurrentTotal);

        /// <summary>
        /// Finds the vital deserializer helper that reads flat and percent reservation fields.
        ///
        /// Recovered values for build sha256-c5da3833:
        ///     VitalStruct.ReservedFlat:    0x18
        ///     VitalStruct.ReservedPercent: 0x1C
        ///
        /// Ghidra location:
        ///     FUN_141CE13A0
        ///
        /// Relevant decompile:
        ///     *(undefined4 *)(Vital + 0x18) = *(undefined4 *)(stream + offset)
        ///     *(int *)(Vital + 0x1C) = (int)*(short *)(stream + offset + 4)
        ///
        /// Why this is a strong keypoint:
        ///     FUN_141CDEB60 calls this helper while deserializing a VitalStruct
        ///     after writing Vital +0x38, and FUN_141CE1860 calls the same path
        ///     for the Health/EnergyShield-style vital objects. It proves these
        ///     are VitalStruct-relative fields, not Life-component absolute
        ///     offsets.
        /// </summary>
        public const string LifeVitalReservationOffsets = nameof(LifeVitalReservationOffsets);

        /// <summary>
        /// Finds the Life component constructor block that initializes the embedded
        /// vital objects.
        ///
        /// Recovered values for build sha256-c5da3833:
        ///     LifeOffset.Health:       0x1A8
        ///     LifeOffset.Mana:         0x200
        ///     LifeOffset.EnergyShield: 0x240
        ///     VitalStruct.UnknownStatId0:   0x08
        ///     VitalStruct.UnknownStatId1:   0x0C
        ///     VitalStruct.LifeComponentPtr: 0x10
        ///     VitalStruct.UnknownStatId3:   0x28
        ///
        /// Ghidra location:
        ///     FUN_141CDC130
        ///
        /// Relevant decompile:
        ///     Health:
        ///       plVar1 = Life + 0x1A8
        ///       *(dword *)(plVar1 + 0x08) = 0x3334
        ///       *(dword *)(plVar1 + 0x0C) = 0x333B
        ///       *(dword *)(plVar1 + 0x28) = 0x759
        ///
        ///     Mana:
        ///       plVar2 = Life + 0x200
        ///       *(dword *)(plVar2 + 0x08) = 0x6986
        ///       *(dword *)(plVar2 + 0x0C) = 0x6986
        ///       *(dword *)(plVar2 + 0x28) = 0x1C9C
        ///
        ///     Energy Shield:
        ///       plVar3 = Life + 0x240
        ///       FUN_141CD0640(plVar3, Life, 0xF1, 0x586, 0x4DE5, ...)
        ///       FUN_141CD0640 writes its fifth argument to Vital +0x28.
        ///
        /// Why this matters:
        ///     This proves the old "Regeneration" interpretation of Vital +0x28
        ///     was not defensible. The constructor writes integer stat-like
        ///     immediates there; it is not a live float regeneration amount.
        /// </summary>
        public const string LifeVitalConstructorShape = nameof(LifeVitalConstructorShape);

        /// <summary>
        /// Finds the shared vital constructor helper used for Energy Shield and other
        /// Life-owned vital-like objects.
        ///
        /// Recovered values for build sha256-c5da3833:
        ///     VitalStruct.LifeComponentPtr: 0x10
        ///     VitalStruct.UnknownStatId0:   0x08
        ///     VitalStruct.UnknownStatId1:   0x0C
        ///     VitalStruct.TotalStatId:      0x20
        ///     VitalStruct.UnknownStatId2:   0x24
        ///     VitalStruct.UnknownStatId3:   0x28
        ///
        /// Ghidra location:
        ///     FUN_141CD0640
        ///
        /// Relevant decompile:
        ///     *(qword *)(Vital +0x10) = LifeComponent
        ///     *(dword *)(Vital +0x28) = stackArg0
        ///     *(dword *)(Vital +0x08) = 0x6986
        ///     *(dword *)(Vital +0x0C) = 0x6986
        ///     *(dword *)(Vital +0x20) = totalStatId
        ///     *(dword *)(Vital +0x24) = unknownStatId2
        ///
        /// Why this exists separately:
        ///     Health and Mana have direct inline stores in FUN_141CDC130, but
        ///     Energy Shield is initialized through this helper. Keeping a
        ///     helper keypoint avoids pretending the ES +0x08/+0x0C defaults
        ///     were recovered from the direct Life constructor block.
        /// </summary>
        public const string LifeVitalSharedConstructorShape = nameof(LifeVitalSharedConstructorShape);
    }

    /// <summary>
    /// Patterns required to bootstrap the live object model.
    ///
    /// Current production consumer:
    ///
    ///     TheGame.UpdateData()
    ///
    /// Current entries:
    ///
    ///     GameStates
    ///
    /// Keep this list small and high confidence. These patterns are used to find
    /// static addresses needed by the main object model. More experimental or
    /// explanatory keypoints belong in <see cref="KeypointPatterns"/>.
    /// </summary>
    public static readonly IBytePattern[] Patterns =
    {
        // GameStates singleton global slot
        // -------------------------------
        //
        // What this finds:
        //
        //     The lazy getter/initializer for the game-state owner singleton.
        //
        // Ghidra build evidence:
        //
        //     Build folder:
        //       docs/PoE/RE/builds/sha256-c5da3833/PathOfExileSteam/
        //
        //     Binary:
        //       binary/PathOfExileSteam.exe
        //
        //     Evidence note:
        //       game-states/GameStatesSingleton.evidence.md
        //
        //     Function:
        //       FUN_14010c350
        //
        //     Relevant addresses in copied binary:
        //       function start:        0x14010C350
        //       pattern starts:        0x14010C365
        //       CMP instruction:       0x14010C36E
        //       caret/displacement:    0x14010C371
        //       resolved global slot:  0x1444E9EB8
        //
        // Relevant disassembly:
        //
        //     14010C365  SUB RSP, 0x40
        //     14010C369  MOV RSI, RCX
        //     14010C36C  XOR EBP, EBP
        //     14010C36E  CMP qword ptr [0x1444E9EB8], RBP
        //
        // The instruction is really RIP-relative:
        //
        //     48 39 2D <rel32>
        //
        // Ghidra renders the resolved absolute VA, but the executable bytes only
        // contain the rel32 displacement. The caret is placed on that rel32.
        //
        // Runtime math:
        //
        //     displacementAddress = moduleBase + patternRva
        //     relativeOffset      = memory.Read<int>(displacementAddress)
        //     globalSlot          = displacementAddress + relativeOffset + 4
        //
        // Live test result for build sha256-c5da3833:
        //
        //     patternRva = 0x10C371
        //     globalSlot = 0x7FF775419EB8
        //
        // The copied binary's image base is 0x140000000, so the static RVA is:
        //
        //     0x1444E9EB8 - 0x140000000 = 0x44E9EB8
        //
        // Why the pattern starts at SUB instead of the CMP:
        //
        //     The CMP alone is too generic: many globals are checked through
        //     RIP-relative operands. The short function prologue gives the match
        //     enough local shape while still wildcarding the volatile stack size
        //     byte.
        //
        // What to do on a new client version:
        //
        //     1. Search for this byte pattern in Ghidra.
        //     2. If it still matches once, decompile the containing function.
        //     3. Confirm the function lazily allocates a 0x140-ish object, writes
        //        the state owner vtable, initializes the state table, and returns
        //        a two-pointer shared object through param_1.
        //     4. Re-run ShouldResolveGameStatesSingletonFromSignatureKeypoint.
        //
        // If it no longer matches:
        //
        //     Search for a function with the same semantic shape:
        //       - checks a global singleton slot for null,
        //       - allocates the state owner,
        //       - calls the table constructor helper with stride/count,
        //       - stores the owner into a global,
        //       - returns owner/ref-sidecar through an out pointer.
        BytePattern.FromTemplate(
            "48 83 EC ?? 48 8B F1 33 ED 48 39 2D ^ ?? ?? ?? ??",
            nameof(GameStates)
        ),
    };

    /// <summary>
    /// Secondary RE keypoints used by tests and future runtime offset derivation.
    ///
    /// These patterns are not currently required for normal bot startup. They are
    /// executable documentation: each one lets us re-derive a hardcoded struct
    /// fact from actual game code.
    ///
    /// Patterns here should be short enough to survive ordinary patch churn and
    /// specific enough to recover a semantic fact. If a pattern needs a huge
    /// unrelated tail to become unique, prefer deriving the fact from stronger
    /// nearby keypoints instead.
    /// </summary>
    public static readonly IBytePattern[] KeypointPatterns =
    {
        // GameStateStaticWrapper
        // ----------------------
        //
        // What this finds:
        //
        //     The tail of FUN_14010C350 where the singleton getter copies two
        //     globals into its caller-provided output buffer.
        //
        // Ghidra evidence:
        //
        //     Function:
        //       FUN_14010C350
        //
        //     Important instruction sequence:
        //
        //       14010C4A8  MOV RAX, qword ptr [0x1444E9EB8]
        //       14010C4AF  MOV qword ptr [RSI], RAX
        //       14010C4B2  MOV RAX, qword ptr [0x1444E9EC0]
        //       14010C4B9  MOV qword ptr [RSI + 0x08], RAX
        //
        // Caret location:
        //
        //     ^ 48 89 06
        //
        // This returns the start of the implicit-offset owner store. The first
        // store is [RSI], so the owner pointer offset is zero. The second store
        // has an explicit +0x08 displacement at returnedRva +0x0D.
        //
        // Runtime extraction:
        //
        //     ownerGlobalSlot   = ResolveRipRelativeSlot(returnedRva - 0x04)
        //     sidecarGlobalSlot = ResolveRipRelativeSlot(returnedRva + 0x06)
        //     ownerStoreOpcode  = bytes at returnedRva      // 48 89 06
        //     sidecarOffset     = byte at returnedRva+0x0D  // 0x08
        BytePattern.FromTemplate(
            "48 89 2E 48 89 6E 08 48 8B 05 ?? ?? ?? ?? 48 85 C0 74 ?? F0 FF 40 08 48 8B 05 ?? ?? ?? ?? ^ 48 89 06 48 8B 05 ?? ?? ?? ?? 48 89 46 08",
            KeypointNames.GameStateStaticWrapper
        ),

        // GameStateTableShape
        // -------------------
        //
        // What this finds:
        //
        //     The state table constructor call inside FUN_14010c350.
        //
        // Why this is a stable keypoint:
        //
        //     It is not a random access to +0x48. It is the code that constructs
        //     the table itself, and it supplies:
        //
        //       RCX = owner + tableOffset
        //       RDX = entry size
        //       R8  = entry count
        //       R9  = element constructor
        //       stack arg = element destructor/cleanup
        //
        // Disassembly for build sha256-c5da3833:
        //
        //     14010C3C0  LEA RCX, [RDI + 0x48]
        //     14010C3C4  LEA RAX, [0x140107E50]
        //     14010C3CB  MOV qword ptr [RSP + 0x20], RAX
        //     14010C3D0  LEA R9, [0x14010C550]
        //     14010C3D7  MOV EDX, 0x10
        //     14010C3DC  MOV R8D, 0x0D
        //     14010C3E2  CALL 0x142AD9C6C
        //
        // The helper at 0x142AD9C6C simply walks count entries and invokes the
        // constructor every stride bytes:
        //
        //     for i in 0..count:
        //         ctor(table + i * stride)
        //
        // Caret location:
        //
        //     48 8D 4F ^ 48
        //
        // This is the one-byte displacement in LEA RCX, [RDI + 0x48].
        //
        // Runtime extraction used by the integration test:
        //
        //     tableShapeAddress = moduleBase + returnedRva
        //     stateTableOffset  = memory.Read<byte>(tableShapeAddress)       // 0x48
        //     stateEntrySize    = memory.Read<int>(tableShapeAddress + 21)   // 0x10
        //     stateCount        = memory.Read<int>(tableShapeAddress + 27)   // 0x0D
        //
        // Why +21 and +27 are acceptable here:
        //
        //     They are offsets inside this specific keypoint pattern, not struct
        //     offsets in the game object model. The test proves they still point
        //     at MOV EDX, imm32 and MOV R8D, imm32 by validating the recovered
        //     values against the live object chain.
        //
        // What to inspect in a new version:
        //
        //     If the pattern breaks, decompile the GameStates singleton function
        //     and locate the call equivalent to FUN_142AD9C6C. Re-anchor on the
        //     instruction sequence that passes table pointer, stride, and count.
        BytePattern.FromTemplate(
            "48 8D 4F ^ 48 48 8D 05 ?? ?? ?? ?? 48 89 44 24 20 4C 8D 0D ?? ?? ?? ?? BA 10 00 00 00 41 B8 0D 00 00 00 E8",
            KeypointNames.GameStateTableShape
        ),

        // GameStateCurrentStateVector
        // ---------------------------
        //
        // What this finds:
        //
        //     The GameStates owner dirty-copy path which copies the source
        //     current-state vector into the dispatch vector.
        //
        // Ghidra evidence:
        //
        //     Function:
        //       FUN_1415B0360
        //
        //     Unique pattern location:
        //       pattern starts at 0x1415B03D9
        //       source/current vector First displacement is at 0x1415B03EB
        //
        //     Important instruction sequence:
        //
        //       1415B03D9  CMP byte ptr [R15 + 0x40], 0x0
        //       1415B03E4  MOV RDI, qword ptr [R15 + 0x10]
        //       1415B03E8  SUB RDI, qword ptr [R15 + 0x08]
        //       1415B03EC  SAR RDI, 0x4
        //       1415B03F0  MOV RBX, qword ptr [R15 + 0x28]
        //       1415B03F4  MOV RDX, qword ptr [R15 + 0x20]
        //
        // Caret location:
        //
        //     49 2B 7F ^ 08
        //
        // This is the one-byte displacement in SUB RDI, [R15 + disp8]. It is
        // the First field of the source vector used by the managed
        // GameStateOffset.CurrentStatePtr field.
        //
        // Runtime extraction:
        //
        //     currentVectorFirstOffset  = memory.Read<byte>(moduleBase + returnedRva)        // 0x08
        //     currentVectorLastOffset   = memory.Read<byte>(moduleBase + returnedRva - 0x04) // 0x10
        //     dispatchVectorLastOffset  = memory.Read<byte>(moduleBase + returnedRva + 0x08) // 0x28
        //     dispatchVectorFirstOffset = memory.Read<byte>(moduleBase + returnedRva + 0x0C) // 0x20
        //     dispatchVectorEndOffset   = dispatchVectorLastOffset + pointer size           // 0x30
        //     dirtyFlagOffset           = memory.Read<byte>(moduleBase + returnedRva - 0x0F) // 0x40
        //
        // Why this is a strong keypoint:
        //
        //     It starts from the singleton-resolved GameStates owner in R15,
        //     checks the owner's dirty flag, computes the source vector element
        //     count by subtracting [owner +0x08] from [owner +0x10], then copies
        //     0x10-byte entries into the dispatch vector at +0x20. Several
        //     update callers around FUN_1415B2890/FUN_1415B2B00/FUN_1415B2C70
        //     iterate the dispatch vector and then call this dirty-copy helper.
        BytePattern.FromTemplate(
            "41 80 7F 40 00 0F 84 ?? ?? ?? ?? 49 8B 7F 10 49 2B 7F ^ 08 48 C1 FF 04 49 8B 5F 28 49 8B 57 20 48 8B CB 48 2B CA 48 C1 F9 04 48 3B F9",
            KeypointNames.GameStateCurrentStateVector
        ),

        // GameStateVirtualUpdateSlot
        // --------------------------
        //
        // What this finds:
        //
        //     The virtual update call slot used by the state dispatch loop.
        //
        // Ghidra evidence:
        //
        //     Function:
        //       FUN_1415B2890
        //
        //     Important instruction sequence:
        //
        //       1415B2A48  MOV    RAX, [R14]
        //       1415B2A4B  MOVAPS XMM2, XMM6
        //       1415B2A4E  MOV    RDX, R12
        //       1415B2A51  MOV    RCX, R14
        //       1415B2A54  CALL   qword ptr [RAX + 0x8]
        //       1415B2A57  CMP    byte ptr [R14 + 0x20A], 0
        //
        // Caret location:
        //
        //     FF 50 ^ 08
        //
        // This is the one-byte displacement in CALL qword ptr [RAX + disp8].
        //
        // Runtime use:
        //
        //     The provider scans the recovered state table and selects the entry
        //     whose vtable slot +0x08 reaches the pattern-backed InGameState
        //     MsElapsed writer through a direct relative CALL/JMP. This is a
        //     stronger InGame-state identity check than relying only on enum
        //     index 4.
        BytePattern.FromTemplate(
            "4D 85 F6 74 ?? 49 8B 06 0F 28 D6 49 8B D4 49 8B CE FF 50 ^ 08 41 80 BE ?? ?? ?? ?? 00",
            KeypointNames.GameStateVirtualUpdateSlot
        ),

        // InGameStateAreaInstanceData
        // ---------------------------
        //
        // What this finds:
        //
        //     The InGameState field displacement used to read the current
        //     AreaInstance pointer.
        //
        // Ghidra evidence:
        //
        //     Function:
        //       FUN_140233C10
        //
        //     Unique pattern location:
        //       pattern starts at 0x140233CA7
        //       displacement is at 0x140233CB3
        //
        //     Important instruction sequence:
        //
        //       140233CA7  TEST R14, R14
        //       140233CAA  JZ 0x140233DFE
        //       140233CB0  MOV RBX, [R14 + 0x290]
        //       140233CB7  TEST RBX, RBX
        //
        // Caret location:
        //
        //     49 8B 9E ^ ?? ?? ?? ??
        //
        // This is the four-byte displacement in MOV RBX, [R14 + disp32].
        //
        // Runtime extraction:
        //
        //     areaInstanceOffset = memory.Read<int>(moduleBase + returnedRva) // 0x290
        //
        // This is NOT the final AreaInstance address. The final live pointer is:
        //
        //     areaInstanceAddress = memory.Read<IntPtr>(inGameStateAddress + areaInstanceOffset)
        //
        // Test proof:
        //
        //     ShouldResolveInGameAreaKeypointsFromCodePatterns derives 0x290
        //     from this pattern, reads:
        //
        //         *(IntPtr*)(inGameState + 0x290)
        //
        //     and verifies it matches InGameStateOffset.AreaInstanceData.
        //
        // Nuance:
        //
        //     This proves only the pointer from InGameState to AreaInstance.
        //     It does NOT prove the internal AreaInstanceOffsets fields.
        //
        //     During this pass, broad Ghidra searches for AreaInstance-ish
        //     displacements like 0x588 and 0x6C8 produced many unrelated hits.
        //     That means the next layer should start from the area object's vtable
        //     and functions that are known to receive/use the current area object,
        //     not from raw displacement searches.
        //
        // Suspicious/important AreaInstance nuance:
        //
        //     An earlier draft of AreaInstanceOffsets claimed an object field
        //     at +0xB38. Ghidra evidence in FUN_1402332D0 shows a virtual call
        //     through vtable + 0xB38:
        //
        //         CALL qword ptr [vtable + 0xB38]
        //
        //     That is not the same thing as reading an object field at +0xB38.
        //     The unmanaged field was removed from the required offset surface.
        //     Do not reintroduce it without a separate object-field keypoint.
        //
        // What to inspect in a new version:
        //
        //     If this pattern fails, locate the function that:
        //
        //       1. Resolves the InGame state entry from GameStates.
        //       2. Null-checks that state pointer.
        //       3. Reads one pointer from it and immediately treats that pointer
        //          as the current area instance.
        //
        //     The displacement on that read is the new AreaInstanceData offset.
        BytePattern.FromTemplate(
            "4D 85 F6 0F 84 ?? ?? ?? ?? 49 8B 9E ^ ?? ?? ?? ?? 48 85 DB",
            KeypointNames.InGameStateAreaInstanceData
        ),

        // InGameStateMsElapsedWriterFunctionStart
        // ---------------------------------------
        //
        // What this finds:
        //
        //     The entry point of the InGameState elapsed/tick writer:
        //     FUN_140FAD6E0.
        //
        // Why this is separate from InGameStateMsElapsed:
        //
        //     InGameStateMsElapsed lands on the displacement operand for:
        //
        //         MOV dword ptr [RCX + 0x400], EAX
        //
        //     That is the correct place to read the field offset. It is not the
        //     correct place to derive the owning function start by subtracting a
        //     fixed byte distance. This start-anchored pattern gives the runtime
        //     provider the function address directly and keeps the displacement
        //     pattern focused on the field.
        //
        // Ghidra evidence, 2026-06-13:
        //
        //     Ghidra search_byte_patterns found this signature only at:
        //
        //         0x140FAD6E0
        //
        //     The signature starts at the function prologue and includes the
        //     elapsed double accumulation plus the final +0x400 dword write:
        //
        //         140FAD6E0  PUSH RBX
        //         140FAD6E2  SUB RSP, 0x90
        //         ...
        //         140FAD708  CVTSS2SD XMM0, XMM6
        //         140FAD70F  ADDSD XMM0, qword ptr [RCX + 0x408]
        //         140FAD717  MOVSD qword ptr [RCX + 0x408], XMM0
        //         140FAD728  CVTTSD2SI RAX, XMM0
        //         140FAD72C  MOV dword ptr [RCX + 0x400], EAX
        //
        // Runtime use:
        //
        //     msElapsedWriterFunctionAddress =
        //         moduleBase + keypointRva[InGameStateMsElapsedWriterFunctionStart]
        //
        // Caret note:
        //
        //     There is intentionally no caret in this template. With no caret,
        //     BytePattern.FromTemplate returns the match start, which is exactly
        //     the function entry. The parser rejects a caret at byte zero as
        //     redundant.
        //
        //     the FF/runtime-layout tests then verify the live InGameState update
        //     vtable slot branches to this function.
        BytePattern.FromTemplate(
            "40 53 48 81 EC ?? ?? ?? ?? 0F 57 C0 48 89 AC 24 ?? ?? ?? ?? 4C 89 7C 24 ?? 33 ED 0F 29 74 24 ?? 4C 8B FA 0F 28 F2 33 D2 F3 0F 5A C6 48 8B D9 F2 0F 58 81 08 04 00 00 F2 0F 11 81 08 04 00 00 F2 0F 59 05 ?? ?? ?? ?? F2 48 0F 2C C0 89 81 00 04 00 00 E8",
            KeypointNames.InGameStateMsElapsedWriterFunctionStart
        ),

        // InGameStateMsElapsed
        // --------------------
        //
        // What this finds:
        //
        //     The direct 32-bit elapsed/tick field written by the InGameState
        //     update path.
        //
        // Ghidra evidence:
        //
        //     Function:
        //       FUN_140FAD6E0
        //
        //     Unique pattern location:
        //       pattern starts at 0x140FAD708
        //       displacement is at 0x140FAD72E
        //
        //     Important instruction/decompile sequence:
        //
        //       140FAD708  CVTSS2SD XMM0, XMM6
        //       140FAD70F  ADDSD XMM0, qword ptr [RCX + 0x408]
        //       140FAD717  MOVSD qword ptr [RCX + 0x408], XMM0
        //       140FAD71F  MULSD XMM0, qword ptr [0x14352CB18]
        //       140FAD728  CVTTSD2SI RAX, XMM0
        //       140FAD72C  MOV dword ptr [RCX + 0x400], EAX
        //
        // Caret location:
        //
        //     89 81 ^ ?? ?? ?? ??
        //
        // This is the four-byte displacement in MOV dword ptr [RCX + disp32], EAX.
        //
        // Runtime extraction:
        //
        //     msElapsedOffset = memory.Read<int>(moduleBase + returnedRva) // 0x400
        //
        // Why this is a strong keypoint:
        //
        //     The field is proven by the writer, not by a broad displacement
        //     search. The nearby +0x408 double accumulation explains why the
        //     +0x400 dword is monotonically increasing in live reads.
        //
        // Vtable/update-chain nuance:
        //
        //     The live InGameState vtable slot +0x8 is not FUN_140FAD6E0
        //     directly. It points at FUN_140FAD1A0, an update wrapper. Ghidra
        //     decompiles that wrapper as calling FUN_140FAD6E0, and the live
        //     module currently emits that edge as a relative tail JMP at wrapper
        //     +0xD9. The integration test
        //     ShouldResolveMsElapsedWriterFromInGameStateUpdateVtableChain
        //     verifies this without runtime injection or breakpoints by scanning the live
        //     slot function for a relative CALL/JMP to the writer resolved from
        //     InGameStateMsElapsedWriterFunctionStart.
        //
        // Runtime confirmation from the 2026-06-13 live client:
        //
        //     InGameState  = 0x0000043C14E62A10
        //     MsElapsed    = 0x0000043C14E62E10
        //     first dword  = 0x00023A61
        //     later dword  = 0x00024267
        //
        // Hardware-watchpoint note:
        //
        //     Hardware watchpoints against this hot field crashed the live client
        //     even when armed for a single thread and configured to disarm after
        //     one hit. Use this static writer keypoint instead.
        BytePattern.FromTemplate(
            "F3 0F 5A C6 48 8B D9 F2 0F 58 81 08 04 00 00 F2 0F 11 81 08 04 00 00 F2 0F 59 05 ?? ?? ?? ?? F2 48 0F 2C C0 89 81 ^ ?? ?? ?? ?? E8",
            KeypointNames.InGameStateMsElapsed
        ),

        // AreaInstanceCurrentAreaLevel
        // ----------------------------
        //
        // What this finds:
        //
        //     A direct current-area level read from the current AreaInstance.
        //
        // Ghidra evidence:
        //
        //     Label-less analyzed block:
        //       pattern starts at 0x140FB33F0
        //       current-area-level displacement is at 0x140FB33FB
        //
        //     Important instruction sequence:
        //
        //       140FB33F0  MOV RAX, qword ptr [R12 + 0x290]
        //       140FB33F8  MOVZX ECX, byte ptr [RAX + 0xC4]
        //       140FB33FF  MOV dword ptr [0x14434BAC8], ECX
        //
        // Caret location:
        //
        //     0F B6 88 ^ ?? ?? ?? ??
        //
        // This is the four-byte displacement in MOVZX ECX, byte ptr [RAX + disp32].
        //
        // Why the preceding displacement is wildcarded:
        //
        //     The first instruction uses the already-proven
        //     InGameState.AreaInstanceData field. The test reads that operand at
        //     caret - 7 and requires it to equal the independently recovered
        //     AreaInstanceData offset. If a later patch moves the area pointer
        //     inside InGameState but preserves this code shape, the keypoint can
        //     still recover both offsets.
        //
        // Runtime extraction:
        //
        //     areaInstanceDataOffset = memory.Read<int>(moduleBase + returnedRva - 7) // 0x290
        //     currentAreaLevelOffset = memory.Read<int>(moduleBase + returnedRva)     // 0x0C4
        BytePattern.FromTemplate(
            "49 8B 84 24 ?? ?? ?? ?? 0F B6 88 ^ ?? ?? ?? ?? 89 0D ?? ?? ?? ??",
            KeypointNames.AreaInstanceCurrentAreaLevel
        ),

        // AreaInstanceCurrentAreaHash
        // ---------------------------
        //
        // What this finds:
        //
        //     A current-area hash read through the current-area holder object.
        //
        // Ghidra evidence:
        //
        //     Function:
        //       FUN_142061AD0
        //
        //     Unique pattern location:
        //       pattern starts at 0x1420607BB
        //       hash displacement is at 0x1420607C4
        //
        //     Important instruction sequence:
        //
        //       1420607BB  MOV RCX, qword ptr [RDI + 0x2170]
        //       1420607C2  MOV EAX, dword ptr [RCX + 0x11C]
        //       1420607C8  MOV dword ptr [RSP + 0x58], EAX
        //       1420607CC  MOV EAX, dword ptr [RCX + 0x120]
        //       1420607D2  MOV dword ptr [RSP + 0x5C], EAX
        //       1420607D6  LEA RDX, [RSP + 0x40]
        //       1420607DB  MOV RCX, RDI
        //       1420607DE  CALL FUN_14205F360
        //
        // Caret location:
        //
        //     8B 81 ^ ?? ?? ?? ??
        //
        // This is the four-byte displacement in MOV EAX, dword ptr [RCX + disp32].
        //
        // Why the holder path is accepted:
        //
        //     FUN_14205D6D0 constructs this holder and explicitly links it to
        //     the AreaInstance:
        //
        //       holder + 0x2170      = AreaInstance
        //       AreaInstance + 0x580 = holder
        //
        //     That AreaInstance back pointer is immediately before the proven
        //     LocalPlayers vector at +0x588. This is stronger than the broad
        //     +0x11C dword reads, which appear in several unrelated contexts.
        //
        // Runtime extraction:
        //
        //     holderAreaInstanceOffset = memory.Read<int>(moduleBase + returnedRva - 6) // 0x2170
        //     currentAreaHashOffset    = memory.Read<int>(moduleBase + returnedRva)     // 0x011C
        BytePattern.FromTemplate(
            "48 8B 8F ?? ?? ?? ?? 8B 81 ^ ?? ?? ?? ?? 89 44 24 ?? 8B 81 ?? ?? ?? ?? 89 44 24 ?? 48 8D 54 24 ?? 48 8B CF E8 ?? ?? ?? ??",
            KeypointNames.AreaInstanceCurrentAreaHash
        ),

        // AreaInstanceLocalPlayers
        // ------------------------
        //
        // What this finds:
        //
        //     The AreaInstance accessor that appends non-null entries from
        //     AreaInstance + 0x588..0x590 into a caller-provided output vector.
        //
        // Ghidra evidence:
        //
        //     Function:
        //       FUN_1420731D0
        //
        //     Vtable ownership:
        //       live AreaInstance vtable RVA: 0x32C9C48
        //       vtable slot 0x140: 0x1420731D0
        //       vtable slot 0x148: 0x1420731D0
        //
        //     Important instruction sequence:
        //
        //       1420731E7  MOV RCX, [RCX + 0x590]
        //       1420731F2  SUB RCX, [RDI + 0x588]
        //       ...
        //       142073235  MOV RSI, [RDI + 0x590]
        //       14207323C  MOV RDI, [RDI + 0x588]
        //       142073244  CMP RDI, RSI
        //       142073250  MOV RAX, [RDI]
        //       142073253  TEST RAX, RAX
        //       142073266  MOV [RDX], RAX / append to output
        //
        // Caret location:
        //
        //     48 8B BF ^ ?? ?? ?? ??
        //
        // This is the four-byte displacement in MOV RDI, [RDI + disp32].
        //
        // Why the pattern starts in the loop setup:
        //
        //     The count preamble also contains +0x588, but the loop setup makes
        //     the vector semantics obvious: load Last, load First, compare, read
        //     each qword entry, skip zeroes, append non-zero entity pointers.
        //
        // Runtime extraction:
        //
        //     localPlayersOffset = memory.Read<int>(moduleBase + returnedRva) // 0x588
        //
        // Runtime proof:
        //
        //     ShouldResolveAreaInstanceLocalPlayersFromCodePattern reads the
        //     recovered StdVector from live AreaInstance memory and verifies the
        //     first entity pointer matches both the current struct field and the
        //     managed object model's Player.Address.
        BytePattern.FromTemplate(
            "48 89 74 24 38 48 8B B7 90 05 00 00 48 8B BF ^ ?? ?? ?? ?? 48 3B FE 74 ?? 0F 1F 80 00 00 00 00 48 8B 07 48 85 C0 74 ?? 48 8B 53 08",
            KeypointNames.AreaInstanceLocalPlayers
        ),

        // AreaInstanceEntityTreeRoot
        // --------------------------
        //
        // What this finds:
        //
        //     The AreaInstance base constructor's initialization of the first
        //     tree container at AreaInstance +0x6B0. This tree is the source for
        //     AreaInstanceOffsets.EntitiesCount, but the pattern deliberately
        //     returns the root/sentinel pointer slot at +0x6C0.
        //
        // Ghidra evidence:
        //
        //     Function:
        //       FUN_14206E780
        //
        //     Unique tightened pattern location:
        //       pattern starts at 0x14206EBCB
        //       LEA displacement is at 0x14206EBE3
        //
        //     Important instruction sequence:
        //
        //       14206EBD2  MOV qword ptr [RSI + 0x6B0], RAX
        //       14206EBD9  MOV qword ptr [RSI + 0x6B8], RBP
        //       14206EBE0  LEA RBX, [RSI + 0x6C0]
        //       14206EBE7  MOV qword ptr [RSP + 0x68], RBX
        //       14206EBEC  MOV qword ptr [RBX], RBP
        //       14206EBEF  MOV qword ptr [RBX + 0x8], RBP
        //       14206EBF3  MOV ECX, 0x30
        //       14206EBF8  CALL FUN_140155B20
        //       14206EBFD  MOV qword ptr [RAX], RAX
        //       14206EC00  MOV qword ptr [RAX + 0x8], RAX
        //       14206EC04  MOV qword ptr [RAX + 0x10], RAX
        //       14206EC08  MOV word ptr [RAX + 0x18], 0x101
        //       14206EC0E  MOV qword ptr [RBX], RAX
        //
        // Caret location:
        //
        //     48 8D 9E ^ ?? ?? ?? ??
        //
        // This is the four-byte displacement in LEA RBX, [RSI + disp32].
        //
        // Why the pattern includes the preceding +0x6B0/+0x6B8 writes:
        //
        //     The same constructor initializes a second adjacent tree at +0x6D0.
        //     The shorter sentinel-allocation sequence therefore matches twice.
        //     Including the container vtable/data writes at +0x6B0/+0x6B8 makes
        //     the pattern select the first tree, whose count is +0x6C8.
        //
        // Runtime derivation:
        //
        //     entityTreeRootOffset = memory.Read<int>(moduleBase + returnedRva) // 0x6C0
        //     entitiesCountOffset  = entityTreeRootOffset + IntPtr.Size         // 0x6C8
        //
        // Runtime proof:
        //
        //     ShouldResolveAreaInstanceEntitiesCountFromCodePattern reads the
        //     recovered count from live AreaInstance memory and verifies it
        //     equals both AreaInstanceOffsets.EntitiesCount and the object model.
        BytePattern.FromTemplate(
            "48 8D 05 ?? ?? ?? ?? 48 89 86 B0 06 00 00 48 89 AE B8 06 00 00 48 8D 9E ^ ?? ?? ?? ?? 48 89 5C 24 ?? 48 89 2B 48 89 6B 08 B9 30 00 00 00 E8 ?? ?? ?? ?? 48 89 00 48 89 40 08 48 89 40 10 66 C7 40 18 01 01 48 89 03",
            KeypointNames.AreaInstanceEntityTreeRoot
        ),

        // EntityIdentityFilter
        // --------------------
        //
        // What this finds:
        //
        //     The AreaInstance destructor filter that iterates the AreaInstance
        //     entity tree, pulls the entity pointer out of each tree node, and
        //     checks id/status fields before pushing selected entities into a
        //     temporary output vector.
        //
        // Ghidra evidence:
        //
        //     Function:
        //       FUN_14163B7A0
        //
        //     Unique pattern location:
        //       pattern starts at 0x14163B9EB
        //       status displacement is at 0x14163BA06
        //
        //     Important instruction sequence:
        //
        //       14163B9EB  MOV RBX, qword ptr [RSI + 0x6C0]
        //       14163B9F2  MOV RBX, qword ptr [RAX]
        //       14163B9F5  CMP RBX, RAX
        //       14163BA00  MOV RDI, qword ptr [RBX + 0x28]
        //       14163BA04  TEST byte ptr [RDI + 0x8C], 0x1
        //       14163BA0D  CMP dword ptr [RDI + 0x88], 0x40000000
        //       14163BA19  TEST byte ptr [RDI + 0x8D], 0x4
        //
        // Caret location:
        //
        //     F6 87 ^ ?? ?? ?? ?? 01
        //
        // This is the four-byte displacement in TEST byte ptr [RDI + disp32], 1.
        //
        // Why this proves Entity ownership:
        //
        //     The code starts from AreaInstance +0x6C0, the same tree-root slot
        //     recovered by AreaInstanceEntityTreeRoot. It then reads the node
        //     payload at node +0x28 into RDI and uses RDI as the entity pointer.
        //     The filter fields are therefore Entity-relative, not AreaInstance-
        //     relative and not component-relative.
        //
        // Runtime extraction:
        //
        //     entityStatusOffset             = memory.Read<int>(moduleBase + returnedRva)        // 0x8C
        //     entityStatusInvalidMask        = memory.Read<byte>(moduleBase + returnedRva + 0x04) // 0x01
        //     entityIdOffset                 = memory.Read<int>(moduleBase + returnedRva + 0x09) // 0x88
        //     entityIdUpperBound             = memory.Read<uint>(moduleBase + returnedRva + 0x0D) // 0x40000000
        //     entityActiveFlagOffset         = memory.Read<int>(moduleBase + returnedRva + 0x15) // 0x8D
        //     entityActiveFlagRequiredMask   = memory.Read<byte>(moduleBase + returnedRva + 0x19) // 0x04
        //     entityTreeRootOffset           = memory.Read<int>(moduleBase + returnedRva - 0x18) // 0x6C0
        //
        // Runtime proof:
        //
        //     ShouldResolveEntityIdentityFromAreaInstanceTreeFilter starts from
        //     the pattern-backed local player entity, reads id/status/active
        //     flags through these recovered offsets and masks, and compares the
        //     recovered predicate result to the object model's player Id/IsValid.
        BytePattern.FromTemplate(
            "48 8B 86 C0 06 00 00 48 8B 18 48 3B D8 0F 84 ?? ?? ?? ?? 66 90 48 8B 7B 28 F6 87 ^ ?? ?? ?? ?? 01 75 ?? 81 BF ?? ?? ?? ?? 00 00 00 40 73 ?? F6 87 ?? ?? ?? ?? 04",
            KeypointNames.EntityIdentityFilter
        ),

        // EntityDetailsName
        // -----------------
        //
        // What this finds:
        //
        //     The live player Entity vtable accessor that returns a pointer to
        //     the EntityDetails embedded StdWString path/name field.
        //
        // Ghidra evidence:
        //
        //     Live player Entity vtable:
        //       runtime vtable: 0x7FF77432E2F8
        //       copied-binary VA: 0x1433FE2F8
        //
        //     Vtable slot:
        //       +0x10 -> 0x141C7A6D0
        //
        //     Important instruction sequence:
        //
        //       141C7A6D0  MOV RAX, qword ptr [RCX + 0x08]
        //       141C7A6D4  ADD RAX, 0x08
        //       141C7A6D8  RET
        //
        // Caret location:
        //
        //     48 83 C0 ^ ??
        //
        // This is the one-byte immediate in ADD RAX, imm8. The immediate is the
        // EntityDetails-relative path/name field offset.
        //
        // Why the pattern includes the next function prologue:
        //
        //     The three-instruction accessor shape is intentionally tiny and has
        //     several generic matches in the binary. Including the following
        //     function boundary/prologue makes this exact vtable slot unique
        //     without relying on an absolute vtable address in the pattern.
        //
        // Runtime extraction:
        //
        //     entityDetailsNameOffset = memory.Read<byte>(moduleBase + returnedRva)        // 0x08
        //     entityDetailsPtrOffset  = memory.Read<byte>(moduleBase + returnedRva - 0x04) // 0x08
        //
        // Runtime proof:
        //
        //     ShouldResolveEntityDetailsNameFromVtableAccessor starts from the
        //     pattern-backed local player entity, reads EntityDetails +0x08 as
        //     StdWString, and verifies it equals TheGame.Player.Path.
        BytePattern.FromTemplate(
            "48 8B 41 ?? 48 83 C0 ^ ?? C3 CC CC CC CC CC CC CC 48 89 5C 24 08 48 89 7C 24 18 55 48 8D 6C 24 A0 48 81 EC 60 01 00 00",
            KeypointNames.EntityDetailsName
        ),

        // EntityComponentLookupShape
        // --------------------------
        //
        // What this finds:
        //
        //     The generic entity named-component resolver. The concrete build
        //     example is FUN_1401512E0, which resolves the "Animated" component,
        //     but the important part is the shared lookup shape:
        //
        //       Entity -> EntityDetails -> ComponentLookUp -> name/index bucket
        //       Entity -> component pointer vector
        //
        // Ghidra evidence:
        //
        //     Function:
        //       FUN_1401512E0
        //
        //     Important instruction sequence:
        //
        //       1401512ED  MOV RCX, [RCX + 0x08]
        //       1401512FC  MOV RAX, [RDI + 0x08]
        //       140151309  MOV RBX, [RAX + 0x28]
        //       140151319  LEA RCX, [RBX + 0x28]
        //       14015131D  CALL FUN_140163120
        //       140151326  CMP RAX, [RBX + 0x30]
        //       14015132D  MOVSXD RCX, dword ptr [RAX + 0x08]
        //       140151335  MOV RAX, [RDI + 0x10]
        //       140151339  MOV RAX, [RAX + RCX*8]
        //
        // The helper FUN_140163120 is the bucket lookup. It returns a pointer
        // to a 16-byte entry whose first qword is the component-name pointer and
        // whose dword at +0x08 is the component index.
        //
        // Caret location:
        //
        //     48 8B 49 ^ 08
        //
        // This lands on the first Entity + 0x08 displacement. The integration
        // test reads the rest of the offsets at fixed positions inside this
        // compact semantic pattern:
        //
        //       +0x00 -> EntityDetailsPtr, first read
        //       +0x0E -> EntityDetailsPtr, second read
        //       +0x1C -> EntityDetails.ComponentLookUpPtr
        //       +0x2C -> ComponentLookUpStruct.ComponentsNameAndIndex
        //       +0x38 -> lookup bucket Data.Last sentinel
        //       +0x3E -> ComponentNameAndIndexStruct.Index
        //       +0x47 -> ItemStruct.ComponentListPtr first pointer
        //
        // Runtime proof:
        //
        //     ShouldResolveEntityComponentLookupFromCodePattern starts from the
        //     pattern-backed player entity, uses these recovered offsets to walk
        //     the lookup bucket, resolves Player/Life component indexes, and
        //     verifies their component headers point back to the player entity.
        BytePattern.FromTemplate(
            "48 89 5C 24 18 57 48 83 EC 20 48 8B F9 48 8B 49 ^ 08 48 85 C9 74 ?? E8 ?? ?? ?? ?? 48 8B 47 08 4C 8D 44 24 30 48 8D 54 24 38 48 8B 58 28 48 8D 05 ?? ?? ?? ?? 48 89 44 24 30 48 8D 4B 28 E8 ?? ?? ?? ?? 48 8B 00 48 3B 43 30 74 ?? 48 63 48 08 83 F9 FF 74 ?? 48 8B 47 10 48 8B 04 C8",
            KeypointNames.EntityComponentLookupShape
        ),

        // ComponentNameAndIndexEntryStride
        // --------------------------------
        //
        // What this finds:
        //
        //     The bucket helper's entry-address calculation for component
        //     name/index entries. The concrete helper is FUN_140163120, called
        //     by the generic named-component resolver above.
        //
        // Ghidra evidence:
        //
        //       1401631EF  MOV EDX, dword ptr [RSI + RCX*8 + 0x04]
        //       1401631F3  SHL RDX, 0x04
        //       1401631F7  ADD RDX, RDI
        //       1401631FA  MOV RCX, qword ptr [RDX]
        //       1401631FD  CMP qword ptr [RBX], RCX
        //
        // Caret location:
        //
        //     The immediate byte in SHL RDX, 0x04. Runtime derives
        //     entrySize = 1 << 4 = 0x10 and verifies the following
        //     MOV RCX, [RDX] opcode to prove name pointer offset +0x00.
        //
        // Runtime proof:
        //
        //     ShouldResolveEntityComponentLookupFromCodePattern and
        //     ShouldReadEntityComponentsThroughRuntimeGameOffsets enumerate the
        //     player entity's component bucket through test-only runtime layout tooling rather
        //     than ComponentNameAndIndexStruct.
        BytePattern.FromTemplate(
            "3B 04 CE ?? ?? 8B 54 CE 04 48 C1 E2 ^ 04 48 03 D7 48 8B 0A 48 39 0B",
            KeypointNames.ComponentNameAndIndexEntryStride
        ),

        // PlayerComponentHeaderOwnerEntity
        // --------------------------------
        //
        // What this finds:
        //
        //     The Player component debug/info formatter's owner-entity read.
        //     The formatter is a concrete virtual method reached through the
        //     Player component vtable at slot +0x68.
        //
        // Ghidra evidence:
        //
        //     Function:
        //       FUN_141D267F0
        //
        //     Important instruction sequence:
        //
        //       141D267F0  MOV qword ptr [RSP + 0x08], RBX
        //       ...
        //       141D2680A  MOV R14, RCX
        //       141D2680D  MOV RDI, RDX
        //       141D26810  MOV RCX, qword ptr [RCX + 0x08]
        //       141D26814  CALL FUN_140E60A90
        //
        // Caret location:
        //
        //     48 8B 49 ^ 08
        //
        // This lands on the one-byte displacement in the owner read, recovering
        // ComponentHeader.EntityPtr = 0x08. The function start is returnedRva -
        // 0x23 for this compact pattern. The integration test validates that:
        //
        //       *(IntPtr*)(playerComponent +0x00)          -> vtable
        //       *(IntPtr*)(vtable +0x68)                  -> this function
        //       *(IntPtr*)(playerComponent + recovered 8) -> player entity
        //
        // That makes ComponentHeader.StaticPtr +0x00 a vtable field for the
        // tested component and makes EntityPtr +0x08 function-backed.
        BytePattern.FromTemplate(
            "48 89 5C 24 08 48 89 6C 24 10 48 89 74 24 18 48 89 7C 24 20 41 56 48 83 EC 20 4C 8B F1 48 8B FA 48 8B 49 ^ 08 E8 ?? ?? ?? ?? 48 8D 15 ?? ?? ?? ?? 44 0F B6 80 58 01 00 00",
            KeypointNames.PlayerComponentHeaderOwnerEntity
        ),

        // PlayerComponentName
        // -------------------
        //
        // What this finds:
        //
        //     The Player component debug/info formatter that prints:
        //
        //       Character Name: <name>
        //
        //     and reads the name from a StdWString embedded in the Player
        //     component.
        //
        // Ghidra evidence:
        //
        //     Live Player component vtable:
        //       runtime vtable: 0x7FF7741F4758
        //       copied-binary VA: 0x1432C4758
        //
        //     Function:
        //       vtable slot 0x68 -> FUN_141D267F0
        //
        //     Unique string:
        //       0x1433F5AC0: "Character Name: "
        //
        //     Important instruction sequence:
        //
        //       141D26865  LEA RDX, [0x1433F5AC0]      // "Character Name: "
        //       141D2686C  MOV RCX, RDI
        //       141D2686F  LEA RBX, [R14 + 0x1B0]      // Player.Name
        //       141D26876  CALL FUN_14014FA40
        //       141D2687B  CMP qword ptr [RBX + 0x18], 0x7
        //       141D26880  MOV R8, qword ptr [RBX + 0x10]
        //       141D26884  JBE 0x141D26889
        //       141D26886  MOV RBX, qword ptr [RBX]
        //       141D26889  MOV RDX, RBX
        //       141D2688C  MOV RCX, RAX
        //       141D2688F  CALL FUN_14015A460
        //
        // Caret location:
        //
        //     49 8D 9E ^ ?? ?? ?? ??
        //
        // This lands on the four-byte displacement in LEA RBX, [R14 + disp32].
        //
        // Runtime extraction:
        //
        //     nameOffset     = memory.Read<int>(moduleBase + returnedRva)        // 0x1B0
        //     capacityOffset = memory.Read<byte>(moduleBase + returnedRva + 0x0C) // 0x18
        //     smallLimit     = memory.Read<byte>(moduleBase + returnedRva + 0x0D) // 7
        //     lengthOffset   = memory.Read<byte>(moduleBase + returnedRva + 0x11) // 0x10
        //     externalBuffer = memory.Read<byte>(moduleBase + returnedRva + 0x14, 3)
        //                      // 48 8B 1B == MOV RBX, [RBX], buffer offset +0x00
        //
        // Why +0x0C/+0x0D/+0x11/+0x14 are acceptable here:
        //
        //     They are offsets inside this compact formatter keypoint. The test
        //     validates that they still recover the StdWString SSO shape and
        //     that the FF/runtime-layout string reader yields the same non-empty
        //     name as the managed object model.
        BytePattern.FromTemplate(
            "48 8D 15 ?? ?? ?? ?? 48 8B CF 49 8D 9E ^ ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 83 7B 18 07 4C 8B 43 10 76 ?? 48 8B 1B 48 8B D3 48 8B C8 E8 ?? ?? ?? ?? 48 8B C8 E8",
            KeypointNames.PlayerComponentName
        ),

        // LifeComponentVitalOffsets
        // -------------------------
        //
        // What this finds:
        //
        //     The Life component function that walks embedded vital objects for
        //     serialization/update-style processing.
        //
        // Ghidra evidence:
        //
        //     Live Life component vtable:
        //       runtime vtable: 0x7FF77431E948
        //       copied-binary VA: 0x1433EE948
        //
        //     Function:
        //       vtable slot 0x98 -> FUN_141CE1C00
        //
        //     Important instruction sequence:
        //
        //       141CE1C1A  ADD RCX, 0x1A8
        //       141CE1C21  CALL FUN_141CE1860
        //       141CE1C26  LEA RCX, [RBP + 0x200]
        //       141CE1C30  CALL FUN_141CDEB60
        //       141CE1C35  LEA RCX, [RBP + 0x240]
        //       141CE1C3F  CALL FUN_141CE1860
        //
        // Caret location:
        //
        //     48 81 C1 ^ ?? ?? ?? ??
        //
        // This lands on the immediate in ADD RCX, 0x1A8. The integration test
        // reads the rest of the vital starts at fixed positions inside this
        // compact pattern:
        //
        //       +0x00 -> LifeOffset.Health
        //       +0x0C -> LifeOffset.Mana
        //       +0x1B -> LifeOffset.EnergyShield
        //
        // Why this is a strong keypoint:
        //
        //     The function is reached from the live Life component vtable, and
        //     it passes Life-relative addresses into vital-specific helper
        //     routines. This is much stronger than broad searches for 0x1A8,
        //     which appears in many unrelated object layouts.
        BytePattern.FromTemplate(
            "48 8B E9 48 8B FA 48 81 C1 ^ ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 8D 8D ?? ?? ?? ?? 48 8B D7 E8 ?? ?? ?? ?? 48 8D 8D ?? ?? ?? ?? 48 8B D7 E8",
            KeypointNames.LifeComponentVitalOffsets
        ),

        // LifeVitalCurrentTotal
        // ---------------------
        //
        // What this finds:
        //
        //     The Life component debug/info formatter that prints current/total
        //     values for Life, Energy Shield, and Mana.
        //
        // Ghidra evidence:
        //
        //     Function:
        //       Life vtable slot 0x68 -> FUN_141CE2300
        //
        //     Important instruction sequence:
        //
        //       141CE236A  MOV EDX, [RBX + 0x1E0]  // Life current
        //       141CE2387  MOV EDX, [RBX + 0x1DC]  // Life total
        //       141CE23B3  MOV EDX, [RBX + 0x278]  // ES current
        //       141CE23D0  MOV EDX, [RBX + 0x274]  // ES total
        //       141CE23FC  MOV EDX, [RBX + 0x238]  // Mana current
        //       141CE2419  MOV EDX, [RBX + 0x234]  // Mana total
        //
        // Caret location:
        //
        //     8B 93 ^ ?? ?? ?? ??
        //
        // This lands on the absolute Life-component offset for health current
        // (0x1E0). The integration test subtracts the recovered vital object
        // starts from these absolute offsets to derive the VitalStruct layout:
        //
        //       +0x00 -> Health current absolute offset
        //       +0x1D -> Health total absolute offset
        //       +0x49 -> EnergyShield current absolute offset
        //       +0x66 -> EnergyShield total absolute offset
        //       +0x92 -> Mana current absolute offset
        //       +0xAF -> Mana total absolute offset
        //
        // Runtime derivation:
        //
        //       VitalStruct.Current = 0x1E0 - LifeOffset.Health       = 0x38
        //       VitalStruct.Total   = 0x1DC - LifeOffset.Health       = 0x34
        //       VitalStruct.Current = 0x278 - LifeOffset.EnergyShield = 0x38
        //       VitalStruct.Total   = 0x274 - LifeOffset.EnergyShield = 0x34
        //       VitalStruct.Current = 0x238 - LifeOffset.Mana         = 0x38
        //       VitalStruct.Total   = 0x234 - LifeOffset.Mana         = 0x34
        BytePattern.FromTemplate(
            "48 8B D9 4C 8D 7A 10 48 8D 15 ?? ?? ?? ?? 49 8B CF E8 ?? ?? ?? ?? 48 8D 15 ?? ?? ?? ?? 48 8B C8 E8 ?? ?? ?? ?? 48 8B C8 48 8D 15 ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 8D 15 ?? ?? ?? ?? 49 8B CF E8 ?? ?? ?? ?? 8B 93 ^ ?? ?? ?? ?? 48 8B C8 E8 ?? ?? ?? ?? 48 8B C8 48 8D 15 ?? ?? ?? ?? E8 ?? ?? ?? ?? 8B 93 ?? ?? ?? ??",
            KeypointNames.LifeVitalCurrentTotal
        ),

        // LifeVitalReservationOffsets
        // ---------------------------
        //
        // What this finds:
        //
        //     The common VitalStruct deserializer helper that reads reservation
        //     fields from a stream and stores them into the vital object.
        //
        // Ghidra evidence:
        //
        //     Function:
        //       FUN_141CE13A0
        //
        //     Important instruction sequence:
        //
        //       141CE13BE  MOV dword ptr [R8 + 0x18], ECX
        //       141CE13DE  MOVSX ECX, word ptr [RCX + RAX]
        //       141CE13E6  MOV dword ptr [R8 + 0x1C], ECX
        //
        // Caret location:
        //
        //     41 89 48 ^ 18
        //
        // This lands on the one-byte displacement in the flat-reserve write.
        // The percent-reserve write is a second one-byte displacement at
        // returnedRva +0x28.
        //
        // Runtime extraction:
        //
        //     reservedFlatOffset    = memory.Read<byte>(moduleBase + returnedRva)        // 0x18
        //     reservedPercentOffset = memory.Read<byte>(moduleBase + returnedRva + 0x28) // 0x1C
        //
        // Why this is tied to VitalStruct:
        //
        //     FUN_141CDEB60 calls this helper after writing Vital +0x38
        //     (Current). FUN_141CE1860 calls FUN_141CDEB60 and then writes
        //     Vital +0x48. The caller family is already tied back to Life
        //     component vital starts by LifeComponentVitalOffsets.
        BytePattern.FromTemplate(
            "4C 8B 52 08 4C 8B C1 48 8B 4A 10 48 8D 41 04 49 3B C2 77 ?? 48 8B 02 8B 0C 01 41 89 48 ^ 18 48 8B 4A 10 48 8B 42 08 48 83 C1 04 48 89 4A 10 4C 8D 49 02 4C 3B C8 77 ?? 48 8B 02 0F BF 0C 01 4C 89 4A 10 41 89 48 1C",
            KeypointNames.LifeVitalReservationOffsets
        ),

        // LifeVitalConstructorShape
        // -------------------------
        //
        // What this finds:
        //
        //     The Life component constructor block that lays out the embedded
        //     vital objects. This is the static ownership proof for several
        //     VitalStruct header/stat-id fields and, importantly, a rejection
        //     of the old VitalStruct.Regeneration interpretation at +0x28.
        //
        // Ghidra evidence:
        //
        //     Function:
        //       FUN_141CDC130
        //
        //     Important instruction sequence:
        //
        //       141CDC248  LEA R14, [RSI + 0x1A8]       // Health
        //       141CDC24F  MOV dword ptr [R14 + 0x08], 0x3334
        //       141CDC257  MOV dword ptr [R14 + 0x0C], 0x333B
        //       141CDC25F  MOV [R14 + 0x10], RSI        // owner Life component
        //       141CDC271  MOV dword ptr [R14 + 0x20], 0xEF
        //       141CDC279  MOV dword ptr [R14 + 0x24], 0x584
        //       141CDC281  MOV dword ptr [R14 + 0x28], 0x759
        //       141CDC2A9  MOV dword ptr [R14 + 0x34], EAX
        //       141CDC2AD  MOV dword ptr [R14 + 0x38], EAX
        //       141CDC2D8  LEA R15, [RSI + 0x200]       // Mana
        //       141CDC2DF  MOV dword ptr [R15 + 0x08], 0x6986
        //       141CDC2E7  MOV dword ptr [R15 + 0x0C], 0x6986
        //       141CDC30A  MOV dword ptr [R15 + 0x28], 0x1C9C
        //       141CDC344  LEA R12, [RSI + 0x240]       // Energy Shield
        //       141CDC35B  MOV dword ptr [RSP + 0x20], 0x4DE5
        //       141CDC375  CALL FUN_141CD0640
        //
        // Caret location:
        //
        //     4C 8D B6 ^ ?? ?? ?? ??
        //
        // This lands on the four-byte displacement in LEA R14, [RSI + 0x1A8].
        // The integration test reads additional compact offsets from this same
        // constructor block:
        //
        //       +0x07 -> VitalStruct.UnknownStatId0 field offset (0x08)
        //       +0x08 -> Health UnknownStatId0 value (0x3334)
        //       +0x0F -> VitalStruct.UnknownStatId1 field offset (0x0C)
        //       +0x10 -> Health UnknownStatId1 value (0x333B)
        //       +0x17 -> VitalStruct.LifeComponentPtr field offset (0x10)
        //       +0x29 -> VitalStruct.TotalStatId field offset (0x20)
        //       +0x31 -> VitalStruct.UnknownStatId2 field offset (0x24)
        //       +0x39 -> VitalStruct.UnknownStatId3 field offset (0x28)
        //       +0x3A -> Health UnknownStatId3 value (0x759)
        //       +0x90 -> LifeOffset.Mana offset (0x200)
        //       +0x97 -> Mana UnknownStatId0 field offset (0x08)
        //       +0x98 -> Mana UnknownStatId0 value (0x6986)
        //       +0x9F -> Mana UnknownStatId1 field offset (0x0C)
        //       +0xA0 -> Mana UnknownStatId1 value (0x6986)
        //       +0xC3 -> Mana UnknownStatId3 value (0x1C9C)
        //       +0xFC -> LifeOffset.EnergyShield offset (0x240)
        //       +0x114 -> EnergyShield UnknownStatId3 value (0x4DE5)
        //
        // Nuance:
        //
        //     FUN_141CD0640 is the shared constructor helper for some vital-like
        //     objects. It writes the fifth argument to Vital +0x28, which is why
        //     the Energy Shield value is recovered from the call setup rather
        //     than a direct [R12 +0x28] store in this block.
        BytePattern.FromTemplate(
            "4C 8D B6 ^ ?? ?? ?? ?? 41 C7 46 08 ?? ?? ?? ?? 41 C7 46 0C ?? ?? ?? ?? 49 89 76 10 4D 89 6E 18 48 8D 3D ?? ?? ?? ?? 49 89 3E 41 C7 46 20 ?? ?? ?? ?? 41 C7 46 24 ?? ?? ?? ?? 41 C7 46 28 ?? ?? ?? ?? 45 88 6E 2C 45 89 6E 30 48 8B 86 ?? ?? ?? ?? 48 8B 88 ?? ?? ?? ?? 48 8B 01 BA ?? ?? ?? ?? FF 10 41 89 46 34 41 89 46 38 41 C7 46 40 ?? ?? ?? ?? 49 C7 46 44 ?? ?? ?? ?? 45 88 6E 4C 48 8D 05 ?? ?? ?? ?? 49 89 06 66 45 89 6E 50 45 88 6E 52 4C 8D BE ?? ?? ?? ?? 41 C7 47 08 ?? ?? ?? ?? 41 C7 47 0C ?? ?? ?? ?? 49 89 77 10 4D 89 6F 18 49 89 3F 41 C7 47 20 ?? ?? ?? ?? 41 C7 47 24 ?? ?? ?? ?? 41 C7 47 28 ?? ?? ?? ?? 45 88 6F 2C 45 89 6F 30 48 8B 86 ?? ?? ?? ?? 48 8B 88 ?? ?? ?? ?? 48 8B 01 BA ?? ?? ?? ?? FF 10 41 89 47 34 41 89 47 38 48 8D 05 ?? ?? ?? ?? 49 89 07 4C 8D A6 ?? ?? ?? ?? C7 44 24 30 ?? ?? ?? ?? C7 44 24 28 ?? ?? ?? ?? C7 44 24 20 ?? ?? ?? ?? 41 B9 ?? ?? ?? ?? 41 B8 ?? ?? ?? ?? 48 8B D6 49 8B CC E8",
            KeypointNames.LifeVitalConstructorShape
        ),

        // LifeVitalSharedConstructorShape
        // --------------------------------
        //
        // What this finds:
        //
        //     FUN_141CD0640, the shared constructor helper used by Energy Shield
        //     and other Life-owned vital-like objects. It proves the default
        //     stat-id fields for helper-constructed vitals.
        //
        // Ghidra evidence:
        //
        //       141CD064A  MOV qword ptr [RCX + 0x10], RDX
        //       141CD0661  MOV dword ptr [RCX + 0x28], EAX
        //       141CD0664  MOV dword ptr [RCX + 0x08], 0x6986
        //       141CD066B  MOV dword ptr [RCX + 0x0C], 0x6986
        //       141CD0676  MOV dword ptr [RCX + 0x20], R8D
        //       141CD067A  MOV dword ptr [RCX + 0x24], R9D
        //
        // Caret location:
        //
        //     48 89 51 ^ 10
        //
        // This lands on the one-byte displacement in the owner pointer write.
        // The integration test reads additional compact fields from this helper:
        //
        //       +0x00 -> VitalStruct.LifeComponentPtr field offset (0x10)
        //       +0x16 -> VitalStruct.UnknownStatId3 field offset (0x28)
        //       +0x19 -> VitalStruct.UnknownStatId0 field offset (0x08)
        //       +0x1A -> helper default UnknownStatId0 value (0x6986)
        //       +0x20 -> VitalStruct.UnknownStatId1 field offset (0x0C)
        //       +0x21 -> helper default UnknownStatId1 value (0x6986)
        //       +0x2C -> VitalStruct.TotalStatId field offset (0x20)
        //       +0x30 -> VitalStruct.UnknownStatId2 field offset (0x24)
        BytePattern.FromTemplate(
            "48 89 51 ^ 10 48 8D 05 ?? ?? ?? ?? 48 89 01 33 FF 8B 44 24 50 48 8B D9 89 41 28 C7 41 08 ?? ?? ?? ?? C7 41 0C ?? ?? ?? ?? 48 89 79 18 44 89 41 20 44 89 49 24 40 88 79 2C 89 79 30",
            KeypointNames.LifeVitalSharedConstructorShape
        ),

        // InGameStateZoneSwitchCounter
        // ----------------------------
        //
        // What this finds:
        //
        //     A direct dword comparison against InGameState + 0x56C. This is the
        //     code-backed keypoint for InGameStateOffset.ZoneSwitchCounter.
        //
        // Ghidra evidence:
        //
        //     Label-less analyzed block:
        //       exact byte pattern starts at 0x140FB3F59
        //       displacement is at 0x140FB3F62
        //
        //     Important instruction sequence:
        //
        //       140FB3F59  MOV RAX, qword ptr [RBP + 0x9F0]
        //       140FB3F60  CMP dword ptr [RAX + 0x56C], 0x1
        //       140FB3F67  JNZ 0x140FB40AC
        //       140FB3F6D  MOV RBX, qword ptr [RAX + 0x360]
        //       140FB3F74  MOV RAX, qword ptr [R13 + 0x1A8]
        //
        // Why this is the direct counter:
        //
        //     The live snapshot confirmed:
        //
        //       inGameState               = 0x0000033E22E42D10
        //       ZoneSwitchCounter address = 0x0000033E22E4327C
        //       delta                     = 0x56C
        //
        //     This pattern reads the same displacement directly from the state
        //     object. It does not first follow the nested +0x368 pointer.
        //
        // Caret location:
        //
        //     83 B8 ^ ?? ?? ?? ?? 01
        //
        // This is the four-byte displacement in CMP dword ptr [RAX + disp32], 1.
        //
        // Runtime extraction:
        //
        //     directCounterOffset = memory.Read<int>(moduleBase + returnedRva) // 0x56C
        //
        // Nuance:
        //
        //     Ghidra did not currently assign a containing function name to this
        //     block, so the evidence is anchored by exact VA plus surrounding
        //     disassembly rather than by FUN_*. If future analysis creates a
        //     function around this block, update the evidence note with that name.
        BytePattern.FromTemplate(
            "48 8B 85 ?? ?? ?? ?? 83 B8 ^ ?? ?? ?? ?? 01 0F 85 ?? ?? ?? ?? 48 8B 98 ?? ?? ?? ?? 49 8B 85 ?? ?? ?? ??",
            KeypointNames.InGameStateZoneSwitchCounter
        ),

        // InGameStateZoneSwitchCounterReset
        // ---------------------------------
        //
        // What this finds:
        //
        //     The reset/lifetime path that clears the direct InGameState
        //     zone-switch counter and the adjacent byte flag.
        //
        // Ghidra evidence:
        //
        //     Function:
        //       FUN_140FAC260
        //
        //     Unique pattern location:
        //       pattern starts at 0x140FAC374
        //       counter displacement is at 0x140FAC37C
        //
        //     Important instruction sequence:
        //
        //       140FAC37A  MOV dword ptr [RBX + 0x56C], EBP
        //       140FAC380  MOV byte ptr [RBX + 0x568], 0x0
        //       140FAC387  LEA RCX, [RBX + 0x650]
        //
        // Runtime extraction:
        //
        //     counterOffset      = memory.Read<int>(moduleBase + returnedRva)        // 0x56C
        //     adjacentFlagOffset = memory.Read<int>(moduleBase + returnedRva + 0x06) // 0x568
        BytePattern.FromTemplate(
            "FF 15 ?? ?? ?? ?? 89 AB ^ 6C 05 00 00 C6 83 68 05 00 00 00 48 8D 8B 50 06 00 00 45 33 C0 33 D2 E8",
            KeypointNames.InGameStateZoneSwitchCounterReset
        ),

        // InGameStateZoneSwitchCounterIncrementFirst
        // ------------------------------------------
        //
        // What this finds:
        //
        //     The first direct increment site for InGameState +0x56C inside
        //     FUN_140FB24A0.
        //
        // Ghidra evidence:
        //
        //       140FB282A  MOV qword ptr [R14 + 0x4B8], RBX
        //       140FB2831  INC dword ptr [R14 + 0x56C]
        //
        // Runtime extraction:
        //
        //     counterOffset = memory.Read<int>(moduleBase + returnedRva) // 0x56C
        BytePattern.FromTemplate(
            "49 89 9E B8 04 00 00 41 FF 86 ^ 6C 05 00 00 48 8B 5D",
            KeypointNames.InGameStateZoneSwitchCounterIncrementFirst
        ),

        // InGameStateZoneSwitchCounterIncrementSecond
        // -------------------------------------------
        //
        // What this finds:
        //
        //     The second direct increment site for InGameState +0x56C inside
        //     FUN_140FB24A0.
        //
        // Ghidra evidence:
        //
        //       140FB28DF  MOV qword ptr [R14 + 0x4B8], R15
        //       140FB28E6  INC dword ptr [R14 + 0x56C]
        //
        // Runtime extraction:
        //
        //     counterOffset = memory.Read<int>(moduleBase + returnedRva) // 0x56C
        BytePattern.FromTemplate(
            "4D 89 BE B8 04 00 00 41 FF 86 ^ 6C 05 00 00 49 8B BE D0 04 00 00",
            KeypointNames.InGameStateZoneSwitchCounterIncrementSecond
        ),

        // InGameStateZoneSwitchState
        // -------------------------
        //
        // What this finds:
        //
        //     A transition/update path that starts from a confirmed InGameState-
        //     shaped object, follows a nested pointer at +0x368, and checks a
        //     byte at +0x56C in that nested object. We only expose the +0x368
        //     pointer as a keypoint; the nested byte is just useful context until
        //     we actually model that object.
        //
        //     This is not the direct ZoneSwitchCounter at InGameState + 0x56C.
        //     The direct counter has its own code-backed read keypoint above.
        //
        // Ghidra evidence:
        //
        //     Function:
        //       FUN_140FAF2F0
        //
        //     Important instruction sequence:
        //
        //       140FAF315  MOV RSI, [RCX]
        //       140FAF31B  CMP qword ptr [RSI + 0x290], 0x0
        //       ...
        //       140FAF3E6  MOV RAX, [R14]
        //       140FAF3E9  MOV RBX, [RAX + 0x368]
        //       140FAF3F0  CMP byte ptr [RBX + 0x56C], 0x0
        //       140FAF3F7  JNZ 0x140FAF468
        //       140FAF3F9  MOV RDI, [RBX + 0x98]
        //       140FAF400  MOV RCX, [RDI + 0xA0]
        //
        // Why this is the right owner:
        //
        //     The same function first checks [RSI + 0x290], which is the already
        //     proven InGameState.AreaInstanceData offset. Later it reloads that
        //     same state object through [R14], reads [state + 0x368], and only
        //     then checks [nested + 0x56C]. The +0x56C displacement is therefore
        //     a nested transition byte, not the direct InGameState counter.
        //
        // Runtime extraction:
        //
        //     zoneSwitchStateOffset = memory.Read<int>(moduleBase + InGameStateZoneSwitchStateRva) // 0x368
        //
        // Runtime chain:
        //
        //     zoneSwitchStateAddress = *(IntPtr*)(inGameStateAddress + zoneSwitchStateOffset)
        //
        // Nuance:
        //
        //     Existing managed struct field InGameStateOffset.ZoneSwitchCounter
        //     also says +0x56C. That direct field is valid according to the live
        //     snapshot, but this particular pattern is not its proof because it
        //     has already changed owners through [InGameState + 0x368].
        BytePattern.FromTemplate(
            "49 8B 06 48 8B 98 ^ ?? ?? ?? ?? 80 BB ?? ?? ?? ?? 00 75 ?? 48 8B BB ?? ?? ?? ?? 48 8B 8F ?? ?? ?? ?? 48 85 C9",
            KeypointNames.InGameStateZoneSwitchState
        ),

        // InGameStateZoneSwitchStateConstructor
        // ------------------------------------
        //
        // What this finds:
        //
        //     The constructor/lifetime assignment for the same nested
        //     transition object pointer at InGameState +0x368.
        //
        // Ghidra evidence:
        //
        //     Function:
        //       FUN_140FA9D00
        //
        //     Important instruction sequence:
        //
        //       140FAA2D2  MOV RBX, [R14 + 0x360]
        //       140FAA2D9  MOV ECX, 0x578
        //       140FAA2DE  CALL allocator
        //       140FAA2EC  MOV R8, RBX
        //       140FAA2EF  MOV RDX, R14
        //       140FAA2F2  MOV RCX, RAX
        //       140FAA2F5  CALL FUN_14012F740
        //       140FAA2FF  MOV RBX, [R14 + 0x368]
        //       140FAA306  MOV [R14 + 0x368], RAX
        //
        // Runtime extraction:
        //
        //     oldSlotReadOffset = memory.Read<int>(moduleBase + returnedRva)     // 0x368
        //     newSlotWriteOffset = memory.Read<int>(moduleBase + returnedRva + 7) // 0x368
        BytePattern.FromTemplate(
            "B9 78 05 00 00 E8 ?? ?? ?? ?? 48 89 45 48 48 85 C0 74 ?? 4C 8B C3 49 8B D6 48 8B C8 E8 ?? ?? ?? ?? EB ?? 49 8B C7 49 8B 9E ^ ?? ?? ?? ?? 49 89 86 ?? ?? ?? ?? 48 85 DB",
            KeypointNames.InGameStateZoneSwitchStateConstructor
        ),
    };
}
