using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 링베어러가 머리 위 결혼반지를 30초 동안 떨어뜨리지 않는 균형 미니게임.
/// 기울기는 하나의 숫자로 관리합니다. 0이 중앙, 음수가 왼쪽, 양수가 오른쪽입니다.
/// 화면은 RingBearerCanvas 자식으로 씬에 올라가 있으며, 그 위치와 크기가 시작 자세입니다.
/// </summary>
public class RingBearerMinigameController : MonoBehaviour
{
    private const float SafeRatio = 0.30f;
    private const float DangerRatio = 0.71f;

    [Header("시간")]
    [SerializeField] private float roundSeconds = 30f;

    [Header("기울기")]
    [Tooltip("이 값 이상(또는 그 반대)으로 기울면 반지가 떨어집니다.")]
    [SerializeField] private float tiltLimit = 100f;

    [Header("조작")]
    [Tooltip("방향키를 한 번 누를 때마다 기울기가 이만큼 움직입니다. 누르고 있어도 한 번만 적용됩니다. 왼쪽 키는 음수, 오른쪽 키는 양수입니다.")]
    [SerializeField] private float playerTiltStep = 16f;

    [Header("자동 기울기")]
    [SerializeField] private float driftSpeedStart = 12f;
    [SerializeField] private float driftSpeedEnd = 42f;
    [SerializeField] private float driftAcceleration = 55f;
    [SerializeField] private float driftRetargetMin = 0.7f;
    [SerializeField] private float driftRetargetMax = 1.25f;
    [Tooltip("기울어진 쪽으로 더 넘어가려는 힘. 후반으로 갈수록 커집니다.")]
    [SerializeField] private float leanAcceleration = 18f;

    [Header("반지 표시")]
    [SerializeField] private float maxRingAngle = 42f;
    [SerializeField] private float maxRingSlide = 74f;
    [SerializeField] private float maxRingSink = 40f;

    [Header("글꼴")]
    [SerializeField] private TMP_FontAsset font;

    [Header("화면 오브젝트")]
    [Tooltip("Hierarchy의 RingBearerCanvas. 자식 위치를 옮기면 그 자리가 시작 자세가 됩니다.")]
    [SerializeField] private RectTransform _canvasRect;
    [SerializeField] private RectTransform _actor;
    [SerializeField] private RectTransform _ring;
    [SerializeField] private Image _ringImage;
    [SerializeField] private RectTransform _marker;
    [SerializeField] private Image _markerDot;
    [SerializeField] private TextMeshProUGUI _timerText;
    [SerializeField] private TextMeshProUGUI _warningText;
    [SerializeField] private GameObject _resultRoot;
    [SerializeField] private TextMeshProUGUI _resultText;

    private enum Phase
    {
        Playing,
        Falling,
        Ended
    }

    private Phase _phase;
    private float _tilt;
    private float _timeLeft;
    private float _elapsed;
    private float _driftVelocity;
    private float _driftTarget;
    private float _retargetIn;
    private float _fallVelocity;
    private float _fallSpin;

    private Vector2 _actorRest;
    private Vector2 _ringRest;
    private Transform _ringHome;
    private Color _ringColor;
    private Color _warningBaseColor;
    private float _markerAnchorY;
    private Sprite _boxOverride;
    private Sprite _circleOverride;
    private Sprite _arrowLeft;
    private Sprite _arrowRight;

    public bool HasWiredHierarchy =>
        _canvasRect != null && _actor != null && _ring != null && _ringImage != null &&
        _marker != null && _markerDot != null && _timerText != null &&
        _warningText != null && _resultRoot != null && _resultText != null;

    /// <summary>
    /// 씬에 RingBearerCanvas가 없으면 만들고, 있으면 연결만 합니다.
    /// 이미 있는 자식의 위치는 바꾸지 않습니다.
    /// </summary>
    public void EnsureSceneHierarchy()
    {
        var existing = transform.Find("RingBearerCanvas") as RectTransform;
        if (existing != null)
        {
            if (!HasWiredHierarchy)
                BindExisting(existing);
            return;
        }

        ResolveLayoutSprites();
        BuildUi();
    }

    private float SafeAbs => tiltLimit * SafeRatio;
    private float DangerAbs => tiltLimit * DangerRatio;

    private void Start()
    {
        if (font == null)
            font = TMP_Settings.defaultFontAsset;

        if (!HasWiredHierarchy)
            EnsureSceneHierarchy();

        if (!HasWiredHierarchy)
        {
            Debug.LogError("[RingBearer] 화면 오브젝트를 찾지 못했습니다. Hierarchy의 RingBearerCanvas를 확인하세요.");
            enabled = false;
            return;
        }

        if (Camera.main != null)
            Camera.main.backgroundColor = new Color(0.11f, 0.07f, 0.12f, 1f);

        CaptureLayout();
        ResetRound();
    }

    private void CaptureLayout()
    {
        _actorRest = _actor.anchoredPosition;
        _ringHome = _ring.parent;
        _ringRest = _ring.anchoredPosition;
        _ringColor = _ringImage.color;
        _markerAnchorY = _marker.anchorMin.y;
        _warningBaseColor = _warningText.color;
    }

    private void Update()
    {
        if (_phase != Phase.Playing && WasRestartPressed())
        {
            ResetRound();
            return;
        }

        if (_phase == Phase.Ended)
            return;

        if (_phase == Phase.Falling)
        {
            UpdateFall();
            return;
        }

        TickPlaying(Time.deltaTime);
    }

    private void TickPlaying(float dt)
    {
        _timeLeft -= dt;
        _elapsed += dt;
        if (_timeLeft <= 0f)
        {
            FinishClear();
            return;
        }

        ApplyDrift(dt);
        ApplyInput();

        if (_tilt <= -tiltLimit || _tilt >= tiltLimit)
        {
            BeginFall();
            return;
        }

        UpdateTimerText();
        RefreshVisuals(ShakeOffset(_tilt));
    }

    private void ApplyDrift(float dt)
    {
        _retargetIn -= dt;
        if (_retargetIn <= 0f)
        {
            float progress = Mathf.Clamp01(_elapsed / Mathf.Max(0.01f, roundSeconds));
            float speed = Mathf.Lerp(driftSpeedStart, driftSpeedEnd, progress);
            float sign = Random.value < 0.5f ? -1f : 1f;
            _driftTarget = sign * speed * Random.Range(0.55f, 1f);
            _retargetIn = Random.Range(driftRetargetMin, driftRetargetMax);
        }

        _driftVelocity = Mathf.MoveTowards(_driftVelocity, _driftTarget, driftAcceleration * dt);
        _tilt += _driftVelocity * dt;

        if (Mathf.Abs(_tilt) > 0.5f)
        {
            float progress = Mathf.Clamp01(_elapsed / Mathf.Max(0.01f, roundSeconds));
            float lean = Mathf.Lerp(0f, leanAcceleration, progress);
            _tilt += Mathf.Sign(_tilt) * lean * (Mathf.Abs(_tilt) / tiltLimit) * dt;
        }
    }

    private void ApplyInput()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (keyboard.leftArrowKey.wasPressedThisFrame)
            _tilt -= playerTiltStep;
        if (keyboard.rightArrowKey.wasPressedThisFrame)
            _tilt += playerTiltStep;
    }

    private void BeginFall()
    {
        _tilt = Mathf.Clamp(_tilt, -tiltLimit, tiltLimit);
        RefreshVisuals(Vector2.zero);
        _phase = Phase.Falling;
        _fallSpin = -(_tilt / tiltLimit) * maxRingAngle;
        _fallVelocity = 80f;
        _ring.SetParent(_canvasRect, true);
        _warningText.gameObject.SetActive(false);
        ShowResult(false);
    }

    private void UpdateFall()
    {
        float dt = Time.deltaTime;
        _fallVelocity += 2100f * dt;
        float dir = _tilt >= 0f ? 1f : -1f;
        var pos = _ring.anchoredPosition;
        pos.x += dir * 260f * dt;
        pos.y -= _fallVelocity * dt;
        _ring.anchoredPosition = pos;
        _fallSpin += dir * 280f * dt;
        _ring.localRotation = Quaternion.Euler(0f, 0f, _fallSpin);

        if (pos.y < -760f)
            _phase = Phase.Ended;
    }

    private void FinishClear()
    {
        _phase = Phase.Ended;
        _timeLeft = 0f;
        UpdateTimerText();
        _warningText.gameObject.SetActive(false);
        RefreshVisuals(Vector2.zero);
        ShowResult(true);
    }

    private void ResetRound()
    {
        _phase = Phase.Playing;
        _tilt = 0f;
        _timeLeft = roundSeconds;
        _elapsed = 0f;
        _driftVelocity = 0f;
        _driftTarget = 0f;
        _retargetIn = 0.45f;
        _fallVelocity = 0f;
        _fallSpin = 0f;

        _actor.anchoredPosition = _actorRest;
        _ring.SetParent(_ringHome != null ? _ringHome : _actor, false);
        _ring.anchoredPosition = _ringRest;
        _ring.localRotation = Quaternion.identity;
        _ring.localScale = Vector3.one;
        _ringImage.color = _ringColor;

        _resultRoot.SetActive(false);
        _warningText.gameObject.SetActive(false);
        UpdateTimerText();
        RefreshVisuals(Vector2.zero);
    }

    private void RefreshVisuals(Vector2 shake)
    {
        _actor.anchoredPosition = _actorRest + shake;

        if (_phase != Phase.Falling)
        {
            float n = Mathf.Clamp(_tilt / tiltLimit, -1.15f, 1.15f);
            _ring.anchoredPosition = _ringRest + new Vector2(n * maxRingSlide, -(n * n) * maxRingSink);
            _ring.localRotation = Quaternion.Euler(0f, 0f, -n * maxRingAngle);

            float heat = Mathf.InverseLerp(DangerAbs, tiltLimit, Mathf.Abs(_tilt));
            _ringImage.color = Color.Lerp(_ringColor, new Color(1f, 0.42f, 0.32f), heat);
        }

        float marker = Mathf.InverseLerp(-tiltLimit, tiltLimit, Mathf.Clamp(_tilt, -tiltLimit, tiltLimit));
        _marker.anchorMin = new Vector2(marker, _markerAnchorY);
        _marker.anchorMax = new Vector2(marker, _markerAnchorY);

        _markerDot.color = BandColor(Mathf.Abs(_tilt));

        bool danger = Mathf.Abs(_tilt) >= DangerAbs && _phase == Phase.Playing;
        _warningText.gameObject.SetActive(danger);
        if (danger)
        {
            float pulse = 0.55f + 0.45f * Mathf.PingPong(Time.time * 2.4f, 1f);
            var color = _warningBaseColor;
            color.a = _warningBaseColor.a * pulse;
            _warningText.color = color;
        }
    }

    private Vector2 ShakeOffset(float tilt)
    {
        float abs = Mathf.Abs(tilt);
        if (abs <= SafeAbs)
            return Vector2.zero;

        float intensity = abs >= DangerAbs
            ? Mathf.Lerp(0.6f, 1.15f, Mathf.InverseLerp(DangerAbs, tiltLimit, abs))
            : Mathf.InverseLerp(SafeAbs, DangerAbs, abs) * 0.32f;

        float time = Time.time;
        return new Vector2(Mathf.Sin(time * 52f), Mathf.Sin(time * 39f + 1.4f)) * (9f * intensity);
    }

    private Color BandColor(float abs)
    {
        if (abs >= DangerAbs)
            return new Color(1f, 0.32f, 0.28f);
        if (abs > SafeAbs)
            return new Color(1f, 0.78f, 0.28f);
        return Color.white;
    }

    private void UpdateTimerText()
    {
        int seconds = Mathf.CeilToInt(Mathf.Max(0f, _timeLeft));
        _timerText.text = $"남은 시간: {seconds}초";
    }

    private void ShowResult(bool clear)
    {
        _resultRoot.SetActive(true);
        _resultRoot.transform.SetAsLastSibling();
        _resultText.text = clear ? "CLEAR" : "GAME OVER";
        _resultText.color = clear
            ? new Color(0.97f, 0.86f, 0.42f)
            : new Color(0.93f, 0.24f, 0.26f);
    }

    private static bool WasRestartPressed()
    {
        var keyboard = Keyboard.current;
        return keyboard != null && keyboard.rKey.wasPressedThisFrame;
    }

    private void BuildUi()
    {
        var canvasGo = new GameObject("RingBearerCanvas", typeof(RectTransform));
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvas.additionalShaderChannels =
            AdditionalCanvasShaderChannels.TexCoord1 |
            AdditionalCanvasShaderChannels.Normal |
            AdditionalCanvasShaderChannels.Tangent;
        canvasGo.AddComponent<GraphicRaycaster>();
        canvasGo.transform.SetParent(transform, false);
        _canvasRect = canvasGo.GetComponent<RectTransform>();

        var background = CreateStretchImage("Background", _canvasRect, WhiteSprite(), new Color(0.11f, 0.07f, 0.12f, 1f));
        background.raycastTarget = false;

        BuildActor();
        BuildGauge();
        BuildHud();
        BuildResult();
        SetLayerRecursively(_canvasRect, 5);
    }

    private void BindExisting(RectTransform canvas)
    {
        _canvasRect = canvas;
        _actor = canvas.Find("RingBearer") as RectTransform;
        _ring = canvas.Find("RingBearer/Ring") as RectTransform;
        _ringImage = _ring != null ? _ring.GetComponent<Image>() : null;
        _marker = canvas.Find("BalanceGauge/Marker") as RectTransform;
        var dot = canvas.Find("BalanceGauge/Marker/Dot");
        _markerDot = dot != null ? dot.GetComponent<Image>() : null;
        var timer = canvas.Find("Timer");
        _timerText = timer != null ? timer.GetComponent<TextMeshProUGUI>() : null;
        var warning = canvas.Find("Warning");
        _warningText = warning != null ? warning.GetComponent<TextMeshProUGUI>() : null;
        var result = canvas.Find("Result");
        _resultRoot = result != null ? result.gameObject : null;
        var resultLabel = canvas.Find("Result/ResultLabel");
        _resultText = resultLabel != null ? resultLabel.GetComponent<TextMeshProUGUI>() : null;
    }

    private void BuildActor()
    {
        _actor = CreateRect("RingBearer", _canvasRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        _actor.anchoredPosition = new Vector2(0f, 24f);
        _actor.sizeDelta = new Vector2(220f, 420f);
        _actorRest = _actor.anchoredPosition;

        CreateCenteredImage("Body", _actor, WhiteSprite(), new Color(0.23f, 0.18f, 0.32f), new Vector2(168f, 228f), new Vector2(0f, -36f));
        CreateCenteredImage("Head", _actor, CircleSprite(), new Color(0.93f, 0.78f, 0.66f), new Vector2(124f, 124f), new Vector2(0f, 128f));

        _ringRest = new Vector2(0f, 196f);
        _ringColor = new Color(0.93f, 0.76f, 0.28f);
        _ringImage = CreateCenteredImage("Ring", _actor, CircleSprite(), _ringColor, new Vector2(96f, 30f), _ringRest);
        _ring = _ringImage.rectTransform;
        CreateCenteredImage("RingHole", _ring, CircleSprite(), new Color(0.16f, 0.10f, 0.08f), new Vector2(62f, 12f), Vector2.zero);

        var caption = CreateText("Caption", _actor, 26, TextAlignmentOptions.Center, new Color(0.86f, 0.8f, 0.74f));
        caption.text = "링베어러";
        caption.rectTransform.anchoredPosition = new Vector2(0f, -176f);
        caption.rectTransform.sizeDelta = new Vector2(240f, 40f);
    }

    private void BuildGauge()
    {
        var gauge = CreateRect("BalanceGauge", _canvasRect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
        gauge.anchoredPosition = new Vector2(0f, 42f);
        gauge.sizeDelta = new Vector2(1080f, 96f);

        var bar = CreateRect("Bar", gauge, Vector2.zero, new Vector2(1f, 0f), new Vector2(0.5f, 0f));
        bar.anchoredPosition = Vector2.zero;
        bar.sizeDelta = new Vector2(0f, 48f);

        float dangerEdge = (1f - DangerRatio) * 0.5f;
        float safeEdge = (1f - SafeRatio) * 0.5f;
        var danger = new Color(0.72f, 0.18f, 0.2f);
        var caution = new Color(0.86f, 0.58f, 0.16f);
        var safe = new Color(0.24f, 0.62f, 0.38f);
        var onDark = Color.white;
        var onLight = new Color(0.16f, 0.1f, 0.08f);

        AddZone(bar, "LeftDanger", "위험", 0f, dangerEdge, danger, onDark);
        AddZone(bar, "LeftCaution", "주의", dangerEdge, safeEdge, caution, onLight);
        AddZone(bar, "Safe", "안전", safeEdge, 1f - safeEdge, safe, onDark);
        AddZone(bar, "RightCaution", "주의", 1f - safeEdge, 1f - dangerEdge, caution, onLight);
        AddZone(bar, "RightDanger", "위험", 1f - dangerEdge, 1f, danger, onDark);

        _marker = CreateRect("Marker", gauge, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
        _marker.anchoredPosition = new Vector2(0f, 50f);
        _marker.sizeDelta = new Vector2(22f, 36f);
        CreateCenteredImage("Stem", _marker, WhiteSprite(), Color.white, new Vector2(4f, 14f), new Vector2(0f, -10f));
        _markerDot = CreateCenteredImage("Dot", _marker, CircleSprite(), Color.white, new Vector2(20f, 20f), new Vector2(0f, 8f));
    }

    private void AddZone(RectTransform bar, string name, string label, float xMin, float xMax, Color color, Color textColor)
    {
        var image = CreateStretchImage(name, bar, WhiteSprite(), color);
        image.rectTransform.anchorMin = new Vector2(xMin, 0f);
        image.rectTransform.anchorMax = new Vector2(xMax, 1f);
        image.rectTransform.offsetMin = Vector2.zero;
        image.rectTransform.offsetMax = Vector2.zero;

        var text = CreateText(name + "Label", image.rectTransform, 22, TextAlignmentOptions.Center, textColor);
        Stretch(text.rectTransform);
        text.text = label;
    }

    private void BuildHud()
    {
        _timerText = CreateText("Timer", _canvasRect, 34, TextAlignmentOptions.Left, Color.white);
        var timerRect = _timerText.rectTransform;
        timerRect.anchorMin = timerRect.anchorMax = new Vector2(0f, 1f);
        timerRect.pivot = new Vector2(0f, 1f);
        timerRect.anchoredPosition = new Vector2(48f, -32f);
        timerRect.sizeDelta = new Vector2(460f, 56f);
        _timerText.text = "남은 시간: 30초";

        var hint = CreateRect("Controls", _canvasRect, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f));
        hint.anchoredPosition = new Vector2(-48f, -32f);
        hint.sizeDelta = new Vector2(420f, 56f);
        CreateCenteredImage("LeftArrow", hint, TriangleSprite(true), Color.white, new Vector2(30f, 30f), new Vector2(-86f, 0f));
        CreateCenteredImage("RightArrow", hint, TriangleSprite(false), Color.white, new Vector2(30f, 30f), new Vector2(-44f, 0f));

        var hintText = CreateText("Hint", hint, 32, TextAlignmentOptions.Left, Color.white);
        hintText.text = "균형 맞추기";
        hintText.rectTransform.anchoredPosition = new Vector2(88f, 0f);
        hintText.rectTransform.sizeDelta = new Vector2(210f, 48f);

        _warningText = CreateText("Warning", _canvasRect, 40, TextAlignmentOptions.Center, new Color(1f, 0.34f, 0.3f));
        _warningText.fontStyle = FontStyles.Bold;
        _warningText.text = "반지가 떨어질 것 같아!";
        _warningText.rectTransform.anchoredPosition = new Vector2(0f, 340f);
        _warningText.rectTransform.sizeDelta = new Vector2(980f, 72f);
        _warningText.gameObject.SetActive(false);
    }

    private void BuildResult()
    {
        var overlay = CreateStretchImage("Result", _canvasRect, WhiteSprite(), new Color(0f, 0f, 0f, 0.55f));
        _resultRoot = overlay.gameObject;
        _resultRoot.SetActive(false);

        _resultText = CreateText("ResultLabel", overlay.rectTransform, 92, TextAlignmentOptions.Center, new Color(0.97f, 0.86f, 0.42f));
        _resultText.text = "CLEAR";
        _resultText.fontStyle = FontStyles.Bold;
        _resultText.rectTransform.anchorMin = _resultText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        _resultText.rectTransform.anchoredPosition = new Vector2(0f, 24f);
        _resultText.rectTransform.sizeDelta = new Vector2(1200f, 140f);

        var retry = CreateText("Retry", overlay.rectTransform, 28, TextAlignmentOptions.Center, new Color(0.86f, 0.84f, 0.8f));
        retry.text = "R 다시 하기";
        retry.rectTransform.anchorMin = retry.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        retry.rectTransform.anchoredPosition = new Vector2(0f, -72f);
        retry.rectTransform.sizeDelta = new Vector2(480f, 48f);
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static RectTransform CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        return rect;
    }

    private static Image CreateStretchImage(string name, Transform parent, Sprite sprite, Color color)
    {
        var image = CreateCenteredImage(name, parent, sprite, color, Vector2.zero, Vector2.zero);
        Stretch(image.rectTransform);
        return image;
    }

    private static Image CreateCenteredImage(string name, Transform parent, Sprite sprite, Color color, Vector2 size, Vector2 position)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        var image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private TextMeshProUGUI CreateText(string name, Transform parent, float size, TextAlignmentOptions align, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var text = go.AddComponent<TextMeshProUGUI>();
        if (font != null)
            text.font = font;
        text.fontSize = size;
        text.color = color;
        text.alignment = align;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.rectTransform.anchorMin = text.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        text.rectTransform.sizeDelta = new Vector2(400f, 48f);
        return text;
    }

    private static Sprite _white;
    private static Sprite _circle;
    private static Sprite _triangleLeft;
    private static Sprite _triangleRight;

    private void ResolveLayoutSprites()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            _boxOverride = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/RingBearer/White.png");
            _circleOverride = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/RingBearer/Circle.png");
            _arrowLeft = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/RingBearer/ArrowLeft.png");
            _arrowRight = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/RingBearer/ArrowRight.png");
        }
#endif
        if (_boxOverride == null)
            _boxOverride = LoadCrispSprite("White.png");
        if (_circleOverride == null)
            _circleOverride = LoadCrispSprite("Circle.png");
    }

    private static Sprite LoadCrispSprite(string fileName)
    {
#if UNITY_EDITOR
        return AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/RingBearer/" + fileName);
#else
        return null;
#endif
    }

    private static void SetLayerRecursively(Transform root, int layer)
    {
        root.gameObject.layer = layer;
        for (int i = 0; i < root.childCount; i++)
            SetLayerRecursively(root.GetChild(i), layer);
    }

    private Sprite WhiteSprite()
    {
        if (_boxOverride != null)
            return _boxOverride;
        if (_white != null)
            return _white;

        var tex = NewTexture(4, 4);
        var pixels = new Color[16];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.Apply();
        _white = Sprite.Create(tex, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 4f);
        _white.hideFlags = HideFlags.HideAndDontSave;
        return _white;
    }

    private Sprite CircleSprite()
    {
        if (_circleOverride != null)
            return _circleOverride;
        if (_circle != null)
            return _circle;

        const int size = 128;
        var tex = NewTexture(size, size);
        float radius = size * 0.5f - 1f;
        var center = new Vector2(size * 0.5f, size * 0.5f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                float alpha = Mathf.Clamp01(radius - distance + 0.75f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        tex.Apply();
        _circle = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        _circle.hideFlags = HideFlags.HideAndDontSave;
        return _circle;
    }

    private Sprite TriangleSprite(bool pointLeft)
    {
        var provided = pointLeft ? _arrowLeft : _arrowRight;
        if (provided != null)
            return provided;
        if (pointLeft && _triangleLeft != null)
            return _triangleLeft;
        if (!pointLeft && _triangleRight != null)
            return _triangleRight;

        const int size = 64;
        var tex = NewTexture(size, size);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = (x + 0.5f) / size;
                float ny = (y + 0.5f) / size;
                float along = pointLeft ? nx : 1f - nx;
                float yMin = 0.5f - along * 0.46f;
                float yMax = 0.5f + along * 0.46f;
                bool inside = along >= 0.08f && along <= 0.92f && ny >= yMin && ny <= yMax;
                tex.SetPixel(x, y, inside ? Color.white : Color.clear);
            }
        }

        tex.Apply();
        var sprite = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        sprite.hideFlags = HideFlags.HideAndDontSave;
        if (pointLeft)
            _triangleLeft = sprite;
        else
            _triangleRight = sprite;
        return sprite;
    }

    private static Texture2D NewTexture(int width, int height)
    {
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.hideFlags = HideFlags.HideAndDontSave;
        return tex;
    }
}
