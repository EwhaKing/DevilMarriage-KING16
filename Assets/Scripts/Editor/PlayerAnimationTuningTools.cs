using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Shared idle timing is authored on the states, so every scene using these assets inherits it.
public static class PlayerAnimationTuningTools
{
    const string Request = "Temp/PlayerAnimationTuning.request";
    [InitializeOnLoadMethod]
    static void ApplyWhenRequested()
    {
        if (File.Exists(Request)) EditorApplication.delayCall += ApplyRequest;
    }
    static void ApplyRequest()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) { EditorApplication.delayCall += ApplyRequest; return; }
        if (!File.Exists(Request)) return;
        if (EditorApplication.isPlaying) { Debug.LogWarning("[Player] Stop Play mode before applying animation asset changes."); return; }
        try { ApplyAndVerify(); File.Delete(Request); }
        catch (Exception error) { File.WriteAllText("Temp/PlayerAnimationTuningReport.txt",error.ToString()); Debug.LogException(error); }
    }
    [MenuItem("DevilMarriage/Player/Apply 1 Second Idle and Verify Facing")]
    public static void ApplyAndVerify()
    {
        var report = new StringBuilder();
        foreach (var path in new [] {"Assets/Art/Player/Player.controller", "Assets/Scripts/PlayerAnimatorController.controller"})
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller == null) throw new InvalidOperationException("Missing controller: "+path);
            foreach (var layer in controller.layers)
            foreach (var child in layer.stateMachine.states)
            {
                var state = child.state;
                if (state.motion is AnimationClip clip && clip.name == "Player_Idle")
                {
                    float before = state.speed;
                    Undo.RecordObject(state,"Slow shared player idle");
                    state.speed = clip.length / 1f;
                    EditorUtility.SetDirty(state);
                    Require(Mathf.Abs(clip.length/state.speed-1f)<.001f,"Idle cycle must be one second");
                    report.AppendLine("PASS "+path+" Idle speed "+before+" -> "+state.speed+"; cycle=1.0s");
                }
                else report.AppendLine("UNCHANGED "+state.name+" speed="+state.speed);
            }
        }
        AssetDatabase.SaveAssets();
        VerifyFacing(report);
        File.WriteAllText("Temp/PlayerAnimationTuningReport.txt",report.ToString());
        Debug.Log("[Player] Idle and facing verification completed.\n"+report);
    }
    static void Require(bool condition,string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
    static void VerifyFacing(StringBuilder report)
    {
        var scene = EditorSceneManager.NewPreviewScene();
        try
        {
            var go = new GameObject("Player facing verification", typeof(SpriteRenderer),typeof(Animator));
            SceneManager.MoveGameObjectToScene(go,scene);
            var visual=go.GetComponent<SpriteRenderer>();
            var player=go.AddComponent<StagePlayerAnimationController>();
            go.transform.localScale=new Vector3(2,3,1);
            player.FaceTowardWorldPosition(Vector3.right);
            Require(visual.flipX,"Right target should face right");
            player.FaceTowardWorldPosition(Vector3.left);
            Require(!visual.flipX,"Left target should face left");
            player.FaceTowardWorldPosition(Vector3.right);
            player.FaceTowardWorldPosition(Vector3.up);
            Require(visual.flipX,"Same X should retain previous facing");
            Require(go.transform.position==Vector3.zero && go.transform.localScale==new Vector3(2,3,1),"Facing must preserve position and scale");
            report.AppendLine("PASS puzzle world clicks: right, left, same-X retain direction; transform unchanged");

            var cameraObject=new GameObject("Facing test camera",typeof(Camera));
            SceneManager.MoveGameObjectToScene(cameraObject,scene);
            var camera=cameraObject.GetComponent<Camera>();
            camera.orthographic=true; cameraObject.transform.position=new Vector3(0,0,-10);
            var centre=camera.WorldToScreenPoint(Vector3.zero);
            player.FaceTowardScreenPosition(new Vector2(centre.x+10,centre.y),camera);
            Require(visual.flipX,"Right screen click should face right");
            player.FaceTowardScreenPosition(new Vector2(centre.x-10,centre.y),camera);
            Require(!visual.flipX,"Left screen click should face left");
            report.AppendLine("PASS puzzle screen clicks: right and left");

            var mapObject=new GameObject("Map facing verification",typeof(RectTransform),typeof(UnityEngine.UI.Image));
            SceneManager.MoveGameObjectToScene(mapObject,scene);
            var map=mapObject.AddComponent<StageSelectMapPlayer>();
            mapObject.transform.localScale=new Vector3(2,3,1);
            map.FaceTowardWorldPosition(Vector3.right);
            Require(mapObject.transform.localScale==new Vector3(-2,3,1),"Map should face right and preserve size");
            map.FaceTowardWorldPosition(Vector3.left);
            Require(mapObject.transform.localScale==new Vector3(2,3,1),"Map should face left and preserve size");
            report.AppendLine("PASS map destination clicks: right and left; scale magnitude unchanged");
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }
}
