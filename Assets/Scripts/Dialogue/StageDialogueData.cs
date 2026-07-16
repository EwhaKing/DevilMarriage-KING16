using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 한 스테이지의 Open(오프닝) / Close(클로징) 대사를 담는 ScriptableObject입니다.
/// Project 창 → 우클릭 → Create → Dialogue → Stage Dialogue Data 로 만들 수 있습니다.
/// </summary>
[CreateAssetMenu(fileName = "StageDialogueData", menuName = "Dialogue/Stage Dialogue Data")]
public class StageDialogueData : ScriptableObject
{
    [Header("기본 정보")]
    [Tooltip("이 대사가 속한 스테이지 번호 (1, 2, 3 ...)")]
    public int stageNumber = 1;

    [Header("Open 대사 (플레이 시작 전)")]
    [Tooltip("스테이지에 들어가기 전에 재생할 대사 목록")]
    public List<DialogueLine> openLines = new();

    [Header("Close 대사 (클리어 후)")]
    [Tooltip("스테이지를 클리어한 뒤에 재생할 대사 목록")]
    public List<DialogueLine> closeLines = new();

    /// <summary>
    /// Open 또는 Close 목록을 꺼낼 때 사용합니다.
    /// </summary>
    public IReadOnlyList<DialogueLine> GetLines(bool isOpening)
    {
        return isOpening ? openLines : closeLines;
    }
}
