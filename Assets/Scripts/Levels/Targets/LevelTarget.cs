using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class LevelTarget : MonoBehaviour
{
    protected LevelTargetItem _targetItem;

    public virtual void Initialize(LevelTargetItem targetItem)
    {
        _targetItem = targetItem;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("DartHead"))
        {
            OnTargetHit();
        }
    }

    public virtual void OnTargetHit()
    {
        // TODO: add logic for when the target is hit
        LevelManager.Instance.OnTargetHit(_targetItem);
        Destroy(gameObject);
    }
}
