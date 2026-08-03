using UnityEngine;

public class RopeDartVisualManager : MonoBehaviour
{
    [SerializeField] private Animator _playerAnimator;
    [SerializeField] private Animator _ropeDartAnimator;

    public void UpdateVisuals(BindingGraphConnection bindingConnection)
    {
        UpdateVisuals(bindingConnection.Animation);
    }

    public void UpdateVisuals(string animationClipRoot)
    {
        string playerClip = CreatePlayerClipString(animationClipRoot);
        string ropeDartClip = CreateRopeDartClipString(animationClipRoot);
        Debug.Log($"Playing animation clips: {playerClip}, {ropeDartClip}");

        _playerAnimator.Play(playerClip);
        _ropeDartAnimator.Play(ropeDartClip);

        if (RopeDartManager.Instance.IsFrontPlane)
        {
            _ropeDartAnimator.transform.GetComponent<SpriteRenderer>().sortingOrder = 1;
        }
        else
        {
            _ropeDartAnimator.transform.GetComponent<SpriteRenderer>().sortingOrder = -1;
        }
    }

    private string CreatePlayerClipString(string animationClipRoot)
    {
        string output = "Player@" + animationClipRoot;

        if (animationClipRoot != "Cast")
        {
            output += RopeDartManager.Instance.IsLeadSide ? "_Lead" : "_Anchor";
            output += RopeDartManager.Instance.IsFrontPlane ? "_Front" : "_Back";
            output += RopeDartManager.Instance.IsClockwise ? "_CW" : "_CCW";
        }
        else
        {
            output += RopeDartManager.Instance.IsLastCastEast ? "_East" : "_West";
            output += RopeDartManager.Instance.IsFrontPlane ? "_Front" : "_Back";
        }

        // TODO: temp add "_Loop", eventually replace with "_Start" clip
        output += "_Loop";

        return output;
    }

    private string CreateRopeDartClipString(string animationClipRoot)
    {
        string output = "Rope@";

        if (animationClipRoot == "Spin")
        {
            output += "Spin";
            output += RopeDartManager.Instance.IsClockwise ? "_CW" : "_CCW";

            // TODO: temp add "_Loop", eventually replace with "_Start" clip
            output += "_Loop";
        }
        else if (animationClipRoot == "Cast" || animationClipRoot == "Retrieve")
        {
            output += "Cast";
            output += RopeDartManager.Instance.IsLastCastEast ? "_East" : "_West";

            // TODO: temp add "_Loop", eventually replace with "_Start" clip
            output += "_Loop";
        }
        else
        {
            output += "Decay";
            output += RopeDartManager.Instance.IsClockwise ? "_CW" : "_CCW";
        }

        return output;
    }
}
