using UnityEngine;

public class RopeDartVisualManagerNew : MonoBehaviour
{
    [SerializeField] private Animator _playerAnimator;
    [SerializeField] private Animator _ropeDartAnimator;

    public void UpdateVisuals(BindingGraphData.BindingGraphConnection bindingConnection)
    {
        UpdateVisuals(bindingConnection.animation);
    }

    public void UpdateVisuals(string animationClip)
    {
        string playerClip = CreatePlayerClipString(animationClip);
        string ropeDartClip = CreateRopeDartClipString(animationClip);
        Debug.Log($"Playing animation clips: {playerClip}, {ropeDartClip}");

        _playerAnimator.Play(playerClip);
        _ropeDartAnimator.Play(ropeDartClip);

        if (RopeDartManagerNew.Instance.IsFrontPlane)
        {
            _ropeDartAnimator.transform.GetComponent<SpriteRenderer>().sortingOrder = 1;
        }
        else
        {
            _ropeDartAnimator.transform.GetComponent<SpriteRenderer>().sortingOrder = -1;
        }
    }

    private string CreatePlayerClipString(string bindingConnectionAnimation)
    {
        string output = "Player@" + bindingConnectionAnimation;
        output += RopeDartManagerNew.Instance.IsFrontPlane ? "_Front" : "_Back";

        if (!bindingConnectionAnimation.StartsWith("Cast")) output += RopeDartManagerNew.Instance.IsClockwise ? "_CW" : "_CCW";

        // TODO: temp add "_Loop", eventually replace with "_Start" clip
        output += "_Loop";

        return output;
    }

    private string CreateRopeDartClipString(string bindingConnectionAnimation)
    {
        string output = "Rope@";

        if (bindingConnectionAnimation.StartsWith("Spin"))
        {
            output += "Spin";
            output += RopeDartManagerNew.Instance.IsClockwise ? "_CW" : "_CCW";

            // TODO: temp add "_Loop", eventually replace with "_Start" clip
            output += "_Loop";
        }
        else if (bindingConnectionAnimation.StartsWith("Cast"))
        {
            output += bindingConnectionAnimation;

            // TODO: temp add "_Loop", eventually replace with "_Start" clip
            output += "_Loop";
        }
        else
        {
            output += "Decay";
            output += RopeDartManagerNew.Instance.IsClockwise ? "_CW" : "_CCW";
        }

        return output;
    }
}
