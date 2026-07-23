#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Assets/Art/주인공 스프라이트를 PrologueScene DialogueManager에 연결하고
/// 프롤로그 대화 데이터를 최신 DialogueContentLibrary로 갱신합니다.
/// </summary>
public static class ProloguePortraitSetupMenu
{
    private const string ArtFolder = "Assets/Art/주인공";
    private const string PrologueScenePath = "Assets/Scenes/PrologueScene.unity";

    [MenuItem("DevilMarriage/Wire Prologue Character Sprites + Refresh Dialogue")]
    [MenuItem("Tools/DevilMarriage/Wire Prologue Character Sprites + Refresh Dialogue")]
    public static void WireAndRefresh()
    {
        DialogueDataAssetCreator.CreateAllDialogueData();

        var sprites = LoadProtagonistSprites();
        if (sprites.Count == 0)
        {
            Debug.LogError($"[ProloguePortrait] {ArtFolder}에서 스프라이트를 찾지 못했습니다.");
            return;
        }

        var scene = EditorSceneManager.OpenScene(PrologueScenePath, OpenSceneMode.Single);
        var managers = Object.FindObjectsByType<DialogueManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (managers.Length == 0)
        {
            Debug.LogError("[ProloguePortrait] DialogueManager가 없습니다.");
            return;
        }

        foreach (var manager in managers)
            ApplyToManager(manager, sprites);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log($"[ProloguePortrait] 표정 {sprites.Count}개 연결 + 프롤로그 대사 갱신 완료.");
    }

    private static Dictionary<string, Sprite> LoadProtagonistSprites()
    {
        var map = new Dictionary<string, Sprite>();
        if (!AssetDatabase.IsValidFolder(ArtFolder))
            return map;

        foreach (var guid in AssetDatabase.FindAssets("t:Sprite", new[] { ArtFolder }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
                continue;

            var fileName = Path.GetFileNameWithoutExtension(path);
            map[fileName] = sprite;

            // 주인공_잠깸 → 잠깸 별칭
            const string prefix = "주인공_";
            if (fileName.StartsWith(prefix))
                map[fileName.Substring(prefix.Length)] = sprite;
        }

        return map;
    }

    private static void ApplyToManager(DialogueManager manager, Dictionary<string, Sprite> sprites)
    {
        var so = new SerializedObject(manager);

        SetSprite(so, "portraitDefault", Find(sprites, "주인공_기본", "기본"));
        SetSprite(so, "portraitHappy", Find(sprites, "주인공_행복", "행복"));
        SetSprite(so, "portraitNervous", Find(sprites, "주인공_긴장", "긴장"));

        var entries = so.FindProperty("expressionSprites");
        entries.ClearArray();

        AddEntry(entries, "wake", Find(sprites, "주인공_잠깸", "잠깸"));
        AddEntry(entries, "dark", Find(sprites, "주인공_깜깜", "깜깜"));
        AddEntry(entries, "angry", Find(sprites, "주인공_화남", "화남"));
        AddEntry(entries, "cry", Find(sprites, "주인공_울음", "울음"));
        AddEntry(entries, "sparkle", Find(sprites, "주인공_반짝", "반짝"));
        AddEntry(entries, "scheming", Find(sprites, "주인공_깜깜웃음", "깜깜웃음"));
        AddEntry(entries, "base", Find(sprites, "주인공_베이스", "베이스"));

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(manager);

        // CharacterSprite 기본 이미지도 교체
        var characterProp = so.FindProperty("characterImage");
        if (characterProp?.objectReferenceValue is UnityEngine.UI.Image image)
        {
            var def = Find(sprites, "주인공_기본", "기본");
            if (def != null)
            {
                Undo.RecordObject(image, "Set CharacterSprite default");
                image.sprite = def;
                image.type = UnityEngine.UI.Image.Type.Simple;
                image.preserveAspect = true;
                EditorUtility.SetDirty(image);
            }
        }
    }

    private static Sprite Find(Dictionary<string, Sprite> sprites, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (sprites.TryGetValue(key, out var sprite) && sprite != null)
                return sprite;
        }

        return null;
    }

    private static void SetSprite(SerializedObject so, string propertyName, Sprite sprite)
    {
        var prop = so.FindProperty(propertyName);
        if (prop != null)
            prop.objectReferenceValue = sprite;
    }

    private static void AddEntry(SerializedProperty arrayProp, string id, Sprite sprite)
    {
        if (sprite == null)
        {
            Debug.LogWarning($"[ProloguePortrait] '{id}' 스프라이트를 찾지 못해 건너뜁니다.");
            return;
        }

        int index = arrayProp.arraySize;
        arrayProp.InsertArrayElementAtIndex(index);
        var element = arrayProp.GetArrayElementAtIndex(index);
        element.FindPropertyRelative("id").stringValue = id;
        element.FindPropertyRelative("sprite").objectReferenceValue = sprite;
    }
}
#endif
