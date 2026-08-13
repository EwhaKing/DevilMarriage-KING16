using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DevilSlotUI : MonoBehaviour
{
    [Header("UI 연결")]
    public Image iconImage;          // 악마 얼굴 아이콘
    public TMP_Text nameText;        // 악마 이름 텍스트
    public GameObject highlightObj;  // 선택되었을 때 켜질 테두리/배경 이미지

    [Header("미해금 이미지")]
    public Sprite lockedSprite;      // 잠겼을 때 보여줄 물음표/실루엣 이미지

    private DevilData currentData;
    private DevilBookManager manager;
    private Button btn;

    private void Awake()
    {
        btn = GetComponent<Button>();
    }

    public void SetupSlot(DevilData data, DevilBookManager mgr)
    {
        currentData = data;
        manager = mgr;

        // 버튼 클릭 이벤트 리스너 재설정 (중복 방지 및 확실한 연결)
        if (btn == null) btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(OnClickSlot);
        }

        if (data == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);

        // 해금 여부에 따른 표시
        if (data.isUnlocked)
        {
            if (iconImage != null) iconImage.sprite = data.portrait;
            if (nameText != null) nameText.text = data.devilName;
        }
        else
        {
            if (iconImage != null) iconImage.sprite = lockedSprite;
            if (nameText != null) nameText.text = "???";
        }
    }

    // 슬롯(버튼)이 클릭되었을 때 호출되는 함수
    public void OnClickSlot()
    {
        if (currentData != null && manager != null)
        {
            manager.SelectDevil(currentData, this);
        }
        else
        {
            Debug.LogWarning($"[DevilSlotUI] 클릭 실패! currentData: {(currentData != null ? "있음" : "없음")}, manager: {(manager != null ? "있음" : "없음")}");
        }
    }

    public void SetHighlight(bool isOn)
    {
        if (highlightObj != null) highlightObj.SetActive(isOn);
    }
}