using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Stage33 PlayScene: 이중 잠금 선 소환 직전 안내.
/// </summary>
public class Stage33PlayIntroController : MonoBehaviour
{
    [SerializeField] private Stage1PuzzleController puzzleController;

    private GameObject _panelRoot;
    private TextMeshProUGUI _speakerText;
    private TextMeshProUGUI _bodyText;
    private bool _waitingForAdvance;

    private void Start()
    {
        var stage = GameFlowManager.Instance != null ? GameFlowManager.Instance.CurrentStage : null;
        if (stage == null || stage.stageNumber != 33)
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
            "시스템|모든 룬을 연결하여 결혼식 사회자에게 적합한 악마를 소환하세요.",
            "시스템|겹쳐진 문양이 표시된 선은 두 번 통과해야 완전히 활성화됩니다.",
            "시스템|첫 번째 통과 후 해당 선으로 다시 돌아올 수 있는 경로를 확보하세요.",
            "시스템|정신력이 감소하는 룬과 일방향 룬도 주의하세요.",
            "루시아|저 선은 아직 한 번밖에 지나지 않았어!",
            "벨리안|두 번째로 통과할 경로를 남겨두십시오.",
            "주인공|다른 룬을 먼저 연결한 다음 다시 돌아오면 되겠네!",
            "주인공|좋아! 두 번째 연결까지 완료했어!"
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
            var canvasObject = new GameObject("Stage33IntroCanvas");
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        _panelRoot = new GameObject("Stage33PlayIntroDialogue");
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
