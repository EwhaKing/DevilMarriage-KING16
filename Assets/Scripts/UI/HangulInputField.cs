using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using TMPro;

/// <summary>
/// Input System 전용 모드에서는 TMP 입력창이 한글 IME 조합을 받지 못합니다.
/// 키보드의 조합/확정 이벤트를 직접 받아 이름을 넣습니다.
/// </summary>
[RequireComponent(typeof(TMP_InputField))]
public class HangulInputField : MonoBehaviour
{
    TMP_InputField _field;
    string _committed = "";
    string _composition = "";
    string _echo;
    bool _hooked;

    public string CommittedText => _committed;

    void Awake()
    {
        _field = GetComponent<TMP_InputField>();
        _field.richText = false;
        _field.readOnly = true;
    }

    void OnEnable()
    {
        Hook(true);
        Keyboard.current?.SetIMEEnabled(true);
        ApplyDisplay();
    }

    void OnDisable()
    {
        CommitVisibleText();
        Hook(false);
        Keyboard.current?.SetIMEEnabled(false);
    }

    public void CommitVisibleText()
    {
        if (string.IsNullOrEmpty(_composition))
            return;

        if (!string.IsNullOrEmpty(_committed) && _committed.EndsWith(_composition))
        {
            _composition = "";
            _echo = null;
            ApplyDisplay();
            return;
        }

        CommitComposition();
    }

    public void SetCommitted(string value)
    {
        _committed = value ?? "";
        _composition = "";
        _echo = null;
        TrimToLimit();
        ApplyDisplay();
    }

    void Update()
    {
        if (!_hooked)
            Hook(true);
        if (!_hooked)
            return;

        var keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        keyboard.SetIMEEnabled(true);
        UpdateImeCursor(keyboard);

        if (keyboard.backspaceKey.wasPressedThisFrame && string.IsNullOrEmpty(_composition))
        {
            if (_committed.Length > 0)
            {
                _committed = _committed.Substring(0, _committed.Length - 1);
                _echo = null;
                ApplyDisplay();
            }
        }

        bool ctrl = keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed;
        if (ctrl && keyboard.vKey.wasPressedThisFrame)
            Paste();
    }

    void Hook(bool enable)
    {
        var keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (enable && !_hooked)
        {
            keyboard.onTextInput += OnTextInput;
            keyboard.onIMECompositionChange += OnComposition;
            _hooked = true;
        }
        else if (!enable && _hooked)
        {
            keyboard.onTextInput -= OnTextInput;
            keyboard.onIMECompositionChange -= OnComposition;
            _hooked = false;
        }
    }

    void OnTextInput(char character)
    {
        if (!isActiveAndEnabled || character < ' ')
            return;

        _composition = "";
        _echo = character.ToString();
        Append(character.ToString());
        ApplyDisplay();
    }

    void OnComposition(IMECompositionString composition)
    {
        if (!isActiveAndEnabled)
            return;

        var next = composition.ToString();
        if (!string.IsNullOrEmpty(_echo) && next == _echo)
            return;

        _echo = null;
        _composition = next;
        ApplyDisplay();
    }

    void Paste()
    {
        var clip = GUIUtility.systemCopyBuffer;
        if (string.IsNullOrEmpty(clip))
            return;

        _composition = "";
        _echo = null;
        Append(clip.Replace("\r", "").Replace("\n", ""));
        ApplyDisplay();
    }

    void CommitComposition()
    {
        if (string.IsNullOrEmpty(_composition))
            return;

        var pending = _composition;
        _composition = "";
        _echo = null;
        Append(pending);
        ApplyDisplay();
    }

    void Append(string value)
    {
        if (string.IsNullOrEmpty(value))
            return;

        int limit = _field != null ? _field.characterLimit : 0;
        foreach (var character in value)
        {
            if (character < ' ')
                continue;
            if (limit > 0 && _committed.Length >= limit)
                break;
            _committed += character;
        }
    }

    void TrimToLimit()
    {
        int limit = _field != null ? _field.characterLimit : 0;
        if (limit > 0 && _committed.Length > limit)
            _committed = _committed.Substring(0, limit);
    }

    void ApplyDisplay()
    {
        if (_field == null)
            return;

        _field.SetTextWithoutNotify(_committed + _composition);
        int end = _field.text.Length;
        _field.caretPosition = end;
        _field.stringPosition = end;
    }

    void UpdateImeCursor(Keyboard keyboard)
    {
        var rect = _field.transform as RectTransform;
        if (rect == null)
            return;

        var corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        var camera = _field.GetComponentInParent<Canvas>()?.worldCamera;
        var screen = RectTransformUtility.WorldToScreenPoint(camera, corners[1]);
        keyboard.SetIMECursorPosition(new Vector2(screen.x, Screen.height - screen.y));
    }
}
