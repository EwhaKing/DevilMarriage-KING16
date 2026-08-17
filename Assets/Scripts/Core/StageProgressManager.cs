using System;
using System.Collections.Generic;
using UnityEngine;

public static class StageProgressManager
{
    private const string HighestUnlockedKey = "HighestUnlockedStage";
    private const string ClearedKeyPrefix = "StageCleared_";
    private const string CurrentMapStageKey = "CurrentMapStage";
    private const string PendingWalkFromKey = "PendingWalkFrom";
    private const string PendingWalkToKey = "PendingWalkTo";
    private const string ActivatedPathsKey = "ActivatedStagePaths";
    private const string HasPlayedGameKey = "HasPlayedGame";

    /// <summary>선택 화면에 둘 Stage 버튼 수. 아직 본편이 없는 번호도 맵에 배치합니다.</summary>
    public const int StageSelectButtonCount = 66;

    /// <summary>해금 가능한 마지막 번호. 이 번호 클릭 시 아직 없으면 준비 중 안내.</summary>
    public const int ImplementedStageCount = 34;

    /// <summary>현재 플레이 가능한 마지막 스테이지. 이 스테이지를 클리어하면 콘텐츠 종료로 간주.</summary>
    public const int CurrentContentStageCount = 33;

    private static HashSet<string> _activatedPaths;

    public static int HighestUnlockedStage
    {
        get => PlayerPrefs.GetInt(HighestUnlockedKey, 1);
        private set
        {
            PlayerPrefs.SetInt(HighestUnlockedKey, value);
            PlayerPrefs.Save();
        }
    }

    public static int CurrentMapStage
    {
        get => Mathf.Max(1, PlayerPrefs.GetInt(CurrentMapStageKey, 1));
        set
        {
            PlayerPrefs.SetInt(CurrentMapStageKey, Mathf.Max(1, value));
            PlayerPrefs.Save();
        }
    }

    public static bool HasPlayedBefore
    {
        get
        {
            if (PlayerPrefs.GetInt(HasPlayedGameKey, 0) == 1)
                return true;

            if (HighestUnlockedStage > 1)
                return true;

            if (PlayerNameManager.HasCustomName)
                return true;

            for (int i = 1; i <= CurrentContentStageCount; i++)
            {
                if (IsStageCleared(i))
                    return true;
            }

            return false;
        }
    }

    public static void MarkHasPlayed()
    {
        PlayerPrefs.SetInt(HasPlayedGameKey, 1);
        PlayerPrefs.Save();
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
        int previousHighest = HighestUnlockedStage;
        PlayerPrefs.SetInt(ClearedKeyPrefix + stageNumber, 1);
        MarkHasPlayed();

        if (stageNumber >= HighestUnlockedStage)
            HighestUnlockedStage = stageNumber + 1;

        if (HighestUnlockedStage > previousHighest)
        {
            PlayerPrefs.SetInt(PendingWalkFromKey, stageNumber);
            PlayerPrefs.SetInt(PendingWalkToKey, HighestUnlockedStage);
        }

        PlayerPrefs.Save();
    }

    public static bool TryConsumePendingWalk(out int fromStage, out int toStage)
    {
        fromStage = PlayerPrefs.GetInt(PendingWalkFromKey, 0);
        toStage = PlayerPrefs.GetInt(PendingWalkToKey, 0);
        PlayerPrefs.DeleteKey(PendingWalkFromKey);
        PlayerPrefs.DeleteKey(PendingWalkToKey);
        PlayerPrefs.Save();
        return fromStage > 0 && toStage > 0 && fromStage != toStage;
    }

    public static string GetPathKey(int stageA, int stageB)
    {
        int min = Mathf.Min(stageA, stageB);
        int max = Mathf.Max(stageA, stageB);
        return min + "-" + max;
    }

    public static bool IsPathActivated(int stageA, int stageB)
    {
        if (stageA <= 0 || stageB <= 0)
            return false;

        EnsureActivatedCache();
        return _activatedPaths.Contains(GetPathKey(stageA, stageB));
    }

    public static void ActivatePath(int stageA, int stageB)
    {
        if (stageA <= 0 || stageB <= 0 || stageA == stageB)
            return;

        EnsureActivatedCache();
        var key = GetPathKey(stageA, stageB);
        if (!_activatedPaths.Add(key))
            return;

        SaveActivatedPaths();
    }

    public static bool HasCompletedAllImplementedStages()
    {
        return IsStageCleared(CurrentContentStageCount);
    }

    public static void ResetProgress()
    {
        PlayerPrefs.DeleteKey(HighestUnlockedKey);
        PlayerPrefs.DeleteKey(CurrentMapStageKey);
        PlayerPrefs.DeleteKey(PendingWalkFromKey);
        PlayerPrefs.DeleteKey(PendingWalkToKey);
        PlayerPrefs.DeleteKey(ActivatedPathsKey);
        PlayerPrefs.DeleteKey(HasPlayedGameKey);

        for (int i = 1; i <= StageSelectButtonCount; i++)
            PlayerPrefs.DeleteKey(ClearedKeyPrefix + i);

        _activatedPaths = new HashSet<string>();
        PlayerPrefs.Save();
    }

    private static void EnsureActivatedCache()
    {
        if (_activatedPaths != null)
            return;

        _activatedPaths = new HashSet<string>();
        var packed = PlayerPrefs.GetString(ActivatedPathsKey, string.Empty);
        if (string.IsNullOrEmpty(packed))
            return;

        var parts = packed.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
            _activatedPaths.Add(part);
    }

    private static void SaveActivatedPaths()
    {
        EnsureActivatedCache();
        PlayerPrefs.SetString(ActivatedPathsKey, string.Join("|", _activatedPaths));
        PlayerPrefs.Save();
    }
}
