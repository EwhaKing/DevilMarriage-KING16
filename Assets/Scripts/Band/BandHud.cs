using System;
using UnityEngine;
using UnityEngine.Events;

// Lightweight overlay keeps the hand-placed world-space art intact.
public sealed class BandHud : MonoBehaviour
{
    Font font;
    UnityEngine.UI.Text score, time, feedback, combo, results;
    UnityEngine.UI.Text[] keys = new UnityEngine.UI.Text[3];
    RectTransform canvasRect, content;
    UnityEngine.UI.CanvasScaler scaler;
    GameObject resultPanel;
    float feedbackUntil;
    readonly Color ink = new Color(.35f, .035f, .15f);
    readonly Color[] colors = { new Color(.2f,.85f,.45f), new Color(1f,.3f,.4f), new Color(.35f,.65f,1f) };

    public void Build(Font koreanFont, Action restart)
    {
        font = koreanFont ? koreanFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        gameObject.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        GetComponent<Canvas>().sortingOrder = 50;
        scaler = gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = .5f;
        gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        canvasRect = (RectTransform)transform;
        var viewport = new GameObject("StageViewport", typeof(RectTransform));
        viewport.transform.SetParent(transform, false);
        content = (RectTransform)viewport.transform;
        content.anchorMin = Vector2.zero; content.anchorMax = Vector2.one;
        content.offsetMin = content.offsetMax = Vector2.zero;
        score = Label("Success", "성공  00", new Vector2(.1f,.86f), new Vector2(270,95), 64, ink);
        time = Label("Time", "20.0초", new Vector2(.9f,.86f), new Vector2(270,95), 54, ink);
        feedback = Label("Judgement", "", new Vector2(.5f,.31f), new Vector2(500,115), 80, Color.white);
        combo = Label("Combo", "", new Vector2(.5f,.9f), new Vector2(500,70), 36, Color.white);
        var plate = new GameObject("InstructionPlate", typeof(RectTransform), typeof(UnityEngine.UI.Image));
        plate.transform.SetParent(content, false);
        var plateRect = (RectTransform)plate.transform;
        plateRect.anchorMin = plateRect.anchorMax = new Vector2(.5f,.065f);
        plateRect.sizeDelta = new Vector2(1090,64);
        plate.GetComponent<UnityEngine.UI.Image>().color = new Color(1f,.93f,.84f,.96f);
        plate.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
        Label("Instructions", "노트가 선에 닿을 때 해당 방향키를 누르세요!", new Vector2(.5f,.065f), new Vector2(1070,60), 32, ink);
        string[] names = { "←\n쾅 / 드럼", "↑\n락 / 기타", "→\n붐 / 키보드" };
        for (int i = 0; i < 3; i++) keys[i] = Label("LaneKey" + i, names[i], new Vector2(.4f + i*.1f,.13f), new Vector2(190,115), 32, Color.white);
        resultPanel = new GameObject("Results", typeof(RectTransform), typeof(UnityEngine.UI.Image));
        resultPanel.transform.SetParent(content, false);
        var rect = (RectTransform)resultPanel.transform;
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        resultPanel.GetComponent<UnityEngine.UI.Image>().color = new Color(.07f,.025f,.12f,.94f);
        results = Label("ResultText", "", new Vector2(.5f,.57f), new Vector2(1000,390), 45, Color.white, resultPanel.transform);
        var buttonObject = new GameObject("Retry", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
        buttonObject.transform.SetParent(resultPanel.transform, false);
        rect = (RectTransform)buttonObject.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(.5f,.3f); rect.sizeDelta = new Vector2(420,85);
        buttonObject.GetComponent<UnityEngine.UI.Image>().color = new Color(.6f,.2f,.42f);
        buttonObject.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => restart());
        Label("RetryLabel", "다시 연주  ·  R", new Vector2(.5f,.5f), new Vector2(420,85), 32, Color.white, buttonObject.transform);
        resultPanel.SetActive(false);
    }

    UnityEngine.UI.Text Label(string name, string value, Vector2 anchor, Vector2 size, float fontSize, Color color, Transform parent = null)
    {
        var obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent ? parent : content, false);
        var rect = (RectTransform)obj.transform;
        rect.anchorMin = rect.anchorMax = anchor; rect.sizeDelta = size;
        var text = obj.AddComponent<UnityEngine.UI.Text>();
        if (font) text.font = font;
        text.fontStyle = FontStyle.Bold;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        var outline = obj.AddComponent<UnityEngine.UI.Outline>();
        outline.effectColor = color == Color.white ? new Color(.08f,.01f,.06f,1) : new Color(1f,.94f,.86f,.9f);
        outline.effectDistance = new Vector2(1.1f,-1.1f);
        text.text = value; text.fontSize = Mathf.RoundToInt(fontSize); text.color = color;
        text.alignment = TextAnchor.MiddleCenter; text.raycastTarget = false;
        return text;
    }
    public void ResetView() { resultPanel.SetActive(false); feedback.text = "READY"; feedback.color = Color.white; feedbackUntil = Time.unscaledTime + .9f; }
    public void SetStatus(int good, int missed, int streak, float remaining, Camera cam, Transform[] lanes, float judgementY)
    {
        content.anchorMin = cam.rect.min; content.anchorMax = cam.rect.max;
        scaler.scaleFactor = Mathf.Max(.05f, cam.pixelWidth / 1920f);
        score.text = good.ToString("00");
        Position(score, cam, new Vector3(-8.9f, 4.38f, 0));
        Position(time, cam, new Vector3(8.9f, 4.38f, 0));
        Position(feedback, cam, new Vector3(lanes[1].position.x, judgementY + .85f, 0));
        time.text = Mathf.CeilToInt(remaining).ToString("00") + "초";
        combo.text = streak > 1 ? streak + " COMBO" : "";
        for (int i = 0; i < 3; i++)
        {
            Vector3 p = cam.WorldToViewportPoint(new Vector3(lanes[i].position.x, judgementY - 1.05f, 0));
            keys[i].rectTransform.anchorMin = keys[i].rectTransform.anchorMax = new Vector2(p.x, p.y);
        }
        if (Time.unscaledTime > feedbackUntil) feedback.text = "";
    }
    static void Position(UnityEngine.UI.Text label, Camera cam, Vector3 world)
    {
        Vector3 p = cam.WorldToViewportPoint(world);
        label.rectTransform.anchorMin = label.rectTransform.anchorMax = new Vector2(p.x, p.y);
    }
    public void Feedback(bool success, int lane)
    {
        feedback.text = success ? "GOOD" : "MISS";
        feedback.color = success ? new Color(.3f,1f,.6f) : new Color(1f,.3f,.4f);
        feedbackUntil = Time.unscaledTime + .8f;
    }
    public void ShowResults(int good, int missed, int best)
    {
        feedback.text = "";
        results.text = "테스트 종료\n\n성공: " + good.ToString("00") + "회     실패: " + missed.ToString("00") + "회\n\n최고 콤보  " + best;
        resultPanel.SetActive(true);
        var button = resultPanel.GetComponentInChildren<UnityEngine.UI.Button>();
        if (button && UnityEngine.EventSystems.EventSystem.current) button.Select();
    }
}

