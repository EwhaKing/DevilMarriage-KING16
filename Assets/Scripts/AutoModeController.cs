using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;

public interface ILineTypingStatusProvider
{
    bool IsLineFinishedTyping { get; }
}

public class AutoModeController : MonoBehaviour
{
    [Header("Required")]
    [SerializeField] private DialogueRunner dialogueRunner;

    [Header("Line Typing Status")]
    [Tooltip("ILineTypingStatusProvider를 구현한 LineView/타이프라이터 컴포넌트를 연결" +
             "비워두면 fallbackTypingWaitTime만큼 대기 후 다음 단계로 넘어감")]
    [SerializeField] private MonoBehaviour lineTypingStatusSource;
    [Tooltip("lineTypingStatusSource가 비어있을 때 사용하는 대체 대기시간(초)")]
    [SerializeField] private float fallbackTypingWaitTime = 1.0f;
    [Tooltip("타이핑 완료 여부를 확인하는 폴링 주기(초)")]
    [SerializeField] private float typingCheckInterval = 0.05f;

    [Header("Optional - Options UI")]
    [SerializeField] private GameObject optionsPresenter;
    [SerializeField] private bool stopWhenOptionsShown = false;

    [Header("Blocking Panels - Log, Setting 등")]
    [Tooltip("여기 등록된 패널이 하나라도 활성화되어 있으면 Auto 진행이 일시정지" +
             "모두 닫히면 자동으로 이어서 진행됨")]
    [SerializeField] private List<GameObject> blockingPanels = new List<GameObject>();

    [Header("Auto Settings")]
    [Tooltip("대사 출력이 끝난 후 다음 대사로 넘어가기 전 대기 시간")]
    [SerializeField] private float readDelay = 2.0f;
    [SerializeField] private float nextLineDelay = 0.2f;

    private bool isAutoMode = false;
    private Coroutine autoCoroutine;
    private ILineTypingStatusProvider typingStatusProvider;

    public bool IsAutoMode => isAutoMode;
    public bool IsPausedByPanel => isAutoMode && IsAnyBlockingPanelOpen();

    private void Awake()
    {
        typingStatusProvider = lineTypingStatusSource as ILineTypingStatusProvider;
        if (lineTypingStatusSource != null && typingStatusProvider == null)
        {
            Debug.LogWarning("AutoModeController: lineTypingStatusSource가 ILineTypingStatusProvider를 구현하지 않았습니다. " +
                              "fallbackTypingWaitTime을 대신 사용합니다.");
        }
    }

    public void ToggleAuto()
    {
        if (isAutoMode)
        {
            StopAuto();
        }
        else
        {
            StartAuto();
        }
    }

    public void StartAuto()
    {
        if (dialogueRunner == null)
        {
            Debug.LogWarning("AutoModeController: DialogueRunner is not assigned.");
            return;
        }
        if (isAutoMode)
        {
            return;
        }

        isAutoMode = true;

        if (autoCoroutine != null)
        {
            StopCoroutine(autoCoroutine);
        }
        autoCoroutine = StartCoroutine(AutoRoutine());
        Debug.Log("Auto Mode ON");
    }

    public void StopAuto()
    {
        if (!isAutoMode)
        {
            return;
        }

        isAutoMode = false;
        if (autoCoroutine != null)
        {
            StopCoroutine(autoCoroutine);
            autoCoroutine = null;
        }
        Debug.Log("Auto Mode OFF");
    }

    // 필요하다면 Log/Setting 창 스크립트의 OnEnable/OnDisable에서 직접 호출해서
    // blockingPanels 리스트에 넣지 않고도 런타임에 등록/해제 가능
    public void RegisterBlockingPanel(GameObject panel)
    {
        if (panel != null && !blockingPanels.Contains(panel))
        {
            blockingPanels.Add(panel);
        }
    }

    public void UnregisterBlockingPanel(GameObject panel)
    {
        blockingPanels.Remove(panel);
    }

    private IEnumerator AutoRoutine()
    {
        while (isAutoMode)
        {
            //Log/Setting 창이 열려있으면 대기 (열려있는 동안은 아무 진행도 하지 않음)
            if (IsAnyBlockingPanelOpen())
            {
                yield return null;
                continue;
            }

            if (ShouldStopForOptions())
            {
                StopAuto();
                yield break;
            }

            if (dialogueRunner == null || !dialogueRunner.IsDialogueRunning)
            {
                yield return null;
                continue;
            }

            //강제로 hurry-up 하지 않고, 대사가 자연스럽게 끝까지 출력될 때까지 대기
            yield return StartCoroutine(WaitUntilLineFinishedTyping());

            if (!isAutoMode)
            {
                yield break;
            }

            // 출력 완료 후 readDelay(2~3초) 동안 대기.
            //대기 중에도 패널이 열리면 타이머를 멈추고, 닫히면 이어서 진행.
            float elapsed = 0f;
            while (elapsed < readDelay)
            {
                if (!isAutoMode)
                {
                    yield break;
                }

                if (IsAnyBlockingPanelOpen() || ShouldStopForOptions())
                {
                    yield return null;
                    continue; // 패널이 열려있는 동안은 elapsed를 증가시키지 않음 (타이머 일시정지)
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            if (!isAutoMode)
            {
                yield break;
            }
            if (IsAnyBlockingPanelOpen() || ShouldStopForOptions())
            {
                continue; // 다음 루프에서 다시 패널 상태 확인
            }

            Debug.Log("Auto: request next line");
            dialogueRunner.RequestNextLine();
            yield return new WaitForSeconds(nextLineDelay);
        }
    }

    private IEnumerator WaitUntilLineFinishedTyping()
    {
        while (isAutoMode)
        {
            // 패널이 열려있으면 타이핑 완료 체크도 대기
            if (IsAnyBlockingPanelOpen())
            {
                yield return null;
                continue;
            }

            if (!dialogueRunner.IsDialogueRunning)
            {
                yield break;
            }

            if (typingStatusProvider != null)
            {
                if (typingStatusProvider.IsLineFinishedTyping)
                {
                    yield break;
                }
                yield return new WaitForSeconds(typingCheckInterval);
            }
            else
            {
                // ILineTypingStatusProvider가 연결되지 않은 경우의 대체 동작
                yield return new WaitForSeconds(fallbackTypingWaitTime);
                yield break;
            }
        }
    }

    private bool ShouldStopForOptions()
    {
        if (!stopWhenOptionsShown)
        {
            return false;
        }
        return optionsPresenter != null && optionsPresenter.activeInHierarchy;
    }

    private bool IsAnyBlockingPanelOpen()
    {
        for (int i = 0; i < blockingPanels.Count; i++)
        {
            var panel = blockingPanels[i];
            if (panel != null && panel.activeInHierarchy)
            {
                return true;
            }
        }
        return false;
    }
}
