using UnityEngine;

public static class StageProgressManager
{
    private const string HighestUnlockedKey = "HighestUnlockedStage";
    private const string ClearedKeyPrefix = "StageCleared_";

    public const int ImplementedStageCount = 3;

    public static int HighestUnlockedStage
    {
        get => PlayerPrefs.GetInt(HighestUnlockedKey, 1);
        private set
        {
            PlayerPrefs.SetInt(HighestUnlockedKey, value);
            PlayerPrefs.Save();
        }
    }

    public static bool IsStageUnlocked(int stageNumber)
    {
        return stageNumber >= 1 && stageNumber <= HighestUnlockedStage;
    }

    public static bool IsStageCleared(int stageNumber)
    {
        return PlayerPrefs.GetInt(ClearedKeyPrefix + stageNumber, 0) == 1;
    }

    public static void MarkStageCleared(int stageNumber)
    {
        PlayerPrefs.SetInt(ClearedKeyPrefix + stageNumber, 1);

        if (stageNumber >= HighestUnlockedStage)
            HighestUnlockedStage = stageNumber + 1;

        PlayerPrefs.Save();
    }

    public static bool HasCompletedAllImplementedStages()
    {
        return IsStageCleared(ImplementedStageCount);
    }

    public static void ResetProgress()
    {
        PlayerPrefs.DeleteKey(HighestUnlockedKey);

        for (int i = 1; i <= ImplementedStageCount; i++)
            PlayerPrefs.DeleteKey(ClearedKeyPrefix + i);

        PlayerPrefs.Save();
    }
}
