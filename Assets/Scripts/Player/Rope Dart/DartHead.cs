using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class DartHead : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"DartHead collided with {other.name}");
        if (other.CompareTag("Target"))
        {
            Debug.Log($"DartHead collided with {other.name}");
        }
    }
}
