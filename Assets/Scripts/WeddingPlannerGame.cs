
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class WeddingPlannerGame : MonoBehaviour
{
    [Header("Game Settings")]
    [SerializeField] private float gameTime = 20f;
    [SerializeField] private int targetCount = 20;
    [SerializeField] private float wrongPenalty = 1f;

    [Header("UI")]
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private GameObject resultPanel;

    // Inspector에 위쪽부터 아래쪽 순서대로 연결
    [SerializeField] private TMP_Text[] arrowTexts;
    [SerializeField] private Image[] arrowBackgrounds;

    [Header("Colors")]
    [SerializeField] private Color normalColor = Color.gray;
    [SerializeField] private Color currentColor = Color.cyan;

    private readonly List<int> arrows = new List<int>();

    // 0=Up, 1=Down, 2=Left, 3=Right
    private readonly string[] symbols =
    {
        "위", "밑", "왼", "오"
    };

    private float remainingTime;
    private int correctCount;
    private bool isPlaying;

    private Coroutine feedbackCoroutine;

    void Start()
    {
        StartGame();
    }

    public void StartGame()
    {
        remainingTime = gameTime;
        correctCount = 0;
        isPlaying = true;

        if (feedbackCoroutine != null)
        {
            StopCoroutine(feedbackCoroutine);
            feedbackCoroutine = null;
        }

        feedbackText.text = "";
        resultPanel.SetActive(false);

        arrows.Clear();

        for (int i = 0; i < arrowTexts.Length; i++)
        {
            arrows.Add(Random.Range(0, 4));
        }

        UpdateArrowUI();
        UpdateStatusUI();
    }

    void Update()
    {
        if (!isPlaying)
            return;

        // 시간 감소
        remainingTime -= Time.deltaTime;

        if (remainingTime <= 0f)
        {
            remainingTime = 0f;
            GameOver();
            UpdateStatusUI();
            return;
        }

        int input = GetArrowInput();

        if (input != -1)
        {
            CheckInput(input);
        }

        UpdateStatusUI();
    }

    int GetArrowInput()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
            return -1;

        if (keyboard.upArrowKey.wasPressedThisFrame)
            return 0;

        if (keyboard.downArrowKey.wasPressedThisFrame)
            return 1;

        if (keyboard.leftArrowKey.wasPressedThisFrame)
            return 2;

        if (keyboard.rightArrowKey.wasPressedThisFrame)
            return 3;
#else
        if (Input.GetKeyDown(KeyCode.UpArrow))
            return 0;

        if (Input.GetKeyDown(KeyCode.DownArrow))
            return 1;

        if (Input.GetKeyDown(KeyCode.LeftArrow))
            return 2;

        if (Input.GetKeyDown(KeyCode.RightArrow))
            return 3;
#endif

        return -1;
    }

    void CheckInput(int input)
    {
        // 리스트의 마지막 = 화면 맨 아래 화살표
        int answer = arrows[arrows.Count - 1];

        if (input == answer)
        {
            CorrectInput();
        }
        else
        {
            WrongInput();
        }
    }

    void CorrectInput()
    {
        correctCount++;

        ShowFeedback("정답!", Color.green);

        // 목표 달성
        if (correctCount >= targetCount)
        {
            ClearGame();
            return;
        }

        // 맨 아래 화살표 제거
        arrows.RemoveAt(arrows.Count - 1);

        // 맨 위에 새로운 화살표 추가
        arrows.Insert(0, Random.Range(0, 4));

        UpdateArrowUI();
    }

    void WrongInput()
    {
        remainingTime -= wrongPenalty;

        ShowFeedback("실수!", Color.red);

        if (remainingTime <= 0f)
        {
            remainingTime = 0f;
            GameOver();
        }
    }

    void UpdateArrowUI()
    {
        for (int i = 0; i < arrowTexts.Length; i++)
        {
            arrowTexts[i].text = symbols[arrows[i]];

            if (i < arrowBackgrounds.Length)
            {
                if (i == arrowTexts.Length - 1)
                    arrowBackgrounds[i].color = currentColor;
                else
                    arrowBackgrounds[i].color = normalColor;
            }
        }
    }

    void UpdateStatusUI()
    {
        timerText.text =
            "남은 시간: " + remainingTime.ToString("F1") + "초";

        progressText.text =
            correctCount + " / " + targetCount;
    }

    void ShowFeedback(string message, Color color)
    {
        if (feedbackCoroutine != null)
            StopCoroutine(feedbackCoroutine);

        feedbackCoroutine =
            StartCoroutine(FeedbackRoutine(message, color));
    }

    IEnumerator FeedbackRoutine(string message, Color color)
    {
        feedbackText.text = message;
        feedbackText.color = color;

        yield return new WaitForSeconds(0.4f);

        feedbackText.text = "";
        feedbackCoroutine = null;
    }

    void ClearGame()
    {
        isPlaying = false;

        resultPanel.SetActive(true);
        resultText.text = "CLEAR";
        resultText.color = Color.green;

        UpdateStatusUI();
    }

    void GameOver()
    {
        isPlaying = false;

        resultPanel.SetActive(true);
        resultText.text = "GAME OVER";
        resultText.color = Color.red;

        UpdateStatusUI();
    }
}
