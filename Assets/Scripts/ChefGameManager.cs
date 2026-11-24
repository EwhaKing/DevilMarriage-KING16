
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ChefGameManager : MonoBehaviour
{
    // ==========================================
    // 1. 재료 종류 및 게임 상태
    // ==========================================

    public enum Ingredient
    {
        BottomBun,
        Patty,
        Cheese,
        Lettuce,
        Tomato,
        TopBun
    }

    public enum GameState
    {
        InitialPreview,
        Playing,
        RecipeReview,
        Feedback,
        Clear,
        GameOver
    }

    // ==========================================
    // 2. Inspector에서 연결할 UI
    // ==========================================

    [Header("상단 상태 UI")]
    public TMP_Text lifeText;
    public TMP_Text orderText;
    public TMP_Text timerText;

    [Header("안내 UI")]
    public TMP_Text guideText;
    public TMP_Text feedbackText;

    [Header("레시피북 UI")]
    public Button recipeBookButton;
    public GameObject recipePopup;
    public TMP_Text recipeText;
    public TMP_Text recipeCountdownText;

    [Header("햄버거 제작 영역")]
    public RectTransform burgerStack;
    public GameObject ingredientPrefab;

    [Header("재료 버튼")]
    public Button bottomBunButton;
    public Button pattyButton;
    public Button cheeseButton;
    public Button lettuceButton;
    public Button tomatoButton;
    public Button topBunButton;

    [Header("초기화 버튼")]
    public Button resetButton;

    [Header("게임 종료 UI")]
    public GameObject endPanel;
    public TMP_Text resultText;
    public Button retryButton;

    // ==========================================
    // 3. 게임 설정값
    // ==========================================

    [Header("게임 설정")]
    [Min(1)] public int maxLives = 3;
    [Min(0.1f)] public float recipeViewTime = 3f;
    [Min(0.1f)] public float cookingTime = 12f;
    [Min(0f)] public float feedbackTime = 1f;

    [Header("햄버거 도형 설정 - QHD")]
    public float ingredientWidth = 550f;
    public float maxBurgerHeight = 680f;

    // ==========================================
    // 4. 게임 진행 데이터
    // ==========================================

    private GameState currentState;

    private int remainingLives;
    private int currentOrder;

    private float remainingTime;
    private float recipeRemainingTime;
    private float feedbackRemainingTime;

    private bool lastAnswerCorrect;

    private Button[] ingredientButtons;

    private List<Ingredient> playerIngredients
        = new List<Ingredient>();

    private List<GameObject> burgerLayers
        = new List<GameObject>();

    // ==========================================
    // 5. 주문 레시피 데이터
    // ==========================================

    private Ingredient[][] recipes =
    {
        // 주문 1
        new Ingredient[]
        {
            Ingredient.BottomBun,
            Ingredient.Patty,
            Ingredient.TopBun
        },

        // 주문 2
        new Ingredient[]
        {
            Ingredient.BottomBun,
            Ingredient.Patty,
            Ingredient.Cheese,
            Ingredient.TopBun
        },

        // 주문 3
        new Ingredient[]
        {
            Ingredient.BottomBun,
            Ingredient.Lettuce,
            Ingredient.Patty,
            Ingredient.TopBun
        },

        // 주문 4
        new Ingredient[]
        {
            Ingredient.BottomBun,
            Ingredient.Patty,
            Ingredient.Cheese,
            Ingredient.Tomato,
            Ingredient.TopBun
        },

        // 주문 5
        new Ingredient[]
        {
            Ingredient.BottomBun,
            Ingredient.Lettuce,
            Ingredient.Patty,
            Ingredient.Tomato,
            Ingredient.TopBun
        },

        // 주문 6
        new Ingredient[]
        {
            Ingredient.BottomBun,
            Ingredient.Patty,
            Ingredient.Cheese,
            Ingredient.Patty,
            Ingredient.TopBun
        },

        // 주문 7
        new Ingredient[]
        {
            Ingredient.BottomBun,
            Ingredient.Tomato,
            Ingredient.Lettuce,
            Ingredient.Patty,
            Ingredient.Cheese,
            Ingredient.TopBun
        },

        // 주문 8
        new Ingredient[]
        {
            Ingredient.BottomBun,
            Ingredient.Patty,
            Ingredient.Cheese,
            Ingredient.Lettuce,
            Ingredient.Tomato,
            Ingredient.TopBun
        },

        // 주문 9
        new Ingredient[]
        {
            Ingredient.BottomBun,
            Ingredient.Lettuce,
            Ingredient.Patty,
            Ingredient.Cheese,
            Ingredient.Patty,
            Ingredient.Tomato,
            Ingredient.TopBun
        },

        // 주문 10
        new Ingredient[]
        {
            Ingredient.BottomBun,
            Ingredient.Patty,
            Ingredient.Cheese,
            Ingredient.Tomato,
            Ingredient.Lettuce,
            Ingredient.Patty,
            Ingredient.Cheese,
            Ingredient.TopBun
        }
    };

    // ==========================================
    // 6. 게임 최초 실행
    // ==========================================

    private void Start()
    {
        ingredientButtons = new Button[]
        {
            bottomBunButton,
            pattyButton,
            cheeseButton,
            lettuceButton,
            tomatoButton,
            topBunButton
        };

        // 각 버튼에 함수 연결
        bottomBunButton.onClick.AddListener(
            () => SelectIngredient(Ingredient.BottomBun));

        pattyButton.onClick.AddListener(
            () => SelectIngredient(Ingredient.Patty));

        cheeseButton.onClick.AddListener(
            () => SelectIngredient(Ingredient.Cheese));

        lettuceButton.onClick.AddListener(
            () => SelectIngredient(Ingredient.Lettuce));

        tomatoButton.onClick.AddListener(
            () => SelectIngredient(Ingredient.Tomato));

        topBunButton.onClick.AddListener(
            () => SelectIngredient(Ingredient.TopBun));

        recipeBookButton.onClick.AddListener(OpenRecipeBook);

        resetButton.onClick.AddListener(ResetBurger);

        retryButton.onClick.AddListener(StartGame);

        StartGame();
    }

    // ==========================================
    // 7. 게임 시작 및 재시작
    // ==========================================

    public void StartGame()
    {
        remainingLives = maxLives;
        currentOrder = 0;

        lastAnswerCorrect = false;

        endPanel.SetActive(false);
        recipePopup.SetActive(false);

        feedbackText.text = "";

        ClearBurger();

        UpdateLifeUI();
        UpdateOrderUI();

        StartOrder();
    }

    // ==========================================
    // 8. 새로운 주문 시작
    // ==========================================

    private void StartOrder()
    {
        ClearBurger();

        remainingTime = cookingTime;

        feedbackText.text = "";

        UpdateLifeUI();
        UpdateOrderUI();
        UpdateTimerUI();

        ShowRecipe(true);
    }

    // ==========================================
    // 9. 매 프레임 실행되는 타이머
    // ==========================================

    private void Update()
    {
        float deltaTime = Time.deltaTime;

        switch (currentState)
        {
            case GameState.InitialPreview:

                UpdateRecipeTimer(deltaTime);
                break;

            case GameState.Playing:

                UpdateCookingTimer(deltaTime);
                break;

            case GameState.RecipeReview:

                // 레시피 재확인 중에도
                // 제작 시간은 계속 감소한다.
                UpdateCookingTimer(deltaTime);

                // 시간 초과로 상태가 바뀌었는지 확인
                if (currentState == GameState.RecipeReview)
                {
                    UpdateRecipeTimer(deltaTime);
                }

                break;

            case GameState.Feedback:

                feedbackRemainingTime -= deltaTime;

                if (feedbackRemainingTime <= 0f)
                {
                    FinishFeedback();
                }

                break;

            case GameState.Clear:
            case GameState.GameOver:

                // 게임 종료 상태에서는 아무것도 하지 않음
                break;
        }
    }

    // ==========================================
    // 10. 레시피 표시
    // ==========================================

    private void ShowRecipe(bool isInitialPreview)
    {
        if (isInitialPreview)
        {
            currentState = GameState.InitialPreview;
        }
        else
        {
            currentState = GameState.RecipeReview;
        }

        recipeRemainingTime = recipeViewTime;

        recipeText.text =
            "아래에서 위로 쌓는 순서\n\n";

        Ingredient[] currentRecipe = recipes[currentOrder];

        for (int i = 0; i < currentRecipe.Length; i++)
        {
            recipeText.text +=
                (i + 1) + ". " +
                GetIngredientName(currentRecipe[i]) +
                "\n";
        }

        recipeCountdownText.text =
            Mathf.CeilToInt(recipeRemainingTime)
            + "초 후 닫힙니다.";

        recipePopup.SetActive(true);

        guideText.text = "레시피를 기억하세요!";

        SetInputEnabled(false);
    }

    // ==========================================
    // 11. 레시피북 버튼 클릭
    // ==========================================

    public void OpenRecipeBook()
    {
        // 제작 중에만 레시피북을 열 수 있다.
        if (currentState != GameState.Playing)
        {
            return;
        }

        ShowRecipe(false);
    }

    // ==========================================
    // 12. 레시피 표시 시간 관리
    // ==========================================

    private void UpdateRecipeTimer(float deltaTime)
    {
        recipeRemainingTime -= deltaTime;

        recipeCountdownText.text =
            Mathf.CeilToInt(
                Mathf.Max(0f, recipeRemainingTime))
            + "초 후 닫힙니다.";

        if (recipeRemainingTime <= 0f)
        {
            CloseRecipe();
        }
    }

    private void CloseRecipe()
    {
        recipePopup.SetActive(false);

        currentState = GameState.Playing;

        guideText.text =
            "재료를 순서대로 선택하세요!";

        SetInputEnabled(true);
    }

    // ==========================================
    // 13. 제작 제한 시간 관리
    // ==========================================

    private void UpdateCookingTimer(float deltaTime)
    {
        remainingTime -= deltaTime;

        remainingTime = Mathf.Max(0f, remainingTime);

        UpdateTimerUI();

        if (remainingTime <= 0f)
        {
            FailOrder("시간 초과!");
        }
    }

    // ==========================================
    // 14. 재료 선택
    // ==========================================

    public void SelectIngredient(Ingredient ingredient)
    {
        if (currentState != GameState.Playing)
        {
            return;
        }

        // 선택한 재료의 순서 저장
        playerIngredients.Add(ingredient);

        // 화면에 재료 도형 생성
        CreateIngredientLayer(ingredient);

        // 위쪽 빵을 선택하면 자동 제출
        if (ingredient == Ingredient.TopBun)
        {
            CheckAnswer();
        }
    }

    // ==========================================
    // 15. 햄버거 재료 도형 생성
    // ==========================================

    private void CreateIngredientLayer(
        Ingredient ingredient)
    {
        GameObject newLayer = Instantiate(
            ingredientPrefab,
            burgerStack
        );

        newLayer.name =
            ingredient.ToString() +
            "_" + playerIngredients.Count;

        RectTransform rect =
            newLayer.GetComponent<RectTransform>();

        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);

        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;

        // 재료별 색상 지정
        Image layerImage = newLayer.GetComponent<Image>();

        layerImage.color = GetIngredientColor(ingredient);
        layerImage.raycastTarget = false;

        // 재료 이름 표시
        TMP_Text layerText =
            newLayer.GetComponentInChildren<TMP_Text>(true);

        if (layerText != null)
        {
            layerText.text = GetIngredientName(ingredient);
            layerText.raycastTarget = false;

            if (ingredient == Ingredient.Patty ||
                ingredient == Ingredient.Tomato)
            {
                layerText.color = Color.white;
            }
            else
            {
                layerText.color = Color.black;
            }
        }

        burgerLayers.Add(newLayer);

        ArrangeBurgerLayers();
    }

    // ==========================================
    // 16. 햄버거 아래에서 위로 정렬
    // ==========================================

    private void ArrangeBurgerLayers()
    {
        float totalHeight = 0f;

        for (int i = 0; i < playerIngredients.Count; i++)
        {
            totalHeight +=
                GetIngredientHeight(playerIngredients[i]);
        }

        // 재료가 너무 많으면 전체 높이를 줄여
        // 제작 영역을 벗어나지 않게 한다.
        float heightScale = Mathf.Min(
            1f,
            maxBurgerHeight / Mathf.Max(1f, totalHeight)
        );

        float currentHeight = 0f;

        for (int i = 0; i < burgerLayers.Count; i++)
        {
            Ingredient ingredient = playerIngredients[i];

            RectTransform rect =
                burgerLayers[i].GetComponent<RectTransform>();

            float height =
                GetIngredientHeight(ingredient) * heightScale;

            float width =
                GetIngredientWidth(ingredient);

            rect.sizeDelta = new Vector2(width, height);

            rect.anchoredPosition =
                new Vector2(0f, currentHeight);

            TMP_Text label =
                burgerLayers[i].GetComponentInChildren<TMP_Text>();

            if (label != null)
            {
                label.fontSize = Mathf.Min(
                    36f,
                    height * 0.65f
                );
            }

            currentHeight += height;
        }
    }

    // ==========================================
    // 17. 정답 판정
    // ==========================================

    private void CheckAnswer()
    {
        Ingredient[] correctRecipe = recipes[currentOrder];

        // 재료 개수가 다르면 오답
        if (playerIngredients.Count != correctRecipe.Length)
        {
            FailOrder("재료가 틀렸어요!");
            return;
        }

        // 모든 재료의 종류와 순서 비교
        for (int i = 0; i < correctRecipe.Length; i++)
        {
            if (playerIngredients[i] != correctRecipe[i])
            {
                FailOrder("재료가 틀렸어요!");
                return;
            }
        }

        // 모든 재료가 동일하면 정답
        SuccessOrder();
    }

    // ==========================================
    // 18. 주문 성공
    // ==========================================

    private void SuccessOrder()
    {
        if (currentState != GameState.Playing)
        {
            return;
        }

        currentState = GameState.Feedback;

        lastAnswerCorrect = true;

        feedbackRemainingTime = feedbackTime;

        feedbackText.text = "정답!";
        feedbackText.color = new Color32(100, 230, 120, 255);

        guideText.text = "햄버거 완성!";

        recipePopup.SetActive(false);

        SetInputEnabled(false);
    }

    // ==========================================
    // 19. 주문 실패
    // ==========================================

    private void FailOrder(string message)
    {
        if (currentState != GameState.Playing &&
            currentState != GameState.RecipeReview)
        {
            return;
        }

        currentState = GameState.Feedback;

        remainingLives = Mathf.Max(0, remainingLives - 1);

        lastAnswerCorrect = false;

        feedbackRemainingTime = feedbackTime;

        recipePopup.SetActive(false);

        feedbackText.text = message;

        feedbackText.color =
            new Color32(255, 100, 100, 255);

        guideText.text = "남은 기회가 감소했습니다.";

        UpdateLifeUI();

        SetInputEnabled(false);
    }

    // ==========================================
    // 20. 정답/오답 안내 종료
    // ==========================================

    private void FinishFeedback()
    {
        feedbackText.text = "";

        if (lastAnswerCorrect)
        {
            currentOrder++;

            // 모든 주문을 완료한 경우
            if (currentOrder >= recipes.Length)
            {
                EndGame(true);
                return;
            }
        }
        else
        {
            // 기회를 모두 잃은 경우
            if (remainingLives <= 0)
            {
                EndGame(false);
                return;
            }
        }

        // 성공했다면 다음 주문,
        // 실패했다면 같은 주문 재시도
        StartOrder();
    }

    // ==========================================
    // 21. 현재 햄버거 초기화
    // ==========================================

    public void ResetBurger()
    {
        if (currentState != GameState.Playing)
        {
            return;
        }

        ClearBurger();

        guideText.text =
            "햄버거를 처음부터 만들어 주세요!";
    }

    private void ClearBurger()
    {
        foreach (GameObject layer in burgerLayers)
        {
            if (layer != null)
            {
                layer.SetActive(false);
                Destroy(layer);
            }
        }

        burgerLayers.Clear();
        playerIngredients.Clear();
    }

    // ==========================================
    // 22. 버튼 활성화 및 비활성화
    // ==========================================

    private void SetInputEnabled(bool enabled)
    {
        foreach (Button button in ingredientButtons)
        {
            button.interactable = enabled;
        }

        resetButton.interactable = enabled;
        recipeBookButton.interactable = enabled;
    }

    // ==========================================
    // 23. 게임 종료
    // ==========================================

    private void EndGame(bool isClear)
    {
        SetInputEnabled(false);

        recipePopup.SetActive(false);

        endPanel.SetActive(true);

        feedbackText.text = "";

        if (isClear)
        {
            currentState = GameState.Clear;

            resultText.text =
                "CLEAR\n10개 주문 완성!";

            resultText.color =
                new Color32(255, 220, 150, 255);

            guideText.text = "모든 주문을 완료했습니다!";
        }
        else
        {
            currentState = GameState.GameOver;

            resultText.text =
                "GAME OVER";

            resultText.color =
                new Color32(255, 150, 150, 255);

            guideText.text = "기회를 모두 잃었습니다.";
        }
    }

    // ==========================================
    // 24. 남은 기회 UI
    // ==========================================

    private void UpdateLifeUI()
    {
        string hearts = "";

        for (int i = 0; i < maxLives; i++)
        {
            if (i < remainingLives)
            {
                hearts += "♥ ";
            }
            else
            {
                hearts += "♡ ";
            }
        }

        lifeText.text = "남은 기회: " + hearts;
    }

    // ==========================================
    // 25. 주문 번호 UI
    // ==========================================

    private void UpdateOrderUI()
    {
        orderText.text =
            "주문 " +
            Mathf.Min(currentOrder + 1, recipes.Length) +
            " / " +
            recipes.Length;
    }

    // ==========================================
    // 26. 제한 시간 UI
    // ==========================================

    private void UpdateTimerUI()
    {
        int seconds =
            Mathf.CeilToInt(Mathf.Max(0f, remainingTime));

        timerText.text =
            "남은 시간: " + seconds + "초";

        if (remainingTime <= 3f)
        {
            timerText.color =
                new Color32(255, 100, 100, 255);
        }
        else
        {
            timerText.color = Color.white;
        }
    }

    // ==========================================
    // 27. 재료 이름 반환
    // ==========================================

    private string GetIngredientName(Ingredient ingredient)
    {
        switch (ingredient)
        {
            case Ingredient.BottomBun:
                return "아래쪽 빵";

            case Ingredient.Patty:
                return "패티";

            case Ingredient.Cheese:
                return "치즈";

            case Ingredient.Lettuce:
                return "양상추";

            case Ingredient.Tomato:
                return "토마토";

            case Ingredient.TopBun:
                return "위쪽 빵";

            default:
                return "";
        }
    }

    // ==========================================
    // 28. 재료 색상 반환
    // ==========================================

    private Color GetIngredientColor(Ingredient ingredient)
    {
        switch (ingredient)
        {
            case Ingredient.BottomBun:
                return new Color32(220, 162, 85, 255);

            case Ingredient.Patty:
                return new Color32(137, 82, 59, 255);

            case Ingredient.Cheese:
                return new Color32(242, 204, 73, 255);

            case Ingredient.Lettuce:
                return new Color32(131, 184, 95, 255);

            case Ingredient.Tomato:
                return new Color32(217, 96, 82, 255);

            case Ingredient.TopBun:
                return new Color32(232, 172, 100, 255);

            default:
                return Color.white;
        }
    }

    // ==========================================
    // 29. 재료 높이 반환 - QHD
    // ==========================================

    private float GetIngredientHeight(Ingredient ingredient)
    {
        switch (ingredient)
        {
            case Ingredient.BottomBun:
                return 100f;

            case Ingredient.Patty:
                return 85f;

            case Ingredient.Cheese:
                return 50f;

            case Ingredient.Lettuce:
                return 60f;

            case Ingredient.Tomato:
                return 55f;

            case Ingredient.TopBun:
                return 135f;

            default:
                return 80f;
        }
    }

    // ==========================================
    // 30. 재료 너비 반환 - QHD
    // ==========================================

    private float GetIngredientWidth(Ingredient ingredient)
    {
        switch (ingredient)
        {
            case Ingredient.BottomBun:
                return ingredientWidth;

            case Ingredient.Patty:
                return ingredientWidth + 20f;

            case Ingredient.Cheese:
                return ingredientWidth + 40f;

            case Ingredient.Lettuce:
                return ingredientWidth + 60f;

            case Ingredient.Tomato:
                return ingredientWidth + 10f;

            case Ingredient.TopBun:
                return ingredientWidth;

            default:
                return ingredientWidth;
        }
    }
}
