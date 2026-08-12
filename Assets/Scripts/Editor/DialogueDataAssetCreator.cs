#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Yarn에 있던 프롤로그/스테이지 1~3 대사를 ScriptableObject 에셋으로 저장하는 에디터 메뉴입니다.
/// Unity 메뉴: DevilMarriage → Create Dialogue Data (Stage 1-3 + Prologue)
/// </summary>
public static class DialogueDataAssetCreator
{
    private const string DialogueFolder = "Assets/Data/Dialogue";
    private const string ResourcesFolder = "Assets/Resources";
    private const string StagesFolder = "Assets/Data/Stages";

    // 상단 메뉴바 File 옆에 뜨는 DevilMarriage / Tools 양쪽에서 실행할 수 있게 합니다.
    [MenuItem("DevilMarriage/Create Dialogue Data (Stage 1-4 + Prologue)")]
    [MenuItem("Tools/DevilMarriage/Create Dialogue Data (Stage 1-4 + Prologue)")]
    public static void CreateAllDialogueData()
    {
        EnsureFolder("Assets/Data");
        EnsureFolder(DialogueFolder);
        EnsureFolder(ResourcesFolder);

        var prologue = CreatePrologueData();
        var stage1 = CreateStageDialogue(1);
        var stage2 = CreateStageDialogue(2);
        var stage3 = CreateStageDialogue(3);
        var stage4 = CreateStageDialogue(4);

        LinkStageData(1, stage1);
        LinkStageData(2, stage2);
        LinkStageData(3, stage3);
        LinkStageData(4, stage4);

        var resourcesPath = $"{ResourcesFolder}/PrologueDialogueData.asset";
        var resourcesPrologue = AssetDatabase.LoadAssetAtPath<PrologueDialogueData>(resourcesPath);
        if (resourcesPrologue == null)
        {
            resourcesPrologue = Object.Instantiate(prologue);
            AssetDatabase.CreateAsset(resourcesPrologue, resourcesPath);
        }
        else
        {
            EditorUtility.CopySerialized(prologue, resourcesPrologue);
            EditorUtility.SetDirty(resourcesPrologue);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[DialogueDataAssetCreator] 프롤로그 + Stage 1~4 대화 데이터 생성/갱신 완료.");
    }

    private static PrologueDialogueData CreatePrologueData()
    {
        var path = $"{DialogueFolder}/PrologueDialogueData.asset";
        var asset = AssetDatabase.LoadAssetAtPath<PrologueDialogueData>(path);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<PrologueDialogueData>();
            AssetDatabase.CreateAsset(asset, path);
        }

        asset.narrationLines = DialogueContentLibrary.BuildPrologueNarration();
        asset.roomLines = DialogueContentLibrary.BuildPrologueRoom();
        EditorUtility.SetDirty(asset);
        return asset;
    }

    private static StageDialogueData CreateStageDialogue(int stageNumber)
    {
        var path = $"{DialogueFolder}/Stage{stageNumber:00}_DialogueData.asset";
        var asset = AssetDatabase.LoadAssetAtPath<StageDialogueData>(path);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<StageDialogueData>();
            AssetDatabase.CreateAsset(asset, path);
        }

        var runtime = DialogueContentLibrary.CreateStageRuntime(stageNumber);
        asset.stageNumber = stageNumber;
        asset.openLines = runtime.openLines;
        asset.closeLines = runtime.closeLines;
        EditorUtility.SetDirty(asset);
        Object.DestroyImmediate(runtime);
        return asset;
    }

    private static void LinkStageData(int stageNumber, StageDialogueData dialogue)
    {
        var path = $"{StagesFolder}/Stage{stageNumber:00}_Data.asset";
        var stage = AssetDatabase.LoadAssetAtPath<StageData>(path);
        if (stage == null)
        {
            Debug.LogWarning($"[DialogueDataAssetCreator] {path} 없음. DevilMarriage/Create Stage 1-3 Data 를 먼저 실행하세요.");
            return;
        }

        stage.dialogueData = dialogue;
        EditorUtility.SetDirty(stage);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        var folderName = Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(folderName))
            AssetDatabase.CreateFolder(parent, folderName);
    }
}
#endif
