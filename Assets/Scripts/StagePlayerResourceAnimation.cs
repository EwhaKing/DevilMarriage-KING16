using UnityEngine;

public class StagePlayerResourceAnimation :
    MonoBehaviour
{
    [SerializeField] private StageResourceManager resourceManager;

    [SerializeField] private StagePlayerAnimationController playerAnimation;

    private int _previousSanity;

    private void Start()
    {
        if (resourceManager == null)
        {
            resourceManager =
                StageResourceManager.Instance
                ?? FindAnyObjectByType<StageResourceManager>();
        }

        if (playerAnimation == null)
        {
            playerAnimation =
                GetComponent<StagePlayerAnimationController>();
        }

        if (resourceManager == null)
            return;

        _previousSanity = resourceManager.CurrentSanity;

        resourceManager.OnSanityChanged += HandleSanityChanged;
        resourceManager.OnGameOver += HandleGameOver;
    }

    private void OnDestroy()
    {
        if (resourceManager == null)
            return;

        resourceManager.OnSanityChanged -= HandleSanityChanged;
        resourceManager.OnGameOver -= HandleGameOver;
    }

    private void HandleSanityChanged(int current, int max)
    {
        if (current < _previousSanity &&
            current > 0 &&
            playerAnimation != null)
        {
            playerAnimation.PlayDamaged();
        }

        _previousSanity = current;
    }

    private void HandleGameOver()
    {
        if (playerAnimation != null)
            playerAnimation.PlayDeath();
    }
}
