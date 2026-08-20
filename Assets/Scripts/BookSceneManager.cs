using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class BookSceneManager : MonoBehaviour
{
    [Header("Book Rectangles")]
    public GameObject[] bookRectangles; // 크기 3

    [Header("UI Reference")]
    [Tooltip("BookScene 하이퍼라키에 올려둔 DialogueCanvas 오브젝트를 직접 연결하세요.")]
    public GameObject dialogueCanvas;

    [Header("Buttons")]
    public Button bookButton1;
    public Button bookButton2;
    public Button bookButton3;
    public Button ReButton;

    [Header("Character & Speaker Settings")]
    [Tooltip("대사창에 표시될 화자 이름")]
    public string speakerName = "주인공";

    [Tooltip("대사창에 표시할 캐릭터 일러스트 Sprite")]
    public Sprite characterSprite;

    [Header("Next Scene Settings")]
    [Tooltip("스토리가 끝난 후 이동할 씬 이름")]
    public string nextSceneName = "StageSelectScene";

    // 대사창 내부 컴포넌트
    private TextMeshProUGUI dialogueTextTMP;
    private Text dialogueTextLegacy;

    private TextMeshProUGUI speakerNameTMP;
    private Text speakerNameLegacy;

    private Image characterImageUI;

    // 각 버튼별 대사
    private readonly string[] dialogues = new string[]
    {
        "이건…… \"별에서 온 악마\"? ...어디서 많이 들어본 듯한 제목이네.",
        "이건…… \"왜 악마들이 개쩌는 덤블링을 잘하는지에 대해서\". 앗, 이건 진짜 궁금하네... 나중에 읽어봐야겠다.",
        "이건…… \"악마 개체의 좌측 둔부에 관한 관찰 보고서\". ...악마들의 왼쪽 엉덩이가 이렇게나 자세하게 연구됐었다고?"
    };

    private void Start()
    {
        if (dialogueCanvas == null)
        {
            Debug.LogError("DialogueCanvas 오브젝트가 Inspector에 할당되지 않았습니다!");
            return;
        }

        // 1. 대사창 내부 컴포넌트 탐색
        FindDialogueComponents();

        // 2. 대사창 배경/닫기 버튼 클릭 시 대사창이 닫히도록 이벤트 연결
        SetupCloseClickEvent();

        // 3. 게임 시작 시 대사창 숨기기 (버튼 1, 2, 3이 보이게 됨)
        dialogueCanvas.SetActive(false);

        // 4. 책 버튼 클릭 이벤트 연결
        if (bookButton1 != null) bookButton1.onClick.AddListener(() => OnBookClicked(0));
        if (bookButton2 != null) bookButton2.onClick.AddListener(() => OnBookClicked(1));
        if (bookButton3 != null) bookButton3.onClick.AddListener(() => OnBookClicked(2));

        // 처음에는 사각형 숨기기
        foreach (GameObject obj in bookRectangles)
        {
            if (obj != null)
                obj.SetActive(false);
        }
    }

    private void FindDialogueComponents()
    {
        Transform canvasTr = dialogueCanvas.transform;

        // 텍스트 탐색 (TMP / Legacy UI)
        TextMeshProUGUI[] tmps = canvasTr.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var tmp in tmps)
        {
            string name = tmp.gameObject.name;
            if (name.Contains("Content") || name.Contains("Dialogue") || name.Contains("Text"))
            {
                if (dialogueTextTMP == null) dialogueTextTMP = tmp;
            }
            if (name.Contains("Name") || name.Contains("Speaker"))
            {
                speakerNameTMP = tmp;
            }
        }

        if (dialogueTextTMP == null)
        {
            Text[] legacyTexts = canvasTr.GetComponentsInChildren<Text>(true);
            foreach (var txt in legacyTexts)
            {
                string name = txt.gameObject.name;
                if (name.Contains("Content") || name.Contains("Dialogue") || name.Contains("Text"))
                {
                    if (dialogueTextLegacy == null) dialogueTextLegacy = txt;
                }
                if (name.Contains("Name") || name.Contains("Speaker"))
                {
                    speakerNameLegacy = txt;
                }
            }
        }

        // 캐릭터 이미지 탐색
        Image[] images = canvasTr.GetComponentsInChildren<Image>(true);
        foreach (var img in images)
        {
            string name = img.gameObject.name;
            if (name.Contains("Portrait") || name.Contains("Character") || name.Contains("Illustration") || name.Contains("Face"))
            {
                characterImageUI = img;
                break;
            }
        }
    }

    private void SetupCloseClickEvent()
    {
        if (ReButton != null)
        {
            ReButton.onClick.AddListener(CloseDialogue);
        }
        else
        {
            // 만약 변수 연결을 깜빡했을 경우를 대비해 기존 코드 안전장치 유지
            Button[] buttons = dialogueCanvas.GetComponentsInChildren<Button>(true);
            foreach (var btn in buttons)
            {
                string bName = btn.gameObject.name;
                if (bName.Contains("ReButton") || bName.Contains("Close"))
                {
                    btn.onClick.AddListener(CloseDialogue);
                }
            }
        }
    }

    private void OnBookClicked(int index)
    {
        if (index < 0 || index >= dialogues.Length) return;

        // 대사 텍스트, 화자 이름, 캐릭터 이미지 세팅
        SetSpeakerName(speakerName);
        SetDialogueText(dialogues[index]);
        SetCharacterSprite();

        // 대사 창 켜기
        if (dialogueCanvas != null)
        {
            dialogueCanvas.SetActive(true);
        }

        // 모든 사각형 숨기기
        foreach (GameObject obj in bookRectangles)
        {
            if (obj != null)
                obj.SetActive(false);
        }

        // 선택한 책의 사각형만 보이기
        if (index < bookRectangles.Length && bookRectangles[index] != null)
        {
            bookRectangles[index].SetActive(true);
        }
    }

    private void SetSpeakerName(string nameText)
    {
        if (speakerNameTMP != null) speakerNameTMP.text = nameText;
        else if (speakerNameLegacy != null) speakerNameLegacy.text = nameText;
    }

    private void SetDialogueText(string message)
    {
        if (dialogueTextTMP != null) dialogueTextTMP.text = message;
        else if (dialogueTextLegacy != null) dialogueTextLegacy.text = message;
    }

    private void SetCharacterSprite()
    {
        if (characterImageUI != null && characterSprite != null)
        {
            characterImageUI.sprite = characterSprite;
            characterImageUI.gameObject.SetActive(true);
        }
    }

    public void CloseDialogue()
    {
        dialogueCanvas.SetActive(false);

        foreach (GameObject obj in bookRectangles)
        {
            if (obj != null)
                obj.SetActive(false);
        }
    }

    public void GoToNextScene()
    {
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }
}