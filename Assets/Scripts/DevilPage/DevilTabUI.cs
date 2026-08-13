using UnityEngine;
using UnityEngine.UI;

public class DevilTabUI : MonoBehaviour
{
    public DevilCategory tabCategory;  // 이 탭이 담당할 카테고리
    public GameObject highlightObj;    // 선택된 탭 표시용 테두리/색상 오브젝트

    private DevilBookManager manager;

    public void SetupTab(DevilBookManager mgr)
    {
        manager = mgr;
        // Button 컴포넌트를 가져와 클릭 이벤트 자동 연결
        GetComponent<Button>().onClick.AddListener(OnClickTab);
    }

    private void OnClickTab()
    {
        manager.SelectCategory(tabCategory, this);
    }

    public void SetHighlight(bool isOn)
    {
        if (highlightObj != null) highlightObj.SetActive(isOn);
    }
}