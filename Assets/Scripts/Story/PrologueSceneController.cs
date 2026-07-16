using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 프롤로그 씬에서 ScriptableObject 대사를 재생하고,
/// 이름 입력·BGM·암전·스테이지 선택 이동을 처리합니다.
/// </summary>
public class PrologueSceneController : MonoBehaviour
{
    [Header("대사 데이터")]
    [Tooltip("Resources/PrologueDialogueData 또는 Inspector에서 직접 연결")]
    [SerializeField] private PrologueDialogueData prologueData;

    [SerializeField] private DialogueManager dialogueManager;
    [SerializeField] private SceneChanger sceneChanger;
    [SerializeField] private StageBgmPlayer bgmPlayer;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private float fadeDuration = 0.8f;
    [SerializeField] private float praiseBgmFadeDuration = 2.5f;

    [Header("오디오")]
    [SerializeField] private AudioClip prologueBgm;
    [SerializeField] private AudioClip roomBgm;

    [Header("방 장면")]
    [SerializeField] private Sprite roomBackground;

    private bool _isTransitioning;
    private bool _playingRoomPart;
    private bool _handlingPraiseFade;
    private CanvasGroup _fadeGroup;
    private GameObject _namePromptRoot;
    private TMP_InputField _nameInput;
    private bool _nameSubmitted;

    private void Start()
    {
        GameFlowManager.EnsureExists();

        if (prologueData == null)
            prologueData = Resources.Load<PrologueDialogueData>("PrologueDialogueData");

        if (prologueData == null)
        {
            Debug.LogWarning("[Prologue] PrologueDialogueData 에셋이 없어 기본 대사를 사용합니다. DevilMarriage/Create Dialogue Data 메뉴를 실행하세요.");
            prologueData = DialogueContentLibrary.CreatePrologueRuntime();
        }

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
            Debug.LogError("[Prologue] DialogueManager를 만들 수 없습니다.");
            return;
        }

        if (sceneChanger == null)
            sceneChanger = FindAnyObjectByType<SceneChanger>();

        if (bgmPlayer == null)
            bgmPlayer = FindAnyObjectByType<StageBgmPlayer>();

        if (backgroundImage == null && dialogueManager != null)
            backgroundImage = dialogueManager.GetComponentInChildren<Image>(true);

        // DialogueManager가 bg를 알고 있으면 그걸 사용
        ResolveBackgroundImage();
        ResolveAudioClips();

        if (sceneChanger != null)
        {
            dialogueManager.SetPortraitSprites(
                sceneChanger.PortraitDefault,
                sceneChanger.PortraitHappy,
                sceneChanger.PortraitNervous);
            dialogueManager.SetSettingPopup(sceneChanger.SettingPopup);
        }

        dialogueManager.OnCustomEvent += HandleDialogueEvent;

        if (dialogueManager.onDialogueFinished == null)
            dialogueManager.onDialogueFinished = new UnityEngine.Events.UnityEvent();

        dialogueManager.onDialogueFinished.AddListener(OnNarrationFinished);

        // 프롤로그 BGM 시작
        if (bgmPlayer != null && prologueBgm != null)
            bgmPlayer.Play(prologueBgm, 1f);

        _playingRoomPart = false;
        Debug.Log($"[Prologue] 나레이션 {prologueData.narrationLines.Count}줄 시작");
        dialogueManager.StartDialogue(prologueData.narrationLines);
    }

    private void OnDestroy()
    {
        if (dialogueManager != null)
        {
            dialogueManager.OnCustomEvent -= HandleDialogueEvent;
            if (dialogueManager.onDialogueFinished != null)
                dialogueManager.onDialogueFinished.RemoveListener(OnNarrationFinished);
        }
    }

    private void HandleDialogueEvent(string eventId)
    {
        switch (eventId)
        {
            case "RequestPlayerName":
                StartCoroutine(RequestPlayerNameCoroutine());
                break;

            case "AcceleratePraise":
                // 첫 칭찬 줄부터 BGM 페이드 시작
                if (bgmPlayer != null)
                    bgmPlayer.FadeToVolume(0f, praiseBgmFadeDuration);
                break;

            case "AcceleratePraiseThenFade":
                StartCoroutine(AcceleratePraiseThenFadeToRoomCoroutine());
                break;

            case "FadeToBlack":
                StartCoroutine(FadeThenPlayRoomCoroutine());
                break;

            case "GoToStageSelect":
                GoToStageSelect();
                break;
        }
    }

    private void OnNarrationFinished()
    {
        if (_handlingPraiseFade)
            return;

        if (_playingRoomPart)
        {
            GoToStageSelect();
            return;
        }

        StartCoroutine(FadeThenPlayRoomCoroutine());
    }

    private IEnumerator RequestPlayerNameCoroutine()
    {
        EnsureNamePrompt();
        _nameSubmitted = false;
        _namePromptRoot.SetActive(true);

        if (_nameInput != null)
        {
            _nameInput.text = string.Empty;
            _nameInput.Select();
            _nameInput.ActivateInputField();
        }

        while (!_nameSubmitted)
            yield return null;

        var name = _nameInput != null && !string.IsNullOrWhiteSpace(_nameInput.text)
            ? _nameInput.text.Trim()
            : PlayerNameManager.PlayerName;

        PlayerNameManager.PlayerName = name;
        _namePromptRoot.SetActive(false);

        if (dialogueManager != null)
            dialogueManager.NotifyExternalEventCompleted();
    }

    private IEnumerator AcceleratePraiseThenFadeToRoomCoroutine()
    {
        if (_handlingPraiseFade)
            yield break;

        _handlingPraiseFade = true;

        // BGM이 아직 안 줄어들었으면 마저 페이드 (AcceleratePraise에서 이미 시작했을 수 있음)
        if (bgmPlayer != null && bgmPlayer.Volume > 0.05f)
            bgmPlayer.FadeToVolume(0f, praiseBgmFadeDuration);

        // 대사가 빨리 출력되고 끝난 뒤 외부 대기를 걸 때까지 기다림
        while (dialogueManager != null && !dialogueManager.IsWaitingForExternalEvent)
            yield return null;

        yield return new WaitForSecondsRealtime(0.25f);

        EnsureFadeOverlay();
        yield return FadeCoroutine(0f, 1f);

        yield return BeginRoomPart();

        yield return FadeCoroutine(1f, 0f);
        _handlingPraiseFade = false;
    }

    private IEnumerator FadeThenPlayRoomCoroutine()
    {
        if (_handlingPraiseFade || _playingRoomPart)
            yield break;

        _handlingPraiseFade = true;
        EnsureFadeOverlay();
        yield return FadeCoroutine(0f, 1f);
        yield return BeginRoomPart();
        yield return FadeCoroutine(1f, 0f);
        _handlingPraiseFade = false;
    }

    private IEnumerator BeginRoomPart()
    {
        _playingRoomPart = true;

        if (backgroundImage != null && roomBackground != null)
            backgroundImage.sprite = roomBackground;

        if (bgmPlayer != null && roomBgm != null)
            bgmPlayer.Play(roomBgm, 1f);

        if (dialogueManager != null)
        {
            dialogueManager.EndRushPresentation();
            dialogueManager.onDialogueFinished.RemoveListener(OnNarrationFinished);
            dialogueManager.StartDialogue(prologueData.roomLines);
            dialogueManager.onDialogueFinished.AddListener(OnNarrationFinished);
        }

        yield return null;
    }

    private IEnumerator FadeCoroutine(float from, float to)
    {
        if (_fadeGroup == null)
            yield break;

        float elapsed = 0f;
        _fadeGroup.blocksRaycasts = true;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            _fadeGroup.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / fadeDuration));
            yield return null;
        }

        _fadeGroup.alpha = to;
        _fadeGroup.blocksRaycasts = to > 0.01f;
    }

    private void GoToStageSelect()
    {
        if (_isTransitioning)
            return;

        _isTransitioning = true;

        if (GameFlowManager.EnsureExists() != null)
            GameFlowManager.Instance.GoToStageSelect();
        else
            SceneManager.LoadScene(SceneNames.StageSelect);
    }

    private void ResolveBackgroundImage()
    {
        if (backgroundImage != null)
            return;

        var bgObject = GameObject.Find("bg");
        if (bgObject != null)
            backgroundImage = bgObject.GetComponent<Image>();
    }

    private void ResolveAudioClips()
    {
        if (prologueBgm == null)
            prologueBgm = Resources.Load<AudioClip>("BGM/프롤로그씬 브금");

        if (roomBgm == null)
            roomBgm = Resources.Load<AudioClip>("BGM/Devil in Mary Janes_1");

        // Resources에 없으면 경로 기반 로드는 런타임에서 불가하므로 씬 직렬화 값을 권장.
        // 에디터/빌드에서 참조가 비어 있을 때를 위한 Soft 폴백: StageBgmPlayer clip을 프롤로그로 사용.
        if (prologueBgm == null && bgmPlayer != null && bgmPlayer.Source != null)
            prologueBgm = bgmPlayer.Source.clip;
    }

    private void EnsureFadeOverlay()
    {
        if (_fadeGroup != null)
            return;

        var canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
            return;

        var fadeObject = new GameObject("PrologueFadeOverlay");
        fadeObject.transform.SetParent(canvas.transform, false);
        fadeObject.transform.SetAsLastSibling();

        var image = fadeObject.AddComponent<Image>();
        image.color = Color.black;

        var rect = fadeObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        _fadeGroup = fadeObject.AddComponent<CanvasGroup>();
        _fadeGroup.alpha = 0f;
        _fadeGroup.blocksRaycasts = false;
    }

    private void EnsureNamePrompt()
    {
        if (_namePromptRoot != null)
            return;

        var canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
            return;

        _namePromptRoot = new GameObject("PlayerNamePrompt");
        _namePromptRoot.transform.SetParent(canvas.transform, false);
        _namePromptRoot.transform.SetAsLastSibling();

        var panel = _namePromptRoot.AddComponent<Image>();
        panel.color = new Color(0f, 0f, 0f, 0.75f);

        var panelRect = _namePromptRoot.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.2f, 0.35f);
        panelRect.anchorMax = new Vector2(0.8f, 0.65f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        var titleObject = CreateText(_namePromptRoot.transform, "Title", "신부의 이름을 입력하세요", 28);
        var titleRect = titleObject.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.1f, 0.65f);
        titleRect.anchorMax = new Vector2(0.9f, 0.9f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;

        var inputObject = new GameObject("NameInput");
        inputObject.transform.SetParent(_namePromptRoot.transform, false);
        var inputImage = inputObject.AddComponent<Image>();
        inputImage.color = Color.white;

        var inputRect = inputObject.GetComponent<RectTransform>();
        inputRect.anchorMin = new Vector2(0.15f, 0.35f);
        inputRect.anchorMax = new Vector2(0.85f, 0.58f);
        inputRect.offsetMin = Vector2.zero;
        inputRect.offsetMax = Vector2.zero;

        _nameInput = inputObject.AddComponent<TMP_InputField>();
        var textArea = new GameObject("Text Area");
        textArea.transform.SetParent(inputObject.transform, false);
        var textAreaRect = textArea.AddComponent<RectTransform>();
        textAreaRect.anchorMin = Vector2.zero;
        textAreaRect.anchorMax = Vector2.one;
        textAreaRect.offsetMin = new Vector2(10f, 6f);
        textAreaRect.offsetMax = new Vector2(-10f, -6f);

        var textObject = CreateText(textArea.transform, "Text", string.Empty, 26);
        textObject.color = Color.black;
        textObject.alignment = TextAlignmentOptions.MidlineLeft;

        var placeholder = CreateText(textArea.transform, "Placeholder", "이름", 26);
        placeholder.fontStyle = FontStyles.Italic;
        placeholder.color = new Color(0.3f, 0.3f, 0.3f, 0.7f);

        _nameInput.textViewport = textAreaRect;
        _nameInput.textComponent = textObject;
        _nameInput.placeholder = placeholder;
        _nameInput.characterLimit = 12;
        _nameInput.lineType = TMP_InputField.LineType.SingleLine;

        var confirmObject = new GameObject("ConfirmButton");
        confirmObject.transform.SetParent(_namePromptRoot.transform, false);
        var confirmImage = confirmObject.AddComponent<Image>();
        confirmImage.color = new Color(0.35f, 0.2f, 0.45f, 1f);
        var confirmButton = confirmObject.AddComponent<Button>();
        confirmButton.onClick.AddListener(() => _nameSubmitted = true);

        var confirmRect = confirmObject.GetComponent<RectTransform>();
        confirmRect.anchorMin = new Vector2(0.35f, 0.1f);
        confirmRect.anchorMax = new Vector2(0.65f, 0.28f);
        confirmRect.offsetMin = Vector2.zero;
        confirmRect.offsetMax = Vector2.zero;

        var confirmLabel = CreateText(confirmObject.transform, "Label", "확인", 24);
        confirmLabel.alignment = TextAlignmentOptions.Center;

        _namePromptRoot.SetActive(false);
    }

    private static TextMeshProUGUI CreateText(Transform parent, string name, string content, float size)
    {
        var textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);
        var text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = content;
        text.fontSize = size;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;

        if (TMP_Settings.defaultFontAsset != null)
            text.font = TMP_Settings.defaultFontAsset;

        var rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return text;
    }
}
