using UnityEngine;

public class RopeDartVisualManagerNew : MonoBehaviour
{
    [SerializeField] private Animator _playerAnimator;
    [SerializeField] private Animator _ropeDartAnimator;

    public void UpdateVisuals(BindingGraphData.BindingGraphConnection bindingConnection)
    {
        string fullAnimationClip = CreatePlayerClipString(bindingConnection.animation);
        Debug.Log($"Playing animation clip: {fullAnimationClip}");
        _playerAnimator.Play(fullAnimationClip);
    }

    public void UpdateVisuals(string animationClip)
    {
        string fullAnimationClip = CreatePlayerClipString(animationClip);
        Debug.Log($"Playing animation clip: {fullAnimationClip}");
        _playerAnimator.Play(fullAnimationClip);
    }

    private string CreatePlayerClipString(string bindingConnectionAnimation)
    {
        string output = "Player@" + bindingConnectionAnimation;
        output += RopeDartManagerNew.Instance.IsFrontPlane ? "_Front" : "_Back";

        if (!bindingConnectionAnimation.StartsWith("Cast")) output += RopeDartManagerNew.Instance.IsClockwise ? "_CW" : "_CCW";

        // TODO: temp add "_Loop"
        output += "_Loop";

        return output;
    }
}
