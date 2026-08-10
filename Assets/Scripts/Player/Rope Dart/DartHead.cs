using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class DartHead : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            Debug.Log($"DartHead collided with {other.name}");
            // Handle collision with enemy
        }
    }
}
