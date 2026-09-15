using UnityEngine;

public class RopeDartVisualManager : MonoBehaviour
{
    [SerializeField] private Animator _playerAnimator;

    public void UpdateVisuals(BindingGraphConnection bindingConnection)
    {
        _playerAnimator.SetTrigger(bindingConnection.Animation);
    }

    public void SetDarkTrigger()
    {
        _playerAnimator.SetTrigger("IsDark");
    }
}
