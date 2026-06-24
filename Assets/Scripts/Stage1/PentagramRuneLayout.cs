using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 5개 룬을 정오각형 꼭짓점에 배치한다. (오각별 = K5, 모든 룬이 직접 연결됨)
/// </summary>
public class PentagramRuneLayout : MonoBehaviour
{
    [SerializeField] private float radius = 3f;
    [SerializeField] private bool setStartOnTop = true;

    [ContextMenu("Arrange Runes In Pentagon")]
    public void ArrangeRunes()
    {
        var runes = GetComponentsInChildren<RuneNode>();
        if (runes.Length != 5)
        {
            Debug.LogWarning($"PentagramRuneLayout: 룬이 5개여야 합니다. 현재 {runes.Length}개.");
            return;
        }

        for (int i = 0; i < 5; i++)
        {
            float angleDeg = 90f - i * 72f;
            float rad = angleDeg * Mathf.Deg2Rad;
            var pos = new Vector3(Mathf.Cos(rad) * radius, Mathf.Sin(rad) * radius, 0f);
            runes[i].transform.position = transform.position + pos;
            bool isStart = setStartOnTop && i == 0;
            runes[i].Configure(i, isStart, mandatory: true, forbidden: false);

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(runes[i]);
#endif
        }

        var pathBuilder = GetComponent<PentagramPathBuilder>();
        if (pathBuilder != null)
            pathBuilder.RefreshPathPositions();

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(PentagramRuneLayout))]
public class PentagramRuneLayoutEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (GUILayout.Button("Arrange Runes In Pentagon"))
            ((PentagramRuneLayout)target).ArrangeRunes();
    }
}
#endif
