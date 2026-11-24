
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class StylistGameManager : MonoBehaviour
{
    [Header("캐릭터 이미지")]
    public Sprite mannequinSprite;

    private Image mannequinImage;

    // -----------------------------
    // 데이터 설정
    // -----------------------------

    public enum Category
    {
        Hair,
        Dress,
        Accessory
    }

    public enum Style
    {
        Lovely,
        Sexy,
        Cool
    }

    [System.Serializable]
    public class FashionItem
    {
        public string itemName;
        public Category category;

        public int lovely;
        public int sexy;
        public int cool;

        public FashionItem(string name, Category type,
            int l, int s, int c)
        {
            itemName = name;
            category = type;
            lovely = l;
            sexy = s;
            cool = c;
        }

        public int GetScore(Style style)
        {
            switch (style)
            {
                case Style.Lovely: return lovely;
                case Style.Sexy: return sexy;
                case Style.Cool: return cool;
                default: return 0;
            }
        }
    }

    [System.Serializable]
    public class StageData
    {
        public string stageName;
        public string weddingTheme;
        public Style targetStyle;
        public int targetScore;

        public StageData(string name, string theme,
            Style style, int score)
        {
            stageName = name;
            weddingTheme = theme;
            targetStyle = style;
            targetScore = score;
        }
    }

    // -----------------------------
    // 게임 데이터
    // -----------------------------

    [Header("아이템 데이터")]
    public List<FashionItem> fashionItems =
        new List<FashionItem>();

    private List<StageData> stages =
        new List<StageData>();

    private Dictionary<Category, FashionItem> equippedItems =
        new Dictionary<Category, FashionItem>();

    private int currentStage = 0;
    private Category currentCategory = Category.Hair;

    // -----------------------------
    // UI 변수
    // -----------------------------

    private Canvas canvas;

    private TextMeshProUGUI stageText;
    private TextMeshProUGUI themeText;
    private TextMeshProUGUI characterText;
    private TextMeshProUGUI equippedText;

    private TextMeshProUGUI categoryTitle;
    private TextMeshProUGUI instructionText;

    private Transform itemListParent;

    private GameObject resultPanel;
    private TextMeshProUGUI resultText;
    private TextMeshProUGUI scoreText;
    private Button nextButton;

    private TMP_FontAsset defaultFont;

    // -----------------------------
    // 시작
    // -----------------------------

    void Start()
    {
        InitializeStages();
        InitializeItems();
        InitializeEquippedItems();

        CreateCanvas();
        CreateGameUI();

        ShowStage();
        ShowItems();
        UpdateCharacter();
    }

    // -----------------------------
    // 스테이지 설정
    // -----------------------------

    void InitializeStages()
    {
        stages.Clear();

        stages.Add(new StageData(
            "STAGE 1",
            "사랑스러운 웨딩",
            Style.Lovely,
            30
        ));

        stages.Add(new StageData(
            "STAGE 2",
            "화려하고 매혹적인 웨딩",
            Style.Sexy,
            30
        ));

        stages.Add(new StageData(
            "STAGE 3",
            "세련되고 차가운 웨딩",
            Style.Cool,
            30
        ));
    }

    // -----------------------------
    // 기본 아이템 설정
    // -----------------------------

    void InitializeItems()
    {
        if (fashionItems.Count > 0)
            return;

        // 헤어
        fashionItems.Add(new FashionItem(
            "리본 헤어", Category.Hair, 10, 0, 0));

        fashionItems.Add(new FashionItem(
            "웨이브 헤어", Category.Hair, 0, 10, 0));

        fashionItems.Add(new FashionItem(
            "실버 숏컷", Category.Hair, 0, 0, 10));

        // 드레스
        fashionItems.Add(new FashionItem(
            "진주 드레스", Category.Dress, 20, 0, 0));

        fashionItems.Add(new FashionItem(
            "레드 드레스", Category.Dress, 0, 20, 0));

        fashionItems.Add(new FashionItem(
            "블랙 드레스", Category.Dress, 0, 10, 20));

        // 액세서리
        fashionItems.Add(new FashionItem(
            "하트 목걸이", Category.Accessory, 10, 0, 0));

        fashionItems.Add(new FashionItem(
            "다이아 귀걸이", Category.Accessory, 0, 10, 0));

        fashionItems.Add(new FashionItem(
            "실버 왕관", Category.Accessory, 0, 0, 10));
    }

    void InitializeEquippedItems()
    {
        equippedItems.Clear();

        equippedItems.Add(Category.Hair, null);
        equippedItems.Add(Category.Dress, null);
        equippedItems.Add(Category.Accessory, null);
    }

    // -----------------------------
    // Canvas 생성
    // -----------------------------

    void CreateCanvas()
    {
        GameObject canvasObject =
            new GameObject("StylistCanvas");

        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler =
            canvasObject.AddComponent<CanvasScaler>();

        scaler.uiScaleMode =
            CanvasScaler.ScaleMode.ScaleWithScreenSize;

        scaler.referenceResolution =
            new Vector2(1920, 1080);

        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();

        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject eventObject =
                new GameObject("EventSystem");

            eventObject.AddComponent<EventSystem>();
            eventObject.AddComponent<StandaloneInputModule>();
        }

        defaultFont = Resources.Load<TMP_FontAsset>(
            "Fonts & Materials/LiberationSans SDF");
    }

    // -----------------------------
    // 기본 UI 생성 도구
    // -----------------------------

    GameObject CreatePanel(
        string name,
        Transform parent,
        Vector2 min,
        Vector2 max,
        Color color)
    {
        GameObject obj = new GameObject(name);

        obj.transform.SetParent(parent, false);

        RectTransform rect =
            obj.AddComponent<RectTransform>();

        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = obj.AddComponent<Image>();
        image.color = color;

        return obj;
    }

    TextMeshProUGUI CreateText(
        string name,
        Transform parent,
        string content,
        Vector2 min,
        Vector2 max,
        int fontSize,
        Color color)
    {
        GameObject obj = new GameObject(name);

        obj.transform.SetParent(parent, false);

        RectTransform rect =
            obj.AddComponent<RectTransform>();

        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        TextMeshProUGUI text =
            obj.AddComponent<TextMeshProUGUI>();

        if (defaultFont != null)
            text.font = defaultFont;

        text.text = content;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = true;
        text.fontSizeMin = 14;
        text.fontSizeMax = fontSize;

        return text;
    }

    Button CreateButton(
        string name,
        Transform parent,
        string label,
        Vector2 min,
        Vector2 max,
        Color color,
        UnityEngine.Events.UnityAction action)
    {
        GameObject obj = CreatePanel(
            name, parent, min, max, color);

        Button button = obj.AddComponent<Button>();

        button.targetGraphic = obj.GetComponent<Image>();
        button.onClick.AddListener(action);

        CreateText(
            "ButtonText",
            obj.transform,
            label,
            Vector2.zero,
            Vector2.one,
            24,
            Color.white
        );

        return button;
    }

    // -----------------------------
    // 게임 화면 생성
    // -----------------------------

    void CreateGameUI()
    {
        // 배경
        GameObject background = CreatePanel(
            "Background",
            canvas.transform,
            Vector2.zero,
            Vector2.one,
            new Color(0.96f, 0.91f, 0.92f)
        );

        // 상단 정보
        GameObject header = CreatePanel(
            "Header",
            background.transform,
            new Vector2(0.03f, 0.84f),
            new Vector2(0.97f, 0.97f),
            new Color(0.75f, 0.43f, 0.55f)
        );

        stageText = CreateText(
            "StageText", header.transform,
            "STAGE 1",
            new Vector2(0.02f, 0.1f),
            new Vector2(0.28f, 0.9f),
            32, Color.white
        );

        themeText = CreateText(
            "ThemeText", header.transform,
            "사랑스러운 웨딩",
            new Vector2(0.30f, 0.1f),
            new Vector2(0.98f, 0.9f),
            36, Color.white
        );

        // 왼쪽 캐릭터 영역
        GameObject characterPanel = CreatePanel(
            "CharacterPanel",
            background.transform,
            new Vector2(0.03f, 0.17f),
            new Vector2(0.58f, 0.81f),
            Color.white
        );

        CreateText(
            "CharacterTitle",
            characterPanel.transform,
            "MY WEDDING STYLE",
            new Vector2(0.05f, 0.87f),
            new Vector2(0.95f, 0.98f),
            26,
            new Color(0.65f, 0.35f, 0.45f)
        );


        // 마네킹 이미지 생성
        GameObject mannequinObject = new GameObject("Mannequin");

        mannequinObject.transform.SetParent(
            characterPanel.transform, false
        );

        RectTransform mannequinRect =
            mannequinObject.AddComponent<RectTransform>();

        mannequinRect.anchorMin = new Vector2(0.25f, 0.25f);
        mannequinRect.anchorMax = new Vector2(0.75f, 0.82f);

        mannequinRect.offsetMin = Vector2.zero;
        mannequinRect.offsetMax = Vector2.zero;

        mannequinImage =
            mannequinObject.AddComponent<Image>();

        mannequinImage.sprite = mannequinSprite;
        mannequinImage.preserveAspect = true;
        mannequinImage.color = Color.white;




        equippedText = CreateText(
            "EquippedItems",
            characterPanel.transform,
            "",
            new Vector2(0.05f, 0.04f),
            new Vector2(0.95f, 0.34f),
            22,
            new Color(0.45f, 0.3f, 0.35f)
        );

        // 오른쪽 아이템 영역
        GameObject itemPanel = CreatePanel(
            "ItemPanel",
            background.transform,
            new Vector2(0.61f, 0.17f),
            new Vector2(0.97f, 0.81f),
            Color.white
        );

        categoryTitle = CreateText(
            "CategoryTitle",
            itemPanel.transform,
            "HAIR",
            new Vector2(0.05f, 0.85f),
            new Vector2(0.95f, 0.98f),
            28,
            new Color(0.65f, 0.35f, 0.45f)
        );

        // 카테고리 버튼
        CreateButton(
            "HairButton", itemPanel.transform,
            "헤어",
            new Vector2(0.03f, 0.72f),
            new Vector2(0.32f, 0.84f),
            new Color(0.85f, 0.55f, 0.65f),
            () => ChangeCategory(Category.Hair)
        );

        CreateButton(
            "DressButton", itemPanel.transform,
            "드레스",
            new Vector2(0.35f, 0.72f),
            new Vector2(0.65f, 0.84f),
            new Color(0.85f, 0.55f, 0.65f),
            () => ChangeCategory(Category.Dress)
        );

        CreateButton(
            "AccessoryButton", itemPanel.transform,
            "액세서리",
            new Vector2(0.68f, 0.72f),
            new Vector2(0.97f, 0.84f),
            new Color(0.85f, 0.55f, 0.65f),
            () => ChangeCategory(Category.Accessory)
        );

        // 아이템 목록 영역
        GameObject itemList = CreatePanel(
            "ItemList",
            itemPanel.transform,
            new Vector2(0.05f, 0.04f),
            new Vector2(0.95f, 0.69f),
            new Color(0.98f, 0.96f, 0.97f)
        );

        itemListParent = itemList.transform;

        // 하단 조작 영역
        GameObject bottom = CreatePanel(
            "BottomPanel",
            background.transform,
            new Vector2(0.03f, 0.03f),
            new Vector2(0.97f, 0.13f),
            new Color(0.88f, 0.78f, 0.81f)
        );

        instructionText = CreateText(
            "Instruction",
            bottom.transform,
            "카테고리를 선택하고 원하는 아이템을 착용해 보세요!",
            new Vector2(0.02f, 0.1f),
            new Vector2(0.65f, 0.9f),
            22,
            Color.black
        );

        CreateButton(
            "CompleteButton",
            bottom.transform,
            "코디 완료!",
            new Vector2(0.72f, 0.08f),
            new Vector2(0.98f, 0.92f),
            new Color(0.75f, 0.35f, 0.5f),
            CompleteCoordination
        );

        // 결과 화면
        CreateResultUI(background.transform);
    }

    // -----------------------------
    // 결과 화면 생성
    // -----------------------------

    void CreateResultUI(Transform parent)
    {
        resultPanel = CreatePanel(
            "ResultPanel",
            parent,
            Vector2.zero,
            Vector2.one,
            new Color(0, 0, 0, 0.75f)
        );

        resultText = CreateText(
            "ResultText",
            resultPanel.transform,
            "",
            new Vector2(0.1f, 0.58f),
            new Vector2(0.9f, 0.85f),
            64,
            Color.white
        );

        scoreText = CreateText(
            "ScoreText",
            resultPanel.transform,
            "",
            new Vector2(0.1f, 0.40f),
            new Vector2(0.9f, 0.60f),
            34,
            Color.white
        );

        nextButton = CreateButton(
            "NextButton",
            resultPanel.transform,
            "다음 스테이지",
            new Vector2(0.35f, 0.19f),
            new Vector2(0.65f, 0.31f),
            new Color(0.85f, 0.45f, 0.6f),
            NextStage
        );

        CreateButton(
            "RetryButton",
            resultPanel.transform,
            "다시 도전",
            new Vector2(0.35f, 0.05f),
            new Vector2(0.65f, 0.17f),
            new Color(0.45f, 0.45f, 0.55f),
            RetryStage
        );

        resultPanel.SetActive(false);
    }

    // -----------------------------
    // 스테이지 표시
    // -----------------------------

    void ShowStage()
    {
        StageData stage = stages[currentStage];

        stageText.text =
            stage.stageName +
            "   |   목표 점수: " +
            stage.targetScore;

        themeText.text =
            "WEDDING THEME : " +
            stage.weddingTheme;

        instructionText.text =
            "목표 스타일 : " + GetStyleName(stage.targetStyle);
    }

    string GetStyleName(Style style)
    {
        switch (style)
        {
            case Style.Lovely: return "러블리";
            case Style.Sexy: return "섹시";
            case Style.Cool: return "쿨";
            default: return "";
        }
    }

    // -----------------------------
    // 카테고리 변경
    // -----------------------------

    void ChangeCategory(Category category)
    {
        currentCategory = category;
        ShowItems();
    }

    // -----------------------------
    // 아이템 목록 표시
    // -----------------------------

    void ShowItems()
    {
        foreach (Transform child in itemListParent)
        {
            Destroy(child.gameObject);
        }

        categoryTitle.text = GetCategoryName(currentCategory);

        List<FashionItem> categoryItems =
            fashionItems.FindAll(
                item => item.category == currentCategory
            );

        for (int i = 0; i < categoryItems.Count; i++)
        {
            FashionItem item = categoryItems[i];

            int index = i;

            float top = 0.96f - (i * 0.24f);
            float bottom = top - 0.20f;

            bool isEquipped =
                equippedItems[currentCategory] == item;

            string label = item.itemName;

            if (isEquipped)
                label += "  [착용 중]";

            CreateButton(
                "Item_" + i,
                itemListParent,
                label,
                new Vector2(0.04f, bottom),
                new Vector2(0.96f, top),
                isEquipped
                    ? new Color(0.75f, 0.55f, 0.65f)
                    : new Color(0.85f, 0.78f, 0.8f),
                () => SelectItem(categoryItems[index])
            );
        }
    }

    string GetCategoryName(Category category)
    {
        switch (category)
        {
            case Category.Hair: return "HAIR STYLE";
            case Category.Dress: return "DRESS STYLE";
            case Category.Accessory: return "ACCESSORY";
            default: return "";
        }
    }

    // -----------------------------
    // 아이템 선택 및 착용
    // -----------------------------

    void SelectItem(FashionItem item)
    {
        equippedItems[item.category] = item;

        UpdateCharacter();
        ShowItems();
    }

    // -----------------------------
    // 캐릭터 화면 갱신
    // -----------------------------


    void UpdateCharacter()
    {

        string GetItemName(FashionItem item)
        {
            return item == null ? "선택 안 함" : item.itemName;
        }

        FashionItem hair =
            equippedItems[Category.Hair];

        FashionItem dress =
            equippedItems[Category.Dress];

        FashionItem accessory =
            equippedItems[Category.Accessory];

        // 현재 착용 아이템 표시
        equippedText.text =           
            "헤어 : " + GetItemName(hair) + "\n" +
            "드레스 : " + GetItemName(dress) + "\n" +
            "액세서리 : " + GetItemName(accessory);
    }


    // -----------------------------
    // 코디 점수 계산
    // -----------------------------

    void CompleteCoordination()
    {
        StageData stage = stages[currentStage];

        int totalScore = 0;

        foreach (FashionItem item in equippedItems.Values)
        {
            if (item != null)
            {
                totalScore +=
                    item.GetScore(stage.targetStyle);
            }
        }

        bool isClear =
            totalScore >= stage.targetScore;

        ShowResult(totalScore, isClear);
    }

    // -----------------------------
    // 결과 표시
    // -----------------------------

    void ShowResult(int score, bool isClear)
    {
        resultPanel.SetActive(true);

        if (isClear)
        {
            resultText.text = "CLEAR!";

            if (currentStage == stages.Count - 1)
            {
                scoreText.text =
                    "획득 점수 : " + score +
                    "\n\n모든 웨딩 코디를 완성했어요!";

                nextButton.GetComponentInChildren<
                    TextMeshProUGUI>().text = "게임 완료";
            }
            else
            {
                scoreText.text =
                    "획득 점수 : " + score +
                    "\n목표 점수 : " +
                    stages[currentStage].targetScore;

                nextButton.GetComponentInChildren<
                    TextMeshProUGUI>().text = "다음 스테이지";
            }

            nextButton.gameObject.SetActive(true);
        }
        else
        {
            resultText.text = "GAME OVER";

            scoreText.text =
                "획득 점수 : " + score +
                "\n목표 점수 : " +
                stages[currentStage].targetScore +
                "\n\n다시 도전해 보세요!";

            nextButton.gameObject.SetActive(false);
        }
    }

    // -----------------------------
    // 다음 스테이지
    // -----------------------------

    void NextStage()
    {
        if (currentStage == stages.Count - 1)
        {
            resultText.text = "ALL CLEAR!";
            scoreText.text =
                "축하합니다!\n모든 웨딩 코디를 완성했습니다.";

            nextButton.gameObject.SetActive(false);

            return;
        }

        currentStage++;

        ResetOutfits();

        resultPanel.SetActive(false);

        ShowStage();
        ShowItems();
        UpdateCharacter();
    }

    // -----------------------------
    // 다시 도전
    // -----------------------------

    void RetryStage()
    {
        ResetOutfits();

        resultPanel.SetActive(false);

        ShowItems();
        UpdateCharacter();
    }

    // -----------------------------
    // 아이템 초기화
    // -----------------------------

    void ResetOutfits()
    {
        foreach (Category category in
            System.Enum.GetValues(typeof(Category)))
        {
            equippedItems[category] = null;
        }
    }
}
