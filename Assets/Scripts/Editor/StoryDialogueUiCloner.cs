#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// PrologueScene의 DialogueCanvas(위치/크기/이미지/버튼 배치)를 StoryScene으로 복제합니다.
/// Story 대사 데이터와 StorySceneController 진행 로직은 변경하지 않습니다.
/// </summary>
public static class StoryDialogueUiCloner
{
    private const string PrologueScenePath = "Assets/Scenes/PrologueScene.unity";
    private const string StoryScenePath = "Assets/Scenes/StoryScene.unity";

    [MenuItem("DevilMarriage/Copy Prologue DialogueCanvas To StoryScene")]
    [MenuItem("Tools/DevilMarriage/Copy Prologue DialogueCanvas To StoryScene")]
    public static void CopyPrologueDialogueCanvasToStoryScene()
    {
        try
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            var prologue = EditorSceneManager.OpenScene(PrologueScenePath, OpenSceneMode.Single);
            var sourceCanvas = GameObject.Find(DialogueUiBuilder.CanvasName);
            if (sourceCanvas == null)
            {
                Debug.LogError("[StoryDialogueUiCloner] PrologueScene에 DialogueCanvas가 없습니다.");
                return;
            }

            var clone = Object.Instantiate(sourceCanvas);
            clone.name = DialogueUiBuilder.CanvasName;
            Object.DontDestroyOnLoad(clone);

            var story = EditorSceneManager.OpenScene(StoryScenePath, OpenSceneMode.Single);

            // 기존 DialogueCanvas / 고아 bg·CharacterSprite 제거 (캔버스 밖 잔여물)
            var existingCanvas = GameObject.Find(DialogueUiBuilder.CanvasName);
            if (existingCanvas != null)
                Object.DestroyImmediate(existingCanvas);

            RemoveOrphanNamed("bg");
            RemoveOrphanNamed("CharacterSprite");

            var yarn = GameObject.Find("Dialogue System");
            if (yarn != null)
            {
                yarn.SetActive(false);
                Debug.Log("[StoryDialogueUiCloner] Yarn Dialogue System을 비활성화했습니다.");
            }

            var placed = Object.Instantiate(clone);
            placed.name = DialogueUiBuilder.CanvasName;
            Object.DestroyImmediate(clone);

            // Camera 연결 (Screen Space - Camera)
            var canvas = placed.GetComponent<Canvas>();
            var mainCam = Camera.main;
            if (canvas != null && mainCam != null)
                canvas.worldCamera = mainCam;

            var manager = placed.GetComponent<DialogueManager>();
            if (manager == null)
                manager = placed.AddComponent<DialogueManager>();

            WireStorySceneController(manager, placed);
            WireSceneChangerPortrait(placed);

            EditorSceneManager.MarkSceneDirty(story);
            EditorSceneManager.SaveScene(story);
            Debug.Log("[StoryDialogueUiCloner] Prologue DialogueCanvas를 StoryScene에 복제하고 저장했습니다. 대사 데이터는 변경하지 않았습니다.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[StoryDialogueUiCloner] 실패: {e}");
        }
    }

    private static void RemoveOrphanNamed(string objectName)
    {
        var all = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (var t in all)
        {
            if (t == null || t.name != objectName)
                continue;
            if (!t.gameObject.scene.IsValid() || t.gameObject.scene != EditorSceneManager.GetActiveScene())
                continue;
            // DialogueCanvas 하위는 유지
            if (t.GetComponentInParent<DialogueManager>() != null)
                continue;
            Object.DestroyImmediate(t.gameObject);
        }
    }

    private static void WireStorySceneController(DialogueManager manager, GameObject canvasRoot)
    {
        var controller = Object.FindAnyObjectByType<StorySceneController>();
        if (controller == null)
            return;

        var so = new SerializedObject(controller);
        var dmProp = so.FindProperty("dialogueManager");
        if (dmProp != null)
            dmProp.objectReferenceValue = manager;

        var bg = canvasRoot.transform.Find("bg");
        if (bg == null)
            bg = FindDeep(canvasRoot.transform, "bg");
        var bgProp = so.FindProperty("backgroundImage");
        if (bgProp != null && bg != null)
            bgProp.objectReferenceValue = bg.GetComponent<Image>();

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(controller);
    }

    private static void WireSceneChangerPortrait(GameObject canvasRoot)
    {
        var changer = Object.FindAnyObjectByType<SceneChanger>();
        if (changer == null)
            return;

        var character = canvasRoot.transform.Find("CharacterSprite");
        if (character == null)
            character = FindDeep(canvasRoot.transform, "CharacterSprite");

        var so = new SerializedObject(changer);
        var portraitProp = so.FindProperty("characterPortrait");
        if (portraitProp != null && character != null)
            portraitProp.objectReferenceValue = character.GetComponent<Image>();

        var runner = so.FindProperty("dialogueRunner");
        if (runner != null)
            runner.objectReferenceValue = null;

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(changer);
    }

    private static Transform FindDeep(Transform root, string name)
    {
        if (root.name == name)
            return root;
        for (int i = 0; i < root.childCount; i++)
        {
            var found = FindDeep(root.GetChild(i), name);
            if (found != null)
                return found;
        }
        return null;
    }
}
#endif
