using System.Collections.Generic;
using UnityEngine;



public class PuzzleController : MonoBehaviour

{

// 유니티 에디터에서 방금 만든 두 매니저를 드래그해서 연결해 줍니다.

public RatBloodManager bloodManager;

public SanityManager sanityManager;



private List<RuneNode> pathList = new List<RuneNode>();



void Start()

{

// 스테이지 시작 시 자원 초기화 (예: 10리터)

bloodManager.Initialize(10.0f);

sanityManager.Initialize();

}



public void OnRuneClicked(RuneNode clickedRune)

{

if (pathList.Count == 0)

{

pathList.Add(clickedRune);

return;

}



RuneNode currentRune = pathList[pathList.Count - 1];

if (clickedRune == currentRune) return;



// [되돌아가기]

if (pathList.Count >= 2 && clickedRune == pathList[pathList.Count - 2])

{

ExecuteUndo();

return;

}



// [선 긋기]

if (!pathList.Contains(clickedRune))

{

TryDrawStroke(clickedRune);

}

}



private void TryDrawStroke(RuneNode newRune)

{

// 쥐의 피 매니저에게 그을 수 있는지 물어봄

if (!bloodManager.CanDraw()) return;



pathList.Add(newRune);

bloodManager.ConsumeBlood(); // 쥐의 피 소모 명령

}



private void ExecuteUndo()

{

// 정신력 매니저에게 살아있는지 물어봄

if (!sanityManager.IsSane()) return;



pathList.RemoveAt(pathList.Count - 1);


// 두 매니저에게 각각 명령을 내림

sanityManager.ConsumeSanity(); // 정신력 깎기

bloodManager.RefundBlood(); // 쥐의 피 회복

}

}
