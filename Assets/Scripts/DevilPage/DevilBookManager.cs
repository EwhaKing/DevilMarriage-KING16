using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class DevilBookManager : MonoBehaviour
{
    [Header("데이터 베이스")]
    public List<DevilData> allDevilData;

    [Header("왼쪽 페이지 (리스트)")]
    public DevilSlotUI[] slots = new DevilSlotUI[4];
    public Button prevButton;
    public Button nextButton;

    [Header("오른쪽 페이지 (상세 정보)")]
    public TMP_Text numberText;
    public TMP_Text nameText;
    public Image largePortrait;
    public TMP_Text descText;
    public TMP_Text quoteText;
    public TMP_Text skillInfoText; // 👈 스킬 정보용 단일 TMP_Text
    public Sprite lockedLargeSprite;

    [Header("탭 설정")]
    public DevilTabUI[] tabs;

    private List<DevilData> currentFilteredList = new List<DevilData>();
    private int currentPage = 0;
    private DevilSlotUI currentSelectedSlot;
    private DevilTabUI currentSelectedTab;

    void Start()
    {
        foreach (var tab in tabs)
        {
            if (tab != null) tab.SetupTab(this);
        }

        if (prevButton != null) prevButton.onClick.AddListener(PageUp);
        if (nextButton != null) nextButton.onClick.AddListener(PageDown);

        if (tabs != null && tabs.Length > 0) SelectCategory(DevilCategory.All, tabs[0]);
    }

    public void SelectCategory(DevilCategory category, DevilTabUI selectedTab)
    {
        if (currentSelectedTab != null) currentSelectedTab.SetHighlight(false);
        currentSelectedTab = selectedTab;
        if (currentSelectedTab != null) currentSelectedTab.SetHighlight(true);

        if (category == DevilCategory.All)
            currentFilteredList = allDevilData;
        else
            currentFilteredList = allDevilData.Where(d => d != null && d.category == category).ToList();

        currentPage = 0;
        UpdatePageUI();

        if (currentFilteredList.Count > 0 && slots.Length > 0 && slots[0] != null)
        {
            slots[0].OnClickSlot();
        }
        else
        {
            ClearDetailPage();
        }
    }

    private void UpdatePageUI()
    {
        int startIndex = currentPage * 4;

        for (int i = 0; i < 4; i++)
        {
            if (slots[i] == null) continue;

            int dataIndex = startIndex + i;

            if (dataIndex < currentFilteredList.Count)
            {
                slots[i].SetupSlot(currentFilteredList[dataIndex], this);
            }
            else
            {
                slots[i].SetupSlot(null, this);
            }
        }

        UpdatePaginationButtons();
    }

    private void UpdatePaginationButtons()
    {
        if (prevButton != null) prevButton.interactable = (currentPage > 0);
        int maxPage = (currentFilteredList.Count - 1) / 4;
        if (nextButton != null) nextButton.interactable = (currentPage < maxPage);
    }

    public void PageUp() { if (currentPage > 0) { currentPage--; UpdatePageUI(); } }
    public void PageDown() { int maxPage = (currentFilteredList.Count - 1) / 4; if (currentPage < maxPage) { currentPage++; UpdatePageUI(); } }

    public void SelectDevil(DevilData data, DevilSlotUI slot)
    {
        if (data == null) return;

        if (currentSelectedSlot != null) currentSelectedSlot.SetHighlight(false);
        currentSelectedSlot = slot;
        if (currentSelectedSlot != null) currentSelectedSlot.SetHighlight(true);

        if (data.isUnlocked)
        {
            if (numberText != null) numberText.text = "NO. " + data.devilNumber.ToString("D2");
            if (nameText != null) nameText.text = data.devilName;
            if (largePortrait != null) largePortrait.sprite = data.portrait;
            if (descText != null) descText.text = data.description;
            if (quoteText != null) quoteText.text = data.quote;
            if (skillInfoText != null) skillInfoText.text = data.skillInfo; // 👈 하나의 스킬 정보로 출력
        }
        else
        {
            if (numberText != null) numberText.text = "NO. ???";
            if (nameText != null) nameText.text = "???";
            if (largePortrait != null) largePortrait.sprite = lockedLargeSprite;
            if (descText != null) descText.text = "아직 해금되지 않은 악마입니다.";
            if (quoteText != null) quoteText.text = "???";
            if (skillInfoText != null) skillInfoText.text = "???";
        }
    }

    private void ClearDetailPage()
    {
        if (numberText != null) numberText.text = "";
        if (nameText != null) nameText.text = "";
        if (largePortrait != null) largePortrait.sprite = null;
        if (descText != null) descText.text = "";
        if (quoteText != null) quoteText.text = "";
        if (skillInfoText != null) skillInfoText.text = "";
    }
}