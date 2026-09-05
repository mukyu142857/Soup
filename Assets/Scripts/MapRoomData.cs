using System;
using System.Collections.Generic;

public enum RoomContentType
{
    FireSpirit = 0,
    GoldSpirit = 1,
    WoodSpirit = 2,
    WaterSpirit = 3,
    EarthSpirit = 4,
    RestSite = 5,
    MysteryEvent = 6,
    Treasure = 7
}

[Serializable]
public sealed class FeatherRewardSaveData
{
    public ElementType element;
    public int amount;
}

/// <summary>A single persisted node in the fixed five-floor map.</summary>
[Serializable]
public sealed class MapRoomSaveData
{
    public string roomId = string.Empty;
    public string slotName = string.Empty;
    public int layer;
    public RoomContentType contentType;
    public bool completed;
    public int mysteryEventId;
    public bool mysterySpecialEventEntered;
    public int mysteryDialogueLineIndex;
    public int mysteryStoryBranch;
    public bool treasureRewardClaimed;
    public List<FeatherRewardSaveData> treasureRewards = new List<FeatherRewardSaveData>();
}
