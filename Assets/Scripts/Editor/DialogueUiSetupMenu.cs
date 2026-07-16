#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Play 없이 대화 UI를 씬/프리팹에 배치하는 에디터 메뉴입니다.
/// DisplayDialog / 즉시 Selection 변경은 Unity Inspector를 깨뜨릴 수 있어 사용하지 않습니다.
/// </summary>
public static class DialogueUiSetupMenu
{
    private const string PrefabFolder = "Assets/Prefabs/Dialogue";
    private const string PrefabPath = PrefabFolder + "/DialogueUI.prefab";
    private const string KoreanFontPath = "Assets/Fonts/NotoSerifKR-Regular SDF.asset";
    private const string TempPrefabRootName = "DialogueCanvas_TempPrefabBuild";

    [MenuItem("DevilMarriage/Add Dialogue UI To Open Scene")]
    [MenuItem("Tools/DevilMarriage/Add Dialogue UI To Open Scene")]
    public static void AddDialogueUiToOpenScene()
    {
        try
        {
            var font = LoadKoreanFont();
            bool replace = GameObject.Find(DialogueUiBuilder.CanvasName) != null;

            // Dialog 대신: 이미 있으면 교체하지 않고 로그만 남김
            if (replace)
            {
                Debug.LogWarning("[DialogueUiSetup] 이미 DialogueCanvas가 있습니다. 중복 생성하지 않습니다. 기존 오브젝트를 수정하세요.");
                return;
            }

            var result = DialogueUiBuilder.BuildNew(font, forceReplace: false);
            if (result == null)
            {
                Debug.LogError("[DialogueUiSetup] DialogueCanvas를 만들지 못했습니다.");
                return;
            }

            var handles = result.Value;
            var manager = EnsureDialogueManager(handles);
            WireManager(manager, handles, font);

            Undo.RegisterCreatedObjectUndo(handles.canvas.gameObject, "Add Dialogue UI");
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[DialogueUiSetup] DialogueCanvas 추가 완료. Hierarchy에서 위치/크기를 조절한 뒤 Ctrl+S로 저장하세요.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[DialogueUiSetup] Add Dialogue UI 실패: {e}");
        }
    }

    [MenuItem("DevilMarriage/Create Dialogue UI Prefab")]
    [MenuItem("Tools/DevilMarriage/Create Dialogue UI Prefab")]
    public static void CreateDialogueUiPrefab()
    {
        try
        {
            EnsureFolder("Assets/Prefabs");
            EnsureFolder(PrefabFolder);

            var font = LoadKoreanFont();
            var leftover = GameObject.Find(TempPrefabRootName);
            if (leftover != null)
                Object.DestroyImmediate(leftover);

            var temp = DialogueUiBuilder.BuildNew(font, forceReplace: false, canvasName: TempPrefabRootName);
            if (temp == null)
            {
                Debug.LogError("[DialogueUiSetup] 프리팹용 DialogueCanvas를 만들지 못했습니다.");
                return;
            }

            var handles = temp.Value;
            handles.canvas.gameObject.name = DialogueUiBuilder.CanvasName;

            var manager = EnsureDialogueManager(handles);
            WireManager(manager, handles, font);

            PrefabUtility.SaveAsPrefabAsset(handles.canvas.gameObject, PrefabPath);
            Object.DestroyImmediate(handles.canvas.gameObject);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[DialogueUiSetup] 프리팹 생성 완료: {PrefabPath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[DialogueUiSetup] Create Prefab 실패: {e}");
            var leftover = GameObject.Find(TempPrefabRootName);
            if (leftover != null)
                Object.DestroyImmediate(leftover);
        }
    }

    [MenuItem("DevilMarriage/Disable Yarn Dialogue System In Open Scene")]
    [MenuItem("Tools/DevilMarriage/Disable Yarn Dialogue System In Open Scene")]
    public static void DisableYarnDialogueSystemInOpenScene()
    {
        GameObject yarn = GameObject.Find("Dialogue System");
        if (yarn == null)
        {
            var all = Resources.FindObjectsOfTypeAll<Transform>();
            foreach (var t in all)
            {
                if (t == null || t.name != "Dialogue System")
                    continue;
                if (t.gameObject.scene.IsValid() && t.gameObject.scene == EditorSceneManager.GetActiveScene())
                {
                    yarn = t.gameObject;
                    break;
                }
            }
        }

        if (yarn == null)
        {
            Debug.Log("[DialogueUiSetup] 이 씬에 'Dialogue System'이 없습니다.");
            return;
        }

        Undo.RecordObject(yarn, "Disable Yarn Dialogue System");
        yarn.SetActive(false);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[DialogueUiSetup] Yarn Dialogue System을 비활성화했습니다. Ctrl+S로 저장하세요.");
    }

    private static DialogueManager EnsureDialogueManager(DialogueUiBuilder.Result handles)
    {
        var manager = handles.canvas.GetComponent<DialogueManager>();
        if (manager == null)
            manager = handles.canvas.gameObject.AddComponent<DialogueManager>();
        return manager;
    }

    private static void WireManager(DialogueManager manager, DialogueUiBuilder.Result handles, TMP_FontAsset font)
    {
        var so = new SerializedObject(manager);
        SetObjectRef(so, "speakerNameText", handles.speakerNameText);
        SetObjectRef(so, "dialogueBodyText", handles.dialogueBodyText);
        SetObjectRef(so, "nextButton", handles.nextButton);
        SetObjectRef(so, "continueIcon", handles.nextButton != null ? handles.nextButton.gameObject : null);
        SetObjectRef(so, "autoButton", handles.autoButton);
        SetObjectRef(so, "skipButton", handles.skipButton);
        SetObjectRef(so, "logButton", handles.logButton);
        SetObjectRef(so, "settingButton", handles.settingButton);
        SetObjectRef(so, "dialogueFont", font);
        SetBool(so, "createUiAtRuntimeIfMissing", false);

        var bg = GameObject.Find("bg");
        if (bg != null)
            SetObjectRef(so, "backgroundImage", bg.GetComponent<Image>());

        var character = GameObject.Find("CharacterSprite");
        if (character != null)
            SetObjectRef(so, "characterImage", character.GetComponent<Image>());

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(manager);
    }

    private static void SetObjectRef(SerializedObject so, string propertyName, Object value)
    {
        var prop = so.FindProperty(propertyName);
        if (prop != null)
            prop.objectReferenceValue = value;
    }

    private static void SetBool(SerializedObject so, string propertyName, bool value)
    {
        var prop = so.FindProperty(propertyName);
        if (prop != null)
            prop.boolValue = value;
    }

    private static TMP_FontAsset LoadKoreanFont()
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(KoreanFontPath);
        if (font == null)
            font = TMP_Settings.defaultFontAsset;
        return font;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        var parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
        var folderName = System.IO.Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(folderName))
            AssetDatabase.CreateFolder(parent, folderName);
    }
}
#endif
