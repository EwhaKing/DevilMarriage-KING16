using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 씬 이동과 설정 팝업, 캐릭터 표정 스프라이트를 담당합니다.
/// (이전 Yarn 명령어 등록은 제거하고 ScriptableObject 대화 시스템과 함께 씁니다.)
/// </summary>
public class SceneChanger : MonoBehaviour
{
    [Header("Settings Popup")]
    [SerializeField] private GameObject settingPopup;

    [Header("Dialogue Portrait")]
    [SerializeField] private Image characterPortrait;
    [SerializeField] private Sprite portraitDefault;
    [SerializeField] private Sprite portraitHappy;
    [SerializeField] private Sprite portraitNervous;

    private Dictionary<string, Sprite> portraitSprites;

    /// <summary>기본 표정 스프라이트</summary>
    public Sprite PortraitDefault => portraitDefault;
    /// <summary>기쁜 표정 스프라이트</summary>
    public Sprite PortraitHappy => portraitHappy;
    /// <summary>긴장 표정 스프라이트</summary>
    public Sprite PortraitNervous => portraitNervous;
    /// <summary>캐릭터 Image (기존 UI)</summary>
    public Image CharacterPortrait => characterPortrait;
    /// <summary>설정 팝업 오브젝트</summary>
    public GameObject SettingPopup => settingPopup;

    private void Awake()
    {
        portraitSprites = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);

        if (portraitDefault != null)
            portraitSprites["default"] = portraitDefault;
        if (portraitHappy != null)
            portraitSprites["happy"] = portraitHappy;
        if (portraitNervous != null)
            portraitSprites["nervous"] = portraitNervous;

        // DialogueManager가 있으면 표정 스프라이트를 넘겨 줍니다.
        var dialogueManager = FindAnyObjectByType<DialogueManager>();
        if (dialogueManager != null)
            dialogueManager.SetPortraitSprites(portraitDefault, portraitHappy, portraitNervous);
    }

    public void GoToTitleScreen()
    {
        // 홈으로 돌아가기 버튼에 이 함수를 연결하면 됩니다.
        ChangeSceneByName(SceneNames.Title);
    }

    public void GoToPrologue()
    {
        GameFlowManager.EnsureExists();
        ChangeSceneByName(SceneNames.Prologue);
    }

    public void GoToStageSelect()
    {
        GameFlowManager.EnsureExists();
        ChangeSceneByName(SceneNames.StageSelect);
    }

    public void GoToStage1()
    {
        ChangeSceneByName("Stage1Scene");
    }

    public void GoToCurrentStagePlay()
    {
        if (GameFlowManager.Instance != null)
            ChangeSceneByName(GameFlowManager.Instance.GetCurrentStagePlaySceneName());
        else
            ChangeSceneByName(SceneNames.StagePlay);
    }

    public void GoToStoryScene()
    {
        ChangeSceneByName("StoryScene");
    }

    // ==========================================
    // 새로 추가된 재시도 기능
    // ==========================================
    public void ClickRetry()
    {
        // 만약 GameFlowManager가 현재 스테이지 정보를 유지하고 있다면 
        // GoToCurrentStagePlay(); 를 호출하는 방식도 가능합니다.
        
        // 여기서는 실패 시 저장해둔 이전 스테이지 이름을 불러와 로드합니다.
        // 저장된 값이 없을 경우를 대비해 "StagePlayScene"을 기본값으로 둡니다.
        string lastStage = PlayerPrefs.GetString("LastStage", "StagePlayScene");
        ChangeSceneByName(lastStage);
    }
    // ==========================================

    public void OpenSettingsPopup()
    {
        if (settingPopup != null)
            settingPopup.SetActive(true);
    }

    public void CloseSettingsPopup()
    {
        if (settingPopup != null)
            settingPopup.SetActive(false);
    }

    public void ClickExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void ChangeSceneByName(string sceneName)
    {
        Debug.Log($"Load scene: {sceneName}");
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// expressionId에 맞는 표정으로 캐릭터 Image를 바꿉니다.
    /// </summary>
    public void SetCharacterExpression(string expressionId)
    {
        if (characterPortrait == null)
            return;

        if (!portraitSprites.TryGetValue(expressionId, out var sprite))
            return;

        characterPortrait.sprite = sprite;
    }
}