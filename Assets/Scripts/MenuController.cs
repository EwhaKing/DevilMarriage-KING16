using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuController : MonoBehaviour
{
    [Header("설정 팝업 UI 오브젝트")]
    [SerializeField] private GameObject settingPopup;

    private void Awake()
    {
        if (settingPopup == null)
            settingPopup = GameObject.Find("SettingPopup");

        WireButtons();

        if (settingPopup != null)
            settingPopup.SetActive(false);
    }

    private void WireButtons()
    {
        BindButton("Title_Button", GoToTitleScreen);
        BindButton("Option_Button", OpenSettingsPopup);
        BindButton("Stage1_Button", GoToStage1);
        BindCloseButton();
    }

    private void BindCloseButton()
    {
        if (settingPopup == null)
            return;

        var closeButtonTransform = settingPopup.transform.Find("SettingPanel/CloseSettingButton");
        if (closeButtonTransform == null)
        {
            Debug.LogWarning("[MenuController] CloseSettingButton을 찾을 수 없습니다.");
            return;
        }

        var closeButton = closeButtonTransform.GetComponent<Button>();
        if (closeButton == null)
        {
            Debug.LogWarning("[MenuController] CloseSettingButton에 Button 컴포넌트가 없습니다.");
            return;
        }

        closeButton.onClick.AddListener(CloseSettingsPopup);
    }

    private void BindButton(string objectName, UnityEngine.Events.UnityAction action)
    {
        var buttonObject = GameObject.Find(objectName);
        if (buttonObject == null)
        {
            Debug.LogWarning($"[MenuController] '{objectName}' 버튼을 찾을 수 없습니다.");
            return;
        }

        var button = buttonObject.GetComponent<Button>();
        if (button == null)
        {
            Debug.LogWarning($"[MenuController] '{objectName}'에 Button 컴포넌트가 없습니다.");
            return;
        }

        button.onClick.AddListener(action);
    }

    public void GoToTitleScreen()
    {
        SceneManager.LoadScene("TitleScene");
    }

    public void GoToStage1()
    {
        SceneManager.LoadScene("Stage1Scene");
    }

    public void OpenSettingsPopup()
    {
        if (settingPopup == null)
        {
            Debug.LogWarning("[MenuController] settingPopup이 연결되지 않았습니다.");
            return;
        }

        settingPopup.SetActive(true);
    }

    public void CloseSettingsPopup()
    {
        if (settingPopup != null)
            settingPopup.SetActive(false);

        var soundSettings = FindFirstObjectByType<SoundSettings>();
        if (soundSettings != null)
            soundSettings.SaveSoundSettings();
    }
}
