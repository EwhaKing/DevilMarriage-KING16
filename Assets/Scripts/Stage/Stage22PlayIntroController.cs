using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Stage22 PlayScene: 스타일 시험 직전 상황 안내.
/// </summary>
public class Stage22PlayIntroController : MonoBehaviour
{
    [SerializeField] private Stage1PuzzleController puzzleController;

    private GameObject _panelRoot;
    private TextMeshProUGUI _speakerText;
    private TextMeshProUGUI _bodyText;
    private bool _waitingForAdvance;

    private void Start()
    {
        var stage = GameFlowManager.Instance != null ? GameFlowManager.Instance.CurrentStage : null;
        if (stage == null || stage.stageNumber != 22)
        {
            enabled = false;
            return;
        }

        if (puzzleController == null)
            puzzleController = FindAnyObjectByType<Stage1PuzzleController>();

        StartCoroutine(RunIntro());
    }

    private IEnumerator RunIntro()
    {
        if (puzzleController != null)
            puzzleController.InputLocked = true;

        yield return ShowDialogue(new[]
        {
            "시스템|각 결혼식 상황에 어울리는 스타일을 완성하세요.",
            "시스템|의상, 헤어, 화장과 장신구를 조합하여 총 세 가지 스타일을 완성해야 합니다.",
            "시스템|화려함뿐만 아니라 각 상황의 분위기와 활동성도 고려하세요.",
            "아스벨|첫 번째는 하객을 맞이할 때의 모습이야.",
            "아스벨|가까이에서 대화를 나눠야 하니 지나치게 무겁거나 위압적인 장식은 피하도록 해.",
            "주인공|화려하면서도 편안한 분위기로 만들면 되겠네!",
            "루시아|왕관 세 개를 동시에 쓰는 건 편안해 보이지 않아.",
            "주인공|아직 고르지도 않았는데 어떻게 알았어?",
            "루시아|네가 왕관을 보는 표정이 너무 분명했어.",
            "아스벨|두 번째는 신랑과 함께 입장할 때의 모습이야.",
            "아스벨|시선을 끌면서도 옆에 설 상대와 조화를 이루어야 해.",
            "주인공|마왕이 어떻게 생겼는지 모르는데 어떻게 맞춰?",
            "아스벨|그러니까 혼자서 모든 시선을 독차지하는 조합은 피하라는 뜻이야.",
            "주인공|……왕관 세 개는 여기서도 안 되는 거구나.",
            "아스벨|절대 안 돼.",
            "아스벨|마지막은 예식이 끝난 뒤의 모습이야.",
            "아스벨|오래 움직여도 불편하지 않고, 네가 가장 자연스럽게 웃을 수 있는 조합을 골라.",
            "주인공|가장 비싼 드레스가 정답은 아니야?",
            "아스벨|결혼은 가격표를 자랑하는 행사가 아니야.",
            "주인공|알았어. 이번에는 정말 내가 편하고 좋아하는 걸 고를게!"
        });

        if (puzzleController != null)
            puzzleController.InputLocked = false;
    }

    private IEnumerator ShowDialogue(string[] lines)
    {
        EnsurePanel();
        _panelRoot.SetActive(true);

        foreach (var line in lines)
        {
            var parts = line.Split(new[] { '|' }, 2);
            var speaker = parts[0];
            if (speaker == "주인공")
                speaker = PlayerNameManager.PlayerName;

            _speakerText.text = speaker;
            _bodyText.text = parts.Length > 1 ? parts[1] : string.Empty;
            _waitingForAdvance = true;

            while (_waitingForAdvance)
            {
                if (WasAdvancePressed())
                    _waitingForAdvance = false;
                yield return null;
            }
        }

        if (_panelRoot != null)
            _panelRoot.SetActive(false);
    }

    private static bool WasAdvancePressed()
    {
        var keyboard = Keyboard.current;
        if (keyboard != null &&
            (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame))
            return true;

        var mouse = Mouse.current;
        return mouse != null && mouse.leftButton.wasPressedThisFrame;
    }

    private void EnsurePanel()
    {
        if (_panelRoot != null)
            return;

        var canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            var canvasObject = new GameObject("Stage22IntroCanvas");
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        _panelRoot = new GameObject("Stage22PlayIntroDialogue");
        _panelRoot.transform.SetParent(canvas.transform, false);
        var image = _panelRoot.AddComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.72f);

        var rect = _panelRoot.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.08f, 0.05f);
        rect.anchorMax = new Vector2(0.92f, 0.28f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        _speakerText = CreateText(_panelRoot.transform, "Speaker", 26, TextAlignmentOptions.Left);
        var speakerRect = _speakerText.rectTransform;
        speakerRect.anchorMin = new Vector2(0.04f, 0.7f);
        speakerRect.anchorMax = new Vector2(0.96f, 0.95f);
        speakerRect.offsetMin = Vector2.zero;
        speakerRect.offsetMax = Vector2.zero;

        _bodyText = CreateText(_panelRoot.transform, "Body", 24, TextAlignmentOptions.TopLeft);
        var bodyRect = _bodyText.rectTransform;
        bodyRect.anchorMin = new Vector2(0.04f, 0.1f);
        bodyRect.anchorMax = new Vector2(0.96f, 0.68f);
        bodyRect.offsetMin = Vector2.zero;
        bodyRect.offsetMax = Vector2.zero;

        var button = _panelRoot.AddComponent<Button>();
        button.onClick.AddListener(() => _waitingForAdvance = false);
        _panelRoot.SetActive(false);
    }

    private static TextMeshProUGUI CreateText(Transform parent, string name, float size, TextAlignmentOptions align)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var text = go.AddComponent<TextMeshProUGUI>();
        text.fontSize = size;
        text.color = Color.white;
        text.alignment = align;
        return text;
    }
}
