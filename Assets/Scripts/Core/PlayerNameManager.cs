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

    /// <summary>
    /// 화자 이름과 대사 본문의 주인공 표기를 저장된 플레이어 이름으로 바꿉니다.
    /// [주인공], {playerName}, 문장 안의 '주인공'을 모두 대상으로 합니다.
    /// </summary>
    public static string FormatDialogue(string raw)
    {
        if (string.IsNullOrEmpty(raw))
            return raw ?? "";

        var name = PlayerName;
        var formatted = raw
            .Replace("{$playerName}", name)
            .Replace("{playerName}", name)
            .Replace("[주인공]", name);

        if (name == DefaultName)
            return formatted;

        formatted = formatted.Replace("주…… 인공", StutterName(name));
        formatted = formatted.Replace("주... 인공", StutterName(name));
        return formatted.Replace("주인공", name);
    }

    static string StutterName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return name;

        if (name.Length == 1)
            return name + "……";

        return name.Substring(0, 1) + "…… " + name.Substring(1);
    }
}
