using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;

public class LoggingOptionsPresenter : DialoguePresenterBase
{
    [Header("Option UI")]
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private Transform optionContainer;
    [SerializeField] private GameObject optionButtonPrefab;

    [Header("Log")]
    [SerializeField] private DialogueLogManager logManager;

    private readonly List<GameObject> spawnedOptionButtons = new List<GameObject>();

    public override YarnTask OnDialogueStartedAsync()
    {
        if (optionsPanel != null)
            optionsPanel.SetActive(false);

        ClearOptions();

        return YarnTask.CompletedTask;
    }

    public override YarnTask OnDialogueCompleteAsync()
    {
        if (optionsPanel != null)
            optionsPanel.SetActive(false);

        ClearOptions();

        return YarnTask.CompletedTask;
    }

    // 선택지 Presenter라도 DialoguePresenterBase를 상속하면
    // 일반 대사 처리 함수도 반드시 구현해야 함
    public override YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
    {
        return YarnTask.CompletedTask;
    }

    public override async YarnTask<DialogueOption> RunOptionsAsync(
        DialogueOption[] dialogueOptions,
        LineCancellationToken cancellationToken
    )
    {
        ClearOptions();

        if (optionsPanel != null)
            optionsPanel.SetActive(true);

        YarnTaskCompletionSource<DialogueOption> selectedOptionSource =
            new YarnTaskCompletionSource<DialogueOption>();

        foreach (DialogueOption option in dialogueOptions)
        {
            if (option.IsAvailable == false)
                continue;

            DialogueOption capturedOption = option;
            string choiceText = capturedOption.Line.TextWithoutCharacterName.Text;

            // 1. 선택지가 화면에 뜬 순간 저장
            // 로그창에는 그냥 선택지 문장만 표시됨
            // 예: 의식의 규칙을 알아보러 가자...
            if (logManager != null)
            {
                logManager.AddOptionShownLog(choiceText);
            }

            GameObject buttonObject = Instantiate(optionButtonPrefab, optionContainer);
            spawnedOptionButtons.Add(buttonObject);

            TMP_Text buttonText = buttonObject.GetComponentInChildren<TMP_Text>();
            Button button = buttonObject.GetComponent<Button>();

            if (buttonText != null)
            {
                buttonText.text = "• " + choiceText;
            }

            if (button != null)
            {
                button.onClick.AddListener(() =>
                {
                    // 2. 선택지를 클릭한 순간 저장
                    // 로그창에는 주인공 대사처럼 표시됨
                    // 예: 주인공: 의식의 규칙을 알아보러 가자...
                    if (logManager != null)
                    {
                        logManager.AddOptionSelectedLog(choiceText);
                    }

                    selectedOptionSource.TrySetResult(capturedOption);
                });
            }
        }

        DialogueOption selectedOption = await selectedOptionSource.Task;

        ClearOptions();

        if (optionsPanel != null)
            optionsPanel.SetActive(false);

        return selectedOption;
    }

    private void ClearOptions()
    {
        foreach (GameObject button in spawnedOptionButtons)
        {
            Destroy(button);
        }

        spawnedOptionButtons.Clear();
    }
}
