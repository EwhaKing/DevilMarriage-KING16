using UnityEngine;

/// <summary>
/// 플레이어(신부/주인공) 이름을 PlayerPrefs에 저장하고 불러옵니다.
/// </summary>
public static class PlayerNameManager
{
    private const string PrefsKey = "PlayerName";
    private const string PrefsSetKey = "PlayerNameSet";
    private const string DefaultName = "주인공";

    public static string PlayerName
    {
        get
        {
            var name = PlayerPrefs.GetString(PrefsKey, DefaultName);
            return string.IsNullOrWhiteSpace(name) ? DefaultName : name;
        }
        set
        {
            var trimmed = string.IsNullOrWhiteSpace(value) ? DefaultName : value.Trim();
            PlayerPrefs.SetString(PrefsKey, trimmed);
            PlayerPrefs.SetInt(PrefsSetKey, 1);
            PlayerPrefs.Save();
        }
    }

    /// <summary>플레이어가 직접 이름을 입력·저장한 적이 있는지</summary>
    public static bool HasCustomName => PlayerPrefs.GetInt(PrefsSetKey, 0) == 1;

    public static void ClearSavedName()
    {
        PlayerPrefs.DeleteKey(PrefsKey);
        PlayerPrefs.DeleteKey(PrefsSetKey);
        PlayerPrefs.Save();
    }
}
