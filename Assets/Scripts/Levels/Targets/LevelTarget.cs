using UnityEngine;

public class LevelTarget : MonoBehaviour
{
    protected LevelTargetItem _targetItem;

    public virtual void Initialize(LevelTargetItem targetItem)
    {
        _targetItem = targetItem;
    }

    public virtual void OnTargetHit()
    {
        // TODO: add logic for when the target is hit
        LevelManager.Instance.OnTargetHit(_targetItem);
        Destroy(gameObject);
    }
}
