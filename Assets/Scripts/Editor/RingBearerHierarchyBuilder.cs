#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// RingBearerScene에 균형 게임 UI를 Hierarchy로 올려 둡니다.
/// 이미 있으면 위치는 건드리지 않고, 끊긴 참조만 다시 연결합니다.
/// </summary>
[InitializeOnLoad]
public static class RingBearerHierarchyBuilder
{
    private const string ArtFolder = "Assets/Art/RingBearer";

    private static bool _queued;

    static RingBearerHierarchyBuilder()
    {
        if (Application.isBatchMode)
            return;

        EditorSceneManager.sceneOpened += (_, _) => Schedule();
        Schedule();
    }

    public static void BuildAndSaveBatch()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/RingBearerScene.unity");
        TryBuild(forceLog: true);
        EditorSceneManager.SaveScene(scene);
    }

    [UnityEditor.Callbacks.DidReloadScripts]
    private static void OnScriptsReloaded()
    {
        if (Application.isBatchMode)
            return;

        Schedule();
    }

    [MenuItem("DevilMarriage/Build Ring Bearer UI In Open Scene")]
    [MenuItem("Tools/DevilMarriage/Build Ring Bearer UI In Open Scene")]
    public static void BuildFromMenu()
    {
        TryBuild(forceLog: true);
    }

    private static void Schedule()
    {
        if (_queued)
            return;

        _queued = true;
        EditorApplication.delayCall += () =>
        {
            _queued = false;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                Schedule();
                return;
            }

            TryBuild(forceLog: false);
        };
    }

    private static void TryBuild(bool forceLog)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        var controllers = Object.FindObjectsByType<RingBearerMinigameController>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        var built = 0;
        foreach (var controller in controllers)
        {
            if (controller == null)
                continue;

            var scene = controller.gameObject.scene;
            if (!scene.IsValid() || !scene.isLoaded)
                continue;

            var hadCanvas = controller.transform.Find("RingBearerCanvas") != null;
            if (hadCanvas && controller.HasWiredHierarchy)
                continue;

            EnsureSprites();
            Undo.RecordObject(controller, "Build Ring Bearer UI");
            controller.EnsureSceneHierarchy();

            var canvas = controller.transform.Find("RingBearerCanvas");
            if (!hadCanvas && canvas != null)
                Undo.RegisterCreatedObjectUndo(canvas.gameObject, "Build Ring Bearer UI");

            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(scene);
            built++;
        }

        if (built > 0)
        {
            Debug.Log("[RingBearer] Hierarchy에 RingBearerCanvas를 올렸습니다. 자식 위치를 조절한 뒤 씬을 저장하세요. Warning과 Result는 꺼 두었으니, 보려면 체크하면 됩니다.");
        }
        else if (forceLog)
        {
            Debug.Log("[RingBearer] 열린 RingBearerScene에 추가할 UI가 없습니다. 이미 Hierarchy에 있거나 씬이 열려 있지 않습니다.");
        }
    }

    private static void EnsureSprites()
    {
        if (!AssetDatabase.IsValidFolder(ArtFolder))
            AssetDatabase.CreateFolder("Assets/Art", "RingBearer");

        EnsureSprite("White.png", SolidTexture(32, Color.white), FilterMode.Point);
        EnsureSprite("Circle.png", CircleTexture(512), FilterMode.Bilinear);
        EnsureSprite("ArrowLeft.png", ArrowTexture(64, true), FilterMode.Bilinear);
        EnsureSprite("ArrowRight.png", ArrowTexture(64, false), FilterMode.Bilinear);
    }

    private static void EnsureSprite(string fileName, Texture2D texture, FilterMode filter)
    {
        var path = ArtFolder + "/" + fileName;
        if (AssetDatabase.LoadAssetAtPath<Sprite>(path) != null)
        {
            Object.DestroyImmediate(texture);
            return;
        }

        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.filterMode = filter;
        importer.npotScale = TextureImporterNPOTScale.None;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        settings.filterMode = filter;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
    }

    private static Texture2D SolidTexture(int size, Color color)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = color;
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    private static Texture2D CircleTexture(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
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
        return tex;
    }

    private static Texture2D ArrowTexture(int size, bool pointLeft)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
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
        return tex;
    }
}
#endif
