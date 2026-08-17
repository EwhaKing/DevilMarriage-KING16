using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// StageSelect 버튼을 StagePlay Path처럼 Inspector Links로 연결합니다.
/// From/To에 Stage 버튼을 넣고 Rebuild Paths From Links를 누르면 선이 생깁니다.
/// </summary>
public class StageSelectPathLayout : MonoBehaviour
{
    [Serializable]
    public class PathLink
    {
        [Tooltip("Path 시작 Stage 버튼")]
        public RectTransform from;

        [Tooltip("Path 끝 Stage 버튼")]
        public RectTransform to;

        [Tooltip("꺾이거나 휘어진 Path용 중간 지점 (비워두면 직선)")]
        public RectTransform[] waypoints;
    }

    [Header("Links (Inspector에서 연결)")]
    [SerializeField] private PathLink[] links = Array.Empty<PathLink>();

    [Header("Output")]
    [SerializeField] private Transform pathsRoot;
    [SerializeField] private float pathWidth = 12f;
    [SerializeField] private Color inactiveColor = new Color(0.18f, 0.16f, 0.2f, 0.9f);
    [SerializeField] private Color activeColor = new Color(1f, 0.18f, 0.12f, 1f);
    [SerializeField] private Color glowColor = new Color(1f, 0.35f, 0.2f, 0.45f);

    [Header("Options")]
    [SerializeField] private bool clearExistingOnRebuild = true;

    public PathLink[] Links => links;

    [ContextMenu("Rebuild Paths From Links")]
    public void RebuildPathsFromLinks()
    {
        if (links == null || links.Length == 0)
        {
            Debug.LogWarning("[StageSelectPathLayout] Links가 비어 있습니다. Inspector에서 From/To Stage 버튼을 지정하세요.", this);
            return;
        }

        EnsurePathsRoot();
        if (clearExistingOnRebuild)
            ClearExistingPaths();

        int created = 0;
        for (int i = 0; i < links.Length; i++)
        {
            var link = links[i];
            if (link == null || link.from == null || link.to == null)
            {
                Debug.LogWarning($"[StageSelectPathLayout] Link[{i}]에 From/To 버튼이 없습니다.", this);
                continue;
            }

            if (link.from == link.to)
            {
                Debug.LogWarning($"[StageSelectPathLayout] Link[{i}]가 같은 버튼을 가리킵니다.", this);
                continue;
            }

            var pathObject = new GameObject($"Path_{link.from.name}_{link.to.name}");
            pathObject.transform.SetParent(pathsRoot, false);
            var rect = pathObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;

            var view = pathObject.AddComponent<StageSelectPathView>();
            view.From = link.from;
            view.To = link.to;
            view.Waypoints = link.waypoints;
            view.PathWidth = pathWidth;
            view.InactiveColor = inactiveColor;
            view.ActiveColor = activeColor;
            view.GlowColor = glowColor;
            view.RefreshGeometry();
            view.ApplySavedActivation();

#if UNITY_EDITOR
            if (!Application.isPlaying)
                Undo.RegisterCreatedObjectUndo(pathObject, "Rebuild Stage Select Paths");
#endif
            created++;
        }

#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
        if (pathsRoot != null)
            EditorUtility.SetDirty(pathsRoot.gameObject);
#endif
        Debug.Log($"[StageSelectPathLayout] Path {created}개 생성 완료.", this);
    }

    [ContextMenu("Refresh Path Positions")]
    public void RefreshPathPositions()
    {
        EnsurePathsRoot();
        var views = pathsRoot.GetComponentsInChildren<StageSelectPathView>(true);
        foreach (var view in views)
        {
            if (view == null)
                continue;
            view.ApplySavedActivation();
            view.PathWidth = pathWidth;
            view.InactiveColor = inactiveColor;
            view.ActiveColor = activeColor;
            view.GlowColor = glowColor;
            view.RefreshGeometry();
        }
    }

    public void EnsurePathsRoot()
    {
        if (pathsRoot != null)
            return;

        var existing = GameObject.Find("StageSelectPaths");
        if (existing != null)
        {
            pathsRoot = existing.transform;
            SendPathsBehindButtons();
            return;
        }

        var template = GameObject.Find("Stage1_Button");
        Transform parent = template != null ? template.transform.parent : transform;
        var rootObject = new GameObject("StageSelectPaths", typeof(RectTransform));
        rootObject.transform.SetParent(parent, false);
        var rect = rootObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        pathsRoot = rootObject.transform;
        SendPathsBehindButtons();

#if UNITY_EDITOR
        if (!Application.isPlaying)
            Undo.RegisterCreatedObjectUndo(rootObject, "Create StageSelectPaths");
#endif
    }

    public StageSelectPathView FindPath(int stageA, int stageB)
    {
        EnsurePathsRoot();
        var views = pathsRoot.GetComponentsInChildren<StageSelectPathView>(true);
        foreach (var view in views)
        {
            if (view == null)
                continue;
            if (StageProgressManager.GetPathKey(view.StageA, view.StageB)
                == StageProgressManager.GetPathKey(stageA, stageB))
                return view;
        }

        return null;
    }

    public void SendPathsBehindButtons()
    {
        EnsurePathsRoot();
        if (pathsRoot == null)
            return;

        var background = GameObject.Find("BackGround");
        var buttons = GameObject.Find("StageButtons");
        Transform mapParent = background != null
            ? background.transform.parent
            : buttons != null ? buttons.transform.parent : pathsRoot.parent;

        if (mapParent != null && pathsRoot.parent != mapParent)
            pathsRoot.SetParent(mapParent, true);

        if (background != null && pathsRoot.parent == background.transform.parent)
        {
            int index = background.transform.GetSiblingIndex() + 1;
            pathsRoot.SetSiblingIndex(index);
            if (buttons != null && buttons.transform.parent == pathsRoot.parent
                && buttons.transform.GetSiblingIndex() <= pathsRoot.GetSiblingIndex())
                buttons.transform.SetSiblingIndex(pathsRoot.GetSiblingIndex() + 1);
        }
        else
        {
            pathsRoot.SetAsFirstSibling();
        }
    }

    public static int ParseStageNumber(RectTransform rect)
    {
        if (rect == null)
            return 0;

        const string prefix = "Stage";
        const string suffix = "_Button";
        var name = rect.name;
        if (!name.StartsWith(prefix) || !name.EndsWith(suffix))
            return 0;

        var number = name.Substring(prefix.Length, name.Length - prefix.Length - suffix.Length);
        return int.TryParse(number, out int stageNumber) ? stageNumber : 0;
    }

    private void ClearExistingPaths()
    {
        EnsurePathsRoot();
        for (int i = pathsRoot.childCount - 1; i >= 0; i--)
        {
            var child = pathsRoot.GetChild(i).gameObject;
#if UNITY_EDITOR
            if (!Application.isPlaying)
                Undo.DestroyObjectImmediate(child);
            else
#endif
                Destroy(child);
        }
    }
}
