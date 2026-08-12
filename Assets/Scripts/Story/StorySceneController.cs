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
        if (stage != null && stage.stageNumber >= 3 && stage.stageNumber <= 10)
            dialogueData = DialogueContentLibrary.CreateStageRuntime(stage.stageNumber);

        dialogueManager.StartStageDialogue(dialogueData, isOpening);
    }

    private void OnDestroy()
    {
        if (dialogueManager != null && dialogueManager.onDialogueFinished != null)
            dialogueManager.onDialogueFinished.RemoveListener(OnDialogueComplete);
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
}
