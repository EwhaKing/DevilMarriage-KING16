#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(StageSelectPathLayout))]
public class StageSelectPathLayoutEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var layout = target as StageSelectPathLayout;
        if (layout == null)
            return;

        EditorGUILayout.Space(8);
        EditorGUILayout.HelpBox(
            "1) Links 크기를 정하고 From/To에 Stage 버튼을 드래그합니다.\n" +
            "2) 필요하면 Waypoints로 꺾인 Path를 만듭니다.\n" +
            "3) Rebuild Paths From Links를 누릅니다.\n" +
            "버튼을 옮긴 뒤에는 Refresh Path Positions를 누르세요.",
            MessageType.Info);

        if (GUILayout.Button("Rebuild Paths From Links", GUILayout.Height(32)))
        {
            EditorApplication.delayCall += () =>
            {
                if (layout != null)
                    layout.RebuildPathsFromLinks();
            };
        }

        if (GUILayout.Button("Refresh Path Positions"))
        {
            EditorApplication.delayCall += () =>
            {
                if (layout != null)
                    layout.RefreshPathPositions();
            };
        }
    }
}
#endif
