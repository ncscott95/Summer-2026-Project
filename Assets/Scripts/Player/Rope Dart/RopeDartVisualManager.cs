using UnityEngine;

public class RopeDartVisualManager : MonoBehaviour
{
    [SerializeField] private Animator _playerAnimator;
    [SerializeField] private Animator _ropeDartAnimator;

    public void UpdateVisuals(BindingGraphConnection bindingConnection)
    {
        _playerAnimator.SetTrigger(bindingConnection.Animation);
    }

    public void SetDarkTrigger()
    {
        _playerAnimator.SetTrigger("IsDark");
    }
}
