using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class DartHead : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            RopeDartManager.Instance.CollideWithGround();
        }
    }
}
