using System;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Keeps the original scene references and artwork; notes are judged once by time.
public class BandNoteSpawner : MonoBehaviour
{
    [SerializeField] Transform[] lanes = new Transform[3];
    [SerializeField] GameObject[] notePrefabs = new GameObject[3];
    [SerializeField, Min(1)] float playDuration = 20f;
    [SerializeField, Min(.1f)] float spawnInterval = 1f;
    [SerializeField, Min(0)] float startDelay = .5f;
    [SerializeField, Min(.1f)] float noteSpeed = 5f;
    [SerializeField] float edgeMargin = 1f;
    [SerializeField] float judgementY = -4.34f;
    [SerializeField, Range(.05f, .5f)] float hitWindow = .18f;
    [SerializeField] Font koreanSourceFont;

    sealed class Note { public Transform view; public int lane; public float hitTime; }
    readonly List<Note> notes = new List<Note>();
    Camera cam;
    BandHud hud;
    float elapsed, nextSpawn, travelTime;
    int good, missed, combo, best;
    bool finished;
    public int SuccessCount => good;
    public int FailureCount => missed;
    public bool IsFinished => finished;
    public float Elapsed => elapsed;

#if ENABLE_INPUT_SYSTEM
    InputAction[] laneActions;
    InputAction restartAction;
    readonly bool[] pendingKeys = new bool[3];
    bool pendingRestart;
    void OnEnable()
    {
        string[] bindings = { "<Keyboard>/leftArrow", "<Keyboard>/upArrow", "<Keyboard>/rightArrow" };
        laneActions = new InputAction[3];
        for (int i = 0; i < 3; i++)
        {
            int lane = i;
            laneActions[i] = new InputAction("BandLane"+i, InputActionType.Button, bindings[i]);
            laneActions[i].performed += context => pendingKeys[lane] = true;
            laneActions[i].Enable();
        }
        restartAction = new InputAction("BandRestart", InputActionType.Button, "<Keyboard>/r");
        restartAction.performed += context => pendingRestart = true;
        restartAction.Enable();
    }
    void OnDisable()
    {
        if (laneActions != null) foreach (var action in laneActions) action.Dispose();
        restartAction?.Dispose();
    }
#endif

    void Start()
    {
        cam = Camera.main;
        if (cam == null || !cam.orthographic || lanes.Length != 3 || notePrefabs.Length != 3)
        { Debug.LogError("[Band] Orthographic Main Camera and three lanes/notes are required."); enabled = false; return; }
        for (int i = 0; i < 3; i++)
            if (!lanes[i] || !notePrefabs[i])
            { Debug.LogError("[Band] Missing lane or note reference: " + i); enabled = false; return; }
        // Scene objects are templates, not playable notes. Keep their GUID references.
        foreach (var template in notePrefabs) if (template.scene.IsValid()) template.SetActive(false);
        FitCamera();
        hud = new GameObject("BandHUD").AddComponent<BandHud>();
        hud.Build(koreanSourceFont, Restart);
        Restart();
    }

    public void Restart()
    {
        foreach (var n in notes) if (n.view) Destroy(n.view.gameObject);
        notes.Clear();
#if ENABLE_INPUT_SYSTEM
        Array.Clear(pendingKeys,0,3); pendingRestart=false;
#endif
        elapsed = 0; nextSpawn = Mathf.Max(0, startDelay);
        good = missed = combo = best = 0; finished = false;
        travelTime = (TopY - judgementY) / Mathf.Max(.1f, noteSpeed);
        hud.ResetView(); UpdateHud();
    }
    // Original artwork spans roughly 24 world units; keep side panels visible at 16:9.
    void FitCamera()
    {
        const float aspect = 16f / 9f;
        float screenAspect = (float)Screen.width / Mathf.Max(1, Screen.height);
        cam.rect = screenAspect > aspect
            ? new Rect((1f-aspect/screenAspect)*.5f,0,aspect/screenAspect,1)
            : new Rect(0,(1f-screenAspect/aspect)*.5f,1,screenAspect/aspect);
        cam.orthographicSize = 6.75f;
    }
    float TopY => cam.transform.position.y + cam.orthographicSize + edgeMargin;

    void Update()
    {
        FitCamera();
        if (finished) { if (RestartPressed()) Restart(); return; }
#if ENABLE_INPUT_SYSTEM
        pendingRestart = false;
#endif
        elapsed = Mathf.Min(playDuration, elapsed + Time.deltaTime);
        // Stop spawning early enough that every note can be judged within 20 seconds.
        while (nextSpawn + travelTime + hitWindow < playDuration && nextSpawn <= elapsed)
        {
            Spawn(UnityEngine.Random.Range(0, 3), nextSpawn + travelTime);
            nextSpawn += Mathf.Max(.1f, spawnInterval);
        }
        for (int lane = 0; lane < 3; lane++) if (Pressed(lane)) Judge(lane);
        for (int i = notes.Count - 1; i >= 0; i--)
        {
            var n = notes[i];
            n.view.position = new Vector3(lanes[n.lane].position.x,
                judgementY + (n.hitTime - elapsed) * noteSpeed, lanes[n.lane].position.z);
            if (elapsed > n.hitTime + hitWindow) { Remove(i); Miss(); }
        }
        if (elapsed >= playDuration) Finish();
        UpdateHud();
    }

    void Spawn(int lane, float hitTime)
    {
        var obj = Instantiate(notePrefabs[lane], transform);
        var sourceRenderer = notePrefabs[lane].GetComponent<SpriteRenderer>();
        if (sourceRenderer && sourceRenderer.sprite)
        {
            Vector2 size = sourceRenderer.sprite.bounds.size;
            obj.transform.localScale = new Vector3(1.35f/size.x,.8f/size.y,1);
        }
        obj.name = "BandNote_" + lane;
        obj.transform.position = new Vector3(lanes[lane].position.x, TopY, lanes[lane].position.z);
        obj.SetActive(true);
        var renderer = obj.GetComponent<SpriteRenderer>();
        if (renderer) { renderer.sortingLayerID = 0; renderer.sortingOrder = 20; }
        notes.Add(new Note { view = obj.transform, lane = lane, hitTime = hitTime });
    }
    public void Judge(int lane)
    {
        if (finished || lane < 0 || lane > 2) return;
        int closest = -1; float distance = float.MaxValue;
        for (int i = 0; i < notes.Count; i++)
            if (notes[i].lane == lane && Mathf.Abs(notes[i].hitTime - elapsed) < distance)
            { closest = i; distance = Mathf.Abs(notes[i].hitTime - elapsed); }
        if (closest >= 0 && distance <= hitWindow)
        { Remove(closest); good++; combo++; best = Mathf.Max(best, combo); hud.Feedback(true, lane); }
        else Miss(lane);
    }
    void Remove(int index) { Destroy(notes[index].view.gameObject); notes.RemoveAt(index); }
    void Miss(int lane = -1) { missed++; combo = 0; hud.Feedback(false, lane); }
    void Finish()
    {
        // Any unresolved note is counted exactly once before results are shown.
        for (int i = notes.Count - 1; i >= 0; i--) { Remove(i); missed++; }
        finished = true; hud.ShowResults(good, missed, best);
    }
    void OnDestroy() { if (hud) Destroy(hud.gameObject); }
    void UpdateHud() => hud.SetStatus(good, missed, combo, Mathf.Max(0, playDuration - elapsed), cam, lanes, judgementY);
    bool Pressed(int lane)
    {
#if ENABLE_INPUT_SYSTEM
        bool pressed = pendingKeys[lane]; pendingKeys[lane] = false; return pressed;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(lane == 0 ? KeyCode.LeftArrow : lane == 1 ? KeyCode.UpArrow : KeyCode.RightArrow);
#else
        return false;
#endif
    }
    bool RestartPressed()
    {
#if ENABLE_INPUT_SYSTEM
        bool pressed = pendingRestart; pendingRestart = false; return pressed;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.R);
#else
        return false;
#endif
    }
}

