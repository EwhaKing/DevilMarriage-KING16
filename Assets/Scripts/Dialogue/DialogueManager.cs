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

/// <summary>
/// ScriptableObject 대사를 읽어 화면에 출력하는 공용 대화 관리자입니다.
/// StoryScene / PrologueScene 등에서 같은 컴포넌트를 재사용합니다.
/// </summary>
public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("대사 UI (비어 있으면 씬의 DialogueCanvas에서 찾습니다)")]
    [Tooltip("Play 전에 DevilMarriage/Add Dialogue UI To Open Scene 으로 배치해 두고 위치를 조절하세요.")]
    [SerializeField] private TextMeshProUGUI speakerNameText;
    [SerializeField] private TextMeshProUGUI dialogueBodyText;
    [SerializeField] private Image characterImage;
    [Tooltip("주인공 등장 전 스토리/나레이션용 이미지. 없으면 CharacterSprite만 사용합니다.")]
    [SerializeField] private Image storyImage;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private GameObject continueIcon;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button autoButton;
    [SerializeField] private Button skipButton;
    [SerializeField] private Button logButton;
    [SerializeField] private Button settingButton;

    [Header("런타임 생성 (비권장)")]
    [Tooltip("씬에 DialogueCanvas가 없을 때만 Play 중 자동 생성합니다. 에디터에서 조절하려면 끄고 메뉴로 UI를 배치하세요.")]
    [SerializeField] private bool createUiAtRuntimeIfMissing = true;

    [Header("캐릭터 위치")]
    [Tooltip("켜면 DialogueLine의 Left/Center/Right로 캐릭터 위치를 바꿉니다. 씬에서 배치한 위치를 유지하려면 끄세요.")]
    [SerializeField] private bool adjustCharacterPositionFromData = false;

    [Header("표정 스프라이트 (expressionId 키)")]
    [SerializeField] private Sprite portraitDefault;
    [SerializeField] private Sprite portraitHappy;
    [SerializeField] private Sprite portraitNervous;
    [Tooltip("추가 표정. id 예: wake, dark, angry, cry, sparkle, scheming")]
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

    [Header("한글 폰트")]
    [Tooltip("비우면 씬의 Noto 폰트 또는 Resources/Fonts 에서 자동으로 찾습니다.")]
    [SerializeField] private TMP_FontAsset dialogueFont;

    [Header("설정 팝업 (기존 SoundSettings 연결)")]
    [SerializeField] private GameObject settingPopup;

    [Header("이벤트")]
    [Tooltip("대화가 모두 끝났을 때 호출됩니다.")]
    public UnityEvent onDialogueFinished = new UnityEvent();

    /// <summary>요청된 커스텀 이벤트 ID를 외부에서 처리하고 싶을 때 구독합니다.</summary>
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

    /// <summary>외부 이벤트(이름 입력·암전 등) 대기 중인지</summary>
    public bool IsWaitingForExternalEvent => _waitingForExternalEvent;

    /// <summary>현재 줄 타이핑이 끝났는지</summary>
    public bool IsLineFullyShown => _lineFullyShown && !_isTyping;

    public Sprite PortraitDefault => portraitDefault;

    /// <summary>
    /// 씬이 로드되자마자 Yarn DialogueRunner 자동 시작을 막아
    /// 새 DialogueManager와 충돌하지 않게 합니다.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void DisableYarnRunnersOnSceneLoad()
    {
        DisableYarnComponentsStatic();
    }

    private void Awake()
    {
        Instance = this;

        // AddComponent로 붙인 경우 UnityEvent가 null일 수 있어 보장합니다.
        if (onDialogueFinished == null)
            onDialogueFinished = new UnityEvent();

        // 한글은 공백 기준으로만 줄바꿈 (전통 규칙은 글자 중간에서도 끊김)
        TMP_Settings.useModernHangulLineBreakingRules = true;

        ResolveDialogueFont();
        BuildExpressionMap();
        TryBindExistingUi();
        EnsureUiExists();
        RescueBackgroundAndCharacterFromYarn();
        ApplyFontToBoundTexts();
        WireUiButtons();
        DisableYarnComponentsIfAny();

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

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (!_isPlaying || _waitingForExternalEvent)
            return;

        // 새 Input System 사용 (Player Settings가 Input System Package 전용)
        var keyboard = Keyboard.current;
        if (keyboard != null &&
            (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame))
        {
            HandleAdvanceInput();
            return;
        }

        // 마우스 클릭: UI 위(다음 버튼 등)면 Update에서 무시 → 버튼 onClick만 처리
        var mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            HandleAdvanceInput();
        }
    }

    /// <summary>
    /// StageDialogueData의 Open 또는 Close 대사를 시작합니다.
    /// </summary>
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

    /// <summary>
    /// 임의의 대사 목록을 재생합니다. (프롤로그 등)
    /// </summary>
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

    /// <summary>
    /// 타이핑/자동 넘김 속도를 점점 빠르게 만듭니다. (프롤로그 칭찬 구간용)
    /// </summary>
    public void BeginRushPresentation(float targetTypingSpeed = 0.004f, float targetAutoDelay = 0.12f, float rampDuration = 2.2f)
    {
        _rushModeActive = true;
        // 칭찬 2줄만 자동 넘김. 방 장면 등 다른 구간에서는 EndRushPresentation이 auto를 끈다.
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

    /// <summary>
    /// 외부(예: 이름 입력창)에서 이벤트가 끝났을 때 호출하여 다음 줄로 넘어갑니다.
    /// </summary>
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

    /// <summary>
    /// 기존 SceneChanger / Title 쪽 SettingPopup을 연결합니다.
    /// </summary>
    public void SetSettingPopup(GameObject popup)
    {
        if (popup != null)
            settingPopup = popup;
    }

    private void HandleAdvanceInput()
    {
        // 로그/설정이 열려 있으면 입력 무시
        if ((_logPanelRoot != null && _logPanelRoot.activeSelf) ||
            (settingPopup != null && settingPopup.activeSelf))
            return;

        if (_isTyping)
        {
            // 타이핑 중 클릭 → 문장 즉시 완성
            CompleteTypingImmediately();
            return;
        }

        if (_lineFullyShown)
            AdvanceToNextLine();
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

        // 이벤트만 있고 대사가 비어 있으면 이벤트 실행
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

        // 이벤트는 줄 시작 시점에 처리 (이름 입력 등)
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

        // 칭찬 가속 후 암전: 다음으로 넘기지 않고 Prologue가 fade 처리할 때까지 대기
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
        // 외부에서 끝날 때까지 대기해야 하는 이벤트들
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
                // 칭찬 첫 줄: 가속+자동만 (암전 대기는 다음 줄)
                BeginRushPresentation();
                OnCustomEvent?.Invoke(eventId);
                break;

            case "AcceleratePraiseThenFade":
                // 칭찬 마지막 줄: 가속 유지 후 타이핑 끝나면 암전 대기
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
            // expressionId가 비어 있으면 이전 표정 유지
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

        // 플레이어 이름 치환 (여러 표기 지원)
        var name = PlayerNameManager.PlayerName;
        var formatted = raw
            .Replace("{$playerName}", name)
            .Replace("{playerName}", name)
            .Replace("[주인공]", name);

        // TMP 폰트에 U+2026(…) 글리프가 없어 점이 빠져 보이는 문제 → ASCII 마침표로 치환
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

        // 이미 플레이어 이름으로 저장된 경우에도 주인공으로 취급
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
            // 초반보다 후반에 더 빠르게 가속
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
        // Skip: 남은 대사를 빠르게 넘김 (이벤트는 유지)
        _skipRequested = true;
        _autoEnabled = false;

        if (_isTyping)
            CompleteTypingImmediately();

        // 남은 줄을 빠르게 순회하되, 외부 이벤트(이름 입력 등)에서는 멈춤
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
                // 중요 이벤트는 Skip으로 건너뛰지 않고 실행
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
            // TitleScene 등에서 쓰던 SoundSettings만 있는 경우
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

    /// <summary>
    /// 다음 대사 버튼을 눌렀을 때: 타이핑 중이면 즉시 완성, 완성되면 다음 줄.
    /// </summary>
    private void OnNextButtonClicked()
    {
        if (!_isPlaying || _waitingForExternalEvent)
            return;

        HandleAdvanceInput();
    }

    /// <summary>
    /// 다음 버튼 표시 여부.
    /// 대화 중에는 항상 보이게 해서, 타이핑 중 클릭(즉시 완성) / 완성 후 클릭(다음 줄)이 가능합니다.
    /// </summary>
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
        // 1) 예전 Yarn "Dialogue System" 오브젝트 전체를 끕니다. (새 DialogueCanvas와 중복)
        var yarnRoot = GameObject.Find("Dialogue System");
        if (yarnRoot != null && yarnRoot.activeSelf)
        {
            yarnRoot.SetActive(false);
            Debug.Log("[DialogueManager] 예전 Yarn Dialogue System을 비활성화했습니다. 이제는 DialogueCanvas만 사용합니다.");
        }

        // 2) DialogueRunner 컴포넌트가 남아 있으면 꺼서 자동 시작을 막습니다.
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
        // 배경/캐릭터 이미지는 씬에 있는 것을 재사용합니다.
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

        // Yarn Line Presenter의 TMP는 사용하지 않고, DialogueCanvas 쪽만 씁니다.
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

    /// <summary>
    /// bg / CharacterSprite가 Yarn Dialogue System 아래에 있으면 DialogueCanvas로 옮겨
    /// Dialogue System을 꺼도 배경·캐릭터가 보이게 합니다.
    /// </summary>
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

        // inactive 포함 검색 (Yarn이 이미 꺼진 경우)
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

        // SceneChanger 표정이 비어 있으면 가져옴
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
        // 1) Inspector에 이미 연결된 경우
        if (speakerNameText != null && dialogueBodyText != null)
        {
            if (nextButton != null)
                continueIcon = nextButton.gameObject;
            return;
        }

        // 2) 씬에 미리 배치된 DialogueCanvas 찾기 (에디터에서 조절한 UI)
        if (DialogueUiBuilder.TryFindExisting(out var found))
        {
            ApplyUiResult(found);
            Debug.Log("[DialogueManager] 씬의 DialogueCanvas UI를 연결했습니다.");
            return;
        }

        // 3) 없을 때만 런타임 생성 (폴백)
        if (!createUiAtRuntimeIfMissing)
        {
            Debug.LogError("[DialogueManager] DialogueCanvas가 없습니다. 메뉴 DevilMarriage → Add Dialogue UI To Open Scene 을 실행하세요.");
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
        Debug.LogWarning("[DialogueManager] Play 중 Dialogue UI를 임시 생성했습니다. 위치 조절을 위해 메뉴로 씬에 미리 배치하세요.");
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

        // 간단한 런타임 폴백 로그 패널
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

    /// <summary>
    /// 한글이 깨지지 않도록 Noto 계열 TMP 폰트를 찾습니다.
    /// </summary>
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
