#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 스테이지별 Puzzle Prefab 생성·연결을 돕는 에디터 메뉴입니다.
/// </summary>
public static class StagePuzzlePrefabCreator
{
    private const string PrefabFolder = "Assets/Prefabs/Puzzles";
    private const string PlayDataFolder = "Assets/Data/PlayData";

    [MenuItem("DevilMarriage/Puzzles/1. Extract Stage1Puzzle Prefab From Open Scene")]
    public static void ExtractStage1PuzzleFromScene()
    {
        EnsureFolder("Assets/Prefabs");
        EnsureFolder(PrefabFolder);

        var controller = Object.FindAnyObjectByType<Stage1PuzzleController>();
        if (controller == null)
        {
            EditorUtility.DisplayDialog(
                "Stage1Puzzle 없음",
                "StagePlayScene(또는 퍼즐이 있는 씬)을 연 뒤 다시 실행하세요.",
                "확인");
            return;
        }

        // Player는 씬에 남겨 두고, 퍼즐 루트만 Prefab으로 저장
        var root = controller.gameObject;
        var layout = root.GetComponent<StagePuzzleLayout>();
        if (layout == null)
            layout = root.AddComponent<StagePuzzleLayout>();

        layout.CollectLinksFromExistingPaths();

        string path = $"{PrefabFolder}/Stage1Puzzle.prefab";
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        if (prefab == null)
        {
            Debug.LogError("[StagePuzzlePrefabCreator] Prefab 저장 실패.");
            return;
        }

        AssignPuzzlePrefabToPlayData("Stage01_PlayData", prefab);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);
        EditorUtility.DisplayDialog(
            "완료",
            $"Stage1Puzzle Prefab 저장:\n{path}\n\nStage01_PlayData.puzzlePrefab에도 연결했습니다.",
            "확인");
    }

    [MenuItem("DevilMarriage/Puzzles/2. Create Empty Puzzle Prefab Template")]
    public static void CreateEmptyPuzzleTemplate()
    {
        string name = EditorUtility.SaveFilePanelInProject(
            "새 Puzzle Prefab",
            "Stage2Puzzle",
            "prefab",
            "퍼즐 Prefab 이름을 정하세요.",
            PrefabFolder);

        if (string.IsNullOrEmpty(name))
            return;

        EnsureFolder("Assets/Prefabs");
        EnsureFolder(PrefabFolder);

        var root = new GameObject(Path.GetFileNameWithoutExtension(name));
        root.AddComponent<Stage1PuzzleController>();
        root.AddComponent<StagePuzzleLayout>();

        var paths = new GameObject("Paths");
        paths.transform.SetParent(root.transform, false);

        // 샘플 룬 3개 (삼각형) — 위치만 잡고 Links는 사용자가 연결
        CreateSampleRune(root.transform, 0, new Vector3(0f, 1.5f, 0f), isStart: true);
        CreateSampleRune(root.transform, 1, new Vector3(-1.5f, -0.8f, 0f), isStart: false);
        CreateSampleRune(root.transform, 2, new Vector3(1.5f, -0.8f, 0f), isStart: false);

        var layout = root.GetComponent<StagePuzzleLayout>();
        var so = new SerializedObject(layout);
        so.FindProperty("pathsRoot").objectReferenceValue = paths.transform;

        var links = so.FindProperty("links");
        links.arraySize = 3;
        SetLink(links.GetArrayElementAtIndex(0), root.transform, 0, 1);
        SetLink(links.GetArrayElementAtIndex(1), root.transform, 1, 2);
        SetLink(links.GetArrayElementAtIndex(2), root.transform, 2, 0);
        so.ApplyModifiedPropertiesWithoutUndo();

        layout.RebuildPathsFromLinks();

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, name);
        Object.DestroyImmediate(root);

        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);
        EditorUtility.DisplayDialog(
            "템플릿 생성",
            $"빈 삼각형 퍼즐 Prefab을 만들었습니다.\n{name}\n\n" +
            "1) Prefab을 더블클릭해 편집\n" +
            "2) 룬 위치를 원하는 대로 이동\n" +
            "3) StagePuzzleLayout → Rebuild Paths\n" +
            "4) PlayData의 Puzzle Prefab에 연결",
            "확인");
    }

    [MenuItem("DevilMarriage/Puzzles/3. Create Square Puzzle Template (Stage2 예시)")]
    public static void CreateSquarePuzzleTemplate()
    {
        EnsureFolder("Assets/Prefabs");
        EnsureFolder(PrefabFolder);

        string path = $"{PrefabFolder}/Stage2Puzzle.prefab";
        var root = new GameObject("Stage2Puzzle");
        root.AddComponent<Stage1PuzzleController>();
        var layout = root.AddComponent<StagePuzzleLayout>();

        var paths = new GameObject("Paths");
        paths.transform.SetParent(root.transform, false);

        CreateSampleRune(root.transform, 0, new Vector3(-1.5f, 1.5f, 0f), isStart: true);
        CreateSampleRune(root.transform, 1, new Vector3(1.5f, 1.5f, 0f), isStart: false);
        CreateSampleRune(root.transform, 2, new Vector3(1.5f, -1.5f, 0f), isStart: false);
        CreateSampleRune(root.transform, 3, new Vector3(-1.5f, -1.5f, 0f), isStart: false);

        var so = new SerializedObject(layout);
        so.FindProperty("pathsRoot").objectReferenceValue = paths.transform;
        var links = so.FindProperty("links");
        links.arraySize = 4;
        SetLink(links.GetArrayElementAtIndex(0), root.transform, 0, 1);
        SetLink(links.GetArrayElementAtIndex(1), root.transform, 1, 2);
        SetLink(links.GetArrayElementAtIndex(2), root.transform, 2, 3);
        SetLink(links.GetArrayElementAtIndex(3), root.transform, 3, 0);
        so.ApplyModifiedPropertiesWithoutUndo();
        layout.RebuildPathsFromLinks();

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);

        AssignPuzzlePrefabToPlayData("Stage02_PlayData", prefab);
        AssetDatabase.SaveAssets();

        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);
        EditorUtility.DisplayDialog(
            "Stage2 사각형 퍼즐",
            $"생성: {path}\nStage02_PlayData.puzzlePrefab에 연결했습니다.\n" +
            "Prefab을 열어 위치·연결을 자유롭게 수정하세요.",
            "확인");
    }

    [MenuItem("DevilMarriage/Puzzles/4. Assign Prefab To Selected PlayData")]
    public static void AssignSelectedPrefabToPlayData()
    {
        var prefab = Selection.activeGameObject;
        if (prefab == null || PrefabUtility.GetPrefabAssetType(prefab) == PrefabAssetType.NotAPrefab)
        {
            // Project 창에서 Prefab 에셋 선택
            prefab = Selection.activeObject as GameObject;
        }

        if (prefab == null)
        {
            EditorUtility.DisplayDialog("선택 없음", "Project 창에서 Puzzle Prefab을 선택한 뒤 실행하세요.", "확인");
            return;
        }

        string playDataName = EditorUtility.SaveFilePanelInProject(
            "연결할 StagePlayData 선택",
            "Stage02_PlayData",
            "asset",
            "이 Prefab을 넣을 PlayData를 고르세요.",
            PlayDataFolder);

        if (string.IsNullOrEmpty(playDataName))
            return;

        var playData = AssetDatabase.LoadAssetAtPath<StagePlayData>(playDataName);
        if (playData == null)
        {
            EditorUtility.DisplayDialog("실패", "StagePlayData 에셋이 아닙니다.", "확인");
            return;
        }

        playData.puzzlePrefab = prefab;
        EditorUtility.SetDirty(playData);
        AssetDatabase.SaveAssets();
        Debug.Log($"[StagePuzzlePrefabCreator] {playData.name}.puzzlePrefab = {prefab.name}");
    }

    [MenuItem("DevilMarriage/Puzzles/Open Prefab Folder")]
    public static void OpenPrefabFolder()
    {
        EnsureFolder("Assets/Prefabs");
        EnsureFolder(PrefabFolder);
        var folder = AssetDatabase.LoadAssetAtPath<Object>(PrefabFolder);
        Selection.activeObject = folder;
        EditorGUIUtility.PingObject(folder);
    }

    private static void AssignPuzzlePrefabToPlayData(string playDataAssetName, GameObject prefab)
    {
        string path = $"{PlayDataFolder}/{playDataAssetName}.asset";
        var playData = AssetDatabase.LoadAssetAtPath<StagePlayData>(path);
        if (playData == null)
        {
            Debug.LogWarning($"[StagePuzzlePrefabCreator] {path} 를 찾지 못했습니다.");
            return;
        }

        playData.puzzlePrefab = prefab;
        EditorUtility.SetDirty(playData);
    }

    private static void CreateSampleRune(Transform parent, int index, Vector3 localPos, bool isStart)
    {
        var go = new GameObject($"Rune{index}");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = LoadDefaultRuneSprite();
        sr.sortingOrder = 10;
        go.transform.localScale = Vector3.one * 0.6f;

        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.5f;

        var rune = go.AddComponent<RuneNode>();
        rune.Configure(index, isStart, mandatory: true, forbidden: false);
    }

    [MenuItem("DevilMarriage/Puzzles/Apply empty_r Sprite To All Puzzle Runes")]
    public static void ApplyEmptyRSpriteToAllPuzzleRunes()
    {
        var emptySprite = LoadDefaultRuneSprite();
        if (emptySprite == null)
        {
            EditorUtility.DisplayDialog("실패", "Assets/Art/empty_r.png 를 찾을 수 없습니다.", "확인");
            return;
        }

        int changed = 0;

        // 열린 씬
        foreach (var rune in Object.FindObjectsByType<RuneNode>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (ApplyEmptyRToRune(rune, emptySprite))
                changed++;
        }

        // Prefab 에셋
        var prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { PrefabFolder });
        foreach (var guid in prefabGuids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                bool dirty = false;
                foreach (var rune in root.GetComponentsInChildren<RuneNode>(true))
                {
                    if (ApplyEmptyRToRune(rune, emptySprite))
                    {
                        dirty = true;
                        changed++;
                    }
                }

                if (dirty)
                    PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("완료", $"empty_r 스프라이트를 {changed}개 룬에 적용했습니다.", "확인");
    }

    private static bool ApplyEmptyRToRune(RuneNode rune, Sprite emptySprite)
    {
        if (rune == null)
            return false;

        var sr = rune.GetComponent<SpriteRenderer>();
        if (sr == null)
            return false;

        if (sr.sprite == emptySprite)
            return false;

        Undo.RecordObject(sr, "Apply empty_r to Rune");
        sr.sprite = emptySprite;
        EditorUtility.SetDirty(sr);
        return true;
    }

    private static Sprite LoadDefaultRuneSprite()
    {
        // 기본 룬 이미지는 항상 empty_r
        const string emptyRPath = "Assets/Art/empty_r.png";
        var sprites = AssetDatabase.LoadAllAssetsAtPath(emptyRPath);
        foreach (var asset in sprites)
        {
            if (asset is Sprite sprite)
                return sprite;
        }

        var direct = AssetDatabase.LoadAssetAtPath<Sprite>(emptyRPath);
        if (direct != null)
            return direct;

        Debug.LogWarning("[StagePuzzlePrefabCreator] Assets/Art/empty_r.png 스프라이트를 찾지 못했습니다.");
        return AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
    }

    private static void SetLink(SerializedProperty linkProp, Transform root, int fromIndex, int toIndex)
    {
        var runes = root.GetComponentsInChildren<RuneNode>();
        RuneNode from = null;
        RuneNode to = null;
        foreach (var r in runes)
        {
            if (r.RuneIndex == fromIndex) from = r;
            if (r.RuneIndex == toIndex) to = r;
        }

        linkProp.FindPropertyRelative("from").objectReferenceValue = from;
        linkProp.FindPropertyRelative("to").objectReferenceValue = to;
        linkProp.FindPropertyRelative("isMandatory").boolValue = true;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
        string name = Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);

        AssetDatabase.CreateFolder(parent ?? "Assets", name);
    }
}
#endif
