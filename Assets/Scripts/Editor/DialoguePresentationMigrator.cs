#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Yarn Dialogue System 아래의 bg/CharacterSprite를 DialogueCanvas로 옮깁니다.
/// DisplayDialog는 사용하지 않습니다 (Unity Inspector 크래시 유발).
/// </summary>
public static class DialoguePresentationMigrator
{
    [MenuItem("DevilMarriage/Migrate Background & Character To DialogueCanvas")]
    [MenuItem("Tools/DevilMarriage/Migrate Background & Character To DialogueCanvas")]
    public static void MigratePresentationToDialogueCanvas()
    {
        try
        {
            var canvasGo = GameObject.Find(DialogueUiBuilder.CanvasName);
            if (canvasGo == null)
            {
                Debug.LogError("[Migrate] DialogueCanvas가 없습니다. 먼저 Add Dialogue UI To Open Scene 을 실행하세요.");
                return;
            }

            var canvas = canvasGo.GetComponent<Canvas>();
            if (canvas != null)
            {
                Undo.RecordObject(canvas, "DialogueCanvas RenderMode");
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 200;
            }

            var bg = FindSceneObject("bg");
            var character = FindSceneObject("CharacterSprite");

            if (bg != null)
                ReparentAsCanvasChild(bg, canvasGo.transform, 0);
            if (character != null)
                ReparentAsCanvasChild(character, canvasGo.transform, 1);

            var manager = canvasGo.GetComponent<DialogueManager>();
            if (manager == null)
                manager = Undo.AddComponent<DialogueManager>(canvasGo);

            WireDialogueManager(manager, bg, character);
            CopyPortraitsFromSceneChanger(manager);

            // Yarn은 Dialog 없이 항상 비활성화만 수행 (삭제는 수동)
            var yarn = FindSceneObject("Dialogue System");
            if (yarn != null)
            {
                Undo.RecordObject(yarn, "Disable Yarn Dialogue System");
                yarn.SetActive(false);
                Debug.Log("[Migrate] Yarn Dialogue System을 비활성화했습니다. 필요 없으면 Hierarchy에서 직접 삭제하세요.");
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[Migrate] bg/CharacterSprite를 DialogueCanvas로 옮겼습니다. Ctrl+S로 저장하세요.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Migrate] 실패: {e}");
        }
    }

    private static GameObject FindSceneObject(string name)
    {
        var found = GameObject.Find(name);
        if (found != null)
            return found;

        var all = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (var t in all)
        {
            if (t == null || t.name != name)
                continue;
            if (!t.gameObject.scene.IsValid())
                continue;
            if (t.gameObject.scene != EditorSceneManager.GetActiveScene())
                continue;
            return t.gameObject;
        }

        return null;
    }

    private static void ReparentAsCanvasChild(GameObject go, Transform canvas, int insertIndex)
    {
        Undo.SetTransformParent(go.transform, canvas, "Reparent to DialogueCanvas");
        go.SetActive(true);
        go.transform.SetSiblingIndex(Mathf.Clamp(insertIndex, 0, canvas.childCount - 1));

        foreach (var img in go.GetComponentsInChildren<Image>(true))
        {
            Undo.RecordObject(img, "Disable Raycast Target");
            img.raycastTarget = false;
        }

        EditorUtility.SetDirty(go);
    }

    private static void WireDialogueManager(DialogueManager manager, GameObject bg, GameObject character)
    {
        var so = new SerializedObject(manager);
        if (bg != null)
        {
            var prop = so.FindProperty("backgroundImage");
            if (prop != null)
                prop.objectReferenceValue = bg.GetComponent<Image>();
        }

        if (character != null)
        {
            var prop = so.FindProperty("characterImage");
            if (prop != null)
                prop.objectReferenceValue = character.GetComponent<Image>();
        }

        var adjust = so.FindProperty("adjustCharacterPositionFromData");
        if (adjust != null)
            adjust.boolValue = false;

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(manager);
    }

    private static void CopyPortraitsFromSceneChanger(DialogueManager manager)
    {
        var changer = Object.FindAnyObjectByType<SceneChanger>();
        if (changer == null)
            return;

        manager.SetPortraitSprites(
            changer.PortraitDefault,
            changer.PortraitHappy,
            changer.PortraitNervous);

        var so = new SerializedObject(manager);
        SetSprite(so, "portraitDefault", changer.PortraitDefault);
        SetSprite(so, "portraitHappy", changer.PortraitHappy);
        SetSprite(so, "portraitNervous", changer.PortraitNervous);
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(manager);
    }

    private static void SetSprite(SerializedObject so, string name, Sprite sprite)
    {
        var prop = so.FindProperty(name);
        if (prop != null)
            prop.objectReferenceValue = sprite;
    }
}
#endif
