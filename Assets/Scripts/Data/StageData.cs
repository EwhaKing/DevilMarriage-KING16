using UnityEngine;



/// <summary>

/// 한 스테이지의 정체성, 대화 데이터, 플레이 설정을 담는 ScriptableObject입니다.

/// </summary>

[CreateAssetMenu(fileName = "StageData", menuName = "DevilMarriage/Stage Data")]

public class StageData : ScriptableObject

{

    [Header("Identity")]

    public int stageNumber = 1;

    public string stageName = "임시 스테이지";



    [Header("Story (ScriptableObject 대화)")]

    [Tooltip("이 스테이지의 Open/Close 대사 ScriptableObject")]

    public StageDialogueData dialogueData;



    [Header("Play")]

    public StagePlayData playData;

    public StagePlayType playType = StagePlayType.PentagramPuzzle;

    public string playSceneName = SceneNames.StagePlay;



    [Header("Presentation")]

    public Sprite backgroundImage;

    public AudioClip backgroundMusic;

}


