using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;

public class SkipModeController : MonoBehaviour
{
    [Header("Required")]
    [SerializeField] private DialogueRunner dialogueRunner;

    [Header("Skip Button Visual")]
    [SerializeField] private Button skipButton;
    [SerializeField] private Image skipButtonImage;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color pressedColor = Color.red;
    [SerializeField] private float flashDuration = 0.15f;

    [Header("Pause Condition")]
    [SerializeField] private GameObject optionsPresenter;

    [Header("Stop Conditions")]
    [SerializeField] private List<GameObject> playerControlObjects = new List<GameObject>();

    [Header("Skip Settings")]
    [SerializeField] private float requestInterval = 0.03f;
    [SerializeField] private int maxSkipRequestCount = 300;

    [Header("Optional")]
    [SerializeField] private AutoModeController autoModeController;

    private bool isSkipMode = false;
    private Coroutine skipCoroutine;
    private Coroutine flashCoroutine;

    public bool IsSkipMode => isSkipMode;
    public bool IsPausedByOptions => isSkipMode && IsObjectActuallyVisible(optionsPresenter);

    private void Awake()
    {
        if (skipButton == null)
        {
            skipButton = GetComponent<Button>();
        }

        if (skipButtonImage == null && skipButton != null)
        {
            skipButtonImage = skipButton.GetComponent<Image>();
        }

        if (skipButtonImage != null)
        {
            normalColor = skipButtonImage.color;
        }

        if (skipButton != null)
        {
            skipButton.onClick.AddListener(OnSkipButtonClicked);
        }
    }

    private void OnDestroy()
    {
        if (skipButton != null)
        {
            skipButton.onClick.RemoveListener(OnSkipButtonClicked);
        }
    }

    public void OnSkipButtonClicked()
    {
        FlashSkipButton();

        if (isSkipMode)
        {
            StopSkipMode();
        }
        else
        {
            StartSkipMode();
        }
    }

    public void StartSkipMode()
    {
        if (dialogueRunner == null)
        {
            Debug.LogWarning("SkipButtonController: DialogueRunner가 연결되어 있지 않습니다.");
            return;
        }

        if (autoModeController != null && autoModeController.IsAutoMode)
        {
            autoModeController.StopAuto();
        }

        isSkipMode = true;

        if (skipCoroutine != null)
        {
            StopCoroutine(skipCoroutine);
        }

        skipCoroutine = StartCoroutine(SkipRoutine());

        Debug.Log("Skip Mode ON");
    }

    public void StopSkipMode()
    {
        isSkipMode = false;

        if (skipCoroutine != null)
        {
            StopCoroutine(skipCoroutine);
            skipCoroutine = null;
        }

        Debug.Log("Skip Mode OFF");
    }

    private IEnumerator SkipRoutine()
    {
        int requestCount = 0;

        while (isSkipMode)
        {
            if (dialogueRunner == null)
            {
                break;
            }

            // 스테이지 시작 / 플레이어 조작 UI가 켜지면 Skip 완전 종료
            if (ShouldStopSkipping())
            {
                Debug.Log("Skip Mode stopped: player control section reached.");
                break;
            }

            // 선택지가 떠 있으면 Skip 종료가 아니라 일시정지
            // 선택지가 사라지면, 즉 플레이어가 선택지를 누르면 다시 아래로 내려가서 스킵 재개
            if (IsObjectActuallyVisible(optionsPresenter))
            {
                yield return null;
                continue;
            }

            // 대화가 잠깐 멈춰 있는 프레임이면 기다림
            if (!dialogueRunner.IsDialogueRunning)
            {
                yield return null;
                continue;
            }

            dialogueRunner.RequestNextLine();
            requestCount++;

            if (requestCount >= maxSkipRequestCount)
            {
                Debug.LogWarning("SkipButtonController: 최대 스킵 요청 횟수에 도달해서 중단합니다. Stop Conditions 설정을 확인해주세요.");
                break;
            }

            yield return new WaitForSecondsRealtime(requestInterval);
        }

        isSkipMode = false;
        skipCoroutine = null;
    }

    private bool ShouldStopSkipping()
    {
        for (int i = 0; i < playerControlObjects.Count; i++)
        {
            if (IsObjectActuallyVisible(playerControlObjects[i]))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsObjectActuallyVisible(GameObject obj)
    {
        if (obj == null)
        {
            return false;
        }

        if (!obj.activeInHierarchy)
        {
            return false;
        }

        CanvasGroup canvasGroup = obj.GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            return true;
        }

        return canvasGroup.alpha > 0.01f && canvasGroup.blocksRaycasts;
    }

    private void FlashSkipButton()
    {
        if (skipButtonImage == null)
        {
            return;
        }

        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
        }

        flashCoroutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        skipButtonImage.color = pressedColor;

        yield return new WaitForSecondsRealtime(flashDuration);

        skipButtonImage.color = normalColor;
        flashCoroutine = null;
    }
}
