using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Runtime prototype for: main menu -> forest icon -> map node -> Fire Spirit.
/// Persistent state is owned by GameManager; this class only handles UI/combat.
/// </summary>
public sealed class MistwoodMapUI : MonoBehaviour
{
    private static readonly Color Background = new Color(0.07f, 0.10f, 0.08f, 1f);
    private static readonly Color Panel = new Color(0.12f, 0.16f, 0.13f, 1f);
    private static readonly Color ForestGreen = new Color(0.25f, 0.48f, 0.31f, 1f);
    private static readonly Color FireOrange = new Color(0.88f, 0.31f, 0.13f, 1f);
    private static readonly Color Gold = new Color(0.86f, 0.72f, 0.38f, 1f);
    private static readonly Color TextColor = new Color(0.92f, 0.94f, 0.89f, 1f);
    private static readonly Color MutedText = new Color(0.70f, 0.76f, 0.69f, 1f);

    private Font uiFont;
    private GameObject mainMenuPage;
    private GameObject mainMenuStoryPage;
    private GameObject openingPage;
    private GameObject mapPage;
    private GameObject dialogueOverlay;
    private GameObject visualNovelPage;
    private GameObject battlePage;
    private GameObject battleResultOverlay;
    private GameObject cardRewardOverlay;
    private RectTransform mapNodesRoot;
    private MapRoomSaveData selectedMapRoom;

    private Text inventoryText;
    private Button craftSoupButton;
    private Button continueGameButton;
    private Text mainMenuStorySoupText;
    private readonly Button[] mainMenuStoryButtons = new Button[3];
    private Text dialogueTitle;
    private Text dialogueBody;
    private Image dialogueIconImage;
    private readonly Button[] choiceButtons = new Button[3];
    private readonly Text[] choiceLabels = new Text[3];

    private Text visualNovelEventTitle;
    private Text visualNovelSpeakerName;
    private Text visualNovelDialogueText;
    private Text visualNovelProgressText;
    private Image visualNovelSceneImage;
    private Image visualNovelPortraitImage;
    private Button visualNovelBackButton;
    private Button visualNovelNextButton;
    private Text visualNovelNextButtonLabel;
    private readonly Button[] visualNovelChoiceButtons = new Button[2];
    private readonly Text[] visualNovelChoiceLabels = new Text[2];
    private Coroutine visualNovelTypingCoroutine;
    private string visualNovelFullText = string.Empty;
    private bool visualNovelIsTyping;
    private int visualNovelLineIndex;
    private int visualNovelBranch;
    private int activeVisualNovelEventId;
    private bool visualNovelIsGallery;
    private readonly List<MysteryStoryLine> activeVisualNovelLines = new List<MysteryStoryLine>();

    private Text enemyHealthText;
    private Text enemyIntentText;
    private Text battleLogText;
    private Text battleTitleText;
    private Image enemyIconImage;
    private Text playerStatusText;
    private Text pileStatusText;
    private readonly Button[] handButtons = new Button[MaxHandSize];
    private readonly Text[] handLabels = new Text[MaxHandSize];
    private Button endTurnButton;
    private Text battleResultTitle;
    private Text battleResultBody;
    private Button battleResultButton;
    private Text battleResultButtonLabel;
    private Text cardRewardTitle;
    private Text cardRewardBody;
    private readonly Button[] cardRewardButtons = new Button[3];
    private readonly Text[] cardRewardLabels = new Text[3];
    private readonly List<string> removalCardChoices = new List<string>();
    private int removalCardIndex;

    private GameManager gameManager;

    private const int CardsDrawnPerTurn = 5;
    private const int MaxEnergy = 3;
    private const int MaxHandSize = 10;
    private int playerHealth;
    private int playerBlock;
    private int playerEnergy;
    private int enemyHealth;
    private int enemyMaxHealth;
    private int enemyAttack;
    private int enemyNextAttackReduction;
    private int enemyTurnNumber;
    private int enemyStrength;
    private int battleLayer;
    private string enemyDisplayName;
    private ElementType enemyElement;
    private bool battleEnded;

    private readonly List<CardDefinition> drawPile = new List<CardDefinition>();
    private readonly List<CardDefinition> hand = new List<CardDefinition>();
    private readonly List<CardDefinition> discardPile = new List<CardDefinition>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindObjectOfType<MistwoodMapUI>() != null) return;
        new GameObject("Mistwood Map UI").AddComponent<MistwoodMapUI>();
    }

    private void Awake()
    {
        gameManager = GameManager.Instance;
        uiFont = LoadUIFont();

        CreateEventSystemIfNeeded();
        CreateInterface();
    }

    private static Font LoadUIFont()
    {
        string[] preferredFonts = { "Microsoft YaHei UI", "Microsoft YaHei", "SimHei" };
        string[] installedFonts = Font.GetOSInstalledFontNames();
        for (int preferred = 0; preferred < preferredFonts.Length; preferred++)
        {
            for (int installed = 0; installed < installedFonts.Length; installed++)
            {
                if (string.Equals(preferredFonts[preferred], installedFonts[installed], StringComparison.OrdinalIgnoreCase))
                {
                    return Font.CreateDynamicFontFromOSFont(installedFonts[installed], 32);
                }
            }
        }

        try
        {
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
        catch (ArgumentException)
        {
            return Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
    }

    private void CreateEventSystemIfNeeded()
    {
        if (FindObjectOfType<EventSystem>() != null) return;
        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        eventSystem.transform.SetParent(transform, false);
    }

    private void CreateInterface()
    {
        GameObject canvasObject = new GameObject(
            "Map UI Canvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        mainMenuPage = CreatePage("Main Menu", canvasObject.transform, Background);
        openingPage = CreatePage("Opening Page", canvasObject.transform, Background);
        mapPage = CreatePage("Map Page", canvasObject.transform, Background);
        battlePage = CreatePage("Battle Page", canvasObject.transform, Background);

        BuildMainMenuPage();
        BuildMainMenuStoryPage(canvasObject.transform);
        BuildOpeningPage();
        BuildMapPage();
        BuildDialogue(canvasObject.transform);
        BuildVisualNovelPage(canvasObject.transform);
        BuildBattlePage();
        BuildCardRewardOverlay(canvasObject.transform);

        mainMenuPage.SetActive(true);
        mainMenuStoryPage.SetActive(false);
        openingPage.SetActive(false);
        mapPage.SetActive(false);
        dialogueOverlay.SetActive(false);
        visualNovelPage.SetActive(false);
        battlePage.SetActive(false);
        cardRewardOverlay.SetActive(false);
        RefreshInventory();
    }

    private void BuildMainMenuPage()
    {
        Text title = CreateText(mainMenuPage.transform, "我有一碗孟婆汤", 64, TextAnchor.MiddleCenter, TextColor);
        SetAnchored(title.rectTransform, new Vector2(0.5f, 0.67f), Vector2.zero, new Vector2(800f, 120f));

        Button newGameButton = CreateButton(mainMenuPage.transform, "New Game", "新游戏", ForestGreen);
        SetAnchored(newGameButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.47f), Vector2.zero, new Vector2(500f, 110f));
        newGameButton.onClick.AddListener(OnNewGameSelected);

        continueGameButton = CreateButton(mainMenuPage.transform, "Continue Game", "继续游戏", Gold);
        SetAnchored(continueGameButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.37f), Vector2.zero, new Vector2(500f, 110f));
        continueGameButton.onClick.AddListener(OnContinueGameSelected);
        continueGameButton.interactable = gameManager.CanContinueGame;

        Button storyButton = CreateButton(mainMenuPage.transform, "Mystery Stories", "神秘事件", Panel);
        SetAnchored(storyButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.27f), Vector2.zero, new Vector2(500f, 110f));
        storyButton.onClick.AddListener(OpenMainMenuStoryPage);
    }

    private void BuildMainMenuStoryPage(Transform canvasTransform)
    {
        mainMenuStoryPage = CreatePage("Main Menu Mystery Stories", canvasTransform, Background);

        Text title = CreateText(mainMenuStoryPage.transform, "神秘事件", 56, TextAnchor.MiddleCenter, TextColor);
        SetAnchored(title.rectTransform, new Vector2(0.5f, 0.78f), Vector2.zero, new Vector2(800f, 100f));

        mainMenuStorySoupText = CreateText(mainMenuStoryPage.transform, "", 29, TextAnchor.MiddleCenter, MutedText);
        SetAnchored(mainMenuStorySoupText.rectTransform, new Vector2(0.5f, 0.68f), Vector2.zero, new Vector2(850f, 120f));

        string[] storyNames = { "重复日", "猫咪线", "艺术家线" };
        for (int i = 0; i < mainMenuStoryButtons.Length; i++)
        {
            int eventId = i + 1;
            mainMenuStoryButtons[i] = CreateButton(
                mainMenuStoryPage.transform,
                "Select Story " + eventId,
                storyNames[i] + "（消耗1碗汤）",
                ForestGreen);
            SetAnchored(
                mainMenuStoryButtons[i].GetComponent<RectTransform>(),
                new Vector2(0.5f, 0.53f - i * 0.105f),
                Vector2.zero,
                new Vector2(600f, 95f));
            mainMenuStoryButtons[i].onClick.AddListener(() => SelectGalleryStory(eventId));
        }

        Button returnButton = CreateButton(mainMenuStoryPage.transform, "Return To Main Menu", "返回主界面", Panel);
        SetAnchored(returnButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.16f), Vector2.zero, new Vector2(430f, 85f));
        returnButton.onClick.AddListener(ReturnFromStorySelectionToMainMenu);
        mainMenuStoryPage.SetActive(false);
    }

    private void BuildOpeningPage()
    {
        Button forestButton = CreateButton(openingPage.transform, "Mistwood Icon", "", ForestGreen);
        SetAnchored(forestButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(180f, 180f));
        ApplySpriteOrFallback(
            forestButton.GetComponent<Image>(),
            Resources.Load<Sprite>("Art/Map/森林图标"),
            ForestGreen);
        forestButton.onClick.AddListener(OpenMap);
    }

    private void BuildMapPage()
    {
        inventoryText = CreateText(mapPage.transform, "", 24, TextAnchor.MiddleCenter, TextColor);
        SetAnchored(inventoryText.rectTransform, new Vector2(0.5f, 0.92f), Vector2.zero, new Vector2(980f, 110f));

        craftSoupButton = CreateButton(mapPage.transform, "Craft Mengpo Soup", "合成孟婆汤（五种羽毛各1枚）", ForestGreen);
        SetAnchored(craftSoupButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.06f), Vector2.zero, new Vector2(520f, 70f));
        craftSoupButton.GetComponentInChildren<Text>().fontSize = 23;
        craftSoupButton.onClick.AddListener(CraftMengpoSoup);

        GameObject nodes = new GameObject("Map Nodes", typeof(RectTransform));
        nodes.transform.SetParent(mapPage.transform, false);
        mapNodesRoot = nodes.GetComponent<RectTransform>();
        mapNodesRoot.anchorMin = Vector2.zero;
        mapNodesRoot.anchorMax = Vector2.one;
        mapNodesRoot.offsetMin = Vector2.zero;
        mapNodesRoot.offsetMax = Vector2.zero;
    }

    private void BuildDialogue(Transform canvasTransform)
    {
        dialogueOverlay = CreatePage("Fire Spirit Dialogue", canvasTransform, new Color(0f, 0f, 0f, 0.72f));

        GameObject dialoguePanel = CreateImage("Dialogue Panel", dialogueOverlay.transform, Panel);
        SetAnchored(dialoguePanel.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(780f, 900f));

        GameObject spiritIcon = CreateImage("Room Artwork", dialoguePanel.transform, FireOrange);
        SetAnchored(spiritIcon.GetComponent<RectTransform>(), new Vector2(0.5f, 0.84f), Vector2.zero, new Vector2(150f, 150f));
        dialogueIconImage = spiritIcon.GetComponent<Image>();
        dialogueIconImage.raycastTarget = false;

        dialogueTitle = CreateText(dialoguePanel.transform, "火精灵", 42, TextAnchor.MiddleCenter, TextColor);
        SetAnchored(dialogueTitle.rectTransform, new Vector2(0.5f, 0.68f), Vector2.zero, new Vector2(680f, 80f));

        dialogueBody = CreateText(dialoguePanel.transform, "", 30, TextAnchor.MiddleCenter, MutedText);
        SetAnchored(dialogueBody.rectTransform, new Vector2(0.5f, 0.52f), Vector2.zero, new Vector2(660f, 210f));

        for (int i = 0; i < choiceButtons.Length; i++)
        {
            choiceButtons[i] = CreateButton(dialoguePanel.transform, "Choice " + (i + 1), "", ForestGreen);
            SetAnchored(
                choiceButtons[i].GetComponent<RectTransform>(),
                new Vector2(0.5f, 0.36f),
                new Vector2(0f, -i * 115f),
                new Vector2(600f, 90f));
            choiceLabels[i] = choiceButtons[i].GetComponentInChildren<Text>();
        }
    }

    private void BuildVisualNovelPage(Transform canvasTransform)
    {
        visualNovelPage = CreatePage("Mystery Visual Novel", canvasTransform, new Color(0.035f, 0.045f, 0.04f, 1f));

        GameObject sceneObject = CreateImage("Story Scene Artwork", visualNovelPage.transform, Color.white);
        SetAnchored(
            sceneObject.GetComponent<RectTransform>(),
            new Vector2(0.5f, 0.61f),
            Vector2.zero,
            new Vector2(1040f, 585f));
        visualNovelSceneImage = sceneObject.GetComponent<Image>();
        visualNovelSceneImage.preserveAspect = true;
        visualNovelSceneImage.raycastTarget = false;
        visualNovelSceneImage.enabled = false;

        visualNovelEventTitle = CreateText(
            visualNovelPage.transform,
            "回忆",
            38,
            TextAnchor.MiddleCenter,
            MutedText);
        SetAnchored(
            visualNovelEventTitle.rectTransform,
            new Vector2(0.5f, 0.94f),
            Vector2.zero,
            new Vector2(900f, 70f));

        visualNovelBackButton = CreateButton(
            visualNovelPage.transform,
            "Leave Memory",
            "返回",
            Panel);
        SetAnchored(
            visualNovelBackButton.GetComponent<RectTransform>(),
            new Vector2(0.12f, 0.94f),
            Vector2.zero,
            new Vector2(180f, 62f));
        visualNovelBackButton.GetComponentInChildren<Text>().fontSize = 23;
        visualNovelBackButton.onClick.AddListener(ExitVisualNovelEarly);

        GameObject portraitObject = CreateImage("Current Speaker Portrait", visualNovelPage.transform, Color.white);
        SetAnchored(
            portraitObject.GetComponent<RectTransform>(),
            new Vector2(0.5f, 0.61f),
            Vector2.zero,
            new Vector2(600f, 900f));
        visualNovelPortraitImage = portraitObject.GetComponent<Image>();
        visualNovelPortraitImage.preserveAspect = true;
        visualNovelPortraitImage.raycastTarget = false;
        visualNovelPortraitImage.enabled = false;

        GameObject dialoguePanel = CreateImage(
            "Visual Novel Dialogue Panel",
            visualNovelPage.transform,
            new Color(0.07f, 0.09f, 0.08f, 0.96f));
        SetAnchored(
            dialoguePanel.GetComponent<RectTransform>(),
            new Vector2(0.5f, 0.18f),
            Vector2.zero,
            new Vector2(980f, 470f));

        visualNovelSpeakerName = CreateText(
            dialoguePanel.transform,
            "",
            34,
            TextAnchor.MiddleLeft,
            Gold);
        SetAnchored(
            visualNovelSpeakerName.rectTransform,
            new Vector2(0.21f, 0.86f),
            Vector2.zero,
            new Vector2(340f, 70f));

        visualNovelProgressText = CreateText(
            dialoguePanel.transform,
            "",
            23,
            TextAnchor.MiddleRight,
            MutedText);
        SetAnchored(
            visualNovelProgressText.rectTransform,
            new Vector2(0.84f, 0.86f),
            Vector2.zero,
            new Vector2(180f, 60f));

        visualNovelDialogueText = CreateText(
            dialoguePanel.transform,
            "",
            31,
            TextAnchor.UpperLeft,
            TextColor);
        SetAnchored(
            visualNovelDialogueText.rectTransform,
            new Vector2(0.5f, 0.55f),
            Vector2.zero,
            new Vector2(860f, 190f));

        visualNovelNextButton = CreateButton(
            dialoguePanel.transform,
            "Advance Dialogue",
            "继续 ▶",
            ForestGreen);
        SetAnchored(
            visualNovelNextButton.GetComponent<RectTransform>(),
            new Vector2(0.81f, 0.14f),
            Vector2.zero,
            new Vector2(250f, 72f));
        visualNovelNextButtonLabel = visualNovelNextButton.GetComponentInChildren<Text>();
        visualNovelNextButtonLabel.fontSize = 24;
        visualNovelNextButton.onClick.AddListener(AdvanceVisualNovelDialogue);

        for (int i = 0; i < visualNovelChoiceButtons.Length; i++)
        {
            int choiceIndex = i;
            visualNovelChoiceButtons[i] = CreateButton(
                dialoguePanel.transform,
                "Story Choice " + (i + 1),
                "",
                ForestGreen);
            SetAnchored(
                visualNovelChoiceButtons[i].GetComponent<RectTransform>(),
                new Vector2(0.30f + i * 0.40f, 0.14f),
                Vector2.zero,
                new Vector2(340f, 72f));
            visualNovelChoiceLabels[i] = visualNovelChoiceButtons[i].GetComponentInChildren<Text>();
            visualNovelChoiceLabels[i].fontSize = 23;
            visualNovelChoiceButtons[i].onClick.AddListener(() => ChooseVisualNovelOption(choiceIndex));
            visualNovelChoiceButtons[i].gameObject.SetActive(false);
        }
        visualNovelPage.SetActive(false);
    }

    private void BuildBattlePage()
    {
        battleTitleText = CreateText(battlePage.transform, "精灵战斗", 46, TextAnchor.MiddleCenter, TextColor);
        SetAnchored(battleTitleText.rectTransform, new Vector2(0.5f, 0.92f), Vector2.zero, new Vector2(800f, 90f));

        GameObject enemyIcon = CreateImage("Spirit Enemy", battlePage.transform, FireOrange);
        SetAnchored(enemyIcon.GetComponent<RectTransform>(), new Vector2(0.5f, 0.71f), Vector2.zero, new Vector2(170f, 170f));
        enemyIconImage = enemyIcon.GetComponent<Image>();

        enemyHealthText = CreateText(battlePage.transform, "", 31, TextAnchor.MiddleCenter, TextColor);
        SetAnchored(enemyHealthText.rectTransform, new Vector2(0.5f, 0.59f), Vector2.zero, new Vector2(700f, 65f));

        enemyIntentText = CreateText(battlePage.transform, "", 26, TextAnchor.MiddleCenter, Gold);
        SetAnchored(enemyIntentText.rectTransform, new Vector2(0.5f, 0.54f), Vector2.zero, new Vector2(700f, 60f));

        battleLogText = CreateText(battlePage.transform, "", 24, TextAnchor.MiddleCenter, MutedText);
        SetAnchored(battleLogText.rectTransform, new Vector2(0.5f, 0.485f), Vector2.zero, new Vector2(900f, 70f));

        playerStatusText = CreateText(battlePage.transform, "", 29, TextAnchor.MiddleCenter, TextColor);
        SetAnchored(playerStatusText.rectTransform, new Vector2(0.5f, 0.41f), Vector2.zero, new Vector2(900f, 100f));

        pileStatusText = CreateText(battlePage.transform, "", 23, TextAnchor.MiddleCenter, MutedText);
        SetAnchored(pileStatusText.rectTransform, new Vector2(0.5f, 0.345f), Vector2.zero, new Vector2(900f, 60f));

        for (int i = 0; i < handButtons.Length; i++)
        {
            int handIndex = i;
            int row = i / 5;
            int column = i % 5;
            handButtons[i] = CreateButton(battlePage.transform, "Hand Card " + (i + 1), "", Panel);
            SetAnchored(
                handButtons[i].GetComponent<RectTransform>(),
                new Vector2(0.1f + column * 0.2f, row == 0 ? 0.215f : 0.10f),
                Vector2.zero,
                new Vector2(188f, 205f));
            handLabels[i] = handButtons[i].GetComponentInChildren<Text>();
            handLabels[i].fontSize = 21;
            handButtons[i].onClick.AddListener(() => PlayCard(handIndex));
        }

        endTurnButton = CreateButton(battlePage.transform, "End Turn", "结束回合", ForestGreen);
        SetAnchored(endTurnButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.025f), Vector2.zero, new Vector2(340f, 70f));
        endTurnButton.onClick.AddListener(EndPlayerTurn);

        BuildBattleResultOverlay();
    }

    private void BuildBattleResultOverlay()
    {
        battleResultOverlay = CreatePage("Battle Result", battlePage.transform, new Color(0f, 0f, 0f, 0.78f));

        GameObject resultPanel = CreateImage("Result Panel", battleResultOverlay.transform, Panel);
        SetAnchored(resultPanel.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(720f, 560f));

        battleResultTitle = CreateText(resultPanel.transform, "", 50, TextAnchor.MiddleCenter, TextColor);
        SetAnchored(battleResultTitle.rectTransform, new Vector2(0.5f, 0.72f), Vector2.zero, new Vector2(620f, 100f));

        battleResultBody = CreateText(resultPanel.transform, "", 30, TextAnchor.MiddleCenter, MutedText);
        SetAnchored(battleResultBody.rectTransform, new Vector2(0.5f, 0.51f), Vector2.zero, new Vector2(620f, 150f));

        battleResultButton = CreateButton(resultPanel.transform, "Result Action", "", ForestGreen);
        SetAnchored(battleResultButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.22f), Vector2.zero, new Vector2(470f, 100f));
        battleResultButtonLabel = battleResultButton.GetComponentInChildren<Text>();
        battleResultOverlay.SetActive(false);
    }

    private void BuildCardRewardOverlay(Transform canvasTransform)
    {
        cardRewardOverlay = CreatePage("Card Reward", canvasTransform, new Color(0f, 0f, 0f, 0.82f));

        GameObject rewardPanel = CreateImage("Card Reward Panel", cardRewardOverlay.transform, Panel);
        SetAnchored(rewardPanel.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(980f, 1080f));

        cardRewardTitle = CreateText(rewardPanel.transform, "卡牌奖励", 50, TextAnchor.MiddleCenter, TextColor);
        SetAnchored(cardRewardTitle.rectTransform, new Vector2(0.5f, 0.87f), Vector2.zero, new Vector2(850f, 100f));

        cardRewardBody = CreateText(rewardPanel.transform, "", 28, TextAnchor.MiddleCenter, MutedText);
        SetAnchored(cardRewardBody.rectTransform, new Vector2(0.5f, 0.75f), Vector2.zero, new Vector2(850f, 130f));

        for (int i = 0; i < cardRewardButtons.Length; i++)
        {
            int rewardIndex = i;
            cardRewardButtons[i] = CreateButton(rewardPanel.transform, "Reward Card " + (i + 1), "", ForestGreen);
            SetAnchored(
                cardRewardButtons[i].GetComponent<RectTransform>(),
                new Vector2(0.18f + i * 0.32f, 0.40f),
                Vector2.zero,
                new Vector2(270f, 510f));
            cardRewardLabels[i] = cardRewardButtons[i].GetComponentInChildren<Text>();
            cardRewardLabels[i].fontSize = 23;
            cardRewardButtons[i].onClick.AddListener(() => ClaimCardReward(rewardIndex));
        }
        cardRewardOverlay.SetActive(false);
    }

    private void OnNewGameSelected()
    {
        gameManager.StartNewGame();
        EnterMistwoodOpening();
    }

    private void OnContinueGameSelected()
    {
        if (!gameManager.ContinueGame()) return;
        EnterMistwoodOpening();
    }

    private void OpenMainMenuStoryPage()
    {
        if (gameManager.HasPendingGalleryStory)
        {
            StartGalleryVisualNovel(gameManager.PendingGalleryStoryEventId);
            return;
        }

        mainMenuPage.SetActive(false);
        mainMenuStoryPage.SetActive(true);
        RefreshMainMenuStoryPage();
    }

    private void RefreshMainMenuStoryPage()
    {
        bool canWatch = gameManager.MengpoSoupCount > 0;
        mainMenuStorySoupText.text = "消耗1碗孟婆汤，可以直接选择一条剧情观看。\n" +
                                     "当前拥有孟婆汤：" + gameManager.MengpoSoupCount + "碗";
        for (int i = 0; i < mainMenuStoryButtons.Length; i++)
        {
            mainMenuStoryButtons[i].interactable = canWatch;
        }
    }

    private void SelectGalleryStory(int eventId)
    {
        string message;
        if (!gameManager.TryStartGalleryStory(eventId, out message))
        {
            RefreshMainMenuStoryPage();
            mainMenuStorySoupText.text = message + "\n当前拥有孟婆汤：" + gameManager.MengpoSoupCount + "碗";
            return;
        }

        StartGalleryVisualNovel(eventId);
    }

    private void StartGalleryVisualNovel(int eventId)
    {
        selectedMapRoom = null;
        visualNovelIsGallery = true;
        mainMenuPage.SetActive(false);
        mainMenuStoryPage.SetActive(false);
        StartMysteryVisualNovel(eventId);
    }

    private void ReturnFromStorySelectionToMainMenu()
    {
        mainMenuStoryPage.SetActive(false);
        ShowMainMenuPage();
    }

    private void ShowMainMenuPage()
    {
        StopVisualNovelTyping();
        selectedMapRoom = null;
        mainMenuStoryPage.SetActive(false);
        openingPage.SetActive(false);
        mapPage.SetActive(false);
        dialogueOverlay.SetActive(false);
        visualNovelPage.SetActive(false);
        battlePage.SetActive(false);
        battleResultOverlay.SetActive(false);
        cardRewardOverlay.SetActive(false);
        mainMenuPage.SetActive(true);
        continueGameButton.interactable = gameManager.CanContinueGame;
    }

    private void EnterMistwoodOpening()
    {
        StopVisualNovelTyping();
        mainMenuPage.SetActive(false);
        mainMenuStoryPage.SetActive(false);
        openingPage.SetActive(true);
        mapPage.SetActive(false);
        dialogueOverlay.SetActive(false);
        visualNovelPage.SetActive(false);
        battlePage.SetActive(false);
        cardRewardOverlay.SetActive(false);
        RefreshInventory();
        RefreshMapNodes();
    }

    private void OpenMap()
    {
        openingPage.SetActive(false);
        mapPage.SetActive(true);
        RefreshInventory();
        RefreshMapNodes();
        if (gameManager.HasPendingCardReward) ShowPendingCardReward();
    }

    private void ShowPendingCardReward()
    {
        if (!gameManager.HasPendingCardReward || gameManager.PendingRewardCardIds == null ||
            gameManager.PendingRewardCardIds.Count != 3)
        {
            return;
        }

        MapRoomSaveData sourceRoom = gameManager.FindMapRoom(gameManager.CurrentSave.pendingRewardRoomId);
        ElementType featherElement = sourceRoom != null
            ? GetElementForRoom(sourceRoom.contentType)
            : ElementType.None;
        string spiritName = sourceRoom != null
            ? MistwoodMapGenerator.GetDisplayName(sourceRoom.contentType)
            : "精灵";

        cardRewardTitle.text = spiritName + "战斗奖励";
        cardRewardBody.text = "获得了1枚" + ElementSystem.GetDisplayName(featherElement) +
                              "羽毛。\n从以下3张牌中选择1张加入牌组。";

        for (int i = 0; i < cardRewardButtons.Length; i++)
        {
            CardDefinition card = CardCatalog.Get(gameManager.PendingRewardCardIds[i]);
            bool validCard = card != null;
            cardRewardButtons[i].gameObject.SetActive(true);
            cardRewardButtons[i].interactable = validCard;
            if (!validCard)
            {
                cardRewardLabels[i].text = "卡牌数据缺失";
                continue;
            }

            cardRewardButtons[i].GetComponent<Image>().color = card.DisplayColor;
            cardRewardLabels[i].text = card.Name + "\n\n" + ElementSystem.GetDisplayName(card.Element) +
                                       "属性 · " + CardCatalog.GetQualityDisplayName(card.Quality) +
                                       "\n" + card.Description + "\n\n消耗 " + card.Cost + " 点能量";
        }

        cardRewardOverlay.SetActive(true);
    }

    private void ClaimCardReward(int rewardIndex)
    {
        if (!gameManager.HasPendingCardReward || gameManager.PendingRewardCardIds == null ||
            rewardIndex < 0 || rewardIndex >= gameManager.PendingRewardCardIds.Count)
        {
            return;
        }

        string cardId = gameManager.PendingRewardCardIds[rewardIndex];
        if (!gameManager.ClaimPendingCardReward(cardId)) return;

        cardRewardOverlay.SetActive(false);
        battleResultOverlay.SetActive(false);
        battlePage.SetActive(false);
        mapPage.SetActive(true);
        selectedMapRoom = null;
        RefreshInventory();
        RefreshMapNodes();
    }

    private void OpenPendingCardRewardAfterBattle()
    {
        battleResultOverlay.SetActive(false);
        battlePage.SetActive(false);
        mapPage.SetActive(true);
        ShowPendingCardReward();
    }

    private void RefreshMapNodes()
    {
        if (mapNodesRoot == null || gameManager.MapRooms == null) return;

        for (int childIndex = mapNodesRoot.childCount - 1; childIndex >= 0; childIndex--)
        {
            Destroy(mapNodesRoot.GetChild(childIndex).gameObject);
        }

        for (int roomIndex = 0; roomIndex < gameManager.MapRooms.Count; roomIndex++)
        {
            MapRoomSaveData room = gameManager.MapRooms[roomIndex];
            int indexInLayer = GetIndexInLayer(room);
            int roomsInLayer = GetRoomCountInLayer(room.layer);
            float x = GetRoomAnchorX(indexInLayer, roomsInLayer);
            float y = 0.17f + (room.layer - 1) * 0.16f;

            string roomId = room.roomId;
            string completedMark = room.completed ? "✓ " : string.Empty;
            string label = completedMark + room.slotName + "\n" + MistwoodMapGenerator.GetDisplayName(room.contentType);
            Button roomButton = CreateButton(mapNodesRoot, room.roomId, label, GetRoomColor(room.contentType));
            SetAnchored(roomButton.GetComponent<RectTransform>(), new Vector2(x, y), Vector2.zero, new Vector2(210f, 112f));
            roomButton.GetComponentInChildren<Text>().fontSize = 22;
            AddRoomButtonArtwork(roomButton, room);
            roomButton.interactable = room.layer == gameManager.CurrentMapLayer && !room.completed;
            roomButton.onClick.AddListener(() => OnMapRoomSelected(roomId));
        }
    }

    private int GetIndexInLayer(MapRoomSaveData target)
    {
        int index = 0;
        for (int i = 0; i < gameManager.MapRooms.Count; i++)
        {
            MapRoomSaveData room = gameManager.MapRooms[i];
            if (room.layer != target.layer) continue;
            if (ReferenceEquals(room, target)) return index;
            index++;
        }
        return index;
    }

    private int GetRoomCountInLayer(int layer)
    {
        int count = 0;
        for (int i = 0; i < gameManager.MapRooms.Count; i++)
        {
            if (gameManager.MapRooms[i].layer == layer) count++;
        }
        return count;
    }

    private static float GetRoomAnchorX(int index, int count)
    {
        if (count <= 1) return 0.5f;
        if (count == 2) return index == 0 ? 0.32f : 0.68f;
        return 0.2f + index * 0.3f;
    }

    private static Color GetRoomColor(RoomContentType content)
    {
        switch (content)
        {
            case RoomContentType.FireSpirit: return FireOrange;
            case RoomContentType.GoldSpirit: return Gold;
            case RoomContentType.WoodSpirit: return ForestGreen;
            case RoomContentType.WaterSpirit: return new Color(0.20f, 0.43f, 0.66f, 1f);
            case RoomContentType.EarthSpirit: return new Color(0.48f, 0.34f, 0.22f, 1f);
            case RoomContentType.RestSite: return new Color(0.36f, 0.48f, 0.40f, 1f);
            case RoomContentType.MysteryEvent: return new Color(0.42f, 0.27f, 0.55f, 1f);
            case RoomContentType.Treasure: return Gold;
            default: return Panel;
        }
    }

    private void OnMapRoomSelected(string roomId)
    {
        MapRoomSaveData room = gameManager.FindMapRoom(roomId);
        if (room == null || room.layer != gameManager.CurrentMapLayer || room.completed) return;

        selectedMapRoom = room;
        if (IsSpiritRoom(room.contentType))
        {
            MeetSpirit(room);
            return;
        }

        ShowPlaceholderRoom(room);
    }

    private static bool IsSpiritRoom(RoomContentType contentType)
    {
        return contentType == RoomContentType.FireSpirit || MistwoodMapGenerator.IsElementalSpirit(contentType);
    }

    private void MeetSpirit(MapRoomSaveData room)
    {
        dialogueOverlay.SetActive(true);
        ResetDialogueChoices();
        SetDialogueRoomArtwork(room.contentType, false);
        string spiritName = MistwoodMapGenerator.GetDisplayName(room.contentType);
        dialogueTitle.text = "第" + room.layer + "层 · " + spiritName;
        dialogueBody.text = spiritName + "挡住了通往森林深处的道路。\n准备进入卡牌战斗。";
        ConfigureChoice(0, "进入战斗", StartBattle, true);
    }

    private void ShowPlaceholderRoom(MapRoomSaveData room)
    {
        dialogueOverlay.SetActive(true);
        ResetDialogueChoices();
        SetDialogueRoomArtwork(room.contentType, false);
        string roomName = MistwoodMapGenerator.GetDisplayName(room.contentType);
        dialogueTitle.text = room.slotName + "房间 · " + roomName;

        string actionLabel = "完成房间";
        UnityAction roomAction = CompleteSelectedRoomAndClose;
        switch (room.contentType)
        {
            case RoomContentType.MysteryEvent:
                ShowMysteryRoom(room);
                return;
            case RoomContentType.RestSite:
                dialogueBody.text = "你找到了一处安全的休息处。\n当前生命：" +
                                    gameManager.PlayerCurrentHealth + " / " + gameManager.PlayerMaxHealth;
                actionLabel = "休息（恢复15%生命）";
                roomAction = RestAtSelectedRoom;
                break;
            case RoomContentType.Treasure:
                dialogueBody.text = "你抵达了迷雾森林尽头的宝箱。\n里面装着3种随机羽毛。";
                actionLabel = "打开宝箱";
                roomAction = OpenSelectedTreasure;
                break;
            default:
                dialogueBody.text = "你遇到了" + roomName + "。\n该精灵的战斗内容等待后续配置。";
                break;
        }

        ConfigureChoice(0, actionLabel, roomAction, true);
    }

    private void ShowMysteryRoom(MapRoomSaveData room)
    {
        ResetDialogueChoices();
        dialogueTitle.text = room.slotName + "房间 · 神秘事件";

        if (room.mysterySpecialEventEntered)
        {
            ShowMysterySpecialEvent(room.mysteryEventId);
            return;
        }

        dialogueBody.text = "迷雾中出现了一段陌生的回忆。\n" +
                            "进入后会从《重复日》《猫咪线》《艺术家线》中随机播放一条。\n" +
                            "地图中的神秘事件不消耗孟婆汤。";

        ConfigureChoice(0, "观看随机剧情", EnterMysterySpecialEvent, true);
        ConfigureChoice(1, "返回地图", CloseSelectedRoomDialogue, true);
    }

    private void EnterMysterySpecialEvent()
    {
        if (selectedMapRoom == null) return;

        int eventId;
        string message;
        if (!gameManager.TryEnterMysterySpecialEvent(selectedMapRoom.roomId, out eventId, out message))
        {
            ResetDialogueChoices();
            dialogueTitle.text = "无法进入特殊事件";
            dialogueBody.text = message;
            ConfigureChoice(0, "返回", () => ShowMysteryRoom(selectedMapRoom), true);
            return;
        }

        RefreshInventory();
        ShowMysterySpecialEvent(eventId);
    }

    private void ShowMysterySpecialEvent(int eventId)
    {
        visualNovelIsGallery = false;
        StartMysteryVisualNovel(eventId);
    }

    private void StartMysteryVisualNovel(int eventId)
    {
        MysteryStoryDefinition story = MysteryStoryCatalog.GetStory(eventId);
        activeVisualNovelEventId = eventId;
        activeVisualNovelLines.Clear();
        visualNovelBranch = visualNovelIsGallery
            ? gameManager.GalleryStoryBranch
            : (selectedMapRoom != null ? selectedMapRoom.mysteryStoryBranch : 0);
        if (eventId == 2 && visualNovelBranch > 0)
        {
            AddCatRefusalEnding(visualNovelBranch);
        }
        else if (story != null && story.Lines != null)
        {
            visualNovelBranch = 0;
            activeVisualNovelLines.AddRange(story.Lines);
        }
        if (activeVisualNovelLines.Count == 0)
        {
            CompleteSelectedRoomAndClose();
            return;
        }
        int savedLineIndex = visualNovelIsGallery
            ? gameManager.GalleryStoryLineIndex
            : (selectedMapRoom != null ? selectedMapRoom.mysteryDialogueLineIndex : 0);
        visualNovelLineIndex = Mathf.Clamp(savedLineIndex, 0, activeVisualNovelLines.Count - 1);
        visualNovelEventTitle.text = (visualNovelIsGallery ? "剧情回顾 · " : "特殊事件 " + eventId + " · ") + story.Title;
        dialogueOverlay.SetActive(false);
        visualNovelPage.SetActive(true);
        ShowCurrentVisualNovelLine();
    }

    private void ShowCurrentVisualNovelLine()
    {
        if (visualNovelLineIndex < 0 || visualNovelLineIndex >= activeVisualNovelLines.Count) return;

        MysteryStoryLine line = activeVisualNovelLines[visualNovelLineIndex];
        visualNovelSpeakerName.text = line.Speaker;
        visualNovelProgressText.text = (visualNovelLineIndex + 1) + " / " + activeVisualNovelLines.Count;

        Sprite scene = LoadMysteryScene(activeVisualNovelEventId, visualNovelLineIndex);
        visualNovelSceneImage.sprite = scene;
        visualNovelSceneImage.color = Color.white;
        visualNovelSceneImage.enabled = scene != null;

        Sprite portrait = LoadMysteryPortrait(line.Speaker);
        visualNovelPortraitImage.sprite = portrait;
        visualNovelPortraitImage.color = Color.white;
        visualNovelPortraitImage.enabled = scene == null && portrait != null;

        StopVisualNovelTyping();
        HideVisualNovelChoices();
        visualNovelNextButton.gameObject.SetActive(true);
        visualNovelFullText = line.Text ?? string.Empty;
        visualNovelDialogueText.text = string.Empty;
        visualNovelIsTyping = true;
        visualNovelNextButtonLabel.text = "显示全部";
        visualNovelTypingCoroutine = StartCoroutine(RevealVisualNovelText());
    }

    private Sprite LoadMysteryScene(int eventId, int lineIndex)
    {
        string resourceName = null;
        int lastLine = Mathf.Min(lineIndex, activeVisualNovelLines.Count - 1);
        for (int i = 0; i <= lastLine; i++)
        {
            string text = activeVisualNovelLines[i].Text ?? string.Empty;
            if (eventId == 1 && text.Contains("再抱一抱"))
            {
                resourceName = "c1";
            }
            else if (eventId == 2)
            {
                if (text.Contains("套着红色夹袄")) resourceName = "a1";
                else if (text.Contains("残阳照着夫妇二人")) resourceName = "a2";
                else if (text.Contains("男主人一下一下")) resourceName = "a4";
                else if (text.Contains("一瘸一拐地在小巷中逃窜")) resourceName = "a3";
                else if (text.Contains("蜷缩在回收点的纸箱中")) resourceName = "a5";
            }
            else if (eventId == 3 && text.Contains("真正的愿望"))
            {
                resourceName = "b1";
            }
        }

        return string.IsNullOrEmpty(resourceName)
            ? null
            : Resources.Load<Sprite>("Art/StoryCG/" + resourceName);
    }

    private static Sprite LoadMysteryPortrait(string speaker)
    {
        if (string.IsNullOrEmpty(speaker) || speaker == "旁白" || speaker == "章节" ||
            speaker == "日记" || speaker == "评语" || speaker == "回复")
        {
            return null;
        }
        string portraitName;
        switch (speaker)
        {
            case "我": portraitName = "孟婆"; break;
            case "老人": portraitName = "老人男"; break;
            case "老妪": portraitName = "老人女"; break;
            case "猫冬":
            case "猫":
            case "？？？": portraitName = "猫"; break;
            case "泠汐":
            case "文艺的女生": portraitName = "女生"; break;
            case "白无常": portraitName = "白无常"; break;
            case "黑无常": portraitName = "黑无常"; break;
            default: return null;
        }

        return Resources.Load<Sprite>("Art/Portraits/" + portraitName);
    }

    private IEnumerator RevealVisualNovelText()
    {
        for (int characterCount = 1; characterCount <= visualNovelFullText.Length; characterCount++)
        {
            visualNovelDialogueText.text = visualNovelFullText.Substring(0, characterCount);
            yield return new WaitForSecondsRealtime(0.025f);
        }

        visualNovelTypingCoroutine = null;
        visualNovelIsTyping = false;
        UpdateVisualNovelAdvanceLabel();
    }

    private void AdvanceVisualNovelDialogue()
    {
        if (visualNovelIsTyping)
        {
            CompleteVisualNovelTypingInstantly();
            return;
        }

        if (visualNovelLineIndex + 1 < activeVisualNovelLines.Count)
        {
            visualNovelLineIndex++;
            SaveCurrentMysteryStoryProgress();
            ShowCurrentVisualNovelLine();
            return;
        }

        FinishMysteryVisualNovel();
    }

    private void CompleteVisualNovelTypingInstantly()
    {
        StopVisualNovelTyping();
        visualNovelDialogueText.text = visualNovelFullText;
        UpdateVisualNovelAdvanceLabel();
    }

    private void StopVisualNovelTyping()
    {
        if (visualNovelTypingCoroutine != null)
        {
            StopCoroutine(visualNovelTypingCoroutine);
            visualNovelTypingCoroutine = null;
        }
        visualNovelIsTyping = false;
    }

    private void UpdateVisualNovelAdvanceLabel()
    {
        if (visualNovelLineIndex >= 0 && visualNovelLineIndex < activeVisualNovelLines.Count &&
            !string.IsNullOrEmpty(activeVisualNovelLines[visualNovelLineIndex].ChoiceId))
        {
            visualNovelNextButton.gameObject.SetActive(false);
            ShowVisualNovelChoices(activeVisualNovelLines[visualNovelLineIndex].ChoiceId);
            return;
        }

        HideVisualNovelChoices();
        visualNovelNextButton.gameObject.SetActive(true);
        visualNovelNextButtonLabel.text = visualNovelLineIndex + 1 >= activeVisualNovelLines.Count
            ? "结束回忆"
            : "继续 ▶";
    }

    private void ShowVisualNovelChoices(string choiceId)
    {
        if (choiceId == "cat_touch")
        {
            ConfigureVisualNovelChoice(0, "同意给它摸火羽");
            ConfigureVisualNovelChoice(1, "拒绝");
            return;
        }
        if (choiceId == "cat_help")
        {
            ConfigureVisualNovelChoice(0, "答应帮助猫冬");
            ConfigureVisualNovelChoice(1, "拒绝");
            return;
        }

        visualNovelNextButton.gameObject.SetActive(true);
        visualNovelNextButtonLabel.text = "继续 ▶";
    }

    private void ConfigureVisualNovelChoice(int index, string label)
    {
        visualNovelChoiceButtons[index].gameObject.SetActive(true);
        visualNovelChoiceLabels[index].text = label;
    }

    private void HideVisualNovelChoices()
    {
        for (int i = 0; i < visualNovelChoiceButtons.Length; i++)
        {
            visualNovelChoiceButtons[i].gameObject.SetActive(false);
        }
    }

    private void ChooseVisualNovelOption(int choiceIndex)
    {
        if (visualNovelLineIndex < 0 || visualNovelLineIndex >= activeVisualNovelLines.Count) return;

        string choiceId = activeVisualNovelLines[visualNovelLineIndex].ChoiceId;
        if (choiceIndex == 0)
        {
            visualNovelLineIndex++;
            SaveCurrentMysteryStoryProgress();
            ShowCurrentVisualNovelLine();
            return;
        }

        visualNovelBranch = choiceId == "cat_touch" ? 1 : 2;
        activeVisualNovelLines.Clear();
        AddCatRefusalEnding(visualNovelBranch);
        visualNovelLineIndex = 0;
        SaveCurrentMysteryStoryProgress();
        ShowCurrentVisualNovelLine();
    }

    private void AddCatRefusalEnding(int branch)
    {
        if (branch == 1)
        {
            activeVisualNovelLines.Add(new MysteryStoryLine(
                "猫",
                "好吧呜，没关系的，我会一直等的呜……",
                string.Empty));
            activeVisualNovelLines.Add(new MysteryStoryLine(
                "旁白",
                "你拒绝了橘猫的请求，这段回忆到此结束。",
                string.Empty));
            return;
        }

        activeVisualNovelLines.Add(new MysteryStoryLine(
            "旁白",
            "一只猫能有什么好东西？我绕过它离开了，这段回忆到此结束。",
            string.Empty));
    }

    private void SaveCurrentMysteryStoryProgress()
    {
        if (visualNovelIsGallery)
        {
            gameManager.SaveGalleryStoryProgress(visualNovelLineIndex, visualNovelBranch);
            return;
        }

        if (selectedMapRoom == null) return;
        gameManager.SaveMysteryStoryProgress(
            selectedMapRoom.roomId,
            visualNovelLineIndex,
            visualNovelBranch);
    }

    private void FinishMysteryVisualNovel()
    {
        StopVisualNovelTyping();
        visualNovelPage.SetActive(false);
        activeVisualNovelLines.Clear();
        if (visualNovelIsGallery)
        {
            gameManager.CompleteGalleryStory();
            visualNovelIsGallery = false;
            ShowMainMenuPage();
            return;
        }
        CompleteSelectedRoomAndClose();
    }

    private void ExitVisualNovelEarly()
    {
        StopVisualNovelTyping();
        visualNovelPage.SetActive(false);
        activeVisualNovelLines.Clear();

        if (visualNovelIsGallery)
        {
            visualNovelIsGallery = false;
            ShowMainMenuPage();
            return;
        }

        selectedMapRoom = null;
        dialogueOverlay.SetActive(false);
        mapPage.SetActive(true);
        RefreshInventory();
        RefreshMapNodes();
    }

    private void BeginMysteryCardRemoval()
    {
        removalCardChoices.Clear();
        if (gameManager.DeckCardIds != null)
        {
            for (int i = 0; i < gameManager.DeckCardIds.Count; i++)
            {
                string cardId = CardCatalog.GetCanonicalCardId(gameManager.DeckCardIds[i]);
                if (!removalCardChoices.Contains(cardId)) removalCardChoices.Add(cardId);
            }
        }

        removalCardIndex = 0;
        ShowMysteryCardRemovalChoice();
    }

    private void ShowMysteryCardRemovalChoice()
    {
        ResetDialogueChoices();
        dialogueTitle.text = "选择要删除的卡牌";
        if (removalCardChoices.Count == 0)
        {
            dialogueBody.text = "牌堆为空，不能移除。";
            ConfigureChoice(0, "返回神秘事件", () => ShowMysteryRoom(selectedMapRoom), true);
            return;
        }

        removalCardIndex = Mathf.Clamp(removalCardIndex, 0, removalCardChoices.Count - 1);
        string cardId = removalCardChoices[removalCardIndex];
        CardDefinition card = CardCatalog.Get(cardId);
        int ownedCopies = CountCardsInDeck(cardId);
        dialogueBody.text = card != null
            ? (removalCardIndex + 1) + " / " + removalCardChoices.Count + "\n" +
              card.Name + " · " + ElementSystem.GetDisplayName(card.Element) + "属性 · " +
              CardCatalog.GetQualityDisplayName(card.Quality) + "\n" + card.Description +
              "\n消耗 " + card.Cost + " 点能量　持有 " + ownedCopies + " 张"
            : "卡牌数据缺失：" + cardId;

        ConfigureChoice(0, card != null ? "删除「" + card.Name + "」" : "无法删除", RemoveDisplayedMysteryCard, card != null);
        ConfigureChoice(1, "下一张", ShowNextRemovalCard, removalCardChoices.Count > 1);
        ConfigureChoice(2, "返回神秘事件", () => ShowMysteryRoom(selectedMapRoom), true);
    }

    private int CountCardsInDeck(string cardId)
    {
        int count = 0;
        if (gameManager.DeckCardIds == null) return count;
        for (int i = 0; i < gameManager.DeckCardIds.Count; i++)
        {
            if (CardCatalog.GetCanonicalCardId(gameManager.DeckCardIds[i]) == cardId) count++;
        }
        return count;
    }

    private void ShowNextRemovalCard()
    {
        if (removalCardChoices.Count == 0) return;
        removalCardIndex = (removalCardIndex + 1) % removalCardChoices.Count;
        ShowMysteryCardRemovalChoice();
    }

    private void RemoveDisplayedMysteryCard()
    {
        if (selectedMapRoom == null || removalCardChoices.Count == 0) return;

        string message;
        string cardId = removalCardChoices[removalCardIndex];
        if (!gameManager.TryRemoveCardForMystery(selectedMapRoom.roomId, cardId, out message))
        {
            dialogueBody.text = message;
            return;
        }

        removalCardChoices.Clear();
        ResetDialogueChoices();
        dialogueTitle.text = "删牌完成";
        dialogueBody.text = message + "。\n神秘房间已经完成，当前牌组剩余 " +
                            gameManager.DeckCardIds.Count + " 张牌。";
        ConfigureChoice(0, "继续前进", CloseSelectedRoomDialogue, true);
        RefreshInventory();
        RefreshMapNodes();
    }

    private void RestAtSelectedRoom()
    {
        int actualHealing = gameManager.HealPlayerByPercent(0.15f);
        ResetDialogueChoices();
        dialogueTitle.text = "休息完成";
        dialogueBody.text = actualHealing > 0
            ? "你恢复了 " + actualHealing + " 点生命。\n当前生命：" + gameManager.PlayerCurrentHealth + " / " + gameManager.PlayerMaxHealth
            : "你的生命值已经是满值。\n当前生命：" + gameManager.PlayerCurrentHealth + " / " + gameManager.PlayerMaxHealth;
        ConfigureChoice(0, "继续前进", CompleteSelectedRoomAndClose, true);
        RefreshInventory();
    }

    private void OpenSelectedTreasure()
    {
        if (selectedMapRoom == null) return;

        List<FeatherRewardSaveData> rewards = gameManager.OpenTreasureChest(selectedMapRoom.roomId);
        ResetDialogueChoices();
        if (rewards == null || rewards.Count != 3)
        {
            dialogueTitle.text = "宝箱无法打开";
            dialogueBody.text = "宝箱奖励生成失败，请返回地图后重试。";
            ConfigureChoice(0, "返回地图", CloseSelectedRoomDialogue, true);
            return;
        }

        SetDialogueRoomArtwork(RoomContentType.Treasure, true);

        List<string> rewardLines = new List<string>(3);
        for (int i = 0; i < rewards.Count; i++)
        {
            FeatherRewardSaveData reward = rewards[i];
            rewardLines.Add(ElementSystem.GetDisplayName(reward.element) + "羽毛 × " + reward.amount);
        }

        dialogueTitle.text = "宝箱奖励";
        dialogueBody.text = "你获得了3种羽毛：\n" + string.Join("\n", rewardLines.ToArray());
        ConfigureChoice(0, "领取并返回主界面", ReturnToMainMenuAfterTreasure, true);
        RefreshInventory();
        RefreshMapNodes();
    }

    private void ReturnToMainMenuAfterTreasure()
    {
        ShowMainMenuPage();
    }

    private void CraftMengpoSoup()
    {
        if (!gameManager.TryCraftMengpoSoup())
        {
            RefreshInventory();
            return;
        }

        dialogueOverlay.SetActive(true);
        ResetDialogueChoices();
        dialogueIconImage.enabled = false;
        dialogueTitle.text = "合成成功";
        dialogueBody.text = "五种羽毛各消耗1枚。\n你获得了1碗孟婆汤。\n当前拥有：" + gameManager.MengpoSoupCount + "碗";
        ConfigureChoice(0, "确定", CloseSelectedRoomDialogue, true);
        RefreshInventory();
    }

    private void ResetDialogueChoices()
    {
        for (int i = 0; i < choiceButtons.Length; i++)
        {
            choiceButtons[i].gameObject.SetActive(false);
            choiceButtons[i].onClick.RemoveAllListeners();
        }
    }

    private void CompleteSelectedRoomAndClose()
    {
        if (selectedMapRoom != null)
        {
            gameManager.CompleteMapRoom(selectedMapRoom.roomId);
        }
        CloseSelectedRoomDialogue();
    }

    private void CloseSelectedRoomDialogue()
    {
        selectedMapRoom = null;
        dialogueOverlay.SetActive(false);
        mapPage.SetActive(true);
        RefreshInventory();
        RefreshMapNodes();
    }

    private void ConfigureChoice(int index, string label, UnityAction action, bool interactable)
    {
        Button button = choiceButtons[index];
        button.gameObject.SetActive(true);
        button.interactable = interactable;
        choiceLabels[index].text = label;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    private void StartBattle()
    {
        dialogueOverlay.SetActive(false);
        mapPage.SetActive(false);
        battlePage.SetActive(true);
        battleResultOverlay.SetActive(false);

        playerHealth = Mathf.Max(1, gameManager.PlayerCurrentHealth);
        playerBlock = 0;
        battleLayer = selectedMapRoom != null ? selectedMapRoom.layer : 1;
        RoomContentType enemyRoomType = selectedMapRoom != null
            ? selectedMapRoom.contentType
            : RoomContentType.FireSpirit;
        enemyElement = GetElementForRoom(enemyRoomType);
        enemyDisplayName = MistwoodMapGenerator.GetDisplayName(enemyRoomType);
        enemyMaxHealth = UnityEngine.Random.Range(20, 31) + battleLayer * 3;
        enemyHealth = enemyMaxHealth;
        enemyTurnNumber = 1;
        enemyStrength = 0;
        enemyNextAttackReduction = 0;
        enemyAttack = CalculateEnemyAttack();
        battleEnded = false;
        battleTitleText.text = enemyDisplayName + "战斗 · " + ElementSystem.GetDisplayName(enemyElement) + "属性";
        ApplySpriteOrFallback(enemyIconImage, LoadRoomArtwork(enemyRoomType, false), GetRoomColor(enemyRoomType));
        battleLogText.text = "第" + battleLayer + "层精灵　克制：木→土→水→火→金→木";
        CreateStartingDeck();
        BeginPlayerTurn();
        UpdateBattleUI();
    }

    private static ElementType GetElementForRoom(RoomContentType roomType)
    {
        switch (roomType)
        {
            case RoomContentType.GoldSpirit: return ElementType.Gold;
            case RoomContentType.WoodSpirit: return ElementType.Wood;
            case RoomContentType.WaterSpirit: return ElementType.Water;
            case RoomContentType.EarthSpirit: return ElementType.Earth;
            default: return ElementType.Fire;
        }
    }

    private int CalculateEnemyAttack()
    {
        int patternDamage = enemyTurnNumber % 2 == 1
            ? 6 + battleLayer * 2
            : battleLayer * 2;
        return patternDamage + enemyStrength;
    }

    private void CreateStartingDeck()
    {
        drawPile.Clear();
        hand.Clear();
        discardPile.Clear();

        if (gameManager.DeckCardIds != null)
        {
            for (int i = 0; i < gameManager.DeckCardIds.Count; i++)
            {
                CardDefinition card = CardCatalog.Get(gameManager.DeckCardIds[i]);
                if (card != null) drawPile.Add(card);
            }
        }

        ShufflePile(drawPile);
    }

    private static void ShufflePile(List<CardDefinition> pile)
    {
        for (int i = pile.Count - 1; i > 0; i--)
        {
            int randomIndex = UnityEngine.Random.Range(0, i + 1);
            CardDefinition temporary = pile[i];
            pile[i] = pile[randomIndex];
            pile[randomIndex] = temporary;
        }
    }

    private void BeginPlayerTurn()
    {
        playerEnergy = MaxEnergy;
        DrawCards(CardsDrawnPerTurn);
    }

    private int DrawCards(int amount)
    {
        int cardsSentDirectlyToDiscard = 0;
        for (int i = 0; i < amount; i++)
        {
            if (!TryDrawCard(out bool sentDirectlyToDiscard)) break;
            if (sentDirectlyToDiscard) cardsSentDirectlyToDiscard++;
        }
        return cardsSentDirectlyToDiscard;
    }

    private bool TryDrawCard(out bool sentDirectlyToDiscard)
    {
        sentDirectlyToDiscard = false;
        if (drawPile.Count == 0)
        {
            if (discardPile.Count == 0) return false;

            drawPile.AddRange(discardPile);
            discardPile.Clear();
            ShufflePile(drawPile);
        }

        int topCardIndex = drawPile.Count - 1;
        CardDefinition drawnCard = drawPile[topCardIndex];
        drawPile.RemoveAt(topCardIndex);

        if (hand.Count >= MaxHandSize)
        {
            discardPile.Add(drawnCard);
            sentDirectlyToDiscard = true;
        }
        else
        {
            hand.Add(drawnCard);
        }
        return true;
    }

    private void PlayCard(int handIndex)
    {
        if (battleEnded || handIndex < 0 || handIndex >= hand.Count) return;

        CardDefinition card = hand[handIndex];
        if (playerEnergy < card.Cost)
        {
            battleLogText.text = "能量不足，无法打出“" + card.Name + "”";
            UpdateBattleUI();
            return;
        }

        playerEnergy -= card.Cost;
        List<string> resultParts = new List<string>();
        if (card.Damage > 0)
        {
            int actualDamage = ElementSystem.CalculateDamage(card.Damage, card.Element, enemyElement);
            ElementRelationship relationship = ElementSystem.GetRelationship(card.Element, enemyElement);
            enemyHealth = Mathf.Max(0, enemyHealth - actualDamage);
            resultParts.Add(ElementSystem.GetDisplayName(card.Element) + "属性攻击" +
                            ElementSystem.GetRelationshipLabel(relationship) +
                            "，造成" + actualDamage + "点伤害");
        }
        if (card.Block > 0)
        {
            playerBlock += card.Block;
            resultParts.Add("获得" + card.Block + "点格挡");
        }

        switch (card.Effect)
        {
            case CardSpecialEffect.ReduceNextEnemyAttack:
                enemyNextAttackReduction += card.EffectValue;
                resultParts.Add("敌人下次攻击-" + card.EffectValue);
                break;
            case CardSpecialEffect.TrueDamage:
                enemyHealth = Mathf.Max(0, enemyHealth - card.EffectValue);
                resultParts.Add("额外造成" + card.EffectValue + "点真实伤害");
                break;
            case CardSpecialEffect.AllEnemyDamage:
                // The current prototype has one enemy. This remains a distinct
                // effect so it can target every enemy when multi-enemy combat is added.
                enemyHealth = Mathf.Max(0, enemyHealth - card.EffectValue);
                resultParts.Add("对全体敌人额外造成" + card.EffectValue + "点伤害");
                break;
        }

        // Remove the card from the hand while it resolves. It enters the
        // discard pile only after every effect, including card draw, is done.
        hand.RemoveAt(handIndex);

        if (card.Effect == CardSpecialEffect.DrawCards)
        {
            int oldHandCount = hand.Count;
            int overflowDiscardCount = DrawCards(card.EffectValue);
            int cardsActuallyDrawn = hand.Count - oldHandCount + overflowDiscardCount;
            resultParts.Add("抽了" + cardsActuallyDrawn + "张牌");
            if (overflowDiscardCount > 0)
            {
                resultParts.Add(overflowDiscardCount + "张牌因手牌已满直接进入弃牌堆");
            }
        }
        discardPile.Add(card);

        battleLogText.text = card.Name + "：" + string.Join("；", resultParts.ToArray());

        if (enemyHealth <= 0)
        {
            WinBattle();
            return;
        }

        UpdateBattleUI();
    }

    private void EndPlayerTurn()
    {
        if (battleEnded) return;

        DiscardEntireHand();

        int effectiveEnemyAttack = GetEffectiveEnemyAttack();
        int damageTaken = Mathf.Max(0, effectiveEnemyAttack - playerBlock);
        playerHealth = Mathf.Max(0, playerHealth - damageTaken);
        playerBlock = 0;
        int appliedAttackReduction = enemyNextAttackReduction;
        enemyNextAttackReduction = 0;

        bool strengtheningTurn = enemyTurnNumber % 2 == 0;
        if (strengtheningTurn)
        {
            enemyStrength += 2;
        }
        battleLogText.text = enemyDisplayName + "造成 " + damageTaken + " 点伤害" +
                             (appliedAttackReduction > 0 ? "（攻击被削弱" + appliedAttackReduction + "）" : string.Empty) +
                             (strengtheningTurn ? "，并获得了2点永久攻击强化" : string.Empty);

        enemyTurnNumber++;
        enemyAttack = CalculateEnemyAttack();

        if (playerHealth <= 0)
        {
            LoseBattle();
            return;
        }

        BeginPlayerTurn();
        UpdateBattleUI();
    }

    private void DiscardEntireHand()
    {
        discardPile.AddRange(hand);
        hand.Clear();
    }

    private void UpdateBattleUI()
    {
        int effectiveEnemyAttack = GetEffectiveEnemyAttack();
        enemyHealthText.text = enemyDisplayName + "生命：" + enemyHealth + " / " + enemyMaxHealth +
                               "　强化：+" + enemyStrength;
        enemyIntentText.text = "第" + enemyTurnNumber + "回合意图：攻击 " + effectiveEnemyAttack +
                               (enemyNextAttackReduction > 0 ? "（已削弱-" + enemyNextAttackReduction + "）" : string.Empty) +
                               (enemyTurnNumber % 2 == 0 ? "，然后强化攻击+2" : string.Empty);
        playerStatusText.text = "你的生命：" + playerHealth + " / " + gameManager.PlayerMaxHealth +
                                "　格挡：" + playerBlock + "　能量：" + playerEnergy + " / " + MaxEnergy;
        pileStatusText.text = "抽牌堆：" + drawPile.Count + "　弃牌堆：" + discardPile.Count + "　手牌：" + hand.Count;

        UpdateHandUI();
        endTurnButton.interactable = !battleEnded;
    }

    private void UpdateHandUI()
    {
        for (int i = 0; i < handButtons.Length; i++)
        {
            bool hasCard = i < hand.Count;
            handButtons[i].gameObject.SetActive(hasCard);
            if (!hasCard) continue;

            CardDefinition card = hand[i];
            handLabels[i].text = GetBattleCardLabel(card);
            handButtons[i].GetComponent<Image>().color = card.DisplayColor;
            handButtons[i].interactable = !battleEnded;
        }
    }

    private string GetBattleCardLabel(CardDefinition card)
    {
        List<string> effectParts = new List<string>();
        if (card.Damage > 0)
        {
            int actualDamage = ElementSystem.CalculateDamage(card.Damage, card.Element, enemyElement);
            string relationship = ElementSystem.GetRelationshipLabel(
                ElementSystem.GetRelationship(card.Element, enemyElement));
            effectParts.Add(ElementSystem.GetDisplayName(card.Element) + "属性" + relationship +
                            "，伤害" + actualDamage);
        }
        if (card.Block > 0)
        {
            effectParts.Add("格挡" + card.Block);
        }
        switch (card.Effect)
        {
            case CardSpecialEffect.DrawCards:
                effectParts.Add("抽" + card.EffectValue + "张牌");
                break;
            case CardSpecialEffect.ReduceNextEnemyAttack:
                effectParts.Add("敌人下次攻击-" + card.EffectValue);
                break;
            case CardSpecialEffect.TrueDamage:
                effectParts.Add("真实伤害" + card.EffectValue);
                break;
            case CardSpecialEffect.AllEnemyDamage:
                effectParts.Add("全体伤害" + card.EffectValue);
                break;
        }

        return card.Name + "［" + CardCatalog.GetQualityDisplayName(card.Quality) + "］\n" +
               string.Join("\n", effectParts.ToArray()) + "\n消耗" + card.Cost + "点能量";
    }

    private int GetEffectiveEnemyAttack()
    {
        return Mathf.Max(0, enemyAttack - enemyNextAttackReduction);
    }

    private void WinBattle()
    {
        if (battleEnded) return;
        battleEnded = true;
        gameManager.SetPlayerHealth(playerHealth);
        List<string> rewardChoices = CardCatalog.GenerateRewardChoices();
        bool rewardCreated = selectedMapRoom != null &&
                             gameManager.ResolveSpiritBattleVictory(
                                 selectedMapRoom.roomId,
                                 enemyElement,
                                 rewardChoices);
        if (!rewardCreated)
        {
            Debug.LogError("Failed to create the spirit battle reward.");
        }
        UpdateBattleUI();
        string featherName = ElementSystem.GetDisplayName(enemyElement) + "羽毛";
        ShowBattleResult(
            "胜利",
            enemyDisplayName + "被击败。\n你获得了1枚" + featherName + "，还可以选择1张卡牌。",
            "选择卡牌奖励",
            OpenPendingCardRewardAfterBattle);
    }

    private void LoseBattle()
    {
        if (battleEnded) return;
        battleEnded = true;
        // Battle damage is not persisted on defeat. Restore the health stored
        // before this encounter, keep the room incomplete, and return to map.
        playerHealth = gameManager.PlayerCurrentHealth;
        gameManager.SaveGame();
        UpdateBattleUI();
        ShowBattleResult("战斗失败", "你的战斗失败了。", "返回迷雾森林", ReloadMistwoodMapAfterDefeat);
    }

    private void ShowBattleResult(string title, string body, string buttonLabel, UnityAction action)
    {
        battleResultTitle.text = title;
        battleResultBody.text = body;
        battleResultButtonLabel.text = buttonLabel;
        battleResultButton.onClick.RemoveAllListeners();
        battleResultButton.onClick.AddListener(action);
        battleResultOverlay.SetActive(true);
    }

    private void ReloadMistwoodMapAfterDefeat()
    {
        StopVisualNovelTyping();
        gameManager.ResetMapAfterDefeat();
        battleResultOverlay.SetActive(false);
        battlePage.SetActive(false);
        dialogueOverlay.SetActive(false);
        visualNovelPage.SetActive(false);
        cardRewardOverlay.SetActive(false);
        openingPage.SetActive(false);
        mainMenuPage.SetActive(false);
        mapPage.SetActive(true);
        selectedMapRoom = null;
        RefreshInventory();
        RefreshMapNodes();
    }

    private void RefreshInventory()
    {
        if (inventoryText != null)
        {
            inventoryText.text = "生命：" + gameManager.PlayerCurrentHealth + " / " + gameManager.PlayerMaxHealth +
                                 "　水果：" + gameManager.FruitCount + "　孟婆汤：" + gameManager.MengpoSoupCount +
                                 "　牌组：" + gameManager.DeckCardIds.Count +
                                 "\n羽毛　金" + gameManager.GoldFeatherCount + "　木" + gameManager.WoodFeatherCount +
                                 "　水" + gameManager.WaterFeatherCount + "　火" + gameManager.FireFeatherCount +
                                 "　土" + gameManager.EarthFeatherCount;
        }
        if (craftSoupButton != null)
        {
            craftSoupButton.interactable = gameManager.CanCraftMengpoSoup;
        }
    }

    [ContextMenu("Reset Fire Spirit Prototype Save")]
    private void ResetPrototypeSave()
    {
        gameManager.ResetSave();
        RefreshInventory();
    }

    private void AddRoomButtonArtwork(Button button, MapRoomSaveData room)
    {
        Sprite artwork = LoadRoomArtwork(room.contentType, room.completed);
        if (artwork == null) return;

        GameObject iconObject = CreateImage("Room Icon", button.transform, Color.white);
        iconObject.transform.SetAsFirstSibling();
        SetAnchored(
            iconObject.GetComponent<RectTransform>(),
            new Vector2(0.5f, 0.68f),
            Vector2.zero,
            new Vector2(66f, 66f));
        Image icon = iconObject.GetComponent<Image>();
        icon.sprite = artwork;
        icon.preserveAspect = true;
        icon.raycastTarget = false;

        Text label = button.GetComponentInChildren<Text>();
        label.fontSize = 17;
        RectTransform labelRect = label.rectTransform;
        labelRect.offsetMin = new Vector2(6f, 3f);
        labelRect.offsetMax = new Vector2(-6f, -72f);
    }

    private void SetDialogueRoomArtwork(RoomContentType contentType, bool treasureOpened)
    {
        Sprite artwork = LoadRoomArtwork(contentType, treasureOpened);
        dialogueIconImage.sprite = artwork;
        dialogueIconImage.color = Color.white;
        dialogueIconImage.preserveAspect = true;
        dialogueIconImage.enabled = artwork != null;
    }

    private static Sprite LoadRoomArtwork(RoomContentType contentType, bool treasureOpened)
    {
        string resourcePath;
        switch (contentType)
        {
            case RoomContentType.FireSpirit: resourcePath = "Art/Spirits/火精灵"; break;
            case RoomContentType.GoldSpirit: resourcePath = "Art/Spirits/金精灵"; break;
            case RoomContentType.WoodSpirit: resourcePath = "Art/Spirits/木精灵"; break;
            case RoomContentType.WaterSpirit: resourcePath = "Art/Spirits/水精灵"; break;
            case RoomContentType.EarthSpirit: resourcePath = "Art/Spirits/土精灵"; break;
            case RoomContentType.Treasure:
                resourcePath = treasureOpened
                    ? "Art/Treasure/宝箱打开"
                    : "Art/Treasure/宝箱关闭";
                break;
            default: return null;
        }

        return Resources.Load<Sprite>(resourcePath);
    }

    private static void ApplySpriteOrFallback(Image image, Sprite sprite, Color fallbackColor)
    {
        image.sprite = sprite;
        image.color = sprite != null ? Color.white : fallbackColor;
        image.preserveAspect = sprite != null;
        image.enabled = true;
    }

    private static GameObject CreatePage(string name, Transform parent, Color color)
    {
        GameObject page = CreateImage(name, parent, color);
        RectTransform rect = page.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return page;
    }

    private static GameObject CreateImage(string name, Transform parent, Color color)
    {
        GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        return imageObject;
    }

    private Button CreateButton(Transform parent, string name, string label, Color color)
    {
        GameObject buttonObject = new GameObject(
            name,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        Image image = buttonObject.GetComponent<Image>();
        image.color = color;

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
        colors.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
        colors.disabledColor = new Color(0.35f, 0.35f, 0.35f, 0.65f);
        button.colors = colors;

        Text labelText = CreateText(buttonObject.transform, label, 28, TextAnchor.MiddleCenter, TextColor);
        RectTransform labelRect = labelText.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(12f, 8f);
        labelRect.offsetMax = new Vector2(-12f, -8f);
        return button;
    }

    private Text CreateText(Transform parent, string value, int fontSize, TextAnchor alignment, Color color)
    {
        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textObject.transform.SetParent(parent, false);
        Text text = textObject.GetComponent<Text>();
        text.font = uiFont;
        text.text = value;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        return text;
    }

    private static void SetAnchored(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }
}
