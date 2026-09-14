using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class LevelTarget : MonoBehaviour
{
    private const float SineAmplitude = 0.1f;
    private const float SineFrequency = 1f;

    protected LevelTargetItem _targetItem;

    private float _initialY;

    public virtual void Initialize(LevelTargetItem targetItem)
    {
        _targetItem = targetItem;
        _initialY = transform.position.y;
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

    void Update()
    {
        // animate y position on a sine wave
        float sineY = Mathf.Sin(Time.time * SineFrequency) * SineAmplitude;
        transform.position = new Vector3(transform.position.x, _initialY + sineY, transform.position.z);
    }
}
