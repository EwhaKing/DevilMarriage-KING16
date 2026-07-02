using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueLogManager : MonoBehaviour
{
    public enum LogType
    {
        Line,
        OptionShown,
        OptionSelected
    }

    [System.Serializable]
    public class LogEntry
    {
        public LogType type;
        public string speakerName;
        public string text;

        public LogEntry(LogType type, string speakerName, string text)
        {
            this.type = type;
            this.speakerName = speakerName;
            this.text = text;
        }
    }

    [Header("Log UI")]
    [SerializeField] private GameObject logPanel;
    [SerializeField] private Button logButton;
    [SerializeField] private Button closeButton;

    [Header("Scroll View")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private Transform contentParent;
    [SerializeField] private GameObject logItemPrefab;

    [Header("Speaker Names")]
    [SerializeField] private string narrationSpeakerName = "나레이션";
    [SerializeField] private string playerSpeakerName = "주인공";

    private readonly List<LogEntry> logs = new List<LogEntry>();

    private void Awake()
    {
        if (logPanel != null)
            logPanel.SetActive(false);

        if (logButton != null)
            logButton.onClick.AddListener(OpenLog);

        if (closeButton != null)
            closeButton.onClick.AddListener(CloseLog);
    }

    public void AddLineLog(string speakerName, string dialogueText)
    {
        if (string.IsNullOrWhiteSpace(dialogueText))
            return;

        if (string.IsNullOrWhiteSpace(speakerName))
            speakerName = narrationSpeakerName;

        logs.Add(new LogEntry(LogType.Line, speakerName, dialogueText));
    }

    public void AddOptionShownLog(string optionText)
    {
        if (string.IsNullOrWhiteSpace(optionText))
            return;

        logs.Add(new LogEntry(LogType.OptionShown, "", optionText));
    }

    public void AddOptionSelectedLog(string optionText)
    {
        if (string.IsNullOrWhiteSpace(optionText))
            return;

        logs.Add(new LogEntry(LogType.OptionSelected, playerSpeakerName, optionText));
    }

    public void OpenLog()
    {
        if (logPanel != null)
            logPanel.SetActive(true);

        RefreshLogUI();

        if (scrollRect != null)
            StartCoroutine(ScrollToBottomNextFrame());
    }

    public void CloseLog()
    {
        if (logPanel != null)
            logPanel.SetActive(false);
    }

    public void ClearLog()
    {
        logs.Clear();
        RefreshLogUI();
    }

    private void RefreshLogUI()
    {
        if (contentParent == null || logItemPrefab == null)
            return;

        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        foreach (LogEntry log in logs)
        {
            GameObject item = Instantiate(logItemPrefab, contentParent);
            TMP_Text logText = item.GetComponentInChildren<TMP_Text>();

            if (logText == null)
                continue;

            switch (log.type)
            {
                case LogType.Line:
                    logText.text = $"{log.speakerName}: {log.text}";
                    break;

                case LogType.OptionShown:
                    logText.text = log.text;
                    break;

                case LogType.OptionSelected:
                    logText.text = $"{log.speakerName}: {log.text}";
                    break;
            }
        }
    }

    private IEnumerator ScrollToBottomNextFrame()
    {
        yield return null;
        yield return null;

        Canvas.ForceUpdateCanvases();

        if (scrollRect != null)
            scrollRect.verticalNormalizedPosition = 0f;
    }
}
