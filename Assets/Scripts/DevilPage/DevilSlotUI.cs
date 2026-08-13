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

    // 매니저에서 이 슬롯을 초기화할 때 부르는 함수
    public void SetupSlot(DevilData data, DevilBookManager mgr)
    {
        currentData = data;
        manager = mgr;

        if (data == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);

        // 해금 여부에 따른 표시 (null 체크 추가)
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

    // 슬롯(버튼)이 클릭되었을 때 호출할 함수 (Button의 OnClick에 연결)
    public void OnClickSlot()
    {
        if (currentData != null)
        {
            manager.SelectDevil(currentData, this);
        }
    }

    // 강조 테두리 켜기/끄기
    public void SetHighlight(bool isOn)
    {
        if (highlightObj != null) highlightObj.SetActive(isOn);
    }
}