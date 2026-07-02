using Yarn.Unity;

public class YarnLineLogPresenter : DialoguePresenterBase
{
    public DialogueLogManager logManager;
    public bool clearLogOnDialogueStart = true;

    public override YarnTask OnDialogueStartedAsync()
    {
        if (clearLogOnDialogueStart && logManager != null)
        {
            logManager.ClearLog();
        }

        return YarnTask.CompletedTask;
    }

    public override YarnTask OnDialogueCompleteAsync()
    {
        // 이 Presenter는 로그 저장만 담당하므로
        // 대화 종료 시 특별히 할 일 없음
        return YarnTask.CompletedTask;
    }

    public override YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
    {
        if (logManager != null && line != null)
        {
            string speakerName = line.CharacterName;
            string dialogueText = line.TextWithoutCharacterName.Text;

            logManager.AddLineLog(speakerName, dialogueText);
        }

        return YarnTask.CompletedTask;
    }
}
