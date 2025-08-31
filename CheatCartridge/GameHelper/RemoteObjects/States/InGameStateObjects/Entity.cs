// <copyright file="Entity.cs" company="None">
// Copyright (c) None. All rights reserved.
// </copyright>

using CheatCartridge.GameHelper.GameOffsets.States.InGameState;
using CheatCartridge.GameHelper.Scaffolding;
using EyeAuras.Memory;
using ComponentBase = CheatCartridge.GameHelper.RemoteObjects.Components.ComponentBase;

namespace CheatCartridge.GameHelper.RemoteObjects.States.InGameStateObjects;

/// <summary>
///     Points to an Entity/Object in the game.
///     Entity is basically item/monster/effect/player/etc on the ground.
/// </summary>
public class Entity : MemoryObjectBase
{
    public IFluentLog Log { get; }
    private static readonly int MaxComponentsInAnEntity = 50;

    private readonly ConcurrentDictionary<string, IntPtr> componentAddresses = new();
    private readonly ConcurrentDictionary<string, ComponentBase> componentCache = new();

    public Entity(IMemory memory, IFluentLog log)
        : base(memory)
    {
        Log = log;
        Path = string.Empty;
        Id = 0;
        IsValid = false;
    }

    /// <summary>
    ///     Gets the Path (e.g. Metadata/Character/int/int) assocaited to the entity.
    /// </summary>
    public string Path { get; private set; }

    /// <summary>
    ///     Gets the Id associated to the entity. This is unique per map/Area.
    /// </summary>
    public uint Id { get; private set; }

    /// <summary>
    ///     Gets or Sets a value indicating whether the entity
    ///     exists in the game or not.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    ///     Gets the Component data associated with the entity.
    /// </summary>
    /// <typeparam name="T">Component type to get.</typeparam>
    /// <param name="component">component data.</param>
    /// <param name="shouldCache">should entity cache this component or not.</param>
    /// <returns>true if the entity contains the component; otherwise, false.</returns>
    public bool TryGetComponent<T>(out T component, bool shouldCache = true)
        where T : ComponentBase
    {
        var componentName = typeof(T).Name;
        if (componentCache.TryGetValue(componentName, out var comp))
        {
            component = (T)comp;
            return true;
        }

        if (!componentAddresses.TryGetValue(componentName, out var compAddr))
        {
            component = default!;
            return false;
        }

        if (compAddr == IntPtr.Zero)
        {
            component = default!;
            return false;
        }

        var newInstance = Activator.CreateInstance(typeof(T), Memory, compAddr);
        if (newInstance is not T newComponentInstance)
        {
            component = default!;
            return false;
        }
        
        if (shouldCache)
        {
            componentCache[componentName] = newComponentInstance;
        }

        component = newComponentInstance;
        return true;
    }

    /// <summary>
    ///     Updates the component data associated with the Entity base object (i.e. item).
    /// </summary>
    /// <param name="entityBase">Entity base (i.e. item) data.</param>
    /// <param name="hasAddressChanged">has this class Address changed or not.</param>
    /// <returns> false if this method detects an issue, otherwise true</returns>
    private bool TryUpdateComponentData(ItemStruct entityBase, bool hasAddressChanged)
    {
        if (hasAddressChanged)
        {
            componentAddresses.Clear();
            componentCache.Clear();
            var entityDetails = Memory.Read<EntityDetails>(entityBase.EntityDetailsPtr);
            Path = Memory.ReadStdWString(entityDetails.name);
            if (string.IsNullOrEmpty(Path))
            {
                return false;
            }

            if (entityDetails.ComponentLookUpPtr == IntPtr.Zero)
            {
                throw new InvalidOperationException($"ComponentLookUpPtr should never be Zero, but was for {this}");
            }

            var lookupPtr = Memory.Read<ComponentLookUpStruct>(entityDetails.ComponentLookUpPtr);
            if (lookupPtr.ComponentsNameAndIndex.Capacity > MaxComponentsInAnEntity)
            {
                return false;
            }
            
            var namesAndIndexes = Memory.ReadStdBucket<ComponentNameAndIndexStruct>(lookupPtr.ComponentsNameAndIndex);
            var entityComponent = Memory.ReadStdVector<IntPtr>(entityBase.ComponentListPtr);
            foreach (var nameAndIndex in namesAndIndexes)
            {
                if (nameAndIndex.Index < 0 || nameAndIndex.Index >= entityComponent.Length)
                {
                    continue;
                }

                var name = Memory.ReadString(nameAndIndex.NamePtr);
                if (!string.IsNullOrEmpty(name))
                {
                    componentAddresses.TryAdd(name, entityComponent[nameAndIndex.Index]);
                }
            }
            
            Log.Info($"Components:\n\t{componentAddresses.Select(x => new { Name = x.Key, Addr = x.Value.ToHexadecimal() }).DumpToTable()}");
        }
        else
        {
            foreach (var kv in componentCache)
            {
                kv.Value.Address = kv.Value.Address;
                if (!kv.Value.IsParentValid(Address))
                {
                    return false;
                }
            }
        }

        return true;
    }

    protected override void CleanUpData()
    {
        componentAddresses.Clear();
        componentCache.Clear();
    }

    protected override void UpdateData(bool hasAddressChanged)
    {
        var entityData = Memory.Read<EntityOffsets>(Address);
        IsValid = entityData.IsValid == 0xC;
        if (!IsValid)
        {
            // Invalid entity data is normally corrupted. let's not parse it.
            return;
        }

        Id = entityData.Id;
        if (!TryUpdateComponentData(entityData.ItemBase, hasAddressChanged))
        {
            TryUpdateComponentData(entityData.ItemBase, true);
        }
    }
}