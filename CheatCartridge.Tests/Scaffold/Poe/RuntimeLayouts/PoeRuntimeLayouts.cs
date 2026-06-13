using System.Runtime.CompilerServices;
using System.Text;
using CheatCartridge.GameHelper.GameOffsets;
using CheatCartridge.GameHelper.GameOffsets.Objects.Components;
using CheatCartridge.GameHelper.Natives;
using CheatCartridge.GameHelper.RemoteEnums;
using EyeAuras.Memory;
using EyeAuras.Memory.Scaffolding;

namespace CheatCartridge.Tests.Scaffold.Poe.RuntimeLayouts;

/// <summary>
/// Typed runtime layouts recovered from the current game client.
/// </summary>
public sealed record PoeRuntimeLayouts(
    GameStatesLayout GameStates,
    InGameStateLayout InGameState,
    AreaInstanceLayout AreaInstance,
    EntityLayout Entity,
    EntityDetailsLayout EntityDetails,
    ComponentLookupLayout ComponentLookup,
    ComponentNameAndIndexLayout ComponentNameAndIndex,
    ComponentHeaderLayout ComponentHeader,
    PlayerLayout Player,
    LifeLayout Life,
    VitalLayout Vital,
    StdVectorLayout StdVector,
    StdWStringLayout StdWString)
{
    public static PoeRuntimeLayouts GetOrResolve(IMemory memory)
    {
        return RuntimeGameOffsets.GetOrResolve(memory).Layouts;
    }

    public static PoeRuntimeLayouts Resolve(IMemory memory)
    {
        return RuntimeGameOffsets.Resolve(memory).Layouts;
    }
}

public sealed record GameStatesLayout(
    IntPtr GlobalSlot,
    int StaticGameStateOffset,
    int StaticSidecarOffset,
    int CurrentStateVectorOffset,
    int CurrentStateVectorLastOffset,
    int DispatchVectorOffset,
    int DispatchVectorLastOffset,
    int DispatchVectorEndOffset,
    int DirtyFlagOffset,
    int TableOffset,
    int EntrySize,
    int Count,
    int VirtualUpdateSlotOffset)
{
    public int GetEntryOffset(GameStateTypes stateType)
    {
        var index = (int)stateType;
        if ((uint)index >= (uint)Count)
        {
            throw new IndexOutOfRangeException(
                $"Game state index {index} is outside recovered table count {Count}.");
        }

        return TableOffset + index * EntrySize;
    }
}

public sealed record InGameStateLayout(
    IntPtr MsElapsedWriterFunctionAddress,
    int AreaInstanceDataOffset,
    int MsElapsedOffset,
    int ZoneSwitchCounterOffset);

public sealed record AreaInstanceLayout(
    int CurrentAreaLevelOffset,
    int CurrentAreaHashOffset,
    int LocalPlayersOffset,
    int EntityTreeRootOffset)
{
    public int EntitiesCountOffset => EntityTreeRootOffset + IntPtr.Size;
}

public sealed record EntityLayout(
    int DetailsPtrOffset,
    int ComponentListOffset,
    int IdOffset,
    int StatusOffset,
    int ActiveFlagOffset,
    byte StatusInvalidMask,
    uint IdUpperBound,
    byte ActiveFlagRequiredMask)
{
    public bool PassesIdentityFilter(byte status, uint id, byte activeFlags)
    {
        return (status & StatusInvalidMask) == 0 &&
               id < IdUpperBound &&
               (activeFlags & ActiveFlagRequiredMask) == ActiveFlagRequiredMask;
    }
}

public sealed record EntityDetailsLayout(
    int NameOffset,
    int ComponentLookupOffset);

public sealed record ComponentLookupLayout(
    int NameAndIndexBucketOffset);

public sealed record ComponentNameAndIndexLayout(
    int NamePointerOffset,
    int IndexOffset,
    int EntrySize);

public sealed record ComponentHeaderLayout(
    int OwnerEntityOffset);

public sealed record PlayerLayout(
    int NameOffset);

public sealed record LifeLayout(
    int HealthOffset,
    int ManaOffset,
    int EnergyShieldOffset);

public sealed record VitalLayout(
    int UnknownStatId0Offset,
    int UnknownStatId1Offset,
    int LifeComponentPtrOffset,
    int ReservedFlatOffset,
    int ReservedPercentOffset,
    int TotalStatIdOffset,
    int UnknownStatId2Offset,
    int UnknownStatId3Offset,
    int TotalOffset,
    int CurrentOffset);

public sealed record StdVectorLayout(
    int FirstOffset,
    int LastOffset,
    int EndOffset);

public sealed record StdWStringLayout(
    int BufferOffset,
    int InlineBufferOffset,
    int LengthOffset,
    int CapacityOffset,
    int SmallCapacityLimit);

public readonly record struct PoeComponentNameIndexEntry(string Name, int Index, IntPtr NamePtr);

public readonly record struct PoeStdVectorHeader(IntPtr First, IntPtr Last, IntPtr End)
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

public static class RuntimeAddress
{
    public static IntPtr Add(IntPtr address, long offset)
    {
        return new IntPtr(checked((nint)(address.ToInt64() + offset)));
    }

    public static IntPtr Add(long address, long offset)
    {
        return new IntPtr(checked((nint)(address + offset)));
    }
}

public static class PoeRuntimeLayoutReaders
{
    private const int MaxStateUpdateFunctionScanBytes = 0x800;

    public static StdTuple2D<IntPtr> ReadGameStateEntry(
        this PoeRuntimeLayouts layouts,
        IMemory memory,
        IntPtr gameStateOwner,
        GameStateTypes stateType)
    {
        return memory.Read<StdTuple2D<IntPtr>>(
            RuntimeAddress.Add(gameStateOwner, layouts.GameStates.GetEntryOffset(stateType)));
    }

    public static StdTuple2D<IntPtr> ReadInGameStateEntry(
        this PoeRuntimeLayouts layouts,
        IMemory memory,
        IntPtr gameStateOwner)
    {
        return layouts.ReadGameStateEntry(memory, gameStateOwner, GameStateTypes.InGameState);
    }

    public static bool StateUpdateSlotBranchesToElapsedWriter(
        this PoeRuntimeLayouts layouts,
        IMemory memory,
        IntPtr stateAddress)
    {
        try
        {
            var vtable = memory.Read<IntPtr>(stateAddress);
            if (vtable == IntPtr.Zero)
            {
                return false;
            }

            var updateSlotFunction = memory.Read<IntPtr>(
                RuntimeAddress.Add(vtable, layouts.GameStates.VirtualUpdateSlotOffset));
            return updateSlotFunction != IntPtr.Zero &&
                   ContainsRelativeBranchTo(
                       memory,
                       updateSlotFunction,
                       MaxStateUpdateFunctionScanBytes,
                       layouts.InGameState.MsElapsedWriterFunctionAddress);
        }
        catch
        {
            return false;
        }
    }

    public static PoeStdVectorHeader ReadCurrentStateVector(
        this PoeRuntimeLayouts layouts,
        IMemory memory,
        IntPtr gameStateOwner)
    {
        return layouts.ReadStdVectorHeader(
            memory,
            RuntimeAddress.Add(gameStateOwner, layouts.GameStates.CurrentStateVectorOffset));
    }

    public static PoeStdVectorHeader ReadDispatchStateVector(
        this PoeRuntimeLayouts layouts,
        IMemory memory,
        IntPtr gameStateOwner)
    {
        return layouts.ReadStdVectorHeader(
            memory,
            RuntimeAddress.Add(gameStateOwner, layouts.GameStates.DispatchVectorOffset));
    }

    public static StdTuple2D<IntPtr> ReadCurrentStateEntry(
        this PoeRuntimeLayouts layouts,
        IMemory memory,
        IntPtr gameStateOwner)
    {
        var currentStateVector = layouts.ReadCurrentStateVector(memory, gameStateOwner);
        if (!currentStateVector.IsValidFor(layouts.GameStates.EntrySize) ||
            currentStateVector.Count(layouts.GameStates.EntrySize) <= 0)
        {
            return default;
        }

        return memory.Read<StdTuple2D<IntPtr>>(
            RuntimeAddress.Add(currentStateVector.Last, -layouts.GameStates.EntrySize));
    }

    public static IntPtr ReadCurrentStateAddress(
        this PoeRuntimeLayouts layouts,
        IMemory memory,
        IntPtr gameStateOwner)
    {
        return layouts.ReadCurrentStateEntry(memory, gameStateOwner).X;
    }

    public static PoeStdVectorHeader ReadStdVectorHeader(
        this PoeRuntimeLayouts layouts,
        IMemory memory,
        IntPtr vectorAddress)
    {
        return new PoeStdVectorHeader(
            memory.Read<IntPtr>(RuntimeAddress.Add(vectorAddress, layouts.StdVector.FirstOffset)),
            memory.Read<IntPtr>(RuntimeAddress.Add(vectorAddress, layouts.StdVector.LastOffset)),
            memory.Read<IntPtr>(RuntimeAddress.Add(vectorAddress, layouts.StdVector.EndOffset)));
    }

    public static T[] ReadStdVector<T>(
        this PoeRuntimeLayouts layouts,
        IMemory memory,
        IntPtr vectorAddress,
        int maxElements = 4096)
        where T : unmanaged
    {
        return layouts.ReadStdVector<T>(
            memory,
            layouts.ReadStdVectorHeader(memory, vectorAddress),
            maxElements);
    }

    public static T[] ReadStdVector<T>(
        this PoeRuntimeLayouts layouts,
        IMemory memory,
        PoeStdVectorHeader vector,
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

    public static string ReadStdWString(
        this PoeRuntimeLayouts layouts,
        IMemory memory,
        IntPtr stringAddress,
        int maxLength = 1000)
    {
        var length = memory.Read<int>(RuntimeAddress.Add(stringAddress, layouts.StdWString.LengthOffset));
        var capacity = memory.Read<int>(RuntimeAddress.Add(stringAddress, layouts.StdWString.CapacityOffset));
        if (length <= 0 ||
            length > maxLength ||
            capacity < length ||
            capacity > maxLength)
        {
            return string.Empty;
        }

        var bufferAddress = capacity <= layouts.StdWString.SmallCapacityLimit
            ? RuntimeAddress.Add(stringAddress, layouts.StdWString.InlineBufferOffset)
            : memory.Read<IntPtr>(RuntimeAddress.Add(stringAddress, layouts.StdWString.BufferOffset));
        if (bufferAddress == IntPtr.Zero)
        {
            return string.Empty;
        }

        var buffer = memory.Read<byte>(bufferAddress, length * sizeof(char));
        return Encoding.Unicode.GetString(buffer);
    }

    public static VitalStruct ReadVitalStruct(
        this PoeRuntimeLayouts layouts,
        IMemory memory,
        IntPtr vitalAddress)
    {
        return new VitalStruct
        {
            UnknownStatId0 = memory.Read<int>(RuntimeAddress.Add(vitalAddress, layouts.Vital.UnknownStatId0Offset)),
            UnknownStatId1 = memory.Read<int>(RuntimeAddress.Add(vitalAddress, layouts.Vital.UnknownStatId1Offset)),
            LifeComponentPtr = memory.Read<IntPtr>(RuntimeAddress.Add(vitalAddress, layouts.Vital.LifeComponentPtrOffset)),
            ReservedFlat = memory.Read<int>(RuntimeAddress.Add(vitalAddress, layouts.Vital.ReservedFlatOffset)),
            ReservedPercent = memory.Read<int>(RuntimeAddress.Add(vitalAddress, layouts.Vital.ReservedPercentOffset)),
            TotalStatId = memory.Read<int>(RuntimeAddress.Add(vitalAddress, layouts.Vital.TotalStatIdOffset)),
            UnknownStatId2 = memory.Read<int>(RuntimeAddress.Add(vitalAddress, layouts.Vital.UnknownStatId2Offset)),
            UnknownStatId3 = memory.Read<int>(RuntimeAddress.Add(vitalAddress, layouts.Vital.UnknownStatId3Offset)),
            Total = memory.Read<int>(RuntimeAddress.Add(vitalAddress, layouts.Vital.TotalOffset)),
            Current = memory.Read<int>(RuntimeAddress.Add(vitalAddress, layouts.Vital.CurrentOffset)),
        };
    }

    public static bool PassesEntityIdentityFilter(
        this PoeRuntimeLayouts layouts,
        IMemory memory,
        IntPtr entityAddress,
        out uint id)
    {
        id = 0;
        if (entityAddress == IntPtr.Zero)
        {
            return false;
        }

        var status = memory.Read<byte>(RuntimeAddress.Add(entityAddress, layouts.Entity.StatusOffset));
        var activeFlags = memory.Read<byte>(RuntimeAddress.Add(entityAddress, layouts.Entity.ActiveFlagOffset));
        id = memory.Read<uint>(RuntimeAddress.Add(entityAddress, layouts.Entity.IdOffset));
        return layouts.Entity.PassesIdentityFilter(status, id, activeFlags);
    }

    public static IReadOnlyList<PoeComponentNameIndexEntry> ReadComponentNameIndexEntries(
        this PoeRuntimeLayouts layouts,
        IMemory memory,
        IntPtr componentBucketAddress,
        int maxEntries = 256)
    {
        var componentBucketData = layouts.ReadStdVectorHeader(memory, componentBucketAddress);
        return layouts.ReadComponentNameIndexEntries(memory, componentBucketData, maxEntries);
    }

    public static IReadOnlyList<PoeComponentNameIndexEntry> ReadComponentNameIndexEntries(
        this PoeRuntimeLayouts layouts,
        IMemory memory,
        StdBucket componentBucket,
        int maxEntries = 256)
    {
        return layouts.ReadComponentNameIndexEntries(
            memory,
            new PoeStdVectorHeader(componentBucket.Data.First, componentBucket.Data.Last, componentBucket.Data.End),
            maxEntries);
    }

    private static IReadOnlyList<PoeComponentNameIndexEntry> ReadComponentNameIndexEntries(
        this PoeRuntimeLayouts layouts,
        IMemory memory,
        PoeStdVectorHeader componentBucketData,
        int maxEntries)
    {
        var count = componentBucketData.Count(layouts.ComponentNameAndIndex.EntrySize);
        if (count <= 0 ||
            count > maxEntries)
        {
            return [];
        }

        var entries = new List<PoeComponentNameIndexEntry>(count);
        for (var i = 0; i < count; i++)
        {
            var entryAddress = RuntimeAddress.Add(componentBucketData.First, (long)i * layouts.ComponentNameAndIndex.EntrySize);
            var namePtr = memory.Read<IntPtr>(RuntimeAddress.Add(entryAddress, layouts.ComponentNameAndIndex.NamePointerOffset));
            if (namePtr == IntPtr.Zero)
            {
                continue;
            }

            var name = memory.ReadString(namePtr);
            if (string.IsNullOrEmpty(name))
            {
                continue;
            }

            var index = memory.Read<int>(RuntimeAddress.Add(entryAddress, layouts.ComponentNameAndIndex.IndexOffset));
            entries.Add(new PoeComponentNameIndexEntry(name, index, namePtr));
        }

        return entries;
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
            var branchTarget = RuntimeAddress.Add(searchStart, i + 5 + rel32);
            if (branchTarget == target)
            {
                return true;
            }
        }

        return false;
    }
}
