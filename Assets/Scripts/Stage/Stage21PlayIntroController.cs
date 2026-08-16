using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Stage21 PlayScene: 일방향 룬 퍼즐 직전 짧은 안내.
/// </summary>
public class Stage21PlayIntroController : MonoBehaviour
{
    [SerializeField] private Stage1PuzzleController puzzleController;

    private GameObject _panelRoot;
    private TextMeshProUGUI _speakerText;
    private TextMeshProUGUI _bodyText;
    private bool _waitingForAdvance;

    private void Start()
    {
        var stage = GameFlowManager.Instance != null ? GameFlowManager.Instance.CurrentStage : null;
        if (stage == null || stage.stageNumber != 21)
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
            "시스템|모든 룬을 연결하여 스타일리스트에게 적합한 악마를 소환하세요.",
            "시스템|화살표가 표시된 룬은 지정된 방향으로만 이동할 수 있습니다.",
            "시스템|일방향 룬을 통과하면 반대 방향으로 되돌아갈 수 없습니다.",
            "시스템|밟으면 정신력이 감소하는 룬을 피하세요.",
            "주인공|이 룬을 지나면 돌아갈 수 없는 거지?",
            "루시아|응. 막다른 길로 이어지지 않는지 먼저 확인해!",
            "벨리안|일방향 룬을 통과하는 순서가 중요합니다.",
            "주인공|좋아! 이제 마지막 룬만 연결하면 돼!"
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
            var canvasObject = new GameObject("Stage21IntroCanvas");
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        _panelRoot = new GameObject("Stage21PlayIntroDialogue");
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
