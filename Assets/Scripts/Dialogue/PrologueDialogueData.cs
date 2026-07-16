using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 프롤로그 전용 대사 데이터입니다.
/// 나레이션 파트와 방(엄마/주인공) 파트로 나눕니다.
/// </summary>
[CreateAssetMenu(fileName = "PrologueDialogueData", menuName = "Dialogue/Prologue Dialogue Data")]
public class PrologueDialogueData : ScriptableObject
{
    [Header("도입 나레이션")]
    [Tooltip("이름 입력·암전 전까지의 나레이션 대사")]
    public List<DialogueLine> narrationLines = new();

    [Header("방 장면")]
    [Tooltip("암전 이후 방에서의 대사")]
    public List<DialogueLine> roomLines = new();
}
