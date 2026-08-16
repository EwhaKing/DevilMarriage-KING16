using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Stage29 PlayScene: 사진사 소환 직전 안내.
/// </summary>
public class Stage29PlayIntroController : MonoBehaviour
{
    [SerializeField] private Stage1PuzzleController puzzleController;

    private GameObject _panelRoot;
    private TextMeshProUGUI _speakerText;
    private TextMeshProUGUI _bodyText;
    private bool _waitingForAdvance;

    private void Start()
    {
        var stage = GameFlowManager.Instance != null ? GameFlowManager.Instance.CurrentStage : null;
        if (stage == null || stage.stageNumber != 29)
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
            "시스템|모든 룬을 연결하여 결혼식 사진사에게 적합한 악마를 소환하세요.",
            "시스템|정신력이 감소하는 룬을 피하고 일방향 룬의 방향을 확인하세요.",
            "루시아|막다른 길로 들어가지 않도록 전체 경로를 먼저 확인해!",
            "벨리안|모든 룬을 빠짐없이 연결하십시오.",
            "주인공|좋아! 이제 마지막 룬만 남았어!"
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
            var canvasObject = new GameObject("Stage29IntroCanvas");
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        _panelRoot = new GameObject("Stage29PlayIntroDialogue");
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
