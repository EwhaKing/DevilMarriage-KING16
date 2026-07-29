using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Audio;

/// <summary>
/// ScriptableObject 대사를 읽어 화면에 출력하는 공용 대화 관리자입니다.
/// </summary>
public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("대사 UI (비어 있으면 씬의 DialogueCanvas에서 찾습니다)")]
    [SerializeField] private TextMeshProUGUI speakerNameText;
    [SerializeField] private TextMeshProUGUI dialogueBodyText;
    [SerializeField] private Image characterImage;
    [SerializeField] private Image storyImage;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private GameObject continueIcon;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button autoButton;
    [SerializeField] private Button skipButton;
    [SerializeField] private Button logButton;
    [SerializeField] private Button settingButton;

    [Header("런타임 생성 (비권장)")]
    [SerializeField] private bool createUiAtRuntimeIfMissing = true;

    [Header("캐릭터 위치")]
    [SerializeField] private bool adjustCharacterPositionFromData = false;

    [Header("표정 스프라이트 (expressionId 키)")]
    [SerializeField] private Sprite portraitDefault;
    [SerializeField] private Sprite portraitHappy;
    [SerializeField] private Sprite portraitNervous;
    [SerializeField] private ExpressionSprite[] expressionSprites;

    [System.Serializable]
    public class ExpressionSprite
    {
        public string id;
        public Sprite sprite;
    }

    [Header("타이핑")]
    [SerializeField] private float typingSpeed = 0.03f;
    [SerializeField] private float autoDelayAfterLine = 1.2f;

    [Header("효과음")]
    [SerializeField] private AudioClip advanceSfxClip;
    [SerializeField] private AudioMixerGroup sfxMixerGroup;
    [SerializeField] [Range(0f, 1f)] private float advanceSfxVolume = 1f;

    [Header("한글 폰트")]
    [SerializeField] private TMP_FontAsset dialogueFont;

    [Header("설정 팝업 (기존 SoundSettings 연결)")]
    [SerializeField] private GameObject settingPopup;

    [Header("이벤트")]
    public UnityEvent onDialogueFinished = new UnityEvent();

    public event Action<string> OnCustomEvent;

    private readonly List<DialogueLine> _lines = new();
    private readonly List<string> _logEntries = new();
    private readonly Dictionary<string, Sprite> _expressionMap = new(StringComparer.OrdinalIgnoreCase);

    private int _index;
    private bool _isPlaying;
    private bool _isTyping;
    private bool _lineFullyShown;
    private bool _autoEnabled;
    private bool _skipRequested;
    private bool _waitingForExternalEvent;
    private Coroutine _typingCoroutine;
    private Coroutine _autoCoroutine;
    private Coroutine _rushCoroutine;
    private GameObject _logPanelRoot;
    private TextMeshProUGUI _logBodyText;
    private string _fullLineText = "";
    private float _currentTypingSpeed;
    private float _currentAutoDelay;
    private bool _rushThenFadePending;
    private bool _rushModeActive;
    private bool _protagonistRevealed;

    private AudioSource _sfxSource;

    public bool IsWaitingForExternalEvent => _waitingForExternalEvent;
    public bool IsLineFullyShown => _lineFullyShown && !_isTyping;
    public Sprite PortraitDefault => portraitDefault;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void DisableYarnRunnersOnSceneLoad()
    {
        DisableYarnComponentsStatic();
    }

    private void Awake()
    {
        Instance = this;

        if (onDialogueFinished == null)
            onDialogueFinished = new UnityEvent();

        TMP_Settings.useModernHangulLineBreakingRules = true;

        ResolveDialogueFont();
        BuildExpressionMap();
        TryBindExistingUi();
        EnsureUiExists();
        RescueBackgroundAndCharacterFromYarn();
        ApplyFontToBoundTexts();
        WireUiButtons();
        DisableYarnComponentsIfAny();

        InitAudioSource();

        _currentTypingSpeed = typingSpeed;
        _currentAutoDelay = autoDelayAfterLine;

        if (continueIcon != null)
            SetNextButtonVisible(false);

        SetNextButtonVisible(false);

        if (settingPopup == null)
            settingPopup = GameObject.Find("SettingPopup");

        if (settingPopup != null)
            settingPopup.SetActive(false);
    }

    private void InitAudioSource()
    {
        _sfxSource = gameObject.GetComponent<AudioSource>();
        if (_sfxSource == null)
            _sfxSource = gameObject.AddComponent<AudioSource>();

        _sfxSource.playOnAwake = false;
        _sfxSource.loop = false;
        if (sfxMixerGroup != null)
            _sfxSource.outputAudioMixerGroup = sfxMixerGroup;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (!_isPlaying || _waitingForExternalEvent)
            return;

        var keyboard = Keyboard.current;
        if (keyboard != null &&
            (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame))
        {
            HandleAdvanceInput();
            return;
        }

        var mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            HandleAdvanceInput();
        }
    }

    private void PlayAdvanceSfx()
    {
        if (advanceSfxClip == null || _sfxSource == null)
            return;

        _sfxSource.PlayOneShot(advanceSfxClip, advanceSfxVolume);
    }

    private void HandleAdvanceInput()
    {
        // 로그/설정이 열려 있으면 입력 무시
        if ((_logPanelRoot != null && _logPanelRoot.activeSelf) ||
            (settingPopup != null && settingPopup.activeSelf))
            return;

        // 대사 넘김 / 즉시 완성을 수행할 때 효과음 재생
        PlayAdvanceSfx();

        if (_isTyping)
        {
            CompleteTypingImmediately();
            return;
        }

        if (_lineFullyShown)
            AdvanceToNextLine();
    }

    public void StartStageDialogue(StageDialogueData data, bool isOpening)
    {
        if (data == null)
        {
            Debug.LogError("[DialogueManager] StageDialogueData가 없습니다.");
            FinishDialogue();
            return;
        }

        StartDialogue(data.GetLines(isOpening));
    }

    public void StartDialogue(IReadOnlyList<DialogueLine> lines)
    {
        StopAllDialogueCoroutines();

        _lines.Clear();
        if (lines != null)
        {
            foreach (var line in lines)
            {
                if (line != null)
                    _lines.Add(line);
            }
        }

        _index = 0;
        _isPlaying = true;
        _skipRequested = false;
        _waitingForExternalEvent = false;
        _rushThenFadePending = false;
        _rushModeActive = false;
        _autoEnabled = false;
        _protagonistRevealed = false;
        _currentTypingSpeed = typingSpeed;
        _currentAutoDelay = autoDelayAfterLine;
        _logEntries.Clear();

        ResolveStoryImage();
        ApplyProtagonistPresentation(false);

        if (_rushCoroutine != null)
        {
            StopCoroutine(_rushCoroutine);
            _rushCoroutine = null;
        }

        if (_lines.Count == 0)
        {
            FinishDialogue();
            return;
        }

        SetNextButtonVisible(true);
        ShowCurrentLine();
    }

    public void BeginRushPresentation(float targetTypingSpeed = 0.004f, float targetAutoDelay = 0.12f, float rampDuration = 2.2f)
    {
        _rushModeActive = true;
        _autoEnabled = true;

        if (_rushCoroutine != null)
            StopCoroutine(_rushCoroutine);

        _rushCoroutine = StartCoroutine(RushRampCoroutine(targetTypingSpeed, targetAutoDelay, rampDuration));
    }

    public void EndRushPresentation()
    {
        _rushModeActive = false;
        _rushThenFadePending = false;
        _autoEnabled = false;
        _currentTypingSpeed = typingSpeed;
        _currentAutoDelay = autoDelayAfterLine;

        if (_rushCoroutine != null)
        {
            StopCoroutine(_rushCoroutine);
            _rushCoroutine = null;
        }

        if (_autoCoroutine != null)
        {
            StopCoroutine(_autoCoroutine);
            _autoCoroutine = null;
        }
    }

    public void NotifyExternalEventCompleted()
    {
        _waitingForExternalEvent = false;
        SetNextButtonVisible(true);
        AdvanceToNextLine();
    }

    public void SetPortraitSprites(Sprite defaultSprite, Sprite happy, Sprite nervous)
    {
        portraitDefault = defaultSprite;
        portraitHappy = happy;
        portraitNervous = nervous;
        BuildExpressionMap();
    }

    public void SetSettingPopup(GameObject popup)
    {
        if (popup != null)
            settingPopup = popup;
    }

    private void ShowCurrentLine()
    {
        if (_index < 0 || _index >= _lines.Count)
        {
            FinishDialogue();
            return;
        }

        var line = _lines[_index];
        ApplyVisuals(line);

        _fullLineText = FormatText(line.dialogueText);
        _logEntries.Add($"{FormatSpeakerName(line.speakerName)}: {_fullLineText}");

        if (string.IsNullOrWhiteSpace(_fullLineText))
        {
            if (!string.IsNullOrWhiteSpace(line.eventId))
            {
                HandleEvent(line.eventId);
                if (!_waitingForExternalEvent && _isPlaying)
                    AdvanceToNextLine();
            }
            else
            {
                AdvanceToNextLine();
            }

            return;
        }

        if (_skipRequested)
        {
            dialogueBodyText.text = _fullLineText;
            _isTyping = false;
            _lineFullyShown = true;
            if (!string.IsNullOrWhiteSpace(line.eventId))
                HandleEvent(line.eventId);
            else if (_autoEnabled)
                RestartAutoTimer();
            return;
        }

        if (_typingCoroutine != null)
            StopCoroutine(_typingCoroutine);
        _typingCoroutine = StartCoroutine(TypeLineCoroutine(line));
    }

    private IEnumerator TypeLineCoroutine(DialogueLine line)
    {
        _isTyping = true;
        _lineFullyShown = false;
        dialogueBodyText.text = "";

        if (!string.IsNullOrWhiteSpace(line.eventId))
        {
            HandleEvent(line.eventId);
            if (_waitingForExternalEvent)
            {
                _isTyping = false;
                yield break;
            }
        }

        var builder = new StringBuilder();
        for (int i = 0; i < _fullLineText.Length; i++)
        {
            if (_skipRequested)
            {
                dialogueBodyText.text = _fullLineText;
                break;
            }

            char c = _fullLineText[i];
            builder.Append(c);
            dialogueBodyText.text = builder.ToString();
            yield return new WaitForSecondsRealtime(_currentTypingSpeed);
        }

        dialogueBodyText.text = _fullLineText;
        _isTyping = false;
        _lineFullyShown = true;
        _typingCoroutine = null;

        if (_rushThenFadePending)
        {
            _waitingForExternalEvent = true;
            SetNextButtonVisible(false);
            yield break;
        }

        if (_autoEnabled)
            RestartAutoTimer();
    }

    private void CompleteTypingImmediately()
    {
        if (_typingCoroutine != null)
        {
            StopCoroutine(_typingCoroutine);
            _typingCoroutine = null;
        }

        dialogueBodyText.text = _fullLineText;
        _isTyping = false;
        _lineFullyShown = true;

        if (_rushThenFadePending)
        {
            _waitingForExternalEvent = true;
            SetNextButtonVisible(false);
            return;
        }

        if (_autoEnabled)
            RestartAutoTimer();
    }

    private void AdvanceToNextLine()
    {
        if (_autoCoroutine != null)
        {
            StopCoroutine(_autoCoroutine);
            _autoCoroutine = null;
        }

        _index++;
        if (_index >= _lines.Count)
        {
            FinishDialogue();
            return;
        }

        ShowCurrentLine();
    }

    private void FinishDialogue()
    {
        _isPlaying = false;
        _isTyping = false;
        _lineFullyShown = false;
        _skipRequested = false;
        StopAllDialogueCoroutines();

        SetNextButtonVisible(false);

        onDialogueFinished?.Invoke();
    }

    private void HandleEvent(string eventId)
    {
        switch (eventId)
        {
            case "RequestPlayerName":
            case "FadeToBlack":
            case "GoToStageSelect":
                _waitingForExternalEvent = true;
                SetNextButtonVisible(false);
                OnCustomEvent?.Invoke(eventId);
                break;

            case "AcceleratePraise":
                BeginRushPresentation();
                OnCustomEvent?.Invoke(eventId);
                break;

            case "AcceleratePraiseThenFade":
                if (!_rushModeActive)
                    BeginRushPresentation();
                _rushThenFadePending = true;
                OnCustomEvent?.Invoke(eventId);
                break;

            default:
                OnCustomEvent?.Invoke(eventId);
                break;
        }
    }

    private void ApplyVisuals(DialogueLine line)
    {
        string rawSpeaker = line.speakerName ?? "";
        string displaySpeaker = FormatSpeakerName(rawSpeaker);

        if (speakerNameText != null)
            speakerNameText.text = displaySpeaker;

        if (IsProtagonistSpeaker(rawSpeaker) || IsProtagonistSpeaker(displaySpeaker))
            RevealProtagonist();
        else if (!_protagonistRevealed)
            ApplyProtagonistPresentation(false);

        if (backgroundImage != null && line.backgroundImage != null)
            backgroundImage.sprite = line.backgroundImage;

        if (characterImage != null && _protagonistRevealed)
        {
            Sprite sprite = line.characterSprite;
            if (sprite == null && !string.IsNullOrWhiteSpace(line.expressionId))
                _expressionMap.TryGetValue(line.expressionId, out sprite);

            if (sprite != null)
            {
                characterImage.sprite = sprite;
                characterImage.enabled = true;
            }

            if (adjustCharacterPositionFromData)
                ApplyCharacterPosition(line.characterPosition);
        }
    }

    private void ApplyCharacterPosition(CharacterPosition position)
    {
        if (characterImage == null)
            return;

        var rect = characterImage.rectTransform;
        switch (position)
        {
            case CharacterPosition.Left:
                rect.anchorMin = new Vector2(0.15f, 0.5f);
                rect.anchorMax = new Vector2(0.15f, 0.5f);
                break;
            case CharacterPosition.Right:
                rect.anchorMin = new Vector2(0.85f, 0.5f);
                rect.anchorMax = new Vector2(0.85f, 0.5f);
                break;
            default:
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                break;
        }

        rect.anchoredPosition = Vector2.zero;
    }

    private string FormatText(string raw)
    {
        if (string.IsNullOrEmpty(raw))
            return "";

        var name = PlayerNameManager.PlayerName;
        var formatted = raw
            .Replace("{$playerName}", name)
            .Replace("{playerName}", name)
            .Replace("[주인공]", name);

        return formatted.Replace("\u2026", "...");
    }

    private static string FormatSpeakerName(string speaker)
    {
        if (string.IsNullOrWhiteSpace(speaker))
            return "";

        if (IsProtagonistSpeaker(speaker))
            return PlayerNameManager.PlayerName;

        return speaker;
    }

    private static bool IsProtagonistSpeaker(string speaker)
    {
        if (string.IsNullOrWhiteSpace(speaker))
            return false;

        if (speaker == "주인공")
            return true;

        return PlayerNameManager.HasCustomName &&
               string.Equals(speaker, PlayerNameManager.PlayerName, System.StringComparison.Ordinal);
    }

    private void RevealProtagonist()
    {
        _protagonistRevealed = true;
        ApplyProtagonistPresentation(true);
    }

    private void ApplyProtagonistPresentation(bool showProtagonist)
    {
        ResolveStoryImage();

        if (storyImage != null)
        {
            storyImage.gameObject.SetActive(!showProtagonist);
            storyImage.enabled = !showProtagonist;
        }

        if (characterImage != null)
        {
            characterImage.gameObject.SetActive(showProtagonist);
            if (showProtagonist)
                characterImage.enabled = true;
        }
    }

    private void ResolveStoryImage()
    {
        if (storyImage != null)
            return;

        var storyObject = GameObject.Find("StorySprite");
        if (storyObject != null)
            storyImage = storyObject.GetComponent<Image>();
    }

    private IEnumerator RushRampCoroutine(float targetTypingSpeed, float targetAutoDelay, float rampDuration)
    {
        float startTyping = _currentTypingSpeed;
        float startAuto = _currentAutoDelay;
        float elapsed = 0f;

        while (elapsed < rampDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, rampDuration));
            t = t * t;
            _currentTypingSpeed = Mathf.Lerp(startTyping, targetTypingSpeed, t);
            _currentAutoDelay = Mathf.Lerp(startAuto, targetAutoDelay, t);
            yield return null;
        }

        _currentTypingSpeed = targetTypingSpeed;
        _currentAutoDelay = targetAutoDelay;
        _rushCoroutine = null;
    }

    private void RestartAutoTimer()
    {
        if (_autoCoroutine != null)
            StopCoroutine(_autoCoroutine);
        _autoCoroutine = StartCoroutine(AutoAdvanceCoroutine());
    }

    private IEnumerator AutoAdvanceCoroutine()
    {
        yield return new WaitForSecondsRealtime(_currentAutoDelay);
        if (_autoEnabled && _lineFullyShown && !_waitingForExternalEvent)
            AdvanceToNextLine();
    }

    private void ToggleAuto()
    {
        _autoEnabled = !_autoEnabled;
        if (_autoEnabled && _lineFullyShown && !_isTyping)
            RestartAutoTimer();
        else if (!_autoEnabled && _autoCoroutine != null)
        {
            StopCoroutine(_autoCoroutine);
            _autoCoroutine = null;
        }
    }

    private void TriggerSkip()
    {
        _skipRequested = true;
        _autoEnabled = false;

        if (_isTyping)
            CompleteTypingImmediately();

        StartCoroutine(SkipRemainingCoroutine());
    }

    private IEnumerator SkipRemainingCoroutine()
    {
        while (_isPlaying && _index < _lines.Count)
        {
            if (_waitingForExternalEvent)
                yield break;

            if (!_lineFullyShown)
            {
                CompleteTypingImmediately();
                yield return null;
            }

            var line = _lines[_index];
            if (!string.IsNullOrWhiteSpace(line.eventId) &&
                (line.eventId == "RequestPlayerName" || line.eventId == "FadeToBlack" ||
                 line.eventId == "AcceleratePraise" || line.eventId == "AcceleratePraiseThenFade"))
            {
                HandleEvent(line.eventId);
                if (line.eventId == "AcceleratePraiseThenFade")
                {
                    _waitingForExternalEvent = true;
                    SetNextButtonVisible(false);
                }
                yield break;
            }

            AdvanceToNextLine();
            yield return null;
        }
    }

    private void ToggleLog()
    {
        EnsureLogPanel();
        bool show = !_logPanelRoot.activeSelf;
        _logPanelRoot.SetActive(show);

        if (show && _logBodyText != null)
        {
            var sb = new StringBuilder();
            foreach (var entry in _logEntries)
                sb.AppendLine(entry);
            _logBodyText.text = sb.ToString();
        }
    }

    private void OpenSettings()
    {
        if (settingPopup != null)
            settingPopup.SetActive(true);
        else
        {
            var sound = FindAnyObjectByType<SoundSettings>();
            if (sound != null)
                Debug.Log("[DialogueManager] SettingPopup이 연결되지 않았습니다. SoundSettings만 씬에 있습니다.");
        }
    }

    private void WireUiButtons()
    {
        if (nextButton != null)
        {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(OnNextButtonClicked);
        }

        if (autoButton != null)
        {
            autoButton.onClick.RemoveAllListeners();
            autoButton.onClick.AddListener(ToggleAuto);
        }

        if (skipButton != null)
        {
            skipButton.onClick.RemoveAllListeners();
            skipButton.onClick.AddListener(TriggerSkip);
        }

        if (logButton != null)
        {
            logButton.onClick.RemoveAllListeners();
            logButton.onClick.AddListener(ToggleLog);
        }

        if (settingButton != null)
        {
            settingButton.onClick.RemoveAllListeners();
            settingButton.onClick.AddListener(OpenSettings);
        }
    }

    private void OnNextButtonClicked()
    {
        if (!_isPlaying || _waitingForExternalEvent)
            return;

        HandleAdvanceInput();
    }

    private void SetNextButtonVisible(bool visible)
    {
        if (nextButton != null)
            nextButton.gameObject.SetActive(visible);
        else if (continueIcon != null)
            continueIcon.SetActive(visible);
    }

    private void BuildExpressionMap()
    {
        _expressionMap.Clear();
        if (portraitDefault != null)
        {
            _expressionMap["default"] = portraitDefault;
            _expressionMap["plain"] = portraitDefault;
            _expressionMap["normal"] = portraitDefault;
        }

        if (portraitHappy != null)
            _expressionMap["happy"] = portraitHappy;

        if (portraitNervous != null)
        {
            _expressionMap["nervous"] = portraitNervous;
            _expressionMap["upset"] = portraitNervous;
        }

        if (expressionSprites == null)
            return;

        foreach (var entry in expressionSprites)
        {
            if (entry == null || entry.sprite == null || string.IsNullOrWhiteSpace(entry.id))
                continue;
            _expressionMap[entry.id.Trim()] = entry.sprite;
        }
    }

    private void DisableYarnComponentsIfAny()
    {
        DisableYarnComponentsStatic();
    }

    private static void DisableYarnComponentsStatic()
    {
        var yarnRoot = GameObject.Find("Dialogue System");
        if (yarnRoot != null && yarnRoot.activeSelf)
        {
            yarnRoot.SetActive(false);
            Debug.Log("[DialogueManager] 예전 Yarn Dialogue System을 비활성화했습니다.");
        }

        var behaviours = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var mb in behaviours)
        {
            if (mb == null)
                continue;

            var typeName = mb.GetType().FullName;
            if (typeName != null && typeName.Contains("Yarn.Unity.DialogueRunner"))
                mb.enabled = false;
        }
    }

    private void TryBindExistingUi()
    {
        if (backgroundImage == null)
        {
            var bg = GameObject.Find("bg");
            if (bg != null)
                backgroundImage = bg.GetComponent<Image>();
        }

        if (characterImage == null)
        {
            var ch = GameObject.Find("CharacterSprite");
            if (ch != null)
                characterImage = ch.GetComponent<Image>();
        }

        if (storyImage == null)
        {
            var story = GameObject.Find("StorySprite");
            if (story != null)
                storyImage = story.GetComponent<Image>();
        }

        if (speakerNameText == null || dialogueBodyText == null)
        {
            var tmps = FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None);
            foreach (var tmp in tmps)
            {
                if (tmp == null || !tmp.gameObject.activeInHierarchy)
                    continue;

                if (IsUnderYarnDialogueSystem(tmp.transform))
                    continue;

                var n = tmp.gameObject.name;
                if (speakerNameText == null && (n.Contains("Character Name") || n.Contains("Speaker") || n == "SpeakerName"))
                    speakerNameText = tmp;
                else if (dialogueBodyText == null && (n == "DialogueBody" || n.Contains("DialogueBody")))
                    dialogueBodyText = tmp;
            }
        }
    }

    private void RescueBackgroundAndCharacterFromYarn()
    {
        var canvasGo = GameObject.Find(DialogueUiBuilder.CanvasName);
        if (canvasGo == null)
            return;

        var canvas = canvasGo.transform;

        if (backgroundImage == null)
        {
            var bg = GameObject.Find("bg");
            if (bg != null)
                backgroundImage = bg.GetComponent<Image>();
        }

        if (characterImage == null)
        {
            var ch = GameObject.Find("CharacterSprite");
            if (ch != null)
                characterImage = ch.GetComponent<Image>();
        }

        if (backgroundImage == null || characterImage == null)
        {
            var images = FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var img in images)
            {
                if (img == null)
                    continue;
                if (backgroundImage == null && img.gameObject.name == "bg")
                    backgroundImage = img;
                if (characterImage == null && img.gameObject.name == "CharacterSprite")
                    characterImage = img;
            }
        }

        MoveUnderCanvasIfNeeded(backgroundImage != null ? backgroundImage.gameObject : null, canvas, 0);
        MoveUnderCanvasIfNeeded(characterImage != null ? characterImage.gameObject : null, canvas, 1);

        if (portraitDefault == null || portraitHappy == null || portraitNervous == null)
        {
            var changer = FindAnyObjectByType<SceneChanger>();
            if (changer != null)
                SetPortraitSprites(changer.PortraitDefault, changer.PortraitHappy, changer.PortraitNervous);
        }
    }

    private static void MoveUnderCanvasIfNeeded(GameObject go, Transform canvas, int siblingIndex)
    {
        if (go == null || canvas == null)
            return;

        bool underYarn = false;
        var t = go.transform;
        while (t != null)
        {
            if (t.name == "Dialogue System")
            {
                underYarn = true;
                break;
            }

            t = t.parent;
        }

        if (!underYarn && go.transform.parent == canvas)
        {
            go.SetActive(true);
            return;
        }

        if (underYarn || go.transform.parent != canvas)
        {
            go.transform.SetParent(canvas, true);
            go.SetActive(true);
            go.transform.SetSiblingIndex(Mathf.Clamp(siblingIndex, 0, canvas.childCount - 1));

            var img = go.GetComponent<Image>();
            if (img != null)
                img.raycastTarget = false;
        }
    }

    private static bool IsUnderYarnDialogueSystem(Transform t)
    {
        while (t != null)
        {
            var n = t.name;
            if (n.Contains("Dialogue System") || n.Contains("Line Presenter") || n.Contains("Dialogue Runner"))
                return true;
            t = t.parent;
        }

        return false;
    }

    private void EnsureUiExists()
    {
        if (speakerNameText != null && dialogueBodyText != null)
        {
            if (nextButton != null)
                continueIcon = nextButton.gameObject;
            return;
        }

        if (DialogueUiBuilder.TryFindExisting(out var found))
        {
            ApplyUiResult(found);
            return;
        }

        if (!createUiAtRuntimeIfMissing)
        {
            Debug.LogError("[DialogueManager] DialogueCanvas가 없습니다.");
            return;
        }

        var built = DialogueUiBuilder.BuildNew(dialogueFont, forceReplace: false);
        if (built == null)
        {
            Debug.LogError("[DialogueManager] Dialogue UI 생성에 실패했습니다.");
            return;
        }

        ApplyUiResult(built.Value);
        SetNextButtonVisible(false);
    }

    private void ApplyUiResult(DialogueUiBuilder.Result result)
    {
        speakerNameText = result.speakerNameText;
        dialogueBodyText = result.dialogueBodyText;
        nextButton = result.nextButton;
        autoButton = result.autoButton;
        skipButton = result.skipButton;
        logButton = result.logButton;
        settingButton = result.settingButton;
        if (nextButton != null)
            continueIcon = nextButton.gameObject;

        if (result.logPanelRoot != null)
            _logPanelRoot = result.logPanelRoot;
        if (result.logBodyText != null)
            _logBodyText = result.logBodyText;
    }

    private void EnsureLogPanel()
    {
        if (_logPanelRoot != null)
            return;

        if (DialogueUiBuilder.TryFindExisting(out var found) && found.logPanelRoot != null)
        {
            _logPanelRoot = found.logPanelRoot;
            _logBodyText = found.logBodyText;
            return;
        }

        var canvas = GameObject.Find(DialogueUiBuilder.CanvasName)?.GetComponent<Canvas>();
        if (canvas == null)
            canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
            return;

        _logPanelRoot = new GameObject("DialogueLogPanel");
        _logPanelRoot.transform.SetParent(canvas.transform, false);
        var img = _logPanelRoot.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.85f);
        var rect = _logPanelRoot.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.15f, 0.15f);
        rect.anchorMax = new Vector2(0.85f, 0.85f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        _logBodyText = DialogueUiBuilder.CreateTmp(_logPanelRoot.transform, "LogBody", 22, TextAlignmentOptions.TopLeft, dialogueFont);
        var bodyRect = _logBodyText.rectTransform;
        bodyRect.anchorMin = new Vector2(0.05f, 0.15f);
        bodyRect.anchorMax = new Vector2(0.95f, 0.95f);
        bodyRect.offsetMin = Vector2.zero;
        bodyRect.offsetMax = Vector2.zero;

        var realClose = DialogueUiBuilder.CreateUiButton(_logPanelRoot.transform, "CloseLog", "Close",
            new Vector2(0.4f, 0.02f), new Vector2(0.6f, 0.12f), dialogueFont);
        realClose.onClick.AddListener(() => _logPanelRoot.SetActive(false));
        _logPanelRoot.SetActive(false);
    }

    private void ResolveDialogueFont()
    {
        if (dialogueFont != null)
            return;

        if (TMP_Settings.defaultFontAsset != null &&
            (TMP_Settings.defaultFontAsset.name.Contains("Noto") ||
             TMP_Settings.defaultFontAsset.name.Contains("KR")))
        {
            dialogueFont = TMP_Settings.defaultFontAsset;
            return;
        }

        dialogueFont = Resources.Load<TMP_FontAsset>("Fonts/NotoSerifKR-Regular SDF");
        if (dialogueFont != null)
            return;

        var tmps = FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var tmp in tmps)
        {
            if (tmp == null || tmp.font == null)
                continue;

            var fontName = tmp.font.name;
            if (fontName.Contains("Noto") || fontName.Contains("KR") || fontName.Contains("Korean"))
            {
                dialogueFont = tmp.font;
                return;
            }
        }

        dialogueFont = TMP_Settings.defaultFontAsset;
    }

    private void ApplyFontToBoundTexts()
    {
        if (dialogueFont == null)
            return;

        if (speakerNameText != null)
            speakerNameText.font = dialogueFont;

        if (dialogueBodyText != null)
        {
            dialogueBodyText.font = dialogueFont;
            dialogueBodyText.enableAutoSizing = false;
            dialogueBodyText.textWrappingMode = TextWrappingModes.Normal;
            dialogueBodyText.overflowMode = TextOverflowModes.Overflow;
        }
    }

    private void StopAllDialogueCoroutines()
    {
        if (_typingCoroutine != null)
        {
            StopCoroutine(_typingCoroutine);
            _typingCoroutine = null;
        }

        if (_autoCoroutine != null)
        {
            StopCoroutine(_autoCoroutine);
            _autoCoroutine = null;
        }
    }
}