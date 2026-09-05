using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Persistent owner of game state. Gameplay code changes data through this
/// manager; the manager decides when the JSON save is written.
/// </summary>
public sealed class GameManager : MonoBehaviour
{
    private const string LegacyFruitKey = "Mistwood.Inventory.Fruit";
    private const string LegacyFireFeatherKey = "Mistwood.Inventory.FireFeather";

    private static GameManager instance;
    [SerializeField] private GameSaveData currentSave;

    public static GameManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<GameManager>();
                if (instance == null)
                {
                    instance = new GameObject("Game Manager").AddComponent<GameManager>();
                }
            }
            return instance;
        }
    }

    public GameSaveData CurrentSave => currentSave;
    public int FruitCount => CurrentSave != null ? CurrentSave.fruitCount : 0;
    public int FireFeatherCount => CurrentSave != null ? CurrentSave.fireFeatherCount : 0;
    public int GoldFeatherCount => CurrentSave != null ? CurrentSave.goldFeatherCount : 0;
    public int WoodFeatherCount => CurrentSave != null ? CurrentSave.woodFeatherCount : 0;
    public int WaterFeatherCount => CurrentSave != null ? CurrentSave.waterFeatherCount : 0;
    public int EarthFeatherCount => CurrentSave != null ? CurrentSave.earthFeatherCount : 0;
    public int MengpoSoupCount => CurrentSave != null ? CurrentSave.mengpoSoupCount : 0;
    public bool HasPendingGalleryStory => CurrentSave != null && CurrentSave.hasPendingGalleryStory;
    public int PendingGalleryStoryEventId => CurrentSave != null ? CurrentSave.pendingGalleryStoryEventId : 0;
    public int GalleryStoryLineIndex => CurrentSave != null ? CurrentSave.galleryStoryLineIndex : 0;
    public int GalleryStoryBranch => CurrentSave != null ? CurrentSave.galleryStoryBranch : 0;
    public bool CanCraftMengpoSoup => CurrentSave != null &&
                                      CurrentSave.goldFeatherCount > 0 &&
                                      CurrentSave.woodFeatherCount > 0 &&
                                      CurrentSave.waterFeatherCount > 0 &&
                                      CurrentSave.fireFeatherCount > 0 &&
                                      CurrentSave.earthFeatherCount > 0;
    public int PlayerMaxHealth => CurrentSave != null ? CurrentSave.playerMaxHealth : GameSaveData.DefaultPlayerMaxHealth;
    public int PlayerCurrentHealth => CurrentSave != null ? CurrentSave.playerCurrentHealth : GameSaveData.DefaultPlayerMaxHealth;
    public List<string> DeckCardIds => CurrentSave != null ? CurrentSave.deckCardIds : null;
    public bool HasPendingCardReward => CurrentSave != null && CurrentSave.hasPendingCardReward;
    public List<string> PendingRewardCardIds => CurrentSave != null ? CurrentSave.pendingRewardCardIds : null;
    public string SaveFilePath => SaveSystem.SaveFilePath;
    public bool HasSaveGame => SaveSystem.HasSaveFile;
    public bool CanContinueGame => HasSaveGame && CurrentSave != null && CurrentSave.playerCurrentHealth > 0;
    public int CurrentMapLayer => CurrentSave != null ? CurrentSave.currentMapLayer : 1;
    public List<MapRoomSaveData> MapRooms => CurrentSave != null ? CurrentSave.mapRooms : null;

    public event Action SaveDataChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        GameManager unused = Instance;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        LoadGame();
    }

    public void LoadGame()
    {
        bool hadJsonSave = SaveSystem.HasSaveFile;
        currentSave = SaveSystem.Load();

        if (!hadJsonSave && HasLegacyPlayerPrefs())
        {
            MigrateLegacyPlayerPrefs();
            if (SaveGame()) ClearLegacyPlayerPrefs();
        }

        SaveDataChanged?.Invoke();
    }

    public void StartNewGame()
    {
        CreateFreshSaveAndMap();
    }

    public bool ContinueGame()
    {
        if (!HasSaveGame) return false;
        LoadGame();
        if (CurrentSave.playerCurrentHealth <= 0) return false;
        if (CurrentSave.currentMapLayer > 5)
        {
            int nextMapSeed = unchecked(Guid.NewGuid().GetHashCode() ^ (int)DateTime.UtcNow.Ticks);
            MistwoodMapGenerator.Generate(CurrentSave, nextMapSeed);
            SaveGame();
            return true;
        }
        if (EnsureMapGenerated()) SaveGame();
        return true;
    }

    public bool CompleteMapRoom(string roomId)
    {
        MapRoomSaveData room = FindMapRoom(roomId);
        if (room == null || room.completed || room.layer != CurrentSave.currentMapLayer)
        {
            return false;
        }

        room.completed = true;
        CurrentSave.currentMapLayer = Mathf.Clamp(room.layer + 1, 1, 6);
        SaveGame();
        return true;
    }

    public int GetOrRollMysteryEvent(string roomId)
    {
        MapRoomSaveData room = FindMapRoom(roomId);
        if (room == null || room.contentType != RoomContentType.MysteryEvent) return 0;

        if (room.mysteryEventId < 1 || room.mysteryEventId > 3)
        {
            room.mysteryEventId = UnityEngine.Random.Range(1, 4);
            SaveGame();
        }
        return room.mysteryEventId;
    }

    public bool TryEnterMysterySpecialEvent(string roomId, out int eventId, out string message)
    {
        eventId = 0;
        MapRoomSaveData room = FindMapRoom(roomId);
        if (room == null || room.contentType != RoomContentType.MysteryEvent ||
            room.completed || room.layer != CurrentSave.currentMapLayer)
        {
            message = "这个神秘事件当前无法进入";
            return false;
        }

        // A map event can be resumed after loading without rolling a different
        // story or losing its dialogue progress.
        if (room.mysterySpecialEventEntered &&
            room.mysteryEventId >= 1 && room.mysteryEventId <= 3)
        {
            eventId = room.mysteryEventId;
            message = string.Empty;
            return true;
        }

        room.mysteryEventId = UnityEngine.Random.Range(1, 4);
        room.mysterySpecialEventEntered = true;
        eventId = room.mysteryEventId;
        SaveGame();
        message = string.Empty;
        return true;
    }

    public bool TryStartGalleryStory(int eventId, out string message)
    {
        if (eventId < 1 || eventId > 3)
        {
            message = "剧情编号无效";
            return false;
        }

        if (CurrentSave.hasPendingGalleryStory)
        {
            if (CurrentSave.pendingGalleryStoryEventId == eventId)
            {
                message = string.Empty;
                return true;
            }

            message = "请先看完正在进行的剧情";
            return false;
        }

        if (CurrentSave.mengpoSoupCount <= 0)
        {
            message = "孟婆汤不足";
            return false;
        }

        CurrentSave.mengpoSoupCount--;
        CurrentSave.hasPendingGalleryStory = true;
        CurrentSave.pendingGalleryStoryEventId = eventId;
        CurrentSave.galleryStoryLineIndex = 0;
        CurrentSave.galleryStoryBranch = 0;
        SaveGame();
        message = string.Empty;
        return true;
    }

    public void SaveGalleryStoryProgress(int lineIndex, int branch)
    {
        if (!CurrentSave.hasPendingGalleryStory) return;

        int safeLineIndex = Mathf.Max(0, lineIndex);
        int safeBranch = Mathf.Clamp(branch, 0, 2);
        if (CurrentSave.galleryStoryLineIndex == safeLineIndex &&
            CurrentSave.galleryStoryBranch == safeBranch)
        {
            return;
        }

        CurrentSave.galleryStoryLineIndex = safeLineIndex;
        CurrentSave.galleryStoryBranch = safeBranch;
        SaveGame();
    }

    public void CompleteGalleryStory()
    {
        if (!CurrentSave.hasPendingGalleryStory) return;
        CurrentSave.hasPendingGalleryStory = false;
        CurrentSave.pendingGalleryStoryEventId = 0;
        CurrentSave.galleryStoryLineIndex = 0;
        CurrentSave.galleryStoryBranch = 0;
        SaveGame();
    }

    public void SaveMysteryStoryProgress(string roomId, int lineIndex, int branch)
    {
        MapRoomSaveData room = FindMapRoom(roomId);
        if (room == null || room.contentType != RoomContentType.MysteryEvent ||
            room.completed || !room.mysterySpecialEventEntered)
        {
            return;
        }

        int safeLineIndex = Mathf.Max(0, lineIndex);
        int safeBranch = Mathf.Clamp(branch, 0, 2);
        if (room.mysteryDialogueLineIndex == safeLineIndex && room.mysteryStoryBranch == safeBranch)
        {
            return;
        }

        room.mysteryDialogueLineIndex = safeLineIndex;
        room.mysteryStoryBranch = safeBranch;
        SaveGame();
    }

    public MapRoomSaveData FindMapRoom(string roomId)
    {
        if (CurrentSave == null || CurrentSave.mapRooms == null) return null;
        for (int i = 0; i < CurrentSave.mapRooms.Count; i++)
        {
            if (string.Equals(CurrentSave.mapRooms[i].roomId, roomId, StringComparison.Ordinal))
            {
                return CurrentSave.mapRooms[i];
            }
        }
        return null;
    }

    public bool SaveGame()
    {
        bool saved = SaveSystem.Save(CurrentSave);
        if (saved) SaveDataChanged?.Invoke();
        return saved;
    }

    public void AddElementFeathers(ElementType element, int amount)
    {
        if (amount <= 0 || element == ElementType.None) return;
        AddElementFeathersWithoutSaving(element, amount);
        SaveGame();
    }

    public int GetFeatherCount(ElementType element)
    {
        switch (element)
        {
            case ElementType.Gold: return CurrentSave.goldFeatherCount;
            case ElementType.Wood: return CurrentSave.woodFeatherCount;
            case ElementType.Water: return CurrentSave.waterFeatherCount;
            case ElementType.Fire: return CurrentSave.fireFeatherCount;
            case ElementType.Earth: return CurrentSave.earthFeatherCount;
            default: return 0;
        }
    }

    public bool TryCraftMengpoSoup()
    {
        if (!CanCraftMengpoSoup) return false;

        CurrentSave.goldFeatherCount--;
        CurrentSave.woodFeatherCount--;
        CurrentSave.waterFeatherCount--;
        CurrentSave.fireFeatherCount--;
        CurrentSave.earthFeatherCount--;
        CurrentSave.mengpoSoupCount++;
        SaveGame();
        return true;
    }

    public bool ResolveSpiritBattleVictory(
        string roomId,
        ElementType spiritElement,
        List<string> rewardCardIds)
    {
        MapRoomSaveData room = FindMapRoom(roomId);
        if (room == null || room.completed || room.layer != CurrentSave.currentMapLayer ||
            rewardCardIds == null || rewardCardIds.Count != 3)
        {
            return false;
        }

        AddElementFeathersWithoutSaving(spiritElement, 1);
        room.completed = true;
        CurrentSave.currentMapLayer = Mathf.Clamp(room.layer + 1, 1, 6);
        CurrentSave.hasPendingCardReward = true;
        CurrentSave.pendingRewardRoomId = roomId;
        CurrentSave.pendingRewardCardIds = new List<string>(rewardCardIds);
        SaveGame();
        return true;
    }

    public bool ClaimPendingCardReward(string cardId)
    {
        if (!HasPendingCardReward || string.IsNullOrEmpty(cardId) || CardCatalog.Get(cardId) == null ||
            !CurrentSave.pendingRewardCardIds.Contains(cardId))
        {
            return false;
        }

        CurrentSave.deckCardIds.Add(cardId);
        CurrentSave.hasPendingCardReward = false;
        CurrentSave.pendingRewardRoomId = string.Empty;
        CurrentSave.pendingRewardCardIds.Clear();
        SaveGame();
        return true;
    }

    public List<FeatherRewardSaveData> OpenTreasureChest(string roomId)
    {
        MapRoomSaveData room = FindMapRoom(roomId);
        if (room == null || room.contentType != RoomContentType.Treasure)
        {
            return null;
        }

        if (room.treasureRewardClaimed)
        {
            return room.treasureRewards;
        }

        if (room.completed || room.layer != CurrentSave.currentMapLayer)
        {
            return null;
        }

        room.treasureRewards = GenerateTreasureFeatherRewards();
        for (int i = 0; i < room.treasureRewards.Count; i++)
        {
            FeatherRewardSaveData reward = room.treasureRewards[i];
            AddElementFeathersWithoutSaving(reward.element, reward.amount);
        }

        room.treasureRewardClaimed = true;
        room.completed = true;
        CurrentSave.currentMapLayer = Mathf.Clamp(room.layer + 1, 1, 6);
        SaveGame();
        return room.treasureRewards;
    }

    public bool TryRemoveCardFromDeck(string cardId, out string message)
    {
        if (CurrentSave.deckCardIds == null || CurrentSave.deckCardIds.Count == 0)
        {
            message = "牌堆为空，不能移除";
            return false;
        }

        string canonicalId = CardCatalog.GetCanonicalCardId(cardId);
        int cardIndex = CurrentSave.deckCardIds.IndexOf(canonicalId);
        if (cardIndex < 0)
        {
            message = "牌堆中没有这张牌";
            return false;
        }

        CardDefinition removedCard = CardCatalog.Get(canonicalId);
        CurrentSave.deckCardIds.RemoveAt(cardIndex);
        SaveGame();
        message = "已移除" + (removedCard != null ? removedCard.Name : "卡牌");
        return true;
    }

    public bool TryRemoveCardForMystery(string roomId, string cardId, out string message)
    {
        MapRoomSaveData room = FindMapRoom(roomId);
        if (room == null || room.contentType != RoomContentType.MysteryEvent ||
            room.completed || room.layer != CurrentSave.currentMapLayer)
        {
            message = "这个神秘事件当前无法处理";
            return false;
        }

        if (CurrentSave.mengpoSoupCount > 0)
        {
            message = "拥有孟婆汤时不能选择删牌";
            return false;
        }

        if (CurrentSave.deckCardIds == null || CurrentSave.deckCardIds.Count == 0)
        {
            message = "牌堆为空，不能移除";
            return false;
        }

        string canonicalId = CardCatalog.GetCanonicalCardId(cardId);
        int cardIndex = CurrentSave.deckCardIds.IndexOf(canonicalId);
        if (cardIndex < 0)
        {
            message = "牌堆中没有这张牌";
            return false;
        }

        CardDefinition removedCard = CardCatalog.Get(canonicalId);
        CurrentSave.deckCardIds.RemoveAt(cardIndex);
        room.completed = true;
        CurrentSave.currentMapLayer = Mathf.Clamp(room.layer + 1, 1, 6);
        SaveGame();
        message = "已删除「" + (removedCard != null ? removedCard.Name : "卡牌") + "」";
        return true;
    }

    public int HealPlayerByPercent(float percentage)
    {
        if (percentage <= 0f) return 0;

        int oldHealth = CurrentSave.playerCurrentHealth;
        int healAmount = Mathf.CeilToInt(CurrentSave.playerMaxHealth * percentage);
        CurrentSave.playerCurrentHealth = Mathf.Min(
            CurrentSave.playerMaxHealth,
            CurrentSave.playerCurrentHealth + Mathf.Max(1, healAmount));

        int actualHealing = CurrentSave.playerCurrentHealth - oldHealth;
        if (actualHealing > 0) SaveGame();
        return actualHealing;
    }

    public void SetPlayerHealth(int health)
    {
        int clampedHealth = Mathf.Clamp(health, 0, CurrentSave.playerMaxHealth);
        if (CurrentSave.playerCurrentHealth == clampedHealth) return;
        CurrentSave.playerCurrentHealth = clampedHealth;
        SaveGame();
    }

    public void ResetSave()
    {
        CreateFreshSaveAndMap();
    }

    public void ResetMapAfterDefeat()
    {
        int seed = unchecked(Guid.NewGuid().GetHashCode() ^ (int)DateTime.UtcNow.Ticks);
        MistwoodMapGenerator.Generate(CurrentSave, seed);
        SaveGame();
    }

    private void CreateFreshSaveAndMap()
    {
        SaveSystem.DeleteAll();
        ClearLegacyPlayerPrefs();
        currentSave = GameSaveData.CreateDefault();
        int seed = unchecked(Guid.NewGuid().GetHashCode() ^ (int)DateTime.UtcNow.Ticks);
        MistwoodMapGenerator.Generate(CurrentSave, seed);
        SaveGame();
    }

    private bool EnsureMapGenerated()
    {
        if (CurrentSave.mapGenerated && CurrentSave.mapRooms != null && CurrentSave.mapRooms.Count == 9)
        {
            return MistwoodMapGenerator.EnsureGuarantees(CurrentSave);
        }

        int seed = CurrentSave.mapSeed != 0
            ? CurrentSave.mapSeed
            : unchecked(Guid.NewGuid().GetHashCode() ^ (int)DateTime.UtcNow.Ticks);
        MistwoodMapGenerator.Generate(CurrentSave, seed);
        return true;
    }

    private void AddElementFeathersWithoutSaving(ElementType element, int amount)
    {
        switch (element)
        {
            case ElementType.Gold: CurrentSave.goldFeatherCount += amount; break;
            case ElementType.Wood: CurrentSave.woodFeatherCount += amount; break;
            case ElementType.Water: CurrentSave.waterFeatherCount += amount; break;
            case ElementType.Fire: CurrentSave.fireFeatherCount += amount; break;
            case ElementType.Earth: CurrentSave.earthFeatherCount += amount; break;
        }
    }

    private static List<FeatherRewardSaveData> GenerateTreasureFeatherRewards()
    {
        List<ElementType> elements = new List<ElementType>
        {
            ElementType.Gold,
            ElementType.Wood,
            ElementType.Water,
            ElementType.Fire,
            ElementType.Earth
        };

        for (int i = elements.Count - 1; i > 0; i--)
        {
            int randomIndex = UnityEngine.Random.Range(0, i + 1);
            ElementType temporary = elements[i];
            elements[i] = elements[randomIndex];
            elements[randomIndex] = temporary;
        }

        List<FeatherRewardSaveData> rewards = new List<FeatherRewardSaveData>(3);
        for (int i = 0; i < 3; i++)
        {
            rewards.Add(new FeatherRewardSaveData
            {
                element = elements[i],
                amount = UnityEngine.Random.Range(1, 4)
            });
        }
        return rewards;
    }

    [ContextMenu("Save Game")]
    private void SaveFromInspector()
    {
        SaveGame();
    }

    [ContextMenu("Load Game")]
    private void LoadFromInspector()
    {
        LoadGame();
    }

    [ContextMenu("Reset Save")]
    private void ResetFromInspector()
    {
        ResetSave();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused && CurrentSave != null && HasSaveGame) SaveGame();
    }

    private void OnApplicationQuit()
    {
        if (CurrentSave != null && HasSaveGame) SaveGame();
    }

    private static bool HasLegacyPlayerPrefs()
    {
        return PlayerPrefs.HasKey(LegacyFruitKey) ||
               PlayerPrefs.HasKey(LegacyFireFeatherKey);
    }

    private void MigrateLegacyPlayerPrefs()
    {
        CurrentSave.fruitCount = PlayerPrefs.GetInt(LegacyFruitKey, 3);
        CurrentSave.fireFeatherCount = PlayerPrefs.GetInt(LegacyFireFeatherKey, 0);
        CurrentSave.Normalize();
    }

    private static void ClearLegacyPlayerPrefs()
    {
        PlayerPrefs.DeleteKey(LegacyFruitKey);
        PlayerPrefs.DeleteKey(LegacyFireFeatherKey);
        PlayerPrefs.Save();
    }
}
