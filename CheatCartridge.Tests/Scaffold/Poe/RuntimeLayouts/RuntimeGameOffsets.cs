using System.Runtime.CompilerServices;
using System.Text;
using CheatCartridge.GameHelper.GameOffsets;
using CheatCartridge.GameHelper.GameOffsets.Objects.Components;
using CheatCartridge.GameHelper.Natives;
using CheatCartridge.GameHelper.RemoteEnums;
using CheatCartridge.GameHelper.RemoteObjects;
using EyeAuras.Memory;
using EyeAuras.Memory.Scaffolding;

namespace CheatCartridge.Tests.Scaffold.Poe.RuntimeLayouts;

/// <summary>
/// Runtime offsets recovered from code patterns for the current client build.
/// </summary>
public sealed class RuntimeGameOffsets
{
    private const int MaxStateUpdateFunctionScanBytes = 0x800;

    private static readonly ConditionalWeakTable<IMemory, Lazy<RuntimeGameOffsets>> Cache = new();

    public PoeRuntimeLayouts Layouts { get; }

    private RuntimeGameOffsets(
        IntPtr gameStatesGlobalSlot,
        int gameStateStaticGameStateOffset,
        int gameStateStaticSidecarOffset,
        int gameStateCurrentStateVectorOffset,
        int gameStateCurrentStateVectorLastOffset,
        int gameStateDispatchVectorOffset,
        int gameStateDispatchVectorLastOffset,
        int gameStateDispatchVectorEndOffset,
        int gameStateDirtyFlagOffset,
        int stdVectorFirstOffset,
        int stdVectorLastOffset,
        int stdVectorEndOffset,
        int gameStateTableOffset,
        int gameStateEntrySize,
        int gameStateCount,
        int gameStateVirtualUpdateSlotOffset,
        IntPtr inGameStateMsElapsedWriterFunctionAddress,
        int inGameStateAreaInstanceDataOffset,
        int inGameStateMsElapsedOffset,
        int inGameStateZoneSwitchCounterOffset,
        int areaInstanceCurrentAreaLevelOffset,
        int areaInstanceCurrentAreaHashOffset,
        int areaInstanceLocalPlayersOffset,
        int areaInstanceEntityTreeRootOffset,
        int entityDetailsPtrOffset,
        int entityComponentListOffset,
        int entityIdOffset,
        int entityStatusOffset,
        int entityActiveFlagOffset,
        byte entityStatusInvalidMask,
        uint entityIdUpperBound,
        byte entityActiveFlagRequiredMask,
        int entityDetailsNameOffset,
        int entityDetailsComponentLookupOffset,
        int componentLookupNameAndIndexBucketOffset,
        int componentNameAndIndexNamePointerOffset,
        int componentNameAndIndexIndexOffset,
        int componentNameAndIndexEntrySize,
        int componentHeaderOwnerEntityOffset,
        int playerNameOffset,
        int stdWStringBufferOffset,
        int stdWStringInlineBufferOffset,
        int stdWStringLengthOffset,
        int stdWStringCapacityOffset,
        int stdWStringSmallCapacityLimit,
        int lifeHealthOffset,
        int lifeManaOffset,
        int lifeEnergyShieldOffset,
        int vitalUnknownStatId0Offset,
        int vitalUnknownStatId1Offset,
        int vitalLifeComponentPtrOffset,
        int vitalReservedFlatOffset,
        int vitalReservedPercentOffset,
        int vitalTotalStatIdOffset,
        int vitalUnknownStatId2Offset,
        int vitalUnknownStatId3Offset,
        int vitalTotalOffset,
        int vitalCurrentOffset)
    {
        GameStatesGlobalSlot = gameStatesGlobalSlot;
        GameStateStaticGameStateOffset = gameStateStaticGameStateOffset;
        GameStateStaticSidecarOffset = gameStateStaticSidecarOffset;
        GameStateCurrentStateVectorOffset = gameStateCurrentStateVectorOffset;
        GameStateCurrentStateVectorLastOffset = gameStateCurrentStateVectorLastOffset;
        GameStateDispatchVectorOffset = gameStateDispatchVectorOffset;
        GameStateDispatchVectorLastOffset = gameStateDispatchVectorLastOffset;
        GameStateDispatchVectorEndOffset = gameStateDispatchVectorEndOffset;
        GameStateDirtyFlagOffset = gameStateDirtyFlagOffset;
        StdVectorFirstOffset = stdVectorFirstOffset;
        StdVectorLastOffset = stdVectorLastOffset;
        StdVectorEndOffset = stdVectorEndOffset;
        GameStateTableOffset = gameStateTableOffset;
        GameStateEntrySize = gameStateEntrySize;
        GameStateCount = gameStateCount;
        GameStateVirtualUpdateSlotOffset = gameStateVirtualUpdateSlotOffset;
        InGameStateMsElapsedWriterFunctionAddress = inGameStateMsElapsedWriterFunctionAddress;
        InGameStateAreaInstanceDataOffset = inGameStateAreaInstanceDataOffset;
        InGameStateMsElapsedOffset = inGameStateMsElapsedOffset;
        InGameStateZoneSwitchCounterOffset = inGameStateZoneSwitchCounterOffset;
        AreaInstanceCurrentAreaLevelOffset = areaInstanceCurrentAreaLevelOffset;
        AreaInstanceCurrentAreaHashOffset = areaInstanceCurrentAreaHashOffset;
        AreaInstanceLocalPlayersOffset = areaInstanceLocalPlayersOffset;
        AreaInstanceEntityTreeRootOffset = areaInstanceEntityTreeRootOffset;
        EntityDetailsPtrOffset = entityDetailsPtrOffset;
        EntityComponentListOffset = entityComponentListOffset;
        EntityIdOffset = entityIdOffset;
        EntityStatusOffset = entityStatusOffset;
        EntityActiveFlagOffset = entityActiveFlagOffset;
        EntityStatusInvalidMask = entityStatusInvalidMask;
        EntityIdUpperBound = entityIdUpperBound;
        EntityActiveFlagRequiredMask = entityActiveFlagRequiredMask;
        EntityDetailsNameOffset = entityDetailsNameOffset;
        EntityDetailsComponentLookupOffset = entityDetailsComponentLookupOffset;
        ComponentLookupNameAndIndexBucketOffset = componentLookupNameAndIndexBucketOffset;
        ComponentNameAndIndexNamePointerOffset = componentNameAndIndexNamePointerOffset;
        ComponentNameAndIndexIndexOffset = componentNameAndIndexIndexOffset;
        ComponentNameAndIndexEntrySize = componentNameAndIndexEntrySize;
        ComponentHeaderOwnerEntityOffset = componentHeaderOwnerEntityOffset;
        PlayerNameOffset = playerNameOffset;
        StdWStringBufferOffset = stdWStringBufferOffset;
        StdWStringInlineBufferOffset = stdWStringInlineBufferOffset;
        StdWStringLengthOffset = stdWStringLengthOffset;
        StdWStringCapacityOffset = stdWStringCapacityOffset;
        StdWStringSmallCapacityLimit = stdWStringSmallCapacityLimit;
        LifeHealthOffset = lifeHealthOffset;
        LifeManaOffset = lifeManaOffset;
        LifeEnergyShieldOffset = lifeEnergyShieldOffset;
        VitalUnknownStatId0Offset = vitalUnknownStatId0Offset;
        VitalUnknownStatId1Offset = vitalUnknownStatId1Offset;
        VitalLifeComponentPtrOffset = vitalLifeComponentPtrOffset;
        VitalReservedFlatOffset = vitalReservedFlatOffset;
        VitalReservedPercentOffset = vitalReservedPercentOffset;
        VitalTotalStatIdOffset = vitalTotalStatIdOffset;
        VitalUnknownStatId2Offset = vitalUnknownStatId2Offset;
        VitalUnknownStatId3Offset = vitalUnknownStatId3Offset;
        VitalTotalOffset = vitalTotalOffset;
        VitalCurrentOffset = vitalCurrentOffset;
        Layouts = new PoeRuntimeLayouts(
            new GameStatesLayout(
                gameStatesGlobalSlot,
                gameStateStaticGameStateOffset,
                gameStateStaticSidecarOffset,
                gameStateCurrentStateVectorOffset,
                gameStateCurrentStateVectorLastOffset,
                gameStateDispatchVectorOffset,
                gameStateDispatchVectorLastOffset,
                gameStateDispatchVectorEndOffset,
                gameStateDirtyFlagOffset,
                gameStateTableOffset,
                gameStateEntrySize,
                gameStateCount,
                gameStateVirtualUpdateSlotOffset),
            new InGameStateLayout(
                inGameStateMsElapsedWriterFunctionAddress,
                inGameStateAreaInstanceDataOffset,
                inGameStateMsElapsedOffset,
                inGameStateZoneSwitchCounterOffset),
            new AreaInstanceLayout(
                areaInstanceCurrentAreaLevelOffset,
                areaInstanceCurrentAreaHashOffset,
                areaInstanceLocalPlayersOffset,
                areaInstanceEntityTreeRootOffset),
            new EntityLayout(
                entityDetailsPtrOffset,
                entityComponentListOffset,
                entityIdOffset,
                entityStatusOffset,
                entityActiveFlagOffset,
                entityStatusInvalidMask,
                entityIdUpperBound,
                entityActiveFlagRequiredMask),
            new EntityDetailsLayout(
                entityDetailsNameOffset,
                entityDetailsComponentLookupOffset),
            new ComponentLookupLayout(
                componentLookupNameAndIndexBucketOffset),
            new ComponentNameAndIndexLayout(
                componentNameAndIndexNamePointerOffset,
                componentNameAndIndexIndexOffset,
                componentNameAndIndexEntrySize),
            new ComponentHeaderLayout(
                componentHeaderOwnerEntityOffset),
            new PlayerLayout(
                playerNameOffset),
            new LifeLayout(
                lifeHealthOffset,
                lifeManaOffset,
                lifeEnergyShieldOffset),
            new VitalLayout(
                vitalUnknownStatId0Offset,
                vitalUnknownStatId1Offset,
                vitalLifeComponentPtrOffset,
                vitalReservedFlatOffset,
                vitalReservedPercentOffset,
                vitalTotalStatIdOffset,
                vitalUnknownStatId2Offset,
                vitalUnknownStatId3Offset,
                vitalTotalOffset,
                vitalCurrentOffset),
            new StdVectorLayout(
                stdVectorFirstOffset,
                stdVectorLastOffset,
                stdVectorEndOffset),
            new StdWStringLayout(
                stdWStringBufferOffset,
                stdWStringInlineBufferOffset,
                stdWStringLengthOffset,
                stdWStringCapacityOffset,
                stdWStringSmallCapacityLimit));
    }

    public IntPtr GameStatesGlobalSlot { get; }

    public int GameStateStaticGameStateOffset { get; }

    public int GameStateStaticSidecarOffset { get; }

    public int GameStateCurrentStateVectorOffset { get; }

    public int GameStateCurrentStateVectorLastOffset { get; }

    public int GameStateDispatchVectorOffset { get; }

    public int GameStateDispatchVectorLastOffset { get; }

    public int GameStateDispatchVectorEndOffset { get; }

    public int GameStateDirtyFlagOffset { get; }

    public int StdVectorFirstOffset { get; }

    public int StdVectorLastOffset { get; }

    public int StdVectorEndOffset { get; }

    public int GameStateTableOffset { get; }

    public int GameStateEntrySize { get; }

    public int GameStateCount { get; }

    public int GameStateVirtualUpdateSlotOffset { get; }

    public IntPtr InGameStateMsElapsedWriterFunctionAddress { get; }

    public int InGameStateAreaInstanceDataOffset { get; }

    public int InGameStateMsElapsedOffset { get; }

    public int InGameStateZoneSwitchCounterOffset { get; }

    public int AreaInstanceCurrentAreaLevelOffset { get; }

    public int AreaInstanceCurrentAreaHashOffset { get; }

    public int AreaInstanceLocalPlayersOffset { get; }

    public int AreaInstanceEntityTreeRootOffset { get; }

    public int AreaInstanceEntitiesCountOffset => AreaInstanceEntityTreeRootOffset + IntPtr.Size;

    public int EntityDetailsPtrOffset { get; }

    public int EntityComponentListOffset { get; }

    public int EntityIdOffset { get; }

    public int EntityStatusOffset { get; }

    public int EntityActiveFlagOffset { get; }

    public byte EntityStatusInvalidMask { get; }

    public uint EntityIdUpperBound { get; }

    public byte EntityActiveFlagRequiredMask { get; }

    public int EntityDetailsNameOffset { get; }

    public int EntityDetailsComponentLookupOffset { get; }

    public int ComponentLookupNameAndIndexBucketOffset { get; }

    public int ComponentNameAndIndexNamePointerOffset { get; }

    public int ComponentNameAndIndexIndexOffset { get; }

    public int ComponentNameAndIndexEntrySize { get; }

    public int ComponentHeaderOwnerEntityOffset { get; }

    public int PlayerNameOffset { get; }

    public int StdWStringBufferOffset { get; }

    public int StdWStringInlineBufferOffset { get; }

    public int StdWStringLengthOffset { get; }

    public int StdWStringCapacityOffset { get; }

    public int StdWStringSmallCapacityLimit { get; }

    public int LifeHealthOffset { get; }

    public int LifeManaOffset { get; }

    public int LifeEnergyShieldOffset { get; }

    public int VitalUnknownStatId0Offset { get; }

    public int VitalUnknownStatId1Offset { get; }

    public int VitalLifeComponentPtrOffset { get; }

    public int VitalReservedFlatOffset { get; }

    public int VitalReservedPercentOffset { get; }

    public int VitalTotalStatIdOffset { get; }

    public int VitalUnknownStatId2Offset { get; }

    public int VitalUnknownStatId3Offset { get; }

    public int VitalTotalOffset { get; }

    public int VitalCurrentOffset { get; }

    public int GetGameStateEntryOffset(GameStateTypes stateType)
    {
        var index = (int)stateType;
        if ((uint)index >= (uint)GameStateCount)
        {
            throw new IndexOutOfRangeException(
                $"Game state index {index} is outside recovered table count {GameStateCount}.");
        }

        return GameStateTableOffset + index * GameStateEntrySize;
    }

    public StdTuple2D<IntPtr> ReadGameStateEntry(IMemory memory, IntPtr gameStateOwner, GameStateTypes stateType)
    {
        return memory.Read<StdTuple2D<IntPtr>>(Add(gameStateOwner, GetGameStateEntryOffset(stateType)));
    }

    public StdTuple2D<IntPtr> ReadInGameStateEntry(IMemory memory, IntPtr gameStateOwner)
    {
        for (var i = 0; i < GameStateCount; i++)
        {
            var entry = memory.Read<StdTuple2D<IntPtr>>(Add(gameStateOwner, GameStateTableOffset + i * GameStateEntrySize));
            if (entry.X == IntPtr.Zero)
            {
                continue;
            }

            if (StateUpdateSlotBranchesToElapsedWriter(memory, entry.X))
            {
                return entry;
            }
        }

        throw new InvalidOperationException(
            $"Could not find an InGameState entry whose update slot branches to {ToHex(InGameStateMsElapsedWriterFunctionAddress)}.");
    }

    public bool StateUpdateSlotBranchesToElapsedWriter(IMemory memory, IntPtr stateAddress)
    {
        try
        {
            var vtable = memory.Read<IntPtr>(stateAddress);
            if (vtable == IntPtr.Zero)
            {
                return false;
            }

            var updateSlotFunction = memory.Read<IntPtr>(Add(vtable, GameStateVirtualUpdateSlotOffset));
            return updateSlotFunction != IntPtr.Zero &&
                   ContainsRelativeBranchTo(
                       memory,
                       updateSlotFunction,
                       MaxStateUpdateFunctionScanBytes,
                       InGameStateMsElapsedWriterFunctionAddress);
        }
        catch
        {
            return false;
        }
    }

    public StdVectorHeader ReadCurrentStateVector(IMemory memory, IntPtr gameStateOwner)
    {
        return ReadStdVectorHeader(memory, Add(gameStateOwner, GameStateCurrentStateVectorOffset));
    }

    public StdVectorHeader ReadDispatchStateVector(IMemory memory, IntPtr gameStateOwner)
    {
        return ReadStdVectorHeader(memory, Add(gameStateOwner, GameStateDispatchVectorOffset));
    }

    public StdTuple2D<IntPtr> ReadCurrentStateEntry(IMemory memory, IntPtr gameStateOwner)
    {
        var currentStateVector = ReadCurrentStateVector(memory, gameStateOwner);
        if (!currentStateVector.IsValidFor(GameStateEntrySize) ||
            currentStateVector.Count(GameStateEntrySize) <= 0)
        {
            return default;
        }

        return memory.Read<StdTuple2D<IntPtr>>(Add(currentStateVector.Last, -GameStateEntrySize));
    }

    public IntPtr ReadCurrentStateAddress(IMemory memory, IntPtr gameStateOwner)
    {
        return ReadCurrentStateEntry(memory, gameStateOwner).X;
    }

    public StdVectorHeader ReadStdVectorHeader(IMemory memory, IntPtr vectorAddress)
    {
        return new StdVectorHeader(
            memory.Read<IntPtr>(Add(vectorAddress, StdVectorFirstOffset)),
            memory.Read<IntPtr>(Add(vectorAddress, StdVectorLastOffset)),
            memory.Read<IntPtr>(Add(vectorAddress, StdVectorEndOffset)));
    }

    public T[] ReadStdVector<T>(
        IMemory memory,
        IntPtr vectorAddress,
        int maxElements = 4096)
        where T : unmanaged
    {
        return ReadStdVector<T>(memory, ReadStdVectorHeader(memory, vectorAddress), maxElements);
    }

    public T[] ReadStdVector<T>(
        IMemory memory,
        StdVectorHeader vector,
        int maxElements = 4096)
        where T : unmanaged
    {
        var elementSize = Unsafe.SizeOf<T>();
        if (!vector.IsValidFor(elementSize))
        {
            return [];
        }

        var length = vector.Last.ToInt64() - vector.First.ToInt64();
        if (length <= 0)
        {
            return [];
        }

        var count = checked((int)(length / elementSize));
        if (count > maxElements)
        {
            return [];
        }

        return memory.Read<T>(vector.First, count);
    }

    public int CountStdVector<T>(StdVectorHeader vector)
        where T : unmanaged
    {
        return vector.Count(Unsafe.SizeOf<T>());
    }

    public int CapacityStdVector<T>(StdVectorHeader vector)
        where T : unmanaged
    {
        return vector.Capacity(Unsafe.SizeOf<T>());
    }

    public bool IsValidStdVector<T>(StdVectorHeader vector)
        where T : unmanaged
    {
        return vector.IsValidFor(Unsafe.SizeOf<T>());
    }

    public bool IsValidStdVectorHeader(StdVectorHeader vector, int elementSize)
    {
        return vector.IsValidFor(elementSize);
    }

    public string ReadStdWString(IMemory memory, IntPtr stringAddress, int maxLength = 1000)
    {
        var length = memory.Read<int>(Add(stringAddress, StdWStringLengthOffset));
        var capacity = memory.Read<int>(Add(stringAddress, StdWStringCapacityOffset));
        if (length <= 0 ||
            length > maxLength ||
            capacity < length ||
            capacity > maxLength)
        {
            return string.Empty;
        }

        var bufferAddress = capacity <= StdWStringSmallCapacityLimit
            ? Add(stringAddress, StdWStringInlineBufferOffset)
            : memory.Read<IntPtr>(Add(stringAddress, StdWStringBufferOffset));
        if (bufferAddress == IntPtr.Zero)
        {
            return string.Empty;
        }

        var buffer = memory.Read<byte>(bufferAddress, length * sizeof(char));
        return Encoding.Unicode.GetString(buffer);
    }

    public VitalStruct ReadVitalStruct(IMemory memory, IntPtr vitalAddress)
    {
        return new VitalStruct
        {
            UnknownStatId0 = memory.Read<int>(Add(vitalAddress, VitalUnknownStatId0Offset)),
            UnknownStatId1 = memory.Read<int>(Add(vitalAddress, VitalUnknownStatId1Offset)),
            LifeComponentPtr = memory.Read<IntPtr>(Add(vitalAddress, VitalLifeComponentPtrOffset)),
            ReservedFlat = memory.Read<int>(Add(vitalAddress, VitalReservedFlatOffset)),
            ReservedPercent = memory.Read<int>(Add(vitalAddress, VitalReservedPercentOffset)),
            TotalStatId = memory.Read<int>(Add(vitalAddress, VitalTotalStatIdOffset)),
            UnknownStatId2 = memory.Read<int>(Add(vitalAddress, VitalUnknownStatId2Offset)),
            UnknownStatId3 = memory.Read<int>(Add(vitalAddress, VitalUnknownStatId3Offset)),
            Total = memory.Read<int>(Add(vitalAddress, VitalTotalOffset)),
            Current = memory.Read<int>(Add(vitalAddress, VitalCurrentOffset)),
        };
    }

    public bool PassesEntityIdentityFilter(IMemory memory, IntPtr entityAddress, out uint id)
    {
        id = 0;
        if (entityAddress == IntPtr.Zero)
        {
            return false;
        }

        var status = memory.Read<byte>(Add(entityAddress, EntityStatusOffset));
        var activeFlags = memory.Read<byte>(Add(entityAddress, EntityActiveFlagOffset));
        id = memory.Read<uint>(Add(entityAddress, EntityIdOffset));
        return PassesEntityIdentityFilter(status, id, activeFlags);
    }

    public bool PassesEntityIdentityFilter(byte status, uint id, byte activeFlags)
    {
        return (status & EntityStatusInvalidMask) == 0 &&
               id < EntityIdUpperBound &&
               (activeFlags & EntityActiveFlagRequiredMask) == EntityActiveFlagRequiredMask;
    }

    public IReadOnlyList<ComponentNameIndexEntry> ReadComponentNameIndexEntries(
        IMemory memory,
        IntPtr componentBucketAddress,
        int maxEntries = 256)
    {
        var componentBucketData = ReadStdVectorHeader(memory, componentBucketAddress);
        return ReadComponentNameIndexEntries(memory, componentBucketData, maxEntries);
    }

    public IReadOnlyList<ComponentNameIndexEntry> ReadComponentNameIndexEntries(
        IMemory memory,
        StdBucket componentBucket,
        int maxEntries = 256)
    {
        return ReadComponentNameIndexEntries(
            memory,
            new StdVectorHeader(componentBucket.Data.First, componentBucket.Data.Last, componentBucket.Data.End),
            maxEntries);
    }

    private IReadOnlyList<ComponentNameIndexEntry> ReadComponentNameIndexEntries(
        IMemory memory,
        StdVectorHeader componentBucketData,
        int maxEntries)
    {
        var count = componentBucketData.Count(ComponentNameAndIndexEntrySize);
        if (count <= 0 ||
            count > maxEntries)
        {
            return [];
        }

        var entries = new List<ComponentNameIndexEntry>(count);
        for (var i = 0; i < count; i++)
        {
            var entryAddress = Add(componentBucketData.First, (long)i * ComponentNameAndIndexEntrySize);
            var namePtr = memory.Read<IntPtr>(Add(entryAddress, ComponentNameAndIndexNamePointerOffset));
            if (namePtr == IntPtr.Zero)
            {
                continue;
            }

            var name = memory.ReadString(namePtr);
            if (string.IsNullOrEmpty(name))
            {
                continue;
            }

            var index = memory.Read<int>(Add(entryAddress, ComponentNameAndIndexIndexOffset));
            entries.Add(new ComponentNameIndexEntry(name, index, namePtr));
        }

        return entries;
    }

    public static RuntimeGameOffsets GetOrResolve(IMemory memory)
    {
        return Cache.GetValue(
            memory,
            key => new Lazy<RuntimeGameOffsets>(() => Resolve(key))).Value;
    }

    public static RuntimeGameOffsets Resolve(IMemory memory)
    {
        var staticPatternOffsets = MemoryUtils
            .GetOffsets(memory, Offsets.Patterns)
            .ToDictionary(x => x.Key.Name!, x => x.Value);
        var keypointOffsets = MemoryUtils
            .GetOffsets(memory, Offsets.KeypointPatterns)
            .ToDictionary(x => x.Key.Name!, x => x.Value);

        var gameStatesGlobalSlot = ResolveRipRelativeSlot(memory, Require(staticPatternOffsets, nameof(GameStates)));

        var wrapperCopyAddress = Add(memory.BaseAddress, Require(keypointOffsets, Offsets.KeypointNames.GameStateStaticWrapper));
        var ownerStoreOpcode = memory.Read<byte>(wrapperCopyAddress, 3);
        if (!ownerStoreOpcode.SequenceEqual(new byte[] { 0x48, 0x89, 0x06 }))
        {
            throw new InvalidOperationException(
                $"Unexpected GameStateStaticWrapper owner store opcode at {ToHex(wrapperCopyAddress)}.");
        }

        var ownerGlobalSlot = ResolveRipRelativeSlot(memory, Require(keypointOffsets, Offsets.KeypointNames.GameStateStaticWrapper) - 0x04);
        if (ownerGlobalSlot != gameStatesGlobalSlot)
        {
            throw new InvalidOperationException(
                $"GameStates global slot mismatch: primary={ToHex(gameStatesGlobalSlot)}, wrapper={ToHex(ownerGlobalSlot)}.");
        }

        var gameStateStaticSidecarOffset = memory.Read<byte>(Add(wrapperCopyAddress, 0x0D));

        var tableShapeAddress = Add(memory.BaseAddress, Require(keypointOffsets, Offsets.KeypointNames.GameStateTableShape));
        var gameStateTableOffset = memory.Read<byte>(tableShapeAddress);
        var gameStateEntrySize = memory.Read<int>(Add(tableShapeAddress, 21));
        var gameStateCount = memory.Read<int>(Add(tableShapeAddress, 27));

        var currentVectorAddress = Add(memory.BaseAddress, Require(keypointOffsets, Offsets.KeypointNames.GameStateCurrentStateVector));
        var currentStateVectorOffset = memory.Read<byte>(currentVectorAddress);
        var currentStateVectorLastOffset = memory.Read<byte>(Add(currentVectorAddress, -0x04));
        var gameStateDispatchVectorLastOffset = memory.Read<byte>(Add(currentVectorAddress, 0x08));
        var gameStateDispatchVectorOffset = memory.Read<byte>(Add(currentVectorAddress, 0x0C));
        var gameStateDispatchVectorEndOffset = gameStateDispatchVectorLastOffset + IntPtr.Size;
        var gameStateDirtyFlagOffset = memory.Read<byte>(Add(currentVectorAddress, -0x0F));
        var stdVectorFirstOffset = 0;
        var stdVectorLastOffset = currentStateVectorLastOffset - currentStateVectorOffset;
        var stdVectorEndOffset = stdVectorLastOffset + IntPtr.Size;
        if (gameStateDispatchVectorLastOffset - gameStateDispatchVectorOffset != stdVectorLastOffset ||
            gameStateDispatchVectorEndOffset - gameStateDispatchVectorOffset != stdVectorEndOffset)
        {
            throw new InvalidOperationException(
                $"GameState dispatch vector shape mismatch: source=0x{currentStateVectorOffset:X}/0x{currentStateVectorLastOffset:X}, dispatch=0x{gameStateDispatchVectorOffset:X}/0x{gameStateDispatchVectorLastOffset:X}/0x{gameStateDispatchVectorEndOffset:X}.");
        }

        var gameStateVirtualUpdateSlotOffset = memory.Read<byte>(
            Add(memory.BaseAddress, Require(keypointOffsets, Offsets.KeypointNames.GameStateVirtualUpdateSlot)));

        var areaInstanceOffset = memory.Read<int>(
            Add(memory.BaseAddress, Require(keypointOffsets, Offsets.KeypointNames.InGameStateAreaInstanceData)));
        var msElapsedWriterFunctionAddress = Add(
            memory.BaseAddress,
            Require(keypointOffsets, Offsets.KeypointNames.InGameStateMsElapsedWriterFunctionStart));
        var msElapsedOffsetAddress = Add(memory.BaseAddress, Require(keypointOffsets, Offsets.KeypointNames.InGameStateMsElapsed));
        var msElapsedOffset = memory.Read<int>(msElapsedOffsetAddress);
        var zoneSwitchCounterOffset = memory.Read<int>(
            Add(memory.BaseAddress, Require(keypointOffsets, Offsets.KeypointNames.InGameStateZoneSwitchCounter)));

        var areaLevelAddress = Add(memory.BaseAddress, Require(keypointOffsets, Offsets.KeypointNames.AreaInstanceCurrentAreaLevel));
        var areaOffsetFromLevelBlock = memory.Read<int>(Add(areaLevelAddress, -7));
        if (areaOffsetFromLevelBlock != areaInstanceOffset)
        {
            throw new InvalidOperationException(
                $"AreaInstanceData offset mismatch: area keypoint=0x{areaInstanceOffset:X}, level block=0x{areaOffsetFromLevelBlock:X}.");
        }

        var areaHashAddress = Add(memory.BaseAddress, Require(keypointOffsets, Offsets.KeypointNames.AreaInstanceCurrentAreaHash));
        var localPlayersAddress = Add(memory.BaseAddress, Require(keypointOffsets, Offsets.KeypointNames.AreaInstanceLocalPlayers));
        var entityTreeRootAddress = Add(memory.BaseAddress, Require(keypointOffsets, Offsets.KeypointNames.AreaInstanceEntityTreeRoot));
        var entityTreeRootOffset = memory.Read<int>(entityTreeRootAddress);

        var identityFilterAddress = Add(memory.BaseAddress, Require(keypointOffsets, Offsets.KeypointNames.EntityIdentityFilter));
        var entityTreeRootOffsetFromFilter = memory.Read<int>(Add(identityFilterAddress, -0x18));
        if (entityTreeRootOffsetFromFilter != entityTreeRootOffset)
        {
            throw new InvalidOperationException(
                $"Entity tree root offset mismatch: area keypoint=0x{entityTreeRootOffset:X}, identity filter=0x{entityTreeRootOffsetFromFilter:X}.");
        }

        var entityStatusOffset = memory.Read<int>(identityFilterAddress);
        var entityIdOffset = memory.Read<int>(Add(identityFilterAddress, 0x09));
        var entityActiveFlagOffset = memory.Read<int>(Add(identityFilterAddress, 0x15));
        var entityStatusInvalidMask = memory.Read<byte>(Add(identityFilterAddress, 0x04));
        var entityIdUpperBound = memory.Read<uint>(Add(identityFilterAddress, 0x0D));
        var entityActiveFlagRequiredMask = memory.Read<byte>(Add(identityFilterAddress, 0x19));
        if (entityStatusInvalidMask == 0 ||
            entityIdUpperBound == 0 ||
            entityActiveFlagRequiredMask == 0)
        {
            throw new InvalidOperationException(
                $"Unexpected EntityIdentityFilter operands: statusMask=0x{entityStatusInvalidMask:X2}, idUpperBound=0x{entityIdUpperBound:X8}, activeMask=0x{entityActiveFlagRequiredMask:X2}.");
        }

        var entityNameAccessorAddress = Add(memory.BaseAddress, Require(keypointOffsets, Offsets.KeypointNames.EntityDetailsName));
        var entityDetailsNameOffset = memory.Read<byte>(entityNameAccessorAddress);
        var entityDetailsPtrOffsetFromNameAccessor = memory.Read<byte>(Add(entityNameAccessorAddress, -0x04));

        var lookupShapeAddress = Add(memory.BaseAddress, Require(keypointOffsets, Offsets.KeypointNames.EntityComponentLookupShape));
        var entityDetailsPtrOffset = memory.Read<byte>(lookupShapeAddress);
        var entityDetailsPtrOffsetSecondRead = memory.Read<byte>(Add(lookupShapeAddress, 0x0E));
        if (entityDetailsPtrOffsetSecondRead != entityDetailsPtrOffset ||
            entityDetailsPtrOffsetFromNameAccessor != entityDetailsPtrOffset)
        {
            throw new InvalidOperationException(
                $"EntityDetails pointer offset mismatch: lookup=0x{entityDetailsPtrOffset:X}, lookupSecond=0x{entityDetailsPtrOffsetSecondRead:X}, nameAccessor=0x{entityDetailsPtrOffsetFromNameAccessor:X}.");
        }

        var entityDetailsComponentLookupOffset = memory.Read<byte>(Add(lookupShapeAddress, 0x1C));
        var componentLookupNameAndIndexBucketOffset = memory.Read<byte>(Add(lookupShapeAddress, 0x2C));
        var componentLookupNameAndIndexBucketEndOffset = memory.Read<byte>(Add(lookupShapeAddress, 0x38));
        if (componentLookupNameAndIndexBucketEndOffset != componentLookupNameAndIndexBucketOffset + stdVectorLastOffset)
        {
            throw new InvalidOperationException(
                $"Component lookup bucket end offset mismatch: bucket=0x{componentLookupNameAndIndexBucketOffset:X}, end=0x{componentLookupNameAndIndexBucketEndOffset:X}.");
        }

        var componentNameAndIndexIndexOffset = memory.Read<byte>(Add(lookupShapeAddress, 0x3E));
        var entityComponentListOffset = memory.Read<byte>(Add(lookupShapeAddress, 0x47));

        var componentEntryStrideShiftAddress =
            Add(memory.BaseAddress, Require(keypointOffsets, Offsets.KeypointNames.ComponentNameAndIndexEntryStride));
        var componentNameAndIndexEntryShift = memory.Read<byte>(componentEntryStrideShiftAddress);
        var componentNameAndIndexEntrySize = 1 << componentNameAndIndexEntryShift;
        var componentNameAndIndexNamePointerOffset = 0;
        var componentEntryNameReadOpcode = memory.Read<byte>(Add(componentEntryStrideShiftAddress, 0x04), 3);
        if (!componentEntryNameReadOpcode.SequenceEqual(new byte[] { 0x48, 0x8B, 0x0A }) ||
            componentNameAndIndexEntrySize < componentNameAndIndexIndexOffset + sizeof(int))
        {
            throw new InvalidOperationException(
                $"Unexpected ComponentNameAndIndex entry shape at {ToHex(componentEntryStrideShiftAddress)}.");
        }

        var ownerOffsetAddress = Add(memory.BaseAddress, Require(keypointOffsets, Offsets.KeypointNames.PlayerComponentHeaderOwnerEntity));
        var componentHeaderOwnerEntityOffset = memory.Read<byte>(ownerOffsetAddress);
        var ownerReadInstructionBytes = memory.Read<byte>(Add(ownerOffsetAddress, -0x03), 4);
        if (!ownerReadInstructionBytes.SequenceEqual(new byte[] { 0x48, 0x8B, 0x49, 0x08 }))
        {
            throw new InvalidOperationException(
                $"Unexpected Player component owner read opcode at {ToHex(Add(ownerOffsetAddress, -0x03))}.");
        }

        var playerNameAddress = Add(memory.BaseAddress, Require(keypointOffsets, Offsets.KeypointNames.PlayerComponentName));
        var playerNameOffset = memory.Read<int>(playerNameAddress);
        var stdWStringCapacityOffset = memory.Read<byte>(Add(playerNameAddress, 0x0C));
        var stdWStringSmallCapacityLimit = memory.Read<byte>(Add(playerNameAddress, 0x0D));
        var stdWStringLengthOffset = memory.Read<byte>(Add(playerNameAddress, 0x11));
        var stdWStringExternalBufferOpcode = memory.Read<byte>(Add(playerNameAddress, 0x14), 3);
        if (!stdWStringExternalBufferOpcode.SequenceEqual(new byte[] { 0x48, 0x8B, 0x1B }))
        {
            throw new InvalidOperationException(
                $"Unexpected StdWString external-buffer opcode at {ToHex(Add(playerNameAddress, 0x14))}.");
        }

        var stdWStringBufferOffset = 0;
        var stdWStringInlineBufferOffset = 0;

        var vitalStartsAddress = Add(memory.BaseAddress, Require(keypointOffsets, Offsets.KeypointNames.LifeComponentVitalOffsets));
        var lifeHealthOffset = memory.Read<int>(vitalStartsAddress);
        var lifeManaOffset = memory.Read<int>(Add(vitalStartsAddress, 0x0C));
        var lifeEnergyShieldOffset = memory.Read<int>(Add(vitalStartsAddress, 0x1B));

        var currentTotalAddress = Add(memory.BaseAddress, Require(keypointOffsets, Offsets.KeypointNames.LifeVitalCurrentTotal));
        var healthCurrentAbsoluteOffset = memory.Read<int>(currentTotalAddress);
        var healthTotalAbsoluteOffset = memory.Read<int>(Add(currentTotalAddress, 0x1D));
        var energyShieldCurrentAbsoluteOffset = memory.Read<int>(Add(currentTotalAddress, 0x49));
        var energyShieldTotalAbsoluteOffset = memory.Read<int>(Add(currentTotalAddress, 0x66));
        var manaCurrentAbsoluteOffset = memory.Read<int>(Add(currentTotalAddress, 0x92));
        var manaTotalAbsoluteOffset = memory.Read<int>(Add(currentTotalAddress, 0xAF));
        var vitalCurrentOffset = healthCurrentAbsoluteOffset - lifeHealthOffset;
        var vitalTotalOffset = healthTotalAbsoluteOffset - lifeHealthOffset;
        if (energyShieldCurrentAbsoluteOffset - lifeEnergyShieldOffset != vitalCurrentOffset ||
            manaCurrentAbsoluteOffset - lifeManaOffset != vitalCurrentOffset ||
            energyShieldTotalAbsoluteOffset - lifeEnergyShieldOffset != vitalTotalOffset ||
            manaTotalAbsoluteOffset - lifeManaOffset != vitalTotalOffset)
        {
            throw new InvalidOperationException("Life vital current/total offsets are inconsistent across health, mana, and energy shield.");
        }

        var reservationAddress = Add(memory.BaseAddress, Require(keypointOffsets, Offsets.KeypointNames.LifeVitalReservationOffsets));
        var vitalReservedFlatOffset = memory.Read<byte>(reservationAddress);
        var vitalReservedPercentOffset = memory.Read<byte>(Add(reservationAddress, 0x28));

        var constructorShapeAddress = Add(memory.BaseAddress, Require(keypointOffsets, Offsets.KeypointNames.LifeVitalConstructorShape));
        var sharedConstructorShapeAddress = Add(memory.BaseAddress, Require(keypointOffsets, Offsets.KeypointNames.LifeVitalSharedConstructorShape));
        var healthOffsetFromConstructor = memory.Read<int>(constructorShapeAddress);
        var manaOffsetFromConstructor = memory.Read<int>(Add(constructorShapeAddress, 0x90));
        var energyShieldOffsetFromConstructor = memory.Read<int>(Add(constructorShapeAddress, 0xFC));
        if (healthOffsetFromConstructor != lifeHealthOffset ||
            manaOffsetFromConstructor != lifeManaOffset ||
            energyShieldOffsetFromConstructor != lifeEnergyShieldOffset)
        {
            throw new InvalidOperationException(
                $"Life vital start mismatch: vtable=0x{lifeHealthOffset:X}/0x{lifeManaOffset:X}/0x{lifeEnergyShieldOffset:X}, constructor=0x{healthOffsetFromConstructor:X}/0x{manaOffsetFromConstructor:X}/0x{energyShieldOffsetFromConstructor:X}.");
        }

        var vitalUnknownStatId0Offset = memory.Read<byte>(Add(constructorShapeAddress, 0x07));
        var vitalUnknownStatId1Offset = memory.Read<byte>(Add(constructorShapeAddress, 0x0F));
        var vitalLifeComponentPtrOffset = memory.Read<byte>(Add(constructorShapeAddress, 0x17));
        var vitalTotalStatIdOffset = memory.Read<byte>(Add(constructorShapeAddress, 0x29));
        var vitalUnknownStatId2Offset = memory.Read<byte>(Add(constructorShapeAddress, 0x31));
        var vitalUnknownStatId3Offset = memory.Read<byte>(Add(constructorShapeAddress, 0x39));
        var sharedVitalLifeComponentPtrOffset = memory.Read<byte>(sharedConstructorShapeAddress);
        var sharedVitalUnknownStatId3Offset = memory.Read<byte>(Add(sharedConstructorShapeAddress, 0x16));
        var sharedVitalUnknownStatId0Offset = memory.Read<byte>(Add(sharedConstructorShapeAddress, 0x19));
        var sharedVitalUnknownStatId1Offset = memory.Read<byte>(Add(sharedConstructorShapeAddress, 0x20));
        var sharedVitalTotalStatIdOffset = memory.Read<byte>(Add(sharedConstructorShapeAddress, 0x2C));
        var sharedVitalUnknownStatId2Offset = memory.Read<byte>(Add(sharedConstructorShapeAddress, 0x30));
        if (sharedVitalLifeComponentPtrOffset != vitalLifeComponentPtrOffset ||
            sharedVitalUnknownStatId0Offset != vitalUnknownStatId0Offset ||
            sharedVitalUnknownStatId1Offset != vitalUnknownStatId1Offset ||
            sharedVitalUnknownStatId3Offset != vitalUnknownStatId3Offset ||
            sharedVitalTotalStatIdOffset != vitalTotalStatIdOffset ||
            sharedVitalUnknownStatId2Offset != vitalUnknownStatId2Offset)
        {
            throw new InvalidOperationException("Life vital constructor field offsets are inconsistent between direct and shared constructor keypoints.");
        }

        return new RuntimeGameOffsets(
            gameStatesGlobalSlot,
            gameStateStaticGameStateOffset: 0,
            gameStateStaticSidecarOffset,
            currentStateVectorOffset,
            currentStateVectorLastOffset,
            gameStateDispatchVectorOffset,
            gameStateDispatchVectorLastOffset,
            gameStateDispatchVectorEndOffset,
            gameStateDirtyFlagOffset,
            stdVectorFirstOffset,
            stdVectorLastOffset,
            stdVectorEndOffset,
            gameStateTableOffset,
            gameStateEntrySize,
            gameStateCount,
            gameStateVirtualUpdateSlotOffset,
            msElapsedWriterFunctionAddress,
            areaInstanceOffset,
            msElapsedOffset,
            zoneSwitchCounterOffset,
            memory.Read<int>(areaLevelAddress),
            memory.Read<int>(areaHashAddress),
            memory.Read<int>(localPlayersAddress),
            entityTreeRootOffset,
            entityDetailsPtrOffset,
            entityComponentListOffset,
            entityIdOffset,
            entityStatusOffset,
            entityActiveFlagOffset,
            entityStatusInvalidMask,
            entityIdUpperBound,
            entityActiveFlagRequiredMask,
            entityDetailsNameOffset,
            entityDetailsComponentLookupOffset,
            componentLookupNameAndIndexBucketOffset,
            componentNameAndIndexNamePointerOffset,
            componentNameAndIndexIndexOffset,
            componentNameAndIndexEntrySize,
            componentHeaderOwnerEntityOffset,
            playerNameOffset,
            stdWStringBufferOffset,
            stdWStringInlineBufferOffset,
            stdWStringLengthOffset,
            stdWStringCapacityOffset,
            stdWStringSmallCapacityLimit,
            lifeHealthOffset,
            lifeManaOffset,
            lifeEnergyShieldOffset,
            vitalUnknownStatId0Offset,
            vitalUnknownStatId1Offset,
            vitalLifeComponentPtrOffset,
            vitalReservedFlatOffset,
            vitalReservedPercentOffset,
            vitalTotalStatIdOffset,
            vitalUnknownStatId2Offset,
            vitalUnknownStatId3Offset,
            vitalTotalOffset,
            vitalCurrentOffset);
    }

    private static int Require(IReadOnlyDictionary<string, int> offsets, string key)
    {
        if (!offsets.TryGetValue(key, out var value))
        {
            throw new InvalidOperationException($"Required runtime offset pattern was not found: {key}");
        }

        return value;
    }

    private static IntPtr ResolveRipRelativeSlot(IMemory memory, long displacementRva)
    {
        var displacementAddress = Add(memory.BaseAddress, displacementRva);
        var relativeOffset = memory.Read<int>(displacementAddress);
        return Add(displacementAddress, relativeOffset + sizeof(int));
    }

    public static IntPtr Add(IntPtr address, long offset)
    {
        return new IntPtr(checked((nint)(address.ToInt64() + offset)));
    }

    public static IntPtr Add(long address, long offset)
    {
        return new IntPtr(checked((nint)(address + offset)));
    }

    private static string ToHex(IntPtr address)
    {
        return $"0x{address.ToInt64():X}";
    }

    private static bool ContainsRelativeBranchTo(IMemory memory, IntPtr searchStart, int byteCount, IntPtr target)
    {
        var bytes = memory.Read<byte>(searchStart, byteCount);
        for (var i = 0; i <= bytes.Length - 5; i++)
        {
            if (bytes[i] != 0xE8 &&
                bytes[i] != 0xE9)
            {
                continue;
            }

            var rel32 = BitConverter.ToInt32(bytes, i + 1);
            var branchTarget = Add(searchStart, i + 5 + rel32);
            if (branchTarget == target)
            {
                return true;
            }
        }

        return false;
    }

    public readonly record struct ComponentNameIndexEntry(string Name, int Index, IntPtr NamePtr);

    public readonly record struct StdVectorHeader(IntPtr First, IntPtr Last, IntPtr End)
    {
        public bool IsValidFor(int elementSize)
        {
            if (elementSize <= 0)
            {
                return false;
            }

            var first = First.ToInt64();
            var last = Last.ToInt64();
            var end = End.ToInt64();
            if (first == 0 ||
                last < first ||
                end < last)
            {
                return false;
            }

            var length = last - first;
            var capacity = end - first;
            return length % elementSize == 0 &&
                   capacity % elementSize == 0;
        }

        public int Count(int elementSize)
        {
            if (!IsValidFor(elementSize))
            {
                return 0;
            }

            var length = Last.ToInt64() - First.ToInt64();
            return checked((int)(length / elementSize));
        }

        public int Capacity(int elementSize)
        {
            if (!IsValidFor(elementSize))
            {
                return 0;
            }

            var capacity = End.ToInt64() - First.ToInt64();
            return checked((int)(capacity / elementSize));
        }
    }
}
