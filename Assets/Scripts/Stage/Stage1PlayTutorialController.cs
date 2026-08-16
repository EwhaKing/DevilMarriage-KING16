using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Stage1PlayTutorialController : MonoBehaviour
{
    [SerializeField] private Stage1PuzzleController puzzleController;
    [SerializeField] private RuneNode[] runes;

    private enum TutorialStep
    {
        WaitStartClick,
        WaitFirstConnection,
        Playing
    }

    private TutorialStep _step;
    private GameObject _panelRoot;
    private TextMeshProUGUI _speakerText;
    private TextMeshProUGUI _bodyText;
    private bool _waitingForAdvance;
    private bool _startSelected;

    private void Start()
    {
        var stage = GameFlowManager.Instance != null ? GameFlowManager.Instance.CurrentStage : null;
        if (stage == null || stage.stageNumber != 1)
        {
            enabled = false;
            return;
        }

        if (puzzleController == null)
            puzzleController = FindAnyObjectByType<Stage1PuzzleController>();

        if (runes == null || runes.Length == 0)
        {
            if (puzzleController != null && puzzleController.Runes != null && puzzleController.Runes.Length > 0)
                runes = puzzleController.Runes;
            else
                runes = FindObjectsByType<RuneNode>(FindObjectsSortMode.None);
        }

        if (puzzleController != null)
        {
            puzzleController.InputLocked = true;
            puzzleController.OnRuneClicked += HandleRuneClicked;
            puzzleController.OnForwardMoveCompleted += HandleForwardMoveCompleted;
        }

        StartCoroutine(RunTutorial());
    }

    private void OnDestroy()
    {
        if (puzzleController != null)
        {
            puzzleController.OnRuneClicked -= HandleRuneClicked;
            puzzleController.OnForwardMoveCompleted -= HandleForwardMoveCompleted;
        }
    }

    private IEnumerator RunTutorial()
    {
        EnsureDialoguePanel();

        yield return ShowDialogue(new[]
        {
            "주인공|좋아…… 시작점은 내가 정하는 거구나. 뭐, 어디서 시작해도 상관 없겠지?"
        });

        _step = TutorialStep.WaitStartClick;
        _startSelected = false;
        SetAllRuneHighlights(true);
        puzzleController.InputLocked = false;
        puzzleController.AwaitingStartSelection = true;

        while (!_startSelected)
            yield return null;

        SetAllRuneHighlights(false);
        puzzleController.InputLocked = true;

        yield return ShowDialogue(new[]
        {
            "주인공|이 다음에는 점과 점을 이어서 선을 만들어야 해."
        });

        _step = TutorialStep.WaitFirstConnection;
        HighlightNonStartRunes(true);
        puzzleController.InputLocked = false;

        while (_step == TutorialStep.WaitFirstConnection)
            yield return null;

        HighlightNonStartRunes(false);
        puzzleController.InputLocked = true;

        yield return ShowDialogue(new[]
        {
            "주인공|오오... 생각보다 그럴듯한데? 나 혹시 재능 있나?",
            "주인공|좋아. 그럼 이제 복잡하게 생각하지 말고, 모든 점을 전부 이어버리면 되는 거야."
        });

        _step = TutorialStep.Playing;
        puzzleController.InputLocked = false;
        HideDialoguePanel();
    }

    private void HandleRuneClicked(RuneNode rune)
    {
        if (_step == TutorialStep.WaitStartClick)
        {
            _startSelected = true;
        }
    }

    private void HandleForwardMoveCompleted()
    {
        if (_step == TutorialStep.WaitFirstConnection)
            _step = TutorialStep.Playing;
    }

    private IEnumerator ShowDialogue(string[] lines)
    {
        EnsureDialoguePanel();
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
                yield return null;
        }

        HideDialoguePanel();
    }

    private void AdvanceDialogue()
    {
        _waitingForAdvance = false;
    }

    private void SetAllRuneHighlights(bool enabled)
    {
        if (runes == null)
            return;

        foreach (var rune in runes)
        {
            if (rune != null)
                rune.SetHighlight(enabled);
        }
    }

    private void HighlightNonStartRunes(bool enabled)
    {
        if (runes == null || puzzleController == null)
            return;

        int startIndex = puzzleController.CurrentRuneIndex;
        foreach (var rune in runes)
        {
            if (rune == null)
                continue;

            rune.SetHighlight(enabled && rune.RuneIndex != startIndex);
        }
    }

    private void EnsureDialoguePanel()
    {
        if (_panelRoot != null)
            return;

        var canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            var canvasObject = new GameObject("TutorialCanvas");
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        _panelRoot = new GameObject("Stage1TutorialDialogue");
        _panelRoot.transform.SetParent(canvas.transform, false);

        var image = _panelRoot.AddComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.72f);

        var rect = _panelRoot.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.08f, 0.05f);
        rect.anchorMax = new Vector2(0.92f, 0.28f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        _speakerText = CreateText(_panelRoot.transform, "Speaker", string.Empty, 26, TextAlignmentOptions.Left);
        var speakerRect = _speakerText.GetComponent<RectTransform>();
        speakerRect.anchorMin = new Vector2(0.04f, 0.7f);
        speakerRect.anchorMax = new Vector2(0.96f, 0.95f);
        speakerRect.offsetMin = Vector2.zero;
        speakerRect.offsetMax = Vector2.zero;

        _bodyText = CreateText(_panelRoot.transform, "Body", string.Empty, 24, TextAlignmentOptions.TopLeft);
        var bodyRect = _bodyText.GetComponent<RectTransform>();
        bodyRect.anchorMin = new Vector2(0.04f, 0.1f);
        bodyRect.anchorMax = new Vector2(0.96f, 0.68f);
        bodyRect.offsetMin = Vector2.zero;
        bodyRect.offsetMax = Vector2.zero;

        var button = _panelRoot.AddComponent<Button>();
        button.onClick.AddListener(AdvanceDialogue);
        _panelRoot.SetActive(false);
    }

    private void HideDialoguePanel()
    {
        if (_panelRoot != null)
            _panelRoot.SetActive(false);
    }

    private static TextMeshProUGUI CreateText(Transform parent, string name, string content, float size, TextAlignmentOptions align)
    {
        var textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);
        var text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = content;
        text.fontSize = size;
        text.color = Color.white;
        text.alignment = align;
        return text;
    }
}
