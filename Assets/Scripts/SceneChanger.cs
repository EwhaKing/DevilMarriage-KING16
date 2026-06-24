using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Yarn.Unity;

public class SceneChanger : MonoBehaviour
{
    [Header("설정 팝업 UI 오브젝트")]
    [SerializeField] private GameObject settingPopup;

    public DialogueRunner dialogueRunner;

    [Header("Dialogue Portrait")]
    [SerializeField] private Image characterPortrait;
    [SerializeField] private Sprite portraitDefault;
    [SerializeField] private Sprite portraitHappy;
    [SerializeField] private Sprite portraitNervous;

    private Dictionary<string, Sprite> portraitSprites;

    private void Awake()
    {
        portraitSprites = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);

        if (portraitDefault != null)
            portraitSprites["default"] = portraitDefault;
        if (portraitHappy != null)
            portraitSprites["happy"] = portraitHappy;
        if (portraitNervous != null)
            portraitSprites["nervous"] = portraitNervous;
    }

    private void Start()
    {
        if (dialogueRunner == null)
            return;

        dialogueRunner.AddCommandHandler<string>("LoadScene", ChangeSceneByName);
        dialogueRunner.AddCommandHandler<string>("SetExpression", SetCharacterExpression);
    }

    public void GoToTitleScreen()
    {
        ChangeSceneByName("TitleScene");
    }

    public void GoToStage1()
    {
        ChangeSceneByName("Stage1Scene");
    }

    /// <summary>
    /// 2. 설정 팝업창 열기
    /// </summary>
    public void OpenSettingsPopup()
    {
        if (settingPopup != null)
        {
            settingPopup.SetActive(true); // 설정창 켜기
        }
    }

    /// <summary>
    /// 3. 설정 팝업창 닫기
    /// </summary>
    public void CloseSettingsPopup()
    {
        if (settingPopup != null)
        {
            settingPopup.SetActive(false); // 설정창 끄기
        }
    }

    /// <summary>
    /// 4. 게임 종료
    /// </summary>
    public void ClickExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // 얀 스크립트에서 <<LoadScene 씬이름>> 을 호출하면 이름으로 씬 이동
    public void ChangeSceneByName(string sceneName)
    {
        Debug.Log($"얀 스크립트 명령: {sceneName} 씬으로 이동합니다.");
        SceneManager.LoadScene(sceneName);
    }

    public void SetCharacterExpression(string expressionId)
    {
        if (characterPortrait == null)
        {
            Debug.LogWarning("[SceneChanger] Character Portrait Image가 연결되지 않았습니다.");
            return;
        }

        if (!portraitSprites.TryGetValue(expressionId, out var sprite))
        {
            Debug.LogWarning($"[SceneChanger] 알 수 없는 표정: {expressionId}");
            return;
        }

        characterPortrait.sprite = sprite;
    }
}