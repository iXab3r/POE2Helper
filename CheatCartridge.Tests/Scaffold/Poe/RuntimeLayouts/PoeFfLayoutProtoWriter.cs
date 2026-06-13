using CheatCartridge.Tests.Scaffold.FrameFormat;

namespace CheatCartridge.Tests.Scaffold.Poe.RuntimeLayouts;

public sealed class PoeFfLayoutProtoWriterOptions
{
    public string PackageName { get; init; } = "poe.memory";

    public string LayoutName { get; init; } = "poe-game-model";

    public string SourceModuleName { get; init; } = "PathOfExileSteam.exe";

    public string BuildId { get; init; } = string.Empty;

    public string SourceSha256 { get; init; } = string.Empty;

    public DateTimeOffset? CapturedAt { get; init; }
}

/// <summary>
/// Serializes resolved PoE runtime layouts into the same FF proto dialect used
/// by the TL FrameFormat/ReClass tooling.
/// </summary>
public static class PoeFfLayoutProtoWriter
{
    private const int PointerSize = 8;
    private const int StdVectorSize = 0x18;
    private const int StdBucketSize = 0x38;
    private const int StdWStringSize = 0x20;

    public static string WriteResolved(PoeRuntimeLayouts layouts, PoeFfLayoutProtoWriterOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(layouts);

        var writer = new Writer(layouts, options ?? new PoeFfLayoutProtoWriterOptions());
        return writer.Write();
    }

    private sealed class Writer
    {
        private readonly PoeRuntimeLayouts layouts;
        private readonly PoeFfLayoutProtoWriterOptions options;
        private readonly List<FrameFormatProtoMessage> messages = [];

        public Writer(PoeRuntimeLayouts layouts, PoeFfLayoutProtoWriterOptions options)
        {
            this.layouts = layouts;
            this.options = options;
        }

        public string Write()
        {
            WriteStdTypes();
            WriteGameStateTypes();
            WriteEntityTypes();
            WriteComponentTypes();
            return FrameFormatProtoWriter.Write(
                new FrameFormatProtoWriterOptions(
                    PackageName: options.PackageName,
                    LayoutName: options.LayoutName,
                    SourceName: options.SourceModuleName,
                    BuildId: options.BuildId,
                    SourceSha256: options.SourceSha256,
                    CapturedAt: options.CapturedAt,
                    Summary: "Resolved Path Of Exile runtime memory layout."),
                messages);
        }

        private void WriteStdTypes()
        {
            WriteMessage(
                "StdVector",
                "MSVC std::vector header. Element semantics are supplied by the owning field.",
                [
                    Field("uint64", "first", 1, layouts.StdVector.FirstOffset, PointerSize, "primitive(Hex64)")
                        with { Comment = "First element pointer." },
                    Field("uint64", "last", 2, layouts.StdVector.LastOffset, PointerSize, "primitive(Hex64)")
                        with { Comment = "One-past-last element pointer." },
                    Field("uint64", "end", 3, layouts.StdVector.EndOffset, PointerSize, "primitive(Hex64)")
                        with { Comment = "One-past-capacity pointer." }
                ]);

            WriteMessage(
                "StdBucket",
                "Hash/bucket-like wrapper whose first field is the vector used by component lookup.",
                [
                    Field("StdVector", "data", 1, 0, StdVectorSize, "class(StdVector)")
                        with { Comment = "Vector backing the bucket payload." },
                    Field("uint64", "unknown_ptr", 2, 0x18, PointerSize, "primitive(Hex64)")
                        with { Comment = "Unclassified bucket pointer." },
                    Field("int32", "capacity_minus_one", 3, 0x20, 4, "primitive(int32)")
                        with { Comment = "Bucket capacity mask/capacity-minus-one value." },
                    Field("uint32", "unknown1", 4, 0x28, 4, "primitive(uint32)")
                        with { Comment = "Unclassified bucket scalar." },
                    Field("uint32", "unknown2", 5, 0x30, 4, "primitive(uint32)")
                        with { Comment = "Unclassified bucket scalar." },
                    Field("uint32", "unknown3", 6, 0x34, 4, "primitive(uint32)")
                        with { Comment = "Unclassified bucket scalar." }
                ]);

            WriteMessage(
                "StdWString",
                "MSVC UTF-16 string. capacity <= 7 means bytes are inline at +0x00; otherwise +0x00 is an external buffer pointer.",
                [
                    Field("uint64", "buffer_or_inline0", 1, layouts.StdWString.BufferOffset, PointerSize, "primitive(Hex64)")
                        with { Comment = $"External buffer pointer or first inline UTF-16 characters; inline capacity limit is {layouts.StdWString.SmallCapacityLimit}." },
                    Field("uint64", "inline1", 2, layouts.StdWString.InlineBufferOffset + PointerSize, PointerSize, "primitive(Hex64)")
                        with { Comment = "Tail of inline UTF-16 storage." },
                    Field("int32", "length", 3, layouts.StdWString.LengthOffset, 4, "primitive(int32)")
                        with { Comment = "UTF-16 character length." },
                    Field("int32", "capacity", 4, layouts.StdWString.CapacityOffset, 4, "primitive(int32)")
                        with { Comment = "UTF-16 character capacity." }
                ]);
        }

        private void WriteGameStateTypes()
        {
            WriteMessage(
                "GameStateStaticWrapper",
                "Global singleton wrapper returned by the GameStates getter.",
                [
                    Field("GameStates", "owner", 1, layouts.GameStates.StaticGameStateOffset, PointerSize, "ptr(pointer=8,target=class(GameStates))")
                        with { Comment = "Pointer to the GameStates owner object." },
                    Field("uint64", "sidecar", 2, layouts.GameStates.StaticSidecarOffset, PointerSize, "primitive(Hex64)")
                        with { Comment = "Companion pointer stored beside the GameStates owner." }
                ]);

            WriteMessage(
                "GameStateEntry",
                "Pair stored in the state table and current/dispatch vectors.",
                [
                    Field("uint64", "state", 1, 0, PointerSize, "primitive(Hex64)")
                        with { Comment = "Game-state object pointer." },
                    Field("uint64", "sidecar", 2, PointerSize, PointerSize, "primitive(Hex64)")
                        with { Comment = "State-specific companion pointer." }
                ]);

            WriteMessage(
                "GameStates",
                "Owner of the state table plus current/dispatch state vectors.",
                [
                    Field("StdVector", "current_state_vector", 1, layouts.GameStates.CurrentStateVectorOffset, StdVectorSize, "class(StdVector)")
                        with { Comment = "Source vector for current game-state entries." },
                    Field("StdVector", "dispatch_state_vector", 2, layouts.GameStates.DispatchVectorOffset, StdVectorSize, "class(StdVector)")
                        with { Comment = "Dispatch copy of current game-state entries." },
                    Field("bool", "dirty_flag", 3, layouts.GameStates.DirtyFlagOffset, 1, "primitive(bool8)")
                        with { Comment = "Marks current-state vector changes pending dispatch copy." },
                    Field("GameStateEntry", "state_table", 4, layouts.GameStates.TableOffset, layouts.GameStates.EntrySize * layouts.GameStates.Count, $"array(count={layouts.GameStates.Count},selected=0,element=class(GameStateEntry))")
                        with { Comment = "Fixed table indexed by GameStateTypes." }
                ]);

            WriteMessage(
                "InGameState",
                "Live in-world game state. The enum table index is stable for this model.",
                [
                    Field("AreaInstance", "area_instance_data", 1, layouts.InGameState.AreaInstanceDataOffset, PointerSize, "ptr(pointer=8,target=class(AreaInstance))")
                        with { Comment = "Current area-instance data pointer." },
                    Field("int32", "ms_elapsed", 2, layouts.InGameState.MsElapsedOffset, 4, "primitive(int32)")
                        with { Comment = "Increasing in-world elapsed timer." },
                    Field("int32", "zone_switch_counter", 3, layouts.InGameState.ZoneSwitchCounterOffset, 4, "primitive(int32)")
                        with { Comment = "Zone-transition counter." }
                ]);

            WriteMessage(
                "AreaInstance",
                "Current area instance data reached from InGameState.area_instance_data.",
                [
                    Field("uint32", "current_area_hash", 1, layouts.AreaInstance.CurrentAreaHashOffset, 4, "primitive(uint32)")
                        with { Comment = "Hash of the currently loaded area instance." },
                    Field("uint32", "current_area_level", 2, layouts.AreaInstance.CurrentAreaLevelOffset, 1, "primitive(uint8)")
                        with { Comment = "Monster/area level." },
                    Field("StdVector", "local_players", 3, layouts.AreaInstance.LocalPlayersOffset, StdVectorSize, "class(StdVector)")
                        with { Comment = "Vector of local player entity pointers." },
                    Field("uint64", "entity_tree_root", 4, layouts.AreaInstance.EntityTreeRootOffset, PointerSize, "primitive(Hex64)")
                        with { Comment = "Root/sentinel pointer for the area entity tree." },
                    Field("uint32", "entities_count", 5, layouts.AreaInstance.EntitiesCountOffset, 4, "primitive(uint32)")
                        with { Comment = "Entity-tree count stored after the root pointer." }
                ]);
        }

        private void WriteEntityTypes()
        {
            WriteMessage(
                "Entity",
                "In-world object. Identity fields are validated by the recovered entity filter.",
                [
                    Field("EntityDetails", "details", 1, layouts.Entity.DetailsPtrOffset, PointerSize, "ptr(pointer=8,target=class(EntityDetails))")
                        with { Comment = "Pointer to entity metadata/details." },
                    Field("StdBucket", "component_list", 2, layouts.Entity.ComponentListOffset, StdBucketSize, "class(StdBucket)")
                        with { Comment = "Bucket/vector of component header pointers." },
                    Field("uint32", "id", 3, layouts.Entity.IdOffset, 4, "primitive(uint32)")
                        with { Comment = "Area-local entity id." },
                    Field("uint32", "status", 4, layouts.Entity.StatusOffset, 1, "primitive(uint8)")
                        with { Comment = "Entity status byte used by the identity filter." },
                    Field("uint32", "active_flags", 5, layouts.Entity.ActiveFlagOffset, 1, "primitive(uint8)")
                        with { Comment = "Activity flags used by the identity filter." }
                ]);

            WriteMessage(
                "EntityDetails",
                "Entity metadata block containing path/name data and component-name lookup.",
                [
                    Field("StdWString", "name", 1, layouts.EntityDetails.NameOffset, StdWStringSize, "class(StdWString)")
                        with { Comment = "Entity metadata path/name." },
                    Field("ComponentLookup", "component_lookup", 2, layouts.EntityDetails.ComponentLookupOffset, PointerSize, "ptr(pointer=8,target=class(ComponentLookup))")
                        with { Comment = "Lookup table mapping component names to component indices." }
                ]);

            WriteMessage(
                "ComponentLookup",
                "Lookup table mapping component names to indices in Entity.component_list.",
                [
                    Field("StdBucket", "name_and_index_bucket", 1, layouts.ComponentLookup.NameAndIndexBucketOffset, StdBucketSize, "class(StdBucket)")
                        with { Comment = "Bucket/vector of component-name/index entries." }
                ]);

            WriteMessage(
                "ComponentNameAndIndex",
                "Entry in ComponentLookup.name_and_index_bucket.",
                [
                    Field("uint64", "name", 1, layouts.ComponentNameAndIndex.NamePointerOffset, PointerSize, "primitive(Hex64)")
                        with { Comment = "Component name C-string pointer." },
                    Field("int32", "index", 2, layouts.ComponentNameAndIndex.IndexOffset, 4, "primitive(int32)")
                        with { Comment = "Index into the owning entity component list." }
                ]);
        }

        private void WriteComponentTypes()
        {
            WriteMessage(
                "ComponentHeader",
                "Common component header containing the owning entity pointer.",
                [
                    Field("Entity", "owner_entity", 1, layouts.ComponentHeader.OwnerEntityOffset, PointerSize, "ptr(pointer=8,target=class(Entity))")
                        with { Comment = "Owning entity pointer." }
                ]);

            WriteMessage(
                "Player",
                "Player component.",
                [
                    Field("StdWString", "name", 1, layouts.Player.NameOffset, StdWStringSize, "class(StdWString)")
                        with { Comment = "Character display name." }
                ]);

            var vitalSize = MaxEnd(
                (layouts.Vital.UnknownStatId0Offset, 4),
                (layouts.Vital.UnknownStatId1Offset, 4),
                (layouts.Vital.LifeComponentPtrOffset, PointerSize),
                (layouts.Vital.ReservedFlatOffset, 4),
                (layouts.Vital.ReservedPercentOffset, 4),
                (layouts.Vital.TotalStatIdOffset, 4),
                (layouts.Vital.UnknownStatId2Offset, 4),
                (layouts.Vital.UnknownStatId3Offset, 4),
                (layouts.Vital.TotalOffset, 4),
                (layouts.Vital.CurrentOffset, 4));

            WriteMessage(
                "Life",
                "Life component vitals.",
                [
                    Field("Vital", "health", 1, layouts.Life.HealthOffset, vitalSize, "class(Vital)")
                        with { Comment = "Health vital block." },
                    Field("Vital", "mana", 2, layouts.Life.ManaOffset, vitalSize, "class(Vital)")
                        with { Comment = "Mana vital block." },
                    Field("Vital", "energy_shield", 3, layouts.Life.EnergyShieldOffset, vitalSize, "class(Vital)")
                        with { Comment = "Energy shield vital block." }
                ]);

            WriteMessage(
                "Vital",
                "One current/total/resource reservation block inside Life.",
                [
                    Field("int32", "unknown_stat_id0", 1, layouts.Vital.UnknownStatId0Offset, 4, "primitive(int32)")
                        with { Comment = "Constructor-written stat id; exact meaning is still unknown." },
                    Field("int32", "unknown_stat_id1", 2, layouts.Vital.UnknownStatId1Offset, 4, "primitive(int32)")
                        with { Comment = "Constructor-written stat id; exact meaning is still unknown." },
                    Field("Life", "life_component", 3, layouts.Vital.LifeComponentPtrOffset, PointerSize, "ptr(pointer=8,target=class(Life))")
                        with { Comment = "Back pointer to owning Life component." },
                    Field("int32", "reserved_flat", 4, layouts.Vital.ReservedFlatOffset, 4, "primitive(int32)")
                        with { Comment = "Flat reserved amount." },
                    Field("int32", "reserved_percent", 5, layouts.Vital.ReservedPercentOffset, 4, "primitive(int32)")
                        with { Comment = "Percent reserved amount stored as basis points." },
                    Field("int32", "total_stat_id", 6, layouts.Vital.TotalStatIdOffset, 4, "primitive(int32)")
                        with { Comment = "Stat id used when refreshing this vital's total." },
                    Field("int32", "unknown_stat_id2", 7, layouts.Vital.UnknownStatId2Offset, 4, "primitive(int32)")
                        with { Comment = "Constructor-written stat id; exact meaning is still unknown." },
                    Field("int32", "unknown_stat_id3", 8, layouts.Vital.UnknownStatId3Offset, 4, "primitive(int32)")
                        with { Comment = "Constructor-written stat id; exact meaning is still unknown." },
                    Field("int32", "total", 9, layouts.Vital.TotalOffset, 4, "primitive(int32)")
                        with { Comment = "Current maximum value for this vital." },
                    Field("int32", "current", 10, layouts.Vital.CurrentOffset, 4, "primitive(int32)")
                        with { Comment = "Current value for this vital." }
                ]);
        }

        private void WriteMessage(string name, string summary, IReadOnlyList<FrameFormatProtoField> fields)
        {
            messages.Add(new FrameFormatProtoMessage(name, summary, fields));
        }
    }

    private static FrameFormatProtoField Field(
        string typeName,
        string name,
        int number,
        int offset,
        int length,
        string shape)
    {
        return new FrameFormatProtoField(typeName, name, number, offset, length, shape);
    }

    private static int MaxEnd(params (int Offset, int Length)[] ranges)
    {
        return ranges.Length == 0 ? 0 : ranges.Max(x => x.Offset + x.Length);
    }
}
