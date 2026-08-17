using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// StagePlay 하단의 악마 소환 스킬 패널.
/// 해금된 악마만 버튼으로 표시하고, 스테이지 시도당 사용 횟수를 관리합니다.
/// </summary>
public class DemonSummonSkillPanel : MonoBehaviour
{
    private static readonly Color ReadyColor = Color.white;
    private static readonly Color UsedColor = new Color(0.38f, 0.38f, 0.38f, 1f);

    [SerializeField] private DemonSkillCatalog catalog;
    [SerializeField] private RectTransform buttonRoot;
    [SerializeField] private float buttonSize = 88f;

    private readonly Dictionary<DemonSkillId, int> _useCounts = new Dictionary<DemonSkillId, int>();
    private readonly Dictionary<DemonSkillId, SkillButtonView> _buttons = new Dictionary<DemonSkillId, SkillButtonView>();
    private Stage1PuzzleController _puzzle;
    private DemonSkillDefinition _pendingTeleportSkill;
    private bool _handlingClick;
    private int _currentStage;
    private DemonSkillId[] _stageFilter;
    private static readonly Color TargetingColor = new Color(1f, 0.92f, 0.45f, 1f);

    private class SkillButtonView
    {
        public DemonSkillDefinition Skill;
        public Button Button;
        public Image Icon;
        public TextMeshProUGUI Label;
    }

    public static DemonSummonSkillPanel EnsureOnCanvas(Canvas canvas, DemonSkillCatalog catalog)
    {
        var existing = GameObject.Find("DemonSummonSkillPanel");
        if (existing != null)
        {
            var panel = existing.GetComponent<DemonSummonSkillPanel>();
            if (panel == null)
                panel = existing.AddComponent<DemonSummonSkillPanel>();
            if (catalog != null)
                panel.catalog = catalog;
            panel.EnsureLayout();
            return panel;
        }

        if (canvas == null)
            return null;

        var go = new GameObject("DemonSummonSkillPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(canvas.transform, false);
        var created = go.AddComponent<DemonSummonSkillPanel>();
        created.catalog = catalog;
        created.EnsureLayout();
        return created;
    }

    public void BindForStage(int stageNumber, DemonSkillId[] stageFilter = null)
    {
        EnsureLayout();
        _currentStage = stageNumber;
        _stageFilter = stageFilter;
        _useCounts.Clear();
        _handlingClick = false;

        bool visible = DemonSummonProgress.IsSkillPanelVisible(stageNumber, catalog);
        gameObject.SetActive(visible);
        if (!visible)
            return;

        BindPuzzle();
        RebuildButtons();
    }

    public void ResetUsesForCurrentAttempt()
    {
        CancelTeleportTargeting();
        _useCounts.Clear();
        _handlingClick = false;
        RefreshAllButtons();
    }

    private void EnsureLayout()
    {
        if (buttonRoot != null)
            return;

        var rect = GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 12f);
        rect.sizeDelta = new Vector2(520f, 128f);

        var background = GetComponent<Image>();
        if (background != null)
        {
            background.color = new Color(0f, 0f, 0f, 0.72f);
            background.raycastTarget = true;
        }

        var existing = transform.Find("SkillButtons");
        if (existing != null)
            buttonRoot = existing as RectTransform;

        if (buttonRoot == null)
        {
            var rootObject = new GameObject("SkillButtons", typeof(RectTransform));
            rootObject.transform.SetParent(transform, false);
            buttonRoot = rootObject.GetComponent<RectTransform>();
        }

        buttonRoot.anchorMin = Vector2.zero;
        buttonRoot.anchorMax = Vector2.one;
        buttonRoot.offsetMin = new Vector2(12f, 8f);
        buttonRoot.offsetMax = new Vector2(-12f, -8f);

        var layout = buttonRoot.GetComponent<HorizontalLayoutGroup>();
        if (layout == null)
            layout = buttonRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 16f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
    }

    private void RebuildButtons()
    {
        _buttons.Clear();
        for (int i = buttonRoot.childCount - 1; i >= 0; i--)
            Destroy(buttonRoot.GetChild(i).gameObject);

        if (catalog == null)
            catalog = DemonSkillCatalog.Load();
        if (catalog == null)
            return;

        var skills = catalog.GetAvailableSkills(_currentStage, _stageFilter);
        foreach (var skill in skills)
            CreateSkillButton(skill);
    }

    private void CreateSkillButton(DemonSkillDefinition skill)
    {
        var buttonObject = new GameObject(skill.displayName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(buttonRoot, false);

        var buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.sizeDelta = new Vector2(buttonSize, buttonSize);
        var layoutElement = buttonObject.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = buttonSize;
        layoutElement.preferredHeight = buttonSize;

        var icon = buttonObject.GetComponent<Image>();
        icon.sprite = skill.icon;
        icon.preserveAspect = true;
        icon.color = ReadyColor;
        icon.raycastTarget = true;

        var button = buttonObject.GetComponent<Button>();
        button.targetGraphic = icon;
        button.transition = Selectable.Transition.ColorTint;
        var colors = button.colors;
        colors.disabledColor = UsedColor;
        button.colors = colors;
        var captured = skill;
        button.onClick.AddListener(() => OnSkillClicked(captured));

        var labelObject = new GameObject("Label", typeof(RectTransform));
        labelObject.transform.SetParent(buttonObject.transform, false);
        var labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 0f);
        labelRect.anchorMax = new Vector2(1f, 0f);
        labelRect.pivot = new Vector2(0.5f, 1f);
        labelRect.anchoredPosition = new Vector2(0f, -4f);
        labelRect.sizeDelta = new Vector2(0f, 22f);

        var label = labelObject.AddComponent<TextMeshProUGUI>();
        label.text = skill.displayName;
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 14f;
        label.color = Color.white;
        label.raycastTarget = false;
        if (TMP_Settings.defaultFontAsset != null)
            label.font = TMP_Settings.defaultFontAsset;

        var view = new SkillButtonView
        {
            Skill = skill,
            Button = button,
            Icon = icon,
            Label = label
        };
        _buttons[skill.id] = view;
        RefreshButton(view);
    }

    private void OnSkillClicked(DemonSkillDefinition skill)
    {
        if (_handlingClick || skill == null)
            return;

        _handlingClick = true;
        try
        {
            if (!CanUse(skill))
                return;

            if (skill.effectType == DemonSkillEffectType.TeleportToRune)
            {
                ToggleTeleportTargeting(skill);
                return;
            }

            CancelTeleportTargeting();

            if (_buttons.TryGetValue(skill.id, out var view) && view.Button != null)
                view.Button.interactable = false;

            if (!DemonSummonProgress.TryUseSkill(skill))
            {
                if (view != null && view.Button != null)
                    view.Button.interactable = true;
                return;
            }

            int used = GetUseCount(skill.id) + 1;
            _useCounts[skill.id] = used;
            RefreshButton(view);
        }
        finally
        {
            _handlingClick = false;
        }
    }

    private void OnDestroy()
    {
        if (_puzzle != null)
            _puzzle.OnTeleportCompleted -= HandleTeleportCompleted;
    }

    private void BindPuzzle()
    {
        var puzzle = FindAnyObjectByType<Stage1PuzzleController>();
        if (_puzzle == puzzle)
            return;

        if (_puzzle != null)
            _puzzle.OnTeleportCompleted -= HandleTeleportCompleted;

        _puzzle = puzzle;
        if (_puzzle != null)
            _puzzle.OnTeleportCompleted += HandleTeleportCompleted;
    }

    private void ToggleTeleportTargeting(DemonSkillDefinition skill)
    {
        BindPuzzle();
        if (_puzzle == null)
            return;

        if (_pendingTeleportSkill != null
            && _pendingTeleportSkill.id == skill.id
            && _puzzle.AwaitingTeleportSelection)
        {
            CancelTeleportTargeting();
            return;
        }

        CancelTeleportTargeting();
        if (!_puzzle.BeginTeleportTargeting())
            return;

        _pendingTeleportSkill = skill;
        RefreshAllButtons();
    }

    private void CancelTeleportTargeting()
    {
        bool wasTargeting = _pendingTeleportSkill != null;
        _pendingTeleportSkill = null;
        if (_puzzle != null)
            _puzzle.CancelTeleportTargeting();
        if (wasTargeting)
            RefreshAllButtons();
    }

    private void HandleTeleportCompleted()
    {
        if (_pendingTeleportSkill == null)
            return;

        var skill = _pendingTeleportSkill;
        _pendingTeleportSkill = null;
        DemonSummonProgress.TryUseSkill(skill);
        _useCounts[skill.id] = GetUseCount(skill.id) + 1;
        RefreshAllButtons();
    }

    private bool CanUse(DemonSkillDefinition skill)
    {
        if (skill == null)
            return false;

        var resources = StageResourceManager.Instance;
        if (resources != null && resources.IsGameOver)
            return false;

        int limit = Mathf.Max(1, skill.usesPerStage);
        return GetUseCount(skill.id) < limit;
    }

    private int GetUseCount(DemonSkillId id)
    {
        return _useCounts.TryGetValue(id, out int count) ? count : 0;
    }

    private void RefreshAllButtons()
    {
        foreach (var view in _buttons.Values)
            RefreshButton(view);
    }

    private void RefreshButton(SkillButtonView view)
    {
        if (view == null)
            return;

        bool available = CanUse(view.Skill);
        bool targeting = _pendingTeleportSkill != null
                         && view.Skill != null
                         && view.Skill.id == _pendingTeleportSkill.id
                         && _puzzle != null
                         && _puzzle.AwaitingTeleportSelection;

        if (view.Button != null)
            view.Button.interactable = available;
        if (view.Icon != null)
            view.Icon.color = !available ? UsedColor : targeting ? TargetingColor : ReadyColor;
    }
}
