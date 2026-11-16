using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Reapplies the BandScene layout without replacing scene objects or asset GUIDs.
public static class BandSceneTools
{
    [MenuItem("DevilMarriage/Band/Repair Scene Layout")]
    public static void Repair()
    {
        if (EditorApplication.isPlaying) { Debug.LogWarning("[Band] Stop Play mode before repairing the scene."); return; }
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.name != "BandScene") throw new InvalidOperationException("Open BandScene first.");
        Undo.RecordObjects(scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<Transform>(true)).Cast<UnityEngine.Object>().ToArray(), "Repair Band layout");
        var renderers = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<SpriteRenderer>(true)).ToArray();
        var background = renderers.First(x => x.name.StartsWith("배경_"));
        Place(background, new Vector3(0,0,0), 24, 13.5f, -100);
        var laneNames = new [] { "레인_쾅_0", "래인_락_0", "래인_붐_0" };
        for (int i = 0; i < 3; i++)
            Place(renderers.First(x => x.name == laneNames[i]), new Vector3((i-1)*2.6f,1.2f,0), i == 0 ? 3.9f : 1.95f,10.1f, -20);
        Place(renderers.First(x => x.name.StartsWith("판정선")), new Vector3(0,-3.5f,0), 8.5f,1.25f, 30);
        Place(renderers.First(x => x.name.StartsWith("성공_판넬")), new Vector3(-8.9f,4.75f,0), 4.5f,2.7f, 40);
        Place(renderers.First(x => x.name.StartsWith("시간_판넬")), new Vector3(8.9f,4.75f,0), 4.5f,2.7f, 40);
        var spawner = UnityEngine.Object.FindFirstObjectByType<BandNoteSpawner>();
        var so = new SerializedObject(spawner);
        so.FindProperty("judgementY").floatValue = -3.5f;
        so.FindProperty("koreanSourceFont").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/NotoSerifKR-Regular.ttf");
        so.ApplyModifiedProperties();
        var cam = Camera.main;
        cam.transform.position = new Vector3(0,0,-10); cam.orthographic = true; cam.orthographicSize = 6.75f;
        cam.backgroundColor = new Color(.045f,.01f,.04f); cam.clearFlags = CameraClearFlags.SolidColor;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
    }
    static void Place(SpriteRenderer r, Vector3 pos, float width, float height, int order)
    {
        Undo.RecordObject(r, "Band sorting");
        r.gameObject.SetActive(true); r.enabled = true; r.color = Color.white;
        r.sortingLayerID = 0; r.sortingOrder = order;
        r.transform.position = pos;
        Vector2 size = r.sprite.bounds.size;
        var parentScale = r.transform.parent ? r.transform.parent.lossyScale : Vector3.one;
        r.transform.localScale = new Vector3(width/size.x/parentScale.x,height/size.y/parentScale.y,1);
        EditorUtility.SetDirty(r);
    }
}
