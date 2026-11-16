using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class BandDisplayQualityTools
{
    const string Request = "Temp/BandDisplayRepair.request";
    [InitializeOnLoadMethod]
    static void RestoreIfRequested()
    {
        if (File.Exists(Request)) EditorApplication.delayCall += ApplyRequested;
    }
    static void ApplyRequested()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) { EditorApplication.delayCall += ApplyRequested; return; }
        if (EditorApplication.isPlaying) { Debug.LogWarning("[Band] Stop Play mode before display repair."); return; }
        if (!File.Exists(Request)) return;
        File.Delete(Request);
        RepairDisplayAndQuality();
    }
    [MenuItem("DevilMarriage/Band/Restore 16x9 and Image Quality")]
    public static void RepairDisplayAndQuality()
    {
        if (EditorApplication.isPlaying) { Debug.LogWarning("[Band] Stop Play mode first."); return; }
        BandSceneTools.Repair();
        var cam = Camera.main;
        cam.rect = new Rect(0,0,1,1); cam.ResetAspect(); cam.allowDynamicResolution = false;
        foreach (var folder in new [] { "Assets/Art/BandScene", "Assets/Prefabs/Band" })
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new [] {folder}))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (!importer) continue;
            importer.maxTextureSize = 4096;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.crunchedCompression = false;
            importer.mipmapEnabled = false;
            importer.ignoreMipmapLimit = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.filterMode = FilterMode.Bilinear;
            var platform = importer.GetPlatformTextureSettings("Standalone");
            platform.overridden = false;
            importer.SetPlatformTextureSettings(platform);
            importer.SaveAndReimport();
        }
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(cam.gameObject.scene);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(cam.gameObject.scene);
        FitFullHD();
        AssetDatabase.SaveAssets();
        File.WriteAllText("Temp/BandDisplayRepairReport.txt", "Restored planned 24x13.5 layout and camera. Full HD 1920x1080. Band textures uncompressed, max 4096, no NPOT resizing or mipmap reduction.\n"+Camera.main.aspect);
        Debug.Log("[Band] Restored 16:9 layout, Full HD preview and full-quality source textures.");
    }
    static void FitFullHD()
    {
        var assembly = typeof(Editor).Assembly;
        var sizesType = assembly.GetType("UnityEditor.GameViewSizes");
        var singleton = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
        var sizes = singleton.GetProperty("instance").GetValue(null);
        var groupType = assembly.GetType("UnityEditor.GameViewSizeGroupType");
        var group = sizesType.GetMethod("GetGroup").Invoke(sizes,new [] {Enum.Parse(groupType,"Standalone")});
        var labels = (string[])group.GetType().GetMethod("GetDisplayTexts").Invoke(group,null);
        int index = Array.FindIndex(labels,x=>x.Contains("1920x1080") || x.Contains("1920 x 1080"));
        var viewType = assembly.GetType("UnityEditor.GameView");
        var view = EditorWindow.GetWindow(viewType);
        if (index >= 0) viewType.GetProperty("selectedSizeIndex").SetValue(view,index);
        var lowRes = viewType.GetProperty("lowResolutionForAspectRatios",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
        if (lowRes != null && lowRes.CanWrite) lowRes.SetValue(view,false);
        view.Repaint();
    }
}
