using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// StoryScene에서 Open/Close 대사를 DialogueManager로 재생합니다.
/// 대화가 끝나면 GameFlowManager에게 다음 단계(플레이 또는 스테이지 선택)를 알립니다.
/// </summary>
public class StorySceneController : MonoBehaviour
{
    [SerializeField] private DialogueManager dialogueManager;
    [SerializeField] private SceneChanger sceneChanger;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private StageBgmPlayer bgmPlayer;

    private bool _isTransitioning;
    private CanvasGroup _fadeGroup;
    private Image _fadeTarget;
    private Sprite _savedBackgroundSprite;
    private Color _savedBackgroundColor = Color.white;

    private void Start()
    {
        if (dialogueManager == null)
            dialogueManager = FindAnyObjectByType<DialogueManager>();

        if (dialogueManager == null)
        {
            var canvas = GameObject.Find(DialogueUiBuilder.CanvasName);
            if (canvas != null)
                dialogueManager = canvas.GetComponent<DialogueManager>();
        }

        if (dialogueManager == null)
            dialogueManager = gameObject.AddComponent<DialogueManager>();

        if (dialogueManager == null)
        {
            Debug.LogError("[StorySceneController] DialogueManager를 만들 수 없습니다.");
            return;
        }

        if (sceneChanger == null)
            sceneChanger = FindAnyObjectByType<SceneChanger>();

        if (sceneChanger != null)
        {
            if (dialogueManager.PortraitDefault == null)
            {
                dialogueManager.SetPortraitSprites(
                    sceneChanger.PortraitDefault,
                    sceneChanger.PortraitHappy,
                    sceneChanger.PortraitNervous);
            }

            dialogueManager.SetSettingPopup(sceneChanger.SettingPopup);
        }

        if (dialogueManager.onDialogueFinished == null)
            dialogueManager.onDialogueFinished = new UnityEvent();

        dialogueManager.onDialogueFinished.AddListener(OnDialogueComplete);
        dialogueManager.OnCustomEvent += HandleDialogueEvent;

        if (dialogueManager.GetComponent<DialogueBookChoiceController>() == null)
            dialogueManager.gameObject.AddComponent<DialogueBookChoiceController>();

        ApplyStagePresentation();

        var dialogueData = GameFlowManager.Instance != null
            ? GameFlowManager.Instance.GetCurrentStageDialogueData()
            : null;

        if (dialogueData == null)
        {
            Debug.LogError("[StorySceneController] StageDialogueData가 없습니다. StageData.dialogueData를 연결하세요.");
            OnDialogueComplete();
            return;
        }

        bool isOpening = GameFlowManager.Instance == null || GameFlowManager.Instance.IsOpeningStory;

        var stage = GameFlowManager.Instance != null ? GameFlowManager.Instance.CurrentStage : null;
        if (stage != null && stage.stageNumber >= 2 && stage.stageNumber <= 33)
            dialogueData = DialogueContentLibrary.CreateStageRuntime(stage.stageNumber);

        dialogueManager.StartStageDialogue(dialogueData, isOpening);
    }

    private void OnDestroy()
    {
        if (dialogueManager != null)
        {
            dialogueManager.OnCustomEvent -= HandleDialogueEvent;
            if (dialogueManager.onDialogueFinished != null)
                dialogueManager.onDialogueFinished.RemoveListener(OnDialogueComplete);
        }
    }

    private void ApplyStagePresentation()
    {
        var stage = GameFlowManager.Instance != null ? GameFlowManager.Instance.CurrentStage : null;
        if (stage == null)
            return;

        if (backgroundImage != null && stage.backgroundImage != null)
            backgroundImage.sprite = stage.backgroundImage;

        if (bgmPlayer != null && stage.backgroundMusic != null)
            bgmPlayer.SetBgmClip(stage.backgroundMusic);
    }

    private void OnDialogueComplete()
    {
        if (_isTransitioning || GameFlowManager.Instance == null)
            return;

        _isTransitioning = true;

        if (GameFlowManager.Instance.CurrentStoryPhase == StoryPhase.Opening)
            GameFlowManager.Instance.OnOpeningStoryFinished();
        else
            GameFlowManager.Instance.OnClosingStoryFinished();
    }

    private void HandleDialogueEvent(string eventId)
    {
        switch (eventId)
        {
            case "FadeToBlack":
                StartCoroutine(FadeOverlayCoroutine(1f));
                break;
            case "FadeFromBlack":
                StartCoroutine(FadeOverlayCoroutine(0f));
                break;
        }
    }

    private System.Collections.IEnumerator FadeOverlayCoroutine(float targetAlpha)
    {
        EnsureFadeOverlay();
        if (_fadeGroup == null)
        {
            dialogueManager?.NotifyExternalEventCompleted();
            yield break;
        }

        if (targetAlpha >= 1f && _fadeTarget != null)
        {
            _savedBackgroundSprite = _fadeTarget.sprite;
            _savedBackgroundColor = _fadeTarget.color;
        }

        float from = _fadeGroup.alpha;
        float duration = 0.7f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            _fadeGroup.alpha = Mathf.Lerp(from, targetAlpha, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        _fadeGroup.alpha = targetAlpha;
        _fadeGroup.blocksRaycasts = false;

        if (targetAlpha <= 0.01f && _fadeTarget != null)
        {
            _fadeTarget.sprite = _savedBackgroundSprite;
            _fadeTarget.color = _savedBackgroundColor;
        }

        if (dialogueManager != null)
            dialogueManager.NotifyExternalEventCompleted();
    }

    private void EnsureFadeOverlay()
    {
        if (_fadeGroup != null)
            return;

        ResolveFadeTarget();
        if (_fadeTarget == null)
            return;

        var fadeObject = new GameObject("StoryFadeOverlay");
        fadeObject.transform.SetParent(_fadeTarget.transform.parent, false);
        fadeObject.transform.SetSiblingIndex(_fadeTarget.transform.GetSiblingIndex() + 1);

        var image = fadeObject.AddComponent<Image>();
        image.color = Color.black;
        image.raycastTarget = false;

        var rect = fadeObject.GetComponent<RectTransform>();
        CopyRect(_fadeTarget.rectTransform, rect);

        _fadeGroup = fadeObject.AddComponent<CanvasGroup>();
        _fadeGroup.alpha = 0f;
        _fadeGroup.blocksRaycasts = false;
        _fadeGroup.ignoreParentGroups = true;
    }

    private void ResolveFadeTarget()
    {
        if (backgroundImage != null)
        {
            _fadeTarget = backgroundImage;
            return;
        }

        var bgObject = GameObject.Find("bg");
        if (bgObject != null)
            _fadeTarget = bgObject.GetComponent<Image>();
    }

    private static void CopyRect(RectTransform source, RectTransform dest)
    {
        dest.anchorMin = source.anchorMin;
        dest.anchorMax = source.anchorMax;
        dest.pivot = source.pivot;
        dest.anchoredPosition = source.anchoredPosition;
        dest.sizeDelta = source.sizeDelta;
        dest.offsetMin = source.offsetMin;
        dest.offsetMax = source.offsetMax;
        dest.localScale = source.localScale;
    }
}
