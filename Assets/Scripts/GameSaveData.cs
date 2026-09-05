using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

/// <summary>
/// Serializable data written to the game's JSON save file.
/// Add future persistent fields here instead of storing them in UI scripts.
/// </summary>
[Serializable]
public sealed class GameSaveData
{
    public const int CurrentVersion = 14;
    public const int DefaultPlayerMaxHealth = 35;

    public int saveVersion = CurrentVersion;
    public string createdAtUtc = string.Empty;
    public string lastSavedAtUtc = string.Empty;

    public int fruitCount = 3;
    public int fireFeatherCount;
    public int goldFeatherCount;
    public int woodFeatherCount;
    public int waterFeatherCount;
    public int earthFeatherCount;
    public int mengpoSoupCount;
    public bool hasPendingGalleryStory;
    public int pendingGalleryStoryEventId;
    public int galleryStoryLineIndex;
    public int galleryStoryBranch;
    public int playerMaxHealth = DefaultPlayerMaxHealth;
    public int playerCurrentHealth = DefaultPlayerMaxHealth;
    public bool deckInitialized;
    public List<string> deckCardIds = new List<string>();
    public bool hasPendingCardReward;
    public string pendingRewardRoomId = string.Empty;
    public List<string> pendingRewardCardIds = new List<string>();

    public bool mapGenerated;
    public int mapSeed;
    public int currentMapLayer = 1;
    public List<MapRoomSaveData> mapRooms = new List<MapRoomSaveData>();

    public static GameSaveData CreateDefault()
    {
        GameSaveData data = new GameSaveData
        {
            createdAtUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)
        };
        data.Normalize();
        return data;
    }

    public void Normalize()
    {
        int loadedVersion = saveVersion;
        fruitCount = Mathf.Max(0, fruitCount);
        fireFeatherCount = Mathf.Max(0, fireFeatherCount);
        goldFeatherCount = Mathf.Max(0, goldFeatherCount);
        woodFeatherCount = Mathf.Max(0, woodFeatherCount);
        waterFeatherCount = Mathf.Max(0, waterFeatherCount);
        earthFeatherCount = Mathf.Max(0, earthFeatherCount);
        mengpoSoupCount = Mathf.Max(0, mengpoSoupCount);
        galleryStoryLineIndex = Mathf.Max(0, galleryStoryLineIndex);
        galleryStoryBranch = Mathf.Clamp(galleryStoryBranch, 0, 2);
        if (hasPendingGalleryStory &&
            (pendingGalleryStoryEventId < 1 || pendingGalleryStoryEventId > 3))
        {
            hasPendingGalleryStory = false;
            pendingGalleryStoryEventId = 0;
            galleryStoryLineIndex = 0;
            galleryStoryBranch = 0;
        }

        if (deckCardIds == null) deckCardIds = new List<string>();
        if (!deckInitialized)
        {
            if (deckCardIds.Count == 0)
            {
                deckCardIds = CardCatalog.CreateInitialDeckIds();
            }
            deckInitialized = true;
        }
        for (int i = 0; i < deckCardIds.Count; i++)
        {
            deckCardIds[i] = CardCatalog.GetCanonicalCardId(deckCardIds[i]);
        }
        if (pendingRewardCardIds == null) pendingRewardCardIds = new List<string>();
        for (int i = 0; i < pendingRewardCardIds.Count; i++)
        {
            pendingRewardCardIds[i] = CardCatalog.GetCanonicalCardId(pendingRewardCardIds[i]);
        }
        if (hasPendingCardReward && pendingRewardCardIds.Count != 3)
        {
            hasPendingCardReward = false;
            pendingRewardRoomId = string.Empty;
            pendingRewardCardIds.Clear();
        }

        if (loadedVersion < 3 || playerMaxHealth <= 0)
        {
            // Saves from versions before persistent health had no HP fields.
            playerCurrentHealth = DefaultPlayerMaxHealth;
        }
        else if (loadedVersion < 8 && playerCurrentHealth > 0)
        {
            // Preserve the amount of health already lost when upgrading the
            // old 25-HP save to 35 HP.
            playerCurrentHealth += Mathf.Max(0, DefaultPlayerMaxHealth - playerMaxHealth);
        }
        if (loadedVersion < 9 && playerCurrentHealth <= 0)
        {
            // Version 8 briefly treated defeat as permanent game over. Recover
            // those saves now that defeat only returns the player to the map.
            playerCurrentHealth = DefaultPlayerMaxHealth;
        }
        playerMaxHealth = DefaultPlayerMaxHealth;
        playerCurrentHealth = Mathf.Clamp(playerCurrentHealth, 0, playerMaxHealth);

        currentMapLayer = Mathf.Clamp(currentMapLayer, 1, 6);
        if (mapRooms == null) mapRooms = new List<MapRoomSaveData>();
        for (int i = 0; i < mapRooms.Count; i++)
        {
            MapRoomSaveData room = mapRooms[i];
            if (room.mysterySpecialEventEntered &&
                (room.mysteryEventId < 1 || room.mysteryEventId > 3))
            {
                room.mysterySpecialEventEntered = false;
                room.mysteryEventId = 0;
            }
            room.mysteryDialogueLineIndex = Mathf.Max(0, room.mysteryDialogueLineIndex);
            room.mysteryStoryBranch = Mathf.Clamp(room.mysteryStoryBranch, 0, 2);
            if (room.treasureRewards == null)
            {
                room.treasureRewards = new List<FeatherRewardSaveData>();
            }

            if (loadedVersion < 10 && room.contentType == RoomContentType.Treasure &&
                room.completed && !room.treasureRewardClaimed)
            {
                // Older builds completed the final chest without a reward.
                // Reopen it once so the player can receive the new feathers.
                room.completed = false;
                currentMapLayer = room.layer;
            }
        }
        saveVersion = CurrentVersion;

        if (string.IsNullOrEmpty(createdAtUtc))
        {
            createdAtUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
        }
    }
}
