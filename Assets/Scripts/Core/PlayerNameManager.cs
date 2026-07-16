using UnityEngine;



/// <summary>

/// 플레이어(신부) 이름을 PlayerPrefs에 저장하고 불러옵니다.

/// 대화 텍스트의 {playerName} / {$playerName} 자리를 채울 때 사용합니다.

/// </summary>

public static class PlayerNameManager

{

    private const string PrefsKey = "PlayerName";

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

            PlayerPrefs.Save();

        }

    }

}


