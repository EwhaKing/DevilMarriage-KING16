using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class StageSelectButtonEntry
{
    public int stageNumber;
    public Button button;
    public Image buttonImage;
    public GameObject lockOverlay;
    public TextMeshProUGUI labelText;
}

public class StageSelectController : MonoBehaviour
{
    /// <summary>
    /// 실제로 StartStage로 진입 가능한 스테이지 수 (1~33).
    /// Stage 34는 해금만 하고 플레이는 이후 구현.
    /// </summary>
    public const int PlayableStageCount = 33;

    [Header("Stage Buttons")]
    [SerializeField] private StageSelectButtonEntry[] stageButtons;
    [SerializeField] private Sprite emptyStageSprite;
    [SerializeField] private Sprite clearedStageSprite;

    [Header("End Of Content")]
    [SerializeField] private GameObject endOfContentPopup;
    [SerializeField] private TextMeshProUGUI endOfContentText;
    [SerializeField] private string endOfContentMessage =
        "이 Stage는 아직 준비 중입니다.\n클리어한 Stage는 다시 플레이할 수 있습니다.";

    [Header("Settings")]
    [SerializeField] private GameObject settingPopup;

    [Header("Stage Map")]
    [SerializeField] private float defaultStageButtonSpacing = 24f;
    [SerializeField] private StageSelectMapPlayer mapPlayer;
    [SerializeField] private RuntimeAnimatorController mapPlayerAnimator;

    private StageSelectCameraPan _cameraPan;
    private Canvas _hudCanvas;
    private RectTransform _mapBounds;
    private StageSelectMapPlayer _mapPlayer;

    private void Awake()
    {
        GameFlowManager.EnsureExists();

        if (settingPopup == null)
            settingPopup = GameObject.Find("SettingPopup");

        if (settingPopup != null)
            settingPopup.SetActive(false);

        if (endOfContentPopup != null)
            endOfContentPopup.SetActive(false);

        AutoCreateStageButtonsFromTemplate();
        EnsureWorldStageMap();
        EnsureStageSelectPaths();
        AutoWireStageButtonsIfNeeded();
        WireButtons();
        RefreshStageButtons();
        SetupCameraPan();
        ApplyMapLayerOrder();
    }

    private void Start()
    {
        SetupMapPlayer();
    }

    private void AutoCreateStageButtonsFromTemplate()
    {
        var template = GameObject.Find("Stage1_Button");
        if (template == null)
            return;

        var templateRect = template.GetComponent<RectTransform>();
        if (templateRect == null)
            return;

        var parent = template.transform.parent;
        HideStageButtonText(template);
        ApplyStageButtonSprite(template.GetComponent<Image>(), cleared: false);

        for (int stageNumber = 2; stageNumber <= StageProgressManager.StageSelectButtonCount; stageNumber++)
        {
            var existing = GameObject.Find($"Stage{stageNumber}_Button");
            if (existing != null)
            {
                HideStageButtonText(existing);
                ApplyStageButtonSprite(existing.GetComponent<Image>(), cleared: false);
                continue;
            }

            var clone = Instantiate(template, parent);
            clone.name = $"Stage{stageNumber}_Button";
            var cloneRect = clone.GetComponent<RectTransform>();
            if (cloneRect != null)
            {
                cloneRect.anchorMin = templateRect.anchorMin;
                cloneRect.anchorMax = templateRect.anchorMax;
                cloneRect.pivot = templateRect.pivot;
                cloneRect.sizeDelta = templateRect.sizeDelta;
                cloneRect.anchoredPosition = DefaultStageButtonPosition(
                    templateRect.anchoredPosition,
                    templateRect.sizeDelta,
                    stageNumber);
            }

            HideStageButtonText(clone);
            ApplyStageButtonSprite(clone.GetComponent<Image>(), cleared: false);
        }
    }

    private void EnsureWorldStageMap()
    {
        var background = GameObject.Find("BackGround");
        var template = GameObject.Find("Stage1_Button");
        if (background == null || template == null)
            return;

        _mapBounds = background.GetComponent<RectTransform>();

        var hudCanvas = template.GetComponentInParent<Canvas>();
        if (hudCanvas != null)
        {
            _hudCanvas = hudCanvas;
            hudCanvas.sortingOrder = Mathf.Max(hudCanvas.sortingOrder, 10);
        }

        Canvas.ForceUpdateCanvases();

        var mapObject = GameObject.Find("StageMapCanvas");
        RectTransform mapRect;
        if (mapObject == null)
        {
            mapObject = new GameObject("StageMapCanvas", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            var mapCanvas = mapObject.GetComponent<Canvas>();
            mapRect = mapObject.GetComponent<RectTransform>();
            mapCanvas.renderMode = RenderMode.WorldSpace;
            mapCanvas.worldCamera = Camera.main;
            mapCanvas.sortingOrder = 0;

            if (hudCanvas != null)
            {
                mapRect.position = hudCanvas.transform.position;
                mapRect.rotation = hudCanvas.transform.rotation;
                var hudScale = hudCanvas.transform.lossyScale;
                if (hudScale.x > 0.0001f)
                {
                    mapRect.localScale = hudScale;
                }
                else
                {
                    var cam = Camera.main;
                    float mapHeight = Mathf.Max(1f, _mapBounds != null ? _mapBounds.sizeDelta.y : 1440f);
                    float worldHeight = cam != null && cam.orthographic
                        ? cam.orthographicSize * 2f
                        : 10f;
                    float scale = worldHeight / mapHeight;
                    mapRect.localScale = new Vector3(scale, scale, scale);
                    if (cam != null)
                    {
                        var camPos = cam.transform.position;
                        mapRect.position = new Vector3(camPos.x, camPos.y, 0f);
                    }
                }
            }
        }
        else
        {
            var mapCanvas = mapObject.GetComponent<Canvas>();
            mapRect = mapObject.GetComponent<RectTransform>();
            if (mapCanvas != null)
            {
                mapCanvas.renderMode = RenderMode.WorldSpace;
                if (mapCanvas.worldCamera == null)
                    mapCanvas.worldCamera = Camera.main;
            }
        }

        _mapBounds = background.GetComponent<RectTransform>();
        if (_mapBounds != null && mapRect != null)
        {
            mapRect.sizeDelta = new Vector2(
                Mathf.Max(mapRect.sizeDelta.x, _mapBounds.sizeDelta.x),
                Mathf.Max(mapRect.sizeDelta.y, _mapBounds.sizeDelta.y));
        }

        if (background.transform.parent != mapObject.transform)
            background.transform.SetParent(mapObject.transform, true);

        var stageButtonsRoot = GameObject.Find("StageButtons");
        if (stageButtonsRoot != null)
        {
            if (stageButtonsRoot.transform.parent != mapObject.transform)
                stageButtonsRoot.transform.SetParent(mapObject.transform, true);
        }
        else
        {
            for (int i = 1; i <= StageProgressManager.StageSelectButtonCount; i++)
            {
                var buttonObject = GameObject.Find($"Stage{i}_Button");
                if (buttonObject == null)
                    continue;
                if (buttonObject.transform.parent != mapObject.transform)
                    buttonObject.transform.SetParent(mapObject.transform, true);
            }
        }

        var paths = GameObject.Find("StageSelectPaths");
        if (paths != null && paths.transform.parent != mapObject.transform)
            paths.transform.SetParent(mapObject.transform, true);

        var player = mapPlayer != null ? mapPlayer.gameObject : GameObject.Find("StageSelectPlayer");
        if (player != null && player.transform.parent != mapObject.transform)
            player.transform.SetParent(mapObject.transform, true);
    }

    private void SetupCameraPan()
    {
        var cam = Camera.main;
        if (cam == null)
            return;

        _cameraPan = cam.GetComponent<StageSelectCameraPan>();
        if (_cameraPan == null)
            _cameraPan = cam.gameObject.AddComponent<StageSelectCameraPan>();

        _cameraPan.Configure(_mapBounds, settingPopup);
    }

    private void EnsureStageSelectPaths()
    {
        var layout = GetComponent<StageSelectPathLayout>();
        if (layout == null)
            layout = gameObject.AddComponent<StageSelectPathLayout>();

        layout.EnsurePathsRoot();

        var mapObject = GameObject.Find("StageMapCanvas");
        var paths = GameObject.Find("StageSelectPaths");
        if (mapObject != null && paths != null && paths.transform.parent != mapObject.transform)
            paths.transform.SetParent(mapObject.transform, true);

        layout.SendPathsBehindButtons();

        if (layout.Links != null && layout.Links.Length > 0)
        {
            if (paths == null || paths.transform.childCount == 0)
                layout.RebuildPathsFromLinks();
            else
                layout.RefreshPathPositions();
        }
        else
        {
            layout.RefreshPathPositions();
        }
    }

    private void ApplyMapLayerOrder()
    {
        var mapObject = GameObject.Find("StageMapCanvas");
        Transform parent = mapObject != null ? mapObject.transform : null;
        var background = GameObject.Find("BackGround");
        var paths = GameObject.Find("StageSelectPaths");
        var buttons = GameObject.Find("StageButtons");
        var player = GameObject.Find("StageSelectPlayer");

        if (parent == null)
        {
            parent = background != null ? background.transform.parent : null;
            if (parent == null)
                return;
        }

        int sibling = 0;
        if (background != null && background.transform.parent == parent)
            background.transform.SetSiblingIndex(sibling++);
        if (paths != null && paths.transform.parent == parent)
            paths.transform.SetSiblingIndex(sibling++);
        if (buttons != null && buttons.transform.parent == parent)
            buttons.transform.SetSiblingIndex(sibling++);
        if (player != null && player.transform.parent == parent)
            player.transform.SetAsLastSibling();
    }

    private void SetupMapPlayer()
    {
        Canvas.ForceUpdateCanvases();
        GetComponent<StageSelectPathLayout>()?.RefreshPathPositions();

        Transform mapParent = null;
        var mapObject = GameObject.Find("StageMapCanvas");
        if (mapObject != null)
            mapParent = mapObject.transform;
        else
        {
            var buttons = GameObject.Find("StageButtons");
            mapParent = buttons != null ? buttons.transform.parent : transform;
        }

        if (mapPlayer == null)
            mapPlayer = FindAnyObjectByType<StageSelectMapPlayer>();

        _mapPlayer = StageSelectMapPlayer.FindOrCreate(mapParent, mapPlayer, mapPlayerAnimator, _cameraPan);
        mapPlayer = _mapPlayer;
        ApplyMapLayerOrder();

        int currentStage = StageProgressManager.CurrentMapStage;
        if (!StageProgressManager.IsStageUnlocked(currentStage))
        {
            currentStage = 1;
            StageProgressManager.CurrentMapStage = 1;
        }

        if (StageProgressManager.TryConsumePendingWalk(out int fromStage, out int toStage)
            && StageProgressManager.IsStageUnlocked(toStage))
        {
            PlacePlayerOnStage(fromStage > 0 ? fromStage : currentStage);
            FocusCameraOnStage(fromStage > 0 ? fromStage : currentStage);
            BeginWalkToStage(toStage);
            return;
        }

        PlacePlayerOnStage(currentStage);
        FocusCameraOnStage(currentStage);
    }

    private void PlacePlayerOnStage(int stageNumber)
    {
        var button = FindStageButton(stageNumber);
        if (button == null)
            button = FindStageButton(1);
        if (_mapPlayer != null)
            _mapPlayer.PlaceOn(button);
        StageProgressManager.CurrentMapStage = stageNumber > 0 ? stageNumber : 1;
    }

    private RectTransform FindStageButton(int stageNumber)
    {
        var buttonObject = GameObject.Find($"Stage{stageNumber}_Button");
        return buttonObject != null ? buttonObject.GetComponent<RectTransform>() : null;
    }

    private Vector2 DefaultStageButtonPosition(Vector2 origin, Vector2 size, int stageNumber)
    {
        const int columns = 22;
        int index = Mathf.Max(0, stageNumber - 1);
        int col = index % columns;
        int row = index / columns;
        return origin + new Vector2(
            col * (size.x + defaultStageButtonSpacing),
            -row * (size.y + defaultStageButtonSpacing));
    }

    private void FocusCameraOnStage(int stageNumber)
    {
        if (_cameraPan == null)
            return;

        var button = FindStageButton(stageNumber);
        if (button != null)
            _cameraPan.FocusOnWorldX(button.position.x);
        else if (_mapBounds != null)
            _cameraPan.FocusOnWorldX(_mapBounds.position.x);
    }

    private void AutoWireStageButtonsIfNeeded()
    {
        if (stageButtons != null && stageButtons.Length > 0)
            return;

        var entries = new System.Collections.Generic.List<StageSelectButtonEntry>();
        for (int i = 1; i <= StageProgressManager.StageSelectButtonCount; i++)
        {
            var buttonObject = GameObject.Find($"Stage{i}_Button");
            if (buttonObject == null)
                continue;

            entries.Add(new StageSelectButtonEntry
            {
                stageNumber = i,
                button = buttonObject.GetComponent<Button>(),
                buttonImage = buttonObject.GetComponent<Image>(),
                lockOverlay = buttonObject.transform.Find("LockOverlay")?.gameObject,
                labelText = buttonObject.GetComponentInChildren<TextMeshProUGUI>(true)
            });
        }

        stageButtons = entries.ToArray();
    }

    private void WireButtons()
    {
        BindNamedButton("Title_Button", () =>
        {
            GameFlowManager.EnsureExists()?.GoToTitle();
        });

        BindNamedButton("Devil_Page_Button", () =>
        {
            GameFlowManager.EnsureExists()?.GoToDevilPage();
        });

        BindNamedButton("Option_Button", OpenSettingsPopup);

        if (stageButtons == null)
            return;

        foreach (var entry in stageButtons)
        {
            if (entry?.button == null)
                continue;

            int stageNumber = entry.stageNumber;
            entry.button.onClick.RemoveAllListeners();
            entry.button.onClick.AddListener(() => OnStageButtonClicked(stageNumber));
        }
    }

    private void BindNamedButton(string objectName, UnityEngine.Events.UnityAction action)
    {
        var buttonObject = GameObject.Find(objectName);
        if (buttonObject == null)
            return;

        var button = buttonObject.GetComponent<Button>();
        if (button != null)
            button.onClick.AddListener(action);
    }

    private void RefreshStageButtons()
    {
        if (stageButtons == null)
            return;

        foreach (var entry in stageButtons)
        {
            if (entry == null)
                continue;

            bool isUnlocked = StageProgressManager.IsStageUnlocked(entry.stageNumber);
            bool isCleared = StageProgressManager.IsStageCleared(entry.stageNumber);

            if (entry.button != null)
                entry.button.interactable = isUnlocked;

            if (entry.lockOverlay != null)
                entry.lockOverlay.SetActive(!isUnlocked);

            var buttonObject = entry.button != null ? entry.button.gameObject : null;
            HideStageButtonText(buttonObject);

            var image = entry.buttonImage;
            if (image == null && buttonObject != null)
                image = buttonObject.GetComponent<Image>();
            ApplyStageButtonSprite(image, isCleared);
        }
    }

    private static void HideStageButtonText(GameObject buttonObject)
    {
        if (buttonObject == null)
            return;

        var labels = buttonObject.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var label in labels)
        {
            if (label == null)
                continue;

            label.text = string.Empty;
            label.gameObject.SetActive(false);
        }
    }

    private void ApplyStageButtonSprite(Image image, bool cleared)
    {
        if (image == null)
            return;

        image.type = Image.Type.Simple;
        image.preserveAspect = true;

        var sprite = cleared && clearedStageSprite != null
            ? clearedStageSprite
            : emptyStageSprite;
        if (sprite != null)
            image.sprite = sprite;
    }

    private void OnStageButtonClicked(int stageNumber)
    {
        if (_cameraPan != null && _cameraPan.DidDragThisPointer)
            return;

        if (_mapPlayer != null && _mapPlayer.IsMoving)
            return;

        if (!StageProgressManager.IsStageUnlocked(stageNumber))
            return;

        int current = StageProgressManager.CurrentMapStage;
        if (stageNumber != current)
        {
            BeginWalkToStage(stageNumber);
            return;
        }

        if (stageNumber > PlayableStageCount)
        {
            ShowEndOfContentPopup();
            return;
        }

        if (GameFlowManager.EnsureExists() != null)
            GameFlowManager.Instance.StartStage(stageNumber);
        else
            Debug.LogError("[StageSelectController] GameFlowManager를 생성할 수 없습니다.");
    }

    private void BeginWalkToStage(int destinationStage)
    {
        if (_mapPlayer == null)
        {
            PlacePlayerOnStage(destinationStage);
            return;
        }

        int fromStage = StageProgressManager.CurrentMapStage;
        if (fromStage == destinationStage)
            return;

        if (!TryBuildWalkRoute(fromStage, destinationStage, out var hops, out var stages))
        {
            PlacePlayerOnStage(destinationStage);
            FocusCameraOnStage(destinationStage);
            return;
        }

        SetStageButtonsInputEnabled(false);
        _mapPlayer.WalkRoute(hops, stages, FindStageButton, () =>
        {
            PlacePlayerOnStage(destinationStage);
            SetStageButtonsInputEnabled(true);
        });
    }

    private void SetStageButtonsInputEnabled(bool enabled)
    {
        if (stageButtons == null)
            return;

        foreach (var entry in stageButtons)
        {
            if (entry?.button == null)
                continue;

            entry.button.interactable = enabled && StageProgressManager.IsStageUnlocked(entry.stageNumber);
        }
    }

    private bool TryBuildWalkRoute(
        int fromStage,
        int toStage,
        out List<StageSelectPathView> hops,
        out List<int> stages)
    {
        hops = new List<StageSelectPathView>();
        stages = new List<int>();
        if (fromStage == toStage)
            return false;

        var layout = GetComponent<StageSelectPathLayout>();
        var graph = BuildPathGraph(layout);

        if (graph.Count > 0 && TryFindGraphRoute(graph, fromStage, toStage, hops, stages))
            return hops.Count > 0;

        hops.Clear();
        stages.Clear();
        int step = toStage > fromStage ? 1 : -1;
        for (int stage = fromStage; stage != toStage; stage += step)
        {
            int next = stage + step;
            if (!StageProgressManager.IsStageUnlocked(next) && next != toStage)
                return false;

            hops.Add(layout != null ? layout.FindPath(stage, next) : null);
            stages.Add(stage);
        }

        stages.Add(toStage);
        return hops.Count > 0;
    }

    private static Dictionary<int, List<(int other, StageSelectPathView view)>> BuildPathGraph(StageSelectPathLayout layout)
    {
        var graph = new Dictionary<int, List<(int other, StageSelectPathView view)>>();
        if (layout == null || layout.Links == null)
            return graph;

        foreach (var link in layout.Links)
        {
            if (link == null)
                continue;

            int a = StageSelectPathLayout.ParseStageNumber(link.from);
            int b = StageSelectPathLayout.ParseStageNumber(link.to);
            if (a <= 0 || b <= 0 || a == b)
                continue;

            var view = layout.FindPath(a, b);
            AddGraphEdge(graph, a, b, view);
            AddGraphEdge(graph, b, a, view);
        }

        return graph;
    }

    private static void AddGraphEdge(
        Dictionary<int, List<(int other, StageSelectPathView view)>> graph,
        int from,
        int to,
        StageSelectPathView view)
    {
        if (!graph.TryGetValue(from, out var neighbors))
        {
            neighbors = new List<(int other, StageSelectPathView view)>();
            graph[from] = neighbors;
        }

        neighbors.Add((to, view));
    }

    private static bool TryFindGraphRoute(
        Dictionary<int, List<(int other, StageSelectPathView view)>> graph,
        int fromStage,
        int toStage,
        List<StageSelectPathView> hops,
        List<int> stages)
    {
        var visited = new HashSet<int> { fromStage };
        var previous = new Dictionary<int, (int from, StageSelectPathView view)>();
        var queue = new Queue<int>();
        queue.Enqueue(fromStage);

        while (queue.Count > 0)
        {
            int current = queue.Dequeue();
            if (current == toStage)
                break;
            if (!graph.TryGetValue(current, out var neighbors))
                continue;

            foreach (var (next, view) in neighbors)
            {
                if (visited.Contains(next))
                    continue;
                if (next != toStage && !StageProgressManager.IsStageUnlocked(next))
                    continue;

                visited.Add(next);
                previous[next] = (current, view);
                queue.Enqueue(next);
            }
        }

        if (!previous.ContainsKey(toStage))
            return false;

        var reverseHops = new List<StageSelectPathView>();
        var reverseStages = new List<int> { toStage };
        int node = toStage;
        while (node != fromStage)
        {
            var step = previous[node];
            reverseHops.Add(step.view);
            reverseStages.Add(step.from);
            node = step.from;
        }

        reverseHops.Reverse();
        reverseStages.Reverse();
        hops.AddRange(reverseHops);
        stages.AddRange(reverseStages);
        return hops.Count > 0;
    }

    public void ShowEndOfContentPopup()
    {
        EnsureEndOfContentPopup();

        if (endOfContentPopup == null)
            return;

        if (endOfContentText != null)
            endOfContentText.text = endOfContentMessage;

        endOfContentPopup.SetActive(true);
    }

    private void EnsureEndOfContentPopup()
    {
        if (endOfContentPopup != null)
            return;

        var canvas = _hudCanvas;
        if (canvas == null)
        {
            foreach (var found in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (found != null && found.renderMode != RenderMode.WorldSpace)
                {
                    canvas = found;
                    break;
                }
            }
        }

        if (canvas == null)
            return;

        endOfContentPopup = new GameObject("EndOfContentPopup");
        endOfContentPopup.transform.SetParent(canvas.transform, false);

        var panelImage = endOfContentPopup.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.75f);

        var panelRect = endOfContentPopup.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        var textObject = new GameObject("Message");
        textObject.transform.SetParent(endOfContentPopup.transform, false);
        endOfContentText = textObject.AddComponent<TextMeshProUGUI>();
        endOfContentText.alignment = TextAlignmentOptions.Center;
        endOfContentText.fontSize = 28;
        endOfContentText.color = Color.white;
        endOfContentText.text = endOfContentMessage;

        var textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.1f, 0.35f);
        textRect.anchorMax = new Vector2(0.9f, 0.65f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        var closeButtonObject = new GameObject("CloseButton");
        closeButtonObject.transform.SetParent(endOfContentPopup.transform, false);
        var closeButton = closeButtonObject.AddComponent<Button>();
        closeButton.onClick.AddListener(CloseEndOfContentPopup);

        var closeButtonRect = closeButtonObject.GetComponent<RectTransform>();
        closeButtonRect.anchorMin = new Vector2(0.4f, 0.2f);
        closeButtonRect.anchorMax = new Vector2(0.6f, 0.28f);
        closeButtonRect.offsetMin = Vector2.zero;
        closeButtonRect.offsetMax = Vector2.zero;

        var closeButtonImage = closeButtonObject.AddComponent<Image>();
        closeButtonImage.color = new Color(0.25f, 0.25f, 0.25f, 1f);

        var closeLabelObject = new GameObject("Label");
        closeLabelObject.transform.SetParent(closeButtonObject.transform, false);
        var closeLabel = closeLabelObject.AddComponent<TextMeshProUGUI>();
        closeLabel.text = "OK";
        closeLabel.alignment = TextAlignmentOptions.Center;
        closeLabel.fontSize = 24;
        closeLabel.color = Color.white;

        var closeLabelRect = closeLabelObject.GetComponent<RectTransform>();
        closeLabelRect.anchorMin = Vector2.zero;
        closeLabelRect.anchorMax = Vector2.one;
        closeLabelRect.offsetMin = Vector2.zero;
        closeLabelRect.offsetMax = Vector2.zero;

        endOfContentPopup.SetActive(false);
    }

    public void CloseEndOfContentPopup()
    {
        if (endOfContentPopup != null)
            endOfContentPopup.SetActive(false);
    }

    public void OpenSettingsPopup()
    {
        if (settingPopup != null)
            settingPopup.SetActive(true);
    }

    public void CloseSettingsPopup()
    {
        if (settingPopup != null)
            settingPopup.SetActive(false);

        var soundSettings = FindAnyObjectByType<SoundSettings>();
        if (soundSettings != null)
            soundSettings.SaveSoundSettings();
    }
}
