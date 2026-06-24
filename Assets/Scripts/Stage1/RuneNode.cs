using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class RuneNode : MonoBehaviour
{
    [SerializeField] private int runeIndex;
    [SerializeField] private bool isStartRune;
    [SerializeField] private bool isMandatory = true;
    [SerializeField] private bool isForbidden;

    private Stage1PuzzleController _controller;

    public int RuneIndex => runeIndex;
    public bool IsStartRune => isStartRune;
    public bool IsMandatory => isMandatory;
    public bool IsForbidden => isForbidden;
    public Vector3 WorldPosition => transform.position;

    public void Initialize(Stage1PuzzleController controller)
    {
        _controller = controller;
    }

    public void Configure(int index, bool start, bool mandatory, bool forbidden)
    {
        runeIndex = index;
        isStartRune = start;
        isMandatory = mandatory;
        isForbidden = forbidden;
    }

    private void OnMouseDown()
    {
        if (_controller == null)
            return;

        _controller.TryMoveToRune(this);
    }
}
