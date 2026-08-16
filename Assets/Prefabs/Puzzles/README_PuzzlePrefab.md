# Stage Puzzle Prefab 가이드

스테이지마다 다른 한붓그리기 퍼즐을 Prefab으로 만들고, `StagePlayData`에 연결하면 `StagePlayScene`이 자동으로 해당 Prefab만 생성합니다.

## 빠른 시작 (한 번만)

1. Unity에서 `StagePlayScene`을 연다.
2. 메뉴 **DevilMarriage → Puzzles → 1. Extract Stage1Puzzle Prefab From Open Scene** 실행  
   → `Assets/Prefabs/Puzzles/Stage1Puzzle.prefab` 생성 + Stage1 PlayData 연결
3. (선택) **DevilMarriage → Puzzles → 3. Create Square Puzzle Template (Stage2 예시)**  
   → Stage2용 사각형 퍼즐 생성 + Stage2 PlayData 연결

이후부터는 Prefab만 고치면 되고, 코드의 `if/switch`를 추가할 필요가 없습니다.

---

## 1. 새 Puzzle Prefab 만들기

**방법 A – 템플릿**
- **DevilMarriage → Puzzles → 2. Create Empty Puzzle Prefab Template**
- 또는 Stage2 예시: **3. Create Square Puzzle Template**

**방법 B – 직접**
1. Hierarchy에서 빈 GameObject 생성 → 이름을 `Stage3Puzzle` 등으로
2. `Stage1PuzzleController` 컴포넌트 추가
3. `StagePuzzleLayout` 컴포넌트 추가
4. 자식으로 빈 `Paths` 오브젝트 생성
5. Project로 드래그해 Prefab으로 저장 (`Assets/Prefabs/Puzzles/`)

---

## 2. Rune 추가·배치

1. Prefab을 더블클릭해 Prefab 편집 모드로 진입
2. 퍼즐 루트 아래에 빈 GameObject 생성 (예: `Rune0`)
3. 추가 컴포넌트:
   - `SpriteRenderer` (룬 이미지)
   - `CircleCollider2D` (클릭용, Is Trigger 꺼도 됨)
   - `RuneNode`
4. Transform Position으로 원하는 위치에 배치
5. 필요한 만큼 룬을 복제해 배치

---

## 3. Rune ID 설정

각 `RuneNode` Inspector:
- **Rune Index**: 0, 1, 2… (같은 퍼즐 안에서 중복 금지)
- **Is Start Rune**: 시작점 (하나만 켜는 것을 권장)
- **Is End Rune**: 종료점 (켜진 룬이 하나라도 있으면 그 위에서 클리어, 모두 끄면 시작점으로 돌아와야 클리어)
- **Is Mandatory**: 반드시 방문해야 하는 룬
- **Is Forbidden**: 밟으면 실패(정신력 패널티)
- **Is Sanity Hazard**: 밟으면 정신력 감소 (보라색)

---

## 4. Path로 두 Rune 연결

1. 퍼즐 루트의 `StagePuzzleLayout` 선택
2. **Links** 배열 크기 설정
3. 각 Element:
   - **From** / **To**: Hierarchy의 룬을 드래그
   - **Waypoints** (선택): 중간 빈 Transform을 넣어 꺾인/휘어진 Path
   - **Is Mandatory**: 클리어에 필요한 Path인지
4. Inspector 하단 **Rebuild Paths From Links** 클릭  
   → `Paths` 아래에 Line이 자동 생성되고, 룬 위치에 맞춰 길이·방향이 잡힘
5. 룬을 옮긴 뒤 **Refresh Path Positions**로 선만 다시 맞출 수 있음

> 기존 오각별은 `PentagramPathBuilder`의 Build도 그대로 사용 가능합니다.

---

## 5. 시작·마지막 Rune

- 시작: 플레이 시작 전 플레이어가 룬을 클릭해 직접 고릅니다. (`Is Start Rune`은 현재 무시)
- 마지막: **Is End Rune** 체크 (여러 개면 그중 아무 곳에 있어도 클리어 가능)
- End를 전부 끄면 “모든 필수 Path를 지나는 즉시” 클리어 (시작 룬 복귀 불필요)
- 쥐의 피는 런타임에 Path 개수와 동일하게 맞춰집니다. PlayData의 Max Rat Blood를 스테이지마다 손으로 맞출 필요는 없습니다.

---

## 6. 특수 Rune

| 옵션 | 효과 |
|------|------|
| Is Forbidden | 이동 거부 + 정신력 패널티 |
| Is Sanity Hazard | 전진으로 도착 시 정신력 감소 |
| Is Mandatory 해제 | 방문하지 않아도 클리어 가능 |

Stage4는 Prefab에서 Hazard 룬을 직접 체크하세요.  
(체크가 하나도 없으면 기존처럼 홀수 인덱스에 자동 지정되는 폴백이 있습니다.)

---

## 7. 스테이지에 퍼즐 연결

1. `Assets/Data/PlayData/StageXX_PlayData` 에셋 선택
2. **Puzzle Prefab** 칸에 `StageXPuzzle` Prefab을 드래그  
   또는 메뉴 **DevilMarriage → Puzzles → 4. Assign Prefab To Selected PlayData**
3. 저장

`StagePlayScene`은 `GameFlowManager`의 현재 스테이지 `playData.puzzlePrefab`만 생성합니다.  
Prefab이 비어 있으면 씬에 있는 기본 `Stage1Puzzle`을 그대로 사용합니다.

---

## 8. 테스트

1. Title → 프롤로그/스테이지 선택 → 해당 스테이지 Play
2. 확인 항목:
   - 기대한 모양의 퍼즐이 보이는지
   - 시작 룬에서 연결된 룬으로만 이동되는지
   - 이미 지나간 Path를 다시 쓰면 되감기/패널티가 되는지
   - 필수 Path(및 필수 룬)를 다 지나고 종료 조건 만족 시 Closing 스토리로 넘어가는지
   - Hazard/Forbidden 룬 동작
3. Prefab만 수정한 경우 Play Mode를 끄고 Prefab을 저장한 뒤 다시 Play

---

## 주의사항

- 룬 **Index 중복** 금지
- Path는 **양방향** (A→B와 B→A는 같은 길)
- Prefab 안에 **Player를 넣지 마세요**. Player는 `StagePlayScene`에 두고 자동 연결됩니다.
- 새 스테이지를 추가할 때: PlayData에 Prefab만 넣으면 됩니다. `StagePlaySceneController`에 `if (stage == N)`을 추가할 필요 없습니다.
- Stage1 튜토리얼 / Stage4 인트로 대사는 스테이지 번호 기준이라, 퍼즐 Prefab과 별개로 동작합니다.
