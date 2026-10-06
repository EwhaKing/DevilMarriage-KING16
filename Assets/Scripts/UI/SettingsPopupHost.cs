using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Story / DevilPage / StageSelect에 타이틀 씬과 같은 설정 팝업을 붙입니다.
/// 캔버스 기준 해상도를 타이틀과 맞춰 Setting_bg 크기와 슬라이더, 닫기 버튼이 같게 동작합니다.
/// </summary>
public class SettingsPopupHost : MonoBehaviour
{
    public const string CanvasName = "TitleStyleSettingsCanvas";

    private GameObject _popup;

    public GameObject Popup => _popup;

    public static GameObject Ensure()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName != "StoryScene" && sceneName != "DevilPageScene" && sceneName != "StageSelectScene")
            return null;

        var existingCanvas = GameObject.Find(CanvasName);
        if (existingCanvas != null)
            return existingCanvas.GetComponent<SettingsPopupHost>()?.Popup;

        RetireLegacyPopups();

        var prefab = Resources.Load<GameObject>("UI/TitleSettingsPopup");
        if (prefab == null)
        {
            Debug.LogError("[SettingsPopupHost] TitleSettingsPopup 프리팹을 찾을 수 없습니다.");
            return null;
        }

        var canvasObject = new GameObject(
            CanvasName,
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));

        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(2560f, 1440f);
        scaler.matchWidthOrHeight = 0.5f;

        var popup = Instantiate(prefab, canvasObject.transform, false);
        popup.name = "Dim_bg";

        var popupRect = popup.GetComponent<RectTransform>();
        popupRect.anchorMin = Vector2.zero;
        popupRect.anchorMax = Vector2.one;
        popupRect.offsetMin = Vector2.zero;
        popupRect.offsetMax = Vector2.zero;
        popupRect.localScale = Vector3.one;
        popupRect.anchoredPosition = Vector2.zero;
        popup.SetActive(false);

        var host = canvasObject.AddComponent<SettingsPopupHost>();
        host._popup = popup;
        host.WireCloseButton();
        host.ApplyVolumes();
        host.BindSceneControllers();
        return popup;
    }

    private static void RetireLegacyPopups()
    {
        var scene = SceneManager.GetActiveScene();
        var rects = Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var rect in rects)
        {
            var popupObject = rect.gameObject;
            if (!popupObject.scene.IsValid() || popupObject.scene != scene)
                continue;
            if (popupObject.name != "Dim_bg" && popupObject.name != "SettingPopup")
                continue;

            popupObject.SetActive(false);
            popupObject.name = "Legacy_" + popupObject.name;
        }
    }

    private void WireCloseButton()
    {
        var closeTransform = _popup.transform.Find("Setting_bg/CloseSettingButton");
        if (closeTransform == null)
        {
            Debug.LogWarning("[SettingsPopupHost] CloseSettingButton을 찾을 수 없습니다.");
            return;
        }

        var closeButton = closeTransform.GetComponent<Button>();
        if (closeButton == null)
        {
            Debug.LogWarning("[SettingsPopupHost] CloseSettingButton에 Button 컴포넌트가 없습니다.");
            return;
        }

        closeButton.onClick.AddListener(Close);
    }

    private void ApplyVolumes()
    {
        var soundSettings = _popup.GetComponent<SoundSettings>();
        if (soundSettings != null)
            soundSettings.ApplySavedVolumes();
    }

    private void BindSceneControllers()
    {
        var sceneChanger = FindAnyObjectByType<SceneChanger>();
        if (sceneChanger != null)
            sceneChanger.BindSettingsPopup(_popup);

        var dialogueManager = FindAnyObjectByType<DialogueManager>();
        if (dialogueManager != null)
            dialogueManager.SetSettingPopup(_popup);

        var stageSelect = FindAnyObjectByType<StageSelectController>();
        if (stageSelect != null)
            stageSelect.BindSettingsPopup(_popup);
    }

    public void Close()
    {
        if (_popup != null)
            _popup.SetActive(false);

        var soundSettings = _popup != null ? _popup.GetComponent<SoundSettings>() : null;
        if (soundSettings != null)
            soundSettings.SaveSoundSettings();
    }
}
