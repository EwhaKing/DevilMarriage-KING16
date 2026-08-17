using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Stage2 금서고 책 선택. 세 권을 모두 확인하기 전에는 메인 대사를 진행하지 않습니다.
/// </summary>
public class DialogueBookChoiceController : MonoBehaviour
{
    private static readonly string[] BookTitles =
    {
        "별에서 온 악마",
        "왜 악마들이 개쩌는 덤블링을 잘하는지에 대해서",
        "악마 개체의 좌측 둔부에 관한 관찰 보고서"
    };

    private static readonly string[] BookSpriteFileNames =
    {
        "별에서온악마",
        "왜악마들이개쩌는덤블링을잘하는지에대해서",
        "악마개체의좌측둔부보고서"
    };

    private static readonly Color ReadTint = new Color(0.45f, 0.45f, 0.45f, 1f);

    private DialogueManager _dialogueManager;
    private GameObject _root;
    private readonly bool[] _readFlags = new bool[3];
    private readonly Button[] _buttons = new Button[3];
    private readonly Image[] _images = new Image[3];
    private readonly GameObject[] _checks = new GameObject[3];
    private bool _bound;
    private bool _playingBook;

    private void Awake()
    {
        Bind();
    }

    private void OnDestroy()
    {
        if (_dialogueManager != null)
            _dialogueManager.OnCustomEvent -= HandleDialogueEvent;
    }

    private void Bind()
    {
        if (_bound)
            return;

        _dialogueManager = GetComponent<DialogueManager>() ?? FindAnyObjectByType<DialogueManager>();
        if (_dialogueManager == null)
            return;

        _dialogueManager.OnCustomEvent += HandleDialogueEvent;
        _bound = true;
    }

    private void HandleDialogueEvent(string eventId)
    {
        if (eventId != "SelectBooks")
            return;

        for (int i = 0; i < _readFlags.Length; i++)
            _readFlags[i] = false;

        _playingBook = false;
        EnsureUi();
        RefreshButtons();
        ShowChoiceUi();
    }

    private void ShowChoiceUi()
    {
        if (_root == null)
            return;

        _root.SetActive(true);
        _root.transform.SetAsLastSibling();
        RefreshButtons();
    }

    private void HideChoiceUi()
    {
        if (_root != null)
            _root.SetActive(false);
    }

    private void OnBookClicked(int bookIndex)
    {
        if (_playingBook || bookIndex < 0 || bookIndex >= _readFlags.Length)
            return;

        if (_readFlags[bookIndex])
            return;

        _playingBook = true;
        HideChoiceUi();

        var lines = DialogueContentLibrary.BuildStage02BookLines(bookIndex);
        _dialogueManager.PlayInsertedSequence(lines, () => OnBookDialogueFinished(bookIndex));
    }

    private void OnBookDialogueFinished(int bookIndex)
    {
        _readFlags[bookIndex] = true;
        _playingBook = false;

        if (AllBooksRead())
        {
            HideChoiceUi();
            _dialogueManager.NotifyExternalEventCompleted();
            return;
        }

        ShowChoiceUi();
    }

    private bool AllBooksRead()
    {
        for (int i = 0; i < _readFlags.Length; i++)
        {
            if (!_readFlags[i])
                return false;
        }

        return true;
    }

    private void RefreshButtons()
    {
        for (int i = 0; i < _buttons.Length; i++)
        {
            if (_buttons[i] == null)
                continue;

            bool read = _readFlags[i];
            _buttons[i].interactable = !read;
            if (_images[i] != null)
                _images[i].color = read ? ReadTint : Color.white;
            if (_checks[i] != null)
                _checks[i].SetActive(read);
        }
    }

    private void EnsureUi()
    {
        if (_root != null)
            return;

        var canvasGo = GameObject.Find(DialogueUiBuilder.CanvasName);
        var canvas = canvasGo != null ? canvasGo.GetComponent<Canvas>() : FindAnyObjectByType<Canvas>();
        if (canvas == null)
            return;

        _root = new GameObject("BookChoicePanel", typeof(RectTransform), typeof(Image));
        _root.transform.SetParent(canvas.transform, false);

        var blocker = _root.GetComponent<Image>();
        blocker.color = new Color(0f, 0f, 0f, 0.35f);
        blocker.raycastTarget = true;

        var rootRect = _root.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        var prompt = CreateText(_root.transform, "Prompt", "확인할 책을 고르세요", 28, TextAlignmentOptions.Center);
        var promptRect = prompt.rectTransform;
        promptRect.anchorMin = new Vector2(0.1f, 0.82f);
        promptRect.anchorMax = new Vector2(0.9f, 0.92f);
        promptRect.offsetMin = Vector2.zero;
        promptRect.offsetMax = Vector2.zero;

        float[] xs = { 0.08f, 0.36f, 0.64f };
        for (int i = 0; i < 3; i++)
        {
            var buttonGo = new GameObject($"BookButton_{i + 1}", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonGo.transform.SetParent(_root.transform, false);

            var image = buttonGo.GetComponent<Image>();
            image.color = Color.white;
            var cover = LoadBookSprite(i);
            if (cover != null)
            {
                image.sprite = cover;
                image.preserveAspect = true;
                image.type = Image.Type.Simple;
            }

            var rect = buttonGo.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(xs[i], 0.34f);
            rect.anchorMax = new Vector2(xs[i] + 0.28f, 0.8f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            if (cover == null)
            {
                var title = CreateText(buttonGo.transform, "Title", BookTitles[i], 20, TextAlignmentOptions.Center);
                title.textWrappingMode = TextWrappingModes.Normal;
                title.color = Color.white;
                var titleRect = title.rectTransform;
                titleRect.anchorMin = new Vector2(0.08f, 0.12f);
                titleRect.anchorMax = new Vector2(0.92f, 0.88f);
                titleRect.offsetMin = Vector2.zero;
                titleRect.offsetMax = Vector2.zero;
            }

            var check = CreateText(buttonGo.transform, "Check", "✓", 42, TextAlignmentOptions.TopRight);
            check.color = new Color(1f, 0.85f, 0.2f, 1f);
            var checkRect = check.rectTransform;
            checkRect.anchorMin = new Vector2(0.62f, 0.72f);
            checkRect.anchorMax = new Vector2(0.96f, 0.96f);
            checkRect.offsetMin = Vector2.zero;
            checkRect.offsetMax = Vector2.zero;
            check.gameObject.SetActive(false);

            var button = buttonGo.GetComponent<Button>();
            int captured = i;
            button.onClick.AddListener(() => OnBookClicked(captured));

            _buttons[i] = button;
            _images[i] = image;
            _checks[i] = check.gameObject;
        }

        _root.SetActive(false);
    }

    private static Sprite LoadBookSprite(int index)
    {
        if (index < 0 || index >= BookSpriteFileNames.Length)
            return null;

        string fileName = BookSpriteFileNames[index];
        var fromResources = Resources.Load<Sprite>(fileName);
        if (fromResources != null)
            return fromResources;

        var resourceSprites = Resources.LoadAll<Sprite>(fileName);
        if (resourceSprites != null && resourceSprites.Length > 0)
            return resourceSprites[0];

#if UNITY_EDITOR
        string path = $"Assets/Art/{fileName}.png";
        var assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
        if (assets != null)
        {
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Sprite sprite)
                    return sprite;
            }
        }

        return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
#else
        return null;
#endif
    }

    private static TextMeshProUGUI CreateText(Transform parent, string name, string content, float size, TextAlignmentOptions align)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var text = go.AddComponent<TextMeshProUGUI>();
        text.text = content;
        text.fontSize = size;
        text.color = Color.white;
        text.alignment = align;
        if (TMP_Settings.defaultFontAsset != null)
            text.font = TMP_Settings.defaultFontAsset;
        return text;
    }
}
