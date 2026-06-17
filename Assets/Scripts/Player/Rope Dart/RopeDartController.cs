using UnityEngine;
using System.Collections;

public class RopeDartController : Singleton<RopeDartController>
{
    private enum RopeDartState
    {
        Idle,
        Spinning,
        Casting,
        Extended,
        Retrieving
    }

    [Header("Object References")]
    [SerializeField] private Transform head;
    [SerializeField] private Transform origin;
    [SerializeField] private Transform flag;

    [Header("Basic Settings")]
    [SerializeField] private float spinLength;
    [SerializeField] private float maxLength;

    [Header("Spin Settings")]
    [SerializeField] private float baseSpinSpeed;
    [SerializeField] private float spinAcceleration;

    [Header("Retrieval Settings")]
    [SerializeField] private float retrievalSpeed;
    [SerializeField] private float retrievalAcceleration;
    [SerializeField] private float retrievalFinishThreshold;

    private float currentVelocity = 0f;
    private Vector3 currentDirection = Vector3.zero;
    private float currentRadius = 0f;
    private bool isClockwise = false;
    private RopeDartState currentState = RopeDartState.Idle;
    private Coroutine currentCoroutine;

    void Start()
    {
        Idle();
    }

    void Update()
    {
        // point flag in direction of travel
        LookAtDir2D(flag, currentDirection);
    }

    // State Control Methods

    public void StartSpin()
    {
        if (currentState != RopeDartState.Idle)
        {
            return;
        }

        currentCoroutine = StartCoroutine(SpinCoroutine());
        currentState = RopeDartState.Spinning;
    }

    public void StopSpin()
    {
        if (currentState != RopeDartState.Spinning)
        {
            return;
        }

        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
            currentCoroutine = null;
        }

        Idle();
    }

    public void Cast()
    {
        if (currentState != RopeDartState.Spinning)
        {
            return;
        }

        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
            currentCoroutine = null;
        }

        currentState = RopeDartState.Casting;
        currentCoroutine = StartCoroutine(CastCoroutine());
    }

    public void Retrieve()
    {
        if (currentState != RopeDartState.Extended
                && currentState != RopeDartState.Casting)
        {
            return;
        }

        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
            currentCoroutine = null;
        }

        currentState = RopeDartState.Retrieving;
        currentCoroutine = StartCoroutine(RetrieveCoroutine());
    }

    private void Idle()
    {
        head.localPosition = new Vector3(0, -spinLength, 0);
        currentDirection = Vector3.up;
        LookAtDir2D(head, Vector3.down);
        currentVelocity = 0f;
        currentRadius = spinLength;
        currentState = RopeDartState.Idle;
    }

    // Coroutines

    private IEnumerator SpinCoroutine()
    {
        while (true)
        {
            if (currentVelocity < baseSpinSpeed)
            {
                currentVelocity += spinAcceleration * Time.deltaTime;
                if (currentVelocity > baseSpinSpeed) currentVelocity = baseSpinSpeed;
            }

            head.RotateAround(origin.position, Vector3.forward, currentVelocity * Time.deltaTime);
            LookAtPoint2D(head, origin);
            currentDirection = isClockwise ? -head.right : head.right;
            yield return null;
        }
    }

    private IEnumerator CastCoroutine()
    {
        currentVelocity = SpinToCastVelocity(currentVelocity, spinLength);

        while (Vector3.Distance(head.position, origin.position) < maxLength)
        {
            head.position += currentDirection * currentVelocity * Time.deltaTime;
            LookAtDir2D(head, currentDirection);
            yield return null;
        }

        currentState = RopeDartState.Extended;
        currentCoroutine = null;
    }

    private IEnumerator RetrieveCoroutine()
    {
        currentDirection = (origin.position - head.position).normalized;
        // currentVelocity = 0f;

        while (Vector3.Distance(head.position, origin.position) > retrievalFinishThreshold)
        {
            if (currentVelocity < retrievalSpeed)
            {
                currentVelocity += retrievalAcceleration * Time.deltaTime;
                if (currentVelocity > retrievalSpeed) currentVelocity = retrievalSpeed;
            }

            head.position += currentDirection * currentVelocity * Time.deltaTime;
            LookAtDir2D(head, currentDirection);
            yield return null;
        }

        Idle();
        currentCoroutine = null;
    }

    // Helper Methods

    private float SpinToCastVelocity(float spinVelocity, float radius)
    {
        return Mathf.Deg2Rad * spinVelocity * radius;
    }

    private void LookAtPoint2D(Transform from, Transform to)
    {
        LookAtDir2D(from, to.position - from.position);
    }

    private void LookAtDir2D(Transform from, Vector3 direction)
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        from.eulerAngles = new Vector3(0, 0, angle);
    }
}
