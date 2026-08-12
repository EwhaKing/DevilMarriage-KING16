#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Stage 1~4용 StageData / StagePlayData / StageDatabase를 생성하거나 갱신합니다.
/// </summary>
public static class StageDataAssetCreator
{
    private const string DataRoot = "Assets/Data";
    private const string StagesFolder = "Assets/Data/Stages";
    private const string PlayDataFolder = "Assets/Data/PlayData";
    private const string DialogueFolder = "Assets/Data/Dialogue";
    private const string ResourcesFolder = "Assets/Resources";

    [MenuItem("DevilMarriage/Create Stage 1-4 Data")]
    [MenuItem("Tools/DevilMarriage/Create Stage 1-4 Data")]
    public static void CreateStageDataAssets()
    {
        EnsureFolder(DataRoot);
        EnsureFolder(StagesFolder);
        EnsureFolder(PlayDataFolder);
        EnsureFolder(ResourcesFolder);

        var stageBgm = LoadAudioClip("Assets/Sounds/BGM/스테이지브금.mp3");
        var background = LoadSprite("Assets/Art/Stage1Background.jpg");

        var playData1 = CreateOrLoadPlayData("Stage01_PlayData", "1-1", "의식의 시작");
        var playData2 = CreateOrLoadPlayData("Stage02_PlayData", "1-2", "임시 스테이지 2");
        var playData3 = CreateOrLoadPlayData("Stage03_PlayData", "1-3", "임시 스테이지 3");
        var playData4 = CreateOrLoadPlayData("Stage04_PlayData", "1-4", "마계의 좌표");

        var dialogue1 = AssetDatabase.LoadAssetAtPath<StageDialogueData>($"{DialogueFolder}/Stage01_DialogueData.asset");
        var dialogue2 = AssetDatabase.LoadAssetAtPath<StageDialogueData>($"{DialogueFolder}/Stage02_DialogueData.asset");
        var dialogue3 = AssetDatabase.LoadAssetAtPath<StageDialogueData>($"{DialogueFolder}/Stage03_DialogueData.asset");
        var dialogue4 = AssetDatabase.LoadAssetAtPath<StageDialogueData>($"{DialogueFolder}/Stage04_DialogueData.asset");

        var stage1 = CreateOrLoadStageData("Stage01_Data", 1, "의식의 시작", dialogue1, playData1, background, stageBgm);
        var stage2 = CreateOrLoadStageData("Stage02_Data", 2, "임시 스테이지 2", dialogue2, playData2, background, stageBgm);
        var stage3 = CreateOrLoadStageData("Stage03_Data", 3, "임시 스테이지 3", dialogue3, playData3, background, stageBgm);
        var stage4 = CreateOrLoadStageData("Stage04_Data", 4, "마계의 좌표", dialogue4, playData4, background, stageBgm);

        var databasePath = $"{DataRoot}/StageDatabase.asset";
        var database = AssetDatabase.LoadAssetAtPath<StageDatabase>(databasePath);
        if (database == null)
        {
            database = ScriptableObject.CreateInstance<StageDatabase>();
            AssetDatabase.CreateAsset(database, databasePath);
        }

        SetDatabaseStages(database, stage1, stage2, stage3, stage4);

        var resourcesDatabasePath = $"{ResourcesFolder}/StageDatabase.asset";
        var resourcesDatabase = AssetDatabase.LoadAssetAtPath<StageDatabase>(resourcesDatabasePath);
        if (resourcesDatabase == null)
        {
            resourcesDatabase = Object.Instantiate(database);
            AssetDatabase.CreateAsset(resourcesDatabase, resourcesDatabasePath);
        }

        SetDatabaseStages(resourcesDatabase, stage1, stage2, stage3, stage4);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[StageDataAssetCreator] Stage 1~4 데이터 생성 완료. 대화 데이터가 없다면 DevilMarriage/Create Dialogue Data 도 실행하세요.");
    }

    private static void SetDatabaseStages(StageDatabase database, params StageData[] stages)
    {
        var serializedObject = new SerializedObject(database);
        var stagesProperty = serializedObject.FindProperty("stages");
        stagesProperty.ClearArray();

        for (int i = 0; i < stages.Length; i++)
        {
            stagesProperty.InsertArrayElementAtIndex(i);
            stagesProperty.GetArrayElementAtIndex(i).objectReferenceValue = stages[i];
        }

        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(database);
    }

    private static StagePlayData CreateOrLoadPlayData(string assetName, string stageCode, string stageTitle)
    {
        var path = $"{PlayDataFolder}/{assetName}.asset";
        var asset = AssetDatabase.LoadAssetAtPath<StagePlayData>(path);
        if (asset != null)
            return asset;

        asset = ScriptableObject.CreateInstance<StagePlayData>();
        asset.stageCode = stageCode;
        asset.stageTitle = stageTitle;
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    private static StageData CreateOrLoadStageData(
        string assetName,
        int stageNumber,
        string stageName,
        StageDialogueData dialogueData,
        StagePlayData playData,
        Sprite background,
        AudioClip bgm)
    {
        var path = $"{StagesFolder}/{assetName}.asset";
        var asset = AssetDatabase.LoadAssetAtPath<StageData>(path);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<StageData>();
            AssetDatabase.CreateAsset(asset, path);
        }

        asset.stageNumber = stageNumber;
        asset.stageName = stageName;
        asset.dialogueData = dialogueData;
        asset.playData = playData;
        asset.playType = StagePlayType.PentagramPuzzle;
        asset.playSceneName = SceneNames.StagePlay;
        asset.backgroundImage = background;
        asset.backgroundMusic = bgm;
        EditorUtility.SetDirty(asset);
        return asset;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        var folderName = Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent))
            AssetDatabase.CreateFolder(parent, folderName);
    }

    private static AudioClip LoadAudioClip(string path)
    {
        return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
    }

    private static Sprite LoadSprite(string path)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
#endif
