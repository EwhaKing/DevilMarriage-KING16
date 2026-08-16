using UnityEngine;

public static class StageProgressManager
{
    private const string HighestUnlockedKey = "HighestUnlockedStage";
    private const string ClearedKeyPrefix = "StageCleared_";

    /// <summary>선택 화면에 표시할 스테이지 버튼 수 (해금만 되는 Stage34 포함).</summary>
    public const int ImplementedStageCount = 34;

    /// <summary>현재 플레이 가능한 마지막 스테이지. 이 스테이지를 클리어하면 콘텐츠 종료로 간주.</summary>
    public const int CurrentContentStageCount = 33;

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
        return IsStageCleared(CurrentContentStageCount);
    }

    public static void ResetProgress()
    {
        PlayerPrefs.DeleteKey(HighestUnlockedKey);

        for (int i = 1; i <= ImplementedStageCount; i++)
            PlayerPrefs.DeleteKey(ClearedKeyPrefix + i);

        PlayerPrefs.Save();
    }
}
