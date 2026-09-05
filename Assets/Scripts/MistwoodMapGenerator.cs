using System;
using System.Collections.Generic;

/// <summary>
/// Generates the fixed 1/2/3/2/1 Mistwood map while enforcing elemental
/// spirit guarantees. Generated data is saved, so Continue never rerolls it.
/// </summary>
public static class MistwoodMapGenerator
{
    private static readonly RoomContentType[] RandomContentPool =
    {
        RoomContentType.GoldSpirit,
        RoomContentType.WoodSpirit,
        RoomContentType.WaterSpirit,
        RoomContentType.EarthSpirit,
        RoomContentType.RestSite,
        RoomContentType.MysteryEvent
    };

    private static readonly RoomContentType[] RequiredSpirits =
    {
        RoomContentType.GoldSpirit,
        RoomContentType.WoodSpirit,
        RoomContentType.WaterSpirit,
        RoomContentType.EarthSpirit
    };

    public static void Generate(GameSaveData save, int seed)
    {
        if (save == null) throw new ArgumentNullException(nameof(save));

        Random random = new Random(seed);
        save.mapSeed = seed;
        save.currentMapLayer = 1;

        save.mapRooms.Clear();
        AddRoom(save, "L1", "1", 1, RoomContentType.FireSpirit);
        AddRandomLayer(save, 2, new[] { "A", "B" }, false, random);
        AddRandomLayer(save, 3, new[] { "C", "D", "E" }, true, random);
        AddRandomLayer(save, 4, new[] { "F", "G" }, true, random);
        EnsureGuarantees(save);

        AddRoom(save, "L5", "5", 5, RoomContentType.Treasure);
        save.mapGenerated = true;
    }

    public static bool EnsureGuarantees(GameSaveData save)
    {
        if (save == null) throw new ArgumentNullException(nameof(save));
        if (save.mapRooms == null) return false;

        bool changed = ApplyElementalSpiritGuarantee(save.mapRooms);
        if (ApplyRestSiteGuarantee(save.mapRooms)) changed = true;
        return changed;
    }

    public static bool IsElementalSpirit(RoomContentType type)
    {
        return type == RoomContentType.GoldSpirit ||
               type == RoomContentType.WoodSpirit ||
               type == RoomContentType.WaterSpirit ||
               type == RoomContentType.EarthSpirit;
    }

    public static string GetDisplayName(RoomContentType type)
    {
        switch (type)
        {
            case RoomContentType.FireSpirit: return "火精灵";
            case RoomContentType.GoldSpirit: return "金精灵";
            case RoomContentType.WoodSpirit: return "木精灵";
            case RoomContentType.WaterSpirit: return "水精灵";
            case RoomContentType.EarthSpirit: return "土精灵";
            case RoomContentType.RestSite: return "休息处";
            case RoomContentType.MysteryEvent: return "神秘事件";
            case RoomContentType.Treasure: return "宝箱";
            default: return "未知房间";
        }
    }

    private static void AddRandomLayer(
        GameSaveData save,
        int layer,
        string[] slotNames,
        bool requireSpirit,
        Random random)
    {
        List<RoomContentType> contents;
        do
        {
            contents = DrawDistinct(slotNames.Length, random);
        }
        while (requireSpirit && !ContainsSpirit(contents));

        for (int i = 0; i < slotNames.Length; i++)
        {
            AddRoom(save, "L" + layer + slotNames[i], slotNames[i], layer, contents[i]);
        }
    }

    private static List<RoomContentType> DrawDistinct(int count, Random random)
    {
        List<RoomContentType> candidates = new List<RoomContentType>(RandomContentPool);
        for (int i = candidates.Count - 1; i > 0; i--)
        {
            int randomIndex = random.Next(i + 1);
            RoomContentType temporary = candidates[i];
            candidates[i] = candidates[randomIndex];
            candidates[randomIndex] = temporary;
        }
        return candidates.GetRange(0, count);
    }

    private static bool ContainsSpirit(List<RoomContentType> contents)
    {
        for (int i = 0; i < contents.Count; i++)
        {
            if (IsElementalSpirit(contents[i])) return true;
        }
        return false;
    }

    private static bool ApplyElementalSpiritGuarantee(List<MapRoomSaveData> rooms)
    {
        bool changed = false;
        for (int requiredIndex = 0; requiredIndex < RequiredSpirits.Length; requiredIndex++)
        {
            RoomContentType requiredSpirit = RequiredSpirits[requiredIndex];
            if (ContainsContent(rooms, requiredSpirit)) continue;

            int replacementIndex = CountContent(rooms, RoomContentType.RestSite) > 1
                ? FindLastReplacement(rooms, RoomContentType.RestSite)
                : -1;
            if (replacementIndex < 0)
            {
                replacementIndex = FindLastReplacement(rooms, RoomContentType.MysteryEvent);
            }

            if (replacementIndex < 0)
            {
                replacementIndex = FindLastSafeSpiritReplacement(rooms);
            }

            if (replacementIndex < 0)
            {
                throw new InvalidOperationException("No room is available for the elemental spirit guarantee.");
            }
            rooms[replacementIndex].contentType = requiredSpirit;
            changed = true;
        }
        return changed;
    }

    private static bool ApplyRestSiteGuarantee(List<MapRoomSaveData> rooms)
    {
        if (ContainsContent(rooms, RoomContentType.RestSite)) return false;

        int replacementIndex = FindLastReplacement(rooms, RoomContentType.MysteryEvent);
        if (replacementIndex < 0)
        {
            replacementIndex = FindLastSafeSpiritReplacement(rooms);
        }

        if (replacementIndex < 0)
        {
            throw new InvalidOperationException("No room is available for the rest-site guarantee.");
        }
        rooms[replacementIndex].contentType = RoomContentType.RestSite;
        return true;
    }

    private static bool ContainsContent(List<MapRoomSaveData> rooms, RoomContentType content)
    {
        for (int i = 0; i < rooms.Count; i++)
        {
            if (rooms[i].layer >= 2 && rooms[i].layer <= 4 && rooms[i].contentType == content)
            {
                return true;
            }
        }
        return false;
    }

    private static int FindLastReplacement(List<MapRoomSaveData> rooms, RoomContentType content)
    {
        int completedFallback = -1;
        for (int i = rooms.Count - 1; i >= 0; i--)
        {
            if (rooms[i].layer >= 2 && rooms[i].layer <= 4 && rooms[i].contentType == content)
            {
                if (!rooms[i].completed) return i;
                if (completedFallback < 0) completedFallback = i;
            }
        }
        return completedFallback;
    }

    private static int FindLastSafeSpiritReplacement(List<MapRoomSaveData> rooms)
    {
        // If all layer 2-4 rooms are spirits, replace the last duplicated
        // spirit. This is the last-room fallback without deleting the only
        // copy of another required element.
        int completedFallback = -1;
        for (int i = rooms.Count - 1; i >= 0; i--)
        {
            MapRoomSaveData room = rooms[i];
            if (room.layer < 2 || room.layer > 4 || !IsElementalSpirit(room.contentType)) continue;
            if (CountContent(rooms, room.contentType) <= 1) continue;
            if (!room.completed) return i;
            if (completedFallback < 0) completedFallback = i;
        }
        return completedFallback;
    }

    private static int CountContent(List<MapRoomSaveData> rooms, RoomContentType content)
    {
        int count = 0;
        for (int i = 0; i < rooms.Count; i++)
        {
            if (rooms[i].layer >= 2 && rooms[i].layer <= 4 && rooms[i].contentType == content)
            {
                count++;
            }
        }
        return count;
    }

    private static void AddRoom(
        GameSaveData save,
        string roomId,
        string slotName,
        int layer,
        RoomContentType content)
    {
        save.mapRooms.Add(new MapRoomSaveData
        {
            roomId = roomId,
            slotName = slotName,
            layer = layer,
            contentType = content,
            completed = false,
            mysteryEventId = 0
        });
    }
}
