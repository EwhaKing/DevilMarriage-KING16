using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 퍼즐 Prefab 안의 Path를 Inspector에서 연결·자동 배치하기 위한 작성 도구입니다.
/// Links에 From/To 룬을 넣고 Rebuild Paths를 누르면 Path 오브젝트가 생성됩니다.
/// </summary>
public class StagePuzzleLayout : MonoBehaviour
{
    [Serializable]
    public class PathLink
    {
        [Tooltip("Path 시작 룬")]
        public RuneNode from;

        [Tooltip("Path 끝 룬")]
        public RuneNode to;

        [Tooltip("꺾이거나 휘어진 Path용 중간 지점 (비워두면 직선)")]
        public Transform[] waypoints;

        [Tooltip("클리어를 위해 반드시 지나야 하는 Path")]
        public bool isMandatory = true;
    }

    [Header("Links (Inspector에서 연결)")]
    [SerializeField] private PathLink[] links = Array.Empty<PathLink>();

    [Header("Output")]
    [SerializeField] private Transform pathsRoot;
    [SerializeField] private float pathWidth = 0.1f;
    [SerializeField] private Color inactiveColor = new Color(0.45f, 0.42f, 0.5f, 0.55f);

    [Header("Options")]
    [Tooltip("Rebuild 시 기존 Paths 하위를 모두 지우고 다시 만듭니다.")]
    [SerializeField] private bool clearExistingOnRebuild = true;

    public PathLink[] Links => links;
    public Transform PathsRoot => pathsRoot;

    [ContextMenu("Rebuild Paths From Links")]
    public void RebuildPathsFromLinks()
    {
        if (links == null || links.Length == 0)
        {
            Debug.LogWarning("[StagePuzzleLayout] Links가 비어 있습니다. Inspector에서 From/To를 지정하세요.", this);
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
                Debug.LogWarning($"[StagePuzzleLayout] Link[{i}]에 From/To 룬이 없습니다.", this);
                continue;
            }

            if (link.from.RuneIndex == link.to.RuneIndex)
            {
                Debug.LogWarning($"[StagePuzzleLayout] Link[{i}]가 같은 룬을 가리킵니다.", this);
                continue;
            }

            var pathObject = new GameObject($"Path_{link.from.RuneIndex}_{link.to.RuneIndex}");
            pathObject.transform.SetParent(pathsRoot, false);

            var line = pathObject.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.startWidth = pathWidth;
            line.endWidth = pathWidth;
            line.sortingOrder = 1;
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.startColor = inactiveColor;
            line.endColor = inactiveColor;

            var edge = pathObject.AddComponent<RunePathEdge>();
            edge.SetRuneReferences(link.from, link.to);
            edge.SetWaypoints(link.waypoints);
            edge.SetMandatory(link.isMandatory);
            edge.RefreshGeometry();
            edge.SetTraversed(false);

#if UNITY_EDITOR
            if (!Application.isPlaying)
                Undo.RegisterCreatedObjectUndo(pathObject, "Rebuild Puzzle Paths");
#endif
            created++;
        }

        var puzzle = GetComponent<Stage1PuzzleController>();
        if (puzzle != null)
            puzzle.RefreshRuneAndEdgeCache();

#if UNITY_EDITOR
        // Rebuild 후 Controller의 pathEdges 배열이 Missing으로 남지 않게 다시 할당
        if (puzzle != null)
        {
            var so = new SerializedObject(puzzle);
            var edgesProp = so.FindProperty("pathEdges");
            var edges = GetPathEdges();
            edgesProp.arraySize = edges.Length;
            for (int i = 0; i < edges.Length; i++)
                edgesProp.GetArrayElementAtIndex(i).objectReferenceValue = edges[i];

            var runesProp = so.FindProperty("runes");
            var runeNodes = GetComponentsInChildren<RuneNode>(true);
            runesProp.arraySize = runeNodes.Length;
            for (int i = 0; i < runeNodes.Length; i++)
                runesProp.GetArrayElementAtIndex(i).objectReferenceValue = runeNodes[i];

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(puzzle);
        }
#endif

#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
        if (pathsRoot != null)
            EditorUtility.SetDirty(pathsRoot.gameObject);
#endif

        Debug.Log($"[StagePuzzleLayout] Path {created}개 생성 완료.", this);
    }

    [ContextMenu("Refresh Path Positions")]
    public void RefreshPathPositions()
    {
        EnsurePathsRoot();
        var edges = pathsRoot.GetComponentsInChildren<RunePathEdge>(true);
        foreach (var edge in edges)
        {
            if (edge == null)
                continue;
            edge.SyncIndicesFromRefs();
            edge.RefreshGeometry();
#if UNITY_EDITOR
            EditorUtility.SetDirty(edge);
#endif
        }

        Debug.Log($"[StagePuzzleLayout] Path 위치 {edges.Length}개 갱신.", this);
    }

    [ContextMenu("Collect Links From Existing Paths")]
    public void CollectLinksFromExistingPaths()
    {
        EnsurePathsRoot();
        var edges = pathsRoot.GetComponentsInChildren<RunePathEdge>(true);
        var runes = GetComponentsInChildren<RuneNode>(true);
        var runeMap = new Dictionary<int, RuneNode>();
        foreach (var rune in runes)
        {
            if (rune != null)
                runeMap[rune.RuneIndex] = rune;
        }

        var collected = new List<PathLink>();
        foreach (var edge in edges)
        {
            if (edge == null)
                continue;

            runeMap.TryGetValue(edge.RuneIndexA, out var from);
            runeMap.TryGetValue(edge.RuneIndexB, out var to);
            collected.Add(new PathLink
            {
                from = from,
                to = to,
                isMandatory = edge.IsMandatoryPath
            });
        }

        links = collected.ToArray();
#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif
        Debug.Log($"[StagePuzzleLayout] 기존 Path {links.Length}개를 Links로 수집했습니다.", this);
    }

    public RunePathEdge[] GetPathEdges()
    {
        EnsurePathsRoot();
        return pathsRoot.GetComponentsInChildren<RunePathEdge>(true);
    }

    private void EnsurePathsRoot()
    {
        if (pathsRoot != null)
            return;

        var existing = transform.Find("Paths");
        if (existing != null)
        {
            pathsRoot = existing;
            return;
        }

        var rootObject = new GameObject("Paths");
        rootObject.transform.SetParent(transform, false);
        pathsRoot = rootObject.transform;
#if UNITY_EDITOR
        if (!Application.isPlaying)
            Undo.RegisterCreatedObjectUndo(rootObject, "Create Paths Root");
#endif
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

#if UNITY_EDITOR
[CustomEditor(typeof(StagePuzzleLayout))]
public class StagePuzzleLayoutEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var layout = (StagePuzzleLayout)target;

        EditorGUILayout.Space(8);
        EditorGUILayout.HelpBox(
            "1) Links에 From/To 룬을 지정합니다.\n" +
            "2) 필요하면 Waypoints로 꺾인 Path를 만듭니다.\n" +
            "3) Rebuild Paths From Links를 누릅니다.",
            MessageType.Info);

        if (GUILayout.Button("Rebuild Paths From Links", GUILayout.Height(32)))
            layout.RebuildPathsFromLinks();

        if (GUILayout.Button("Refresh Path Positions"))
            layout.RefreshPathPositions();

        if (GUILayout.Button("Collect Links From Existing Paths"))
            layout.CollectLinksFromExistingPaths();
    }
}
#endif
