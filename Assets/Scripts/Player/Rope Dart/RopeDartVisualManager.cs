using System.Collections;
using UnityEngine;

public class RopeDartVisualManager : Singleton<RopeDartVisualManager>
{
    [SerializeField] private Animator _playerAnimator;

    private string _bufferedBinding = "(Nothing)";
    private string _currentAnimation = "";

    public void SetDarkTrigger()
    {
        _playerAnimator.SetTrigger("IsDark");
    }

    public void SetBufferedBinding(string binding)
    {
        Debug.Log($"Setting buffered binding to {binding}.");
        _bufferedBinding = binding;
    }

    public void StartAnimation(string animation)
    {
        AnimatorStateInfo previousState = _playerAnimator.GetCurrentAnimatorStateInfo(0);
        _playerAnimator.SetTrigger(animation);
        _currentAnimation = animation;
        _bufferedBinding = "(Nothing)";
        StartCoroutine(AwaitAnimationEndCoroutine(previousState));
    }

    private void PushBufferedBinding()
    {
        AnimatorStateInfo previousState = _playerAnimator.GetCurrentAnimatorStateInfo(0);

        BindingGraphConnection connection = BindingStack.Instance.TryPushBinding(_bufferedBinding);
        if (connection == null) connection = BindingStack.Instance.TryPushBinding("(Nothing)");

        Debug.Log($"Setting trigger {connection.Animation}.");
        _playerAnimator.SetTrigger(connection.Animation);

        _currentAnimation = connection.Animation;
        _bufferedBinding = "(Nothing)";
        StartCoroutine(AwaitAnimationEndCoroutine(previousState));
    }

    private IEnumerator AwaitAnimationEndCoroutine(AnimatorStateInfo previousState)
    {
        Debug.Log($"Awaiting animation {_currentAnimation} to end...");

        // Wait until the trigger starts a transition. The source state may already be at normalized time 1.
        yield return null;

        // prevent short-circuting the coroutine if the animator is still in the old animation state
        while (!_playerAnimator.IsInTransition(0) &&
            _playerAnimator.GetCurrentAnimatorStateInfo(0).fullPathHash == previousState.fullPathHash &&
            _playerAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime >= previousState.normalizedTime)
        {
            yield return null;
        }

        // prevent short-circuiting the coroutine if the animator is still in transition to the new animation state
        while (_playerAnimator.IsInTransition(0))
        {
            yield return null;
        }

        while (_playerAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f)
        {
            yield return null;
        }

        // Debug.Log($"Animation {_currentAnimation} ended.");

        OnAnimationEnd();
    }

    private void OnAnimationEnd()
    {
        switch (_currentAnimation)
        {
            case "Spin":
                ScoringSystem.Instance.DoMultiplierDecay();
                break;
            case "Cast":
                break;
            case "Elbow":
                break;
            case "Dragon":
                break;
            case "Necklace":
                break;
            case "Retrieve":
                break;
            default:
                break;
        }

        Debug.Log($"Post-{_currentAnimation} execution ended. Pushing buffered binding: {_bufferedBinding}");
        PushBufferedBinding();
    }
}
