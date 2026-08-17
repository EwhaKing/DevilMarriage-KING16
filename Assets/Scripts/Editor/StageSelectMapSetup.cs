#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// StageSelectScene에 Stage2~66 버튼을 만들어 Scene 뷰에서 직접 배치할 수 있게 합니다.
/// </summary>
public static class StageSelectMapSetup
{
    private const string ScenePath = "Assets/Scenes/StageSelectScene.unity";

    [MenuItem("DevilMarriage/Stage Select/Create Missing Stage Buttons")]
    public static void CreateMissingStageButtons()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            EditorUtility.DisplayDialog(
                "Stage Select",
                "StageSelectScene을 연 다음 다시 실행해주세요.",
                "확인");
            return;
        }

        var template = GameObject.Find("Stage1_Button");
        if (template == null)
        {
            EditorUtility.DisplayDialog("실패", "Stage1_Button을 찾을 수 없습니다.", "확인");
            return;
        }

        var controller = UnityEngine.Object.FindAnyObjectByType<StageSelectController>();
        if (controller != null && controller.GetComponent<StageSelectPathLayout>() == null)
            Undo.AddComponent<StageSelectPathLayout>(controller.gameObject);

        var parent = template.transform.parent;
        var templateRect = template.GetComponent<RectTransform>();
        int created = 0;

        for (int i = 2; i <= StageProgressManager.StageSelectButtonCount; i++)
        {
            var name = $"Stage{i}_Button";
            if (GameObject.Find(name) != null)
                continue;

            var clone = UnityEngine.Object.Instantiate(template, parent);
            clone.name = name;
            var rect = clone.GetComponent<RectTransform>();
            if (rect != null && templateRect != null)
            {
                rect.anchorMin = templateRect.anchorMin;
                rect.anchorMax = templateRect.anchorMax;
                rect.pivot = templateRect.pivot;
                rect.sizeDelta = templateRect.sizeDelta;
                rect.anchoredPosition = DefaultGridPosition(templateRect.anchoredPosition, templateRect.sizeDelta, i);
            }

            Undo.RegisterCreatedObjectUndo(clone, "Create Stage Buttons");
            created++;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorUtility.DisplayDialog(
            "완료",
            created > 0
                ? $"{created}개 버튼을 만들었습니다. Scene 뷰에서 위치를 옮긴 뒤, StageSelectPathLayout의 Links에 From/To를 넣고 Rebuild Paths From Links를 누르세요."
                : "이미 Stage 버튼이 66개 있습니다. Path는 StageSelectController의 StageSelectPathLayout에서 연결하세요.",
            "확인");
    }

    private static Vector2 DefaultGridPosition(Vector2 origin, Vector2 size, int stageNumber)
    {
        const int columns = 22;
        const float spacing = 24f;
        int index = Mathf.Max(0, stageNumber - 1);
        int col = index % columns;
        int row = index / columns;
        return origin + new Vector2(col * (size.x + spacing), -row * (size.y + spacing));
    }
}
#endif
