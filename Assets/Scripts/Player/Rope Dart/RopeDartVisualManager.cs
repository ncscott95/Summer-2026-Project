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
        Debug.Log($"Setting buffered binding to {binding}, current animation: {_currentAnimation}.");
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
        BindingGraphConnection connection;

        // trying to bind to Dragon from Spin triggers at 0.75 normalized time
        // if input happened between 0.75 and 1.0, continue spinning but keep the buffered binding
        // TODO: this might cause issues if twine down happens at a weird time, like a retrieval
        if (_bufferedBinding == "Twine Down")
        {
            connection = BindingStack.Instance.TryPushBinding("(Nothing)");

            _playerAnimator.SetTrigger(connection.Animation);

            _currentAnimation = connection.Animation;
            _bufferedBinding = "Twine Down";

            StartCoroutine(AwaitAnimationEndCoroutine(previousState));
        }
        else
        {
            connection = BindingStack.Instance.TryPushBinding(_bufferedBinding);
            if (connection == null) connection = BindingStack.Instance.TryPushBinding("(Nothing)");

            _playerAnimator.SetTrigger(connection.Animation);

            _currentAnimation = connection.Animation;
            _bufferedBinding = "(Nothing)";

            StartCoroutine(AwaitAnimationEndCoroutine(previousState));
        }
    }

    private IEnumerator AwaitAnimationEndCoroutine(AnimatorStateInfo previousState)
    {
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

        while (_playerAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime < 0.75f)
        {
            yield return null;
        }

        // allow transition to Dragon state at 0.75 normalized time
        if (_bufferedBinding == "Twine Down")
        {
            HandleDragonTwine();
            yield break;
        }

        while (_playerAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f)
        {
            yield return null;
        }

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

        PushBufferedBinding();
    }

    private void HandleDragonTwine()
    {
        AnimatorStateInfo previousState = _playerAnimator.GetCurrentAnimatorStateInfo(0);

        BindingGraphConnection connection = BindingStack.Instance.TryPushBinding(_bufferedBinding);
        if (connection == null)
        {
            _bufferedBinding = "(Nothing)";
            return;
        }

        _playerAnimator.SetTrigger(connection.Animation);

        _currentAnimation = connection.Animation;
        _bufferedBinding = "(Nothing)";
        StartCoroutine(AwaitAnimationEndCoroutine(previousState));
    }
}
