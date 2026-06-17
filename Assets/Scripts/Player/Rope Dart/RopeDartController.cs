using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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

    [Header("Current Rope Data")]
    [SerializeField] private RopeDartData data;

    private float currentVelocity = 0f;
    private Vector3 currentDirection = Vector3.zero;
    private float currentRadius = 0f;
    private bool isClockwise = true;
    private RopeDartState currentState = RopeDartState.Idle;
    private Coroutine currentCoroutine;
    private Stack<BindPoint> wrappedPoints = new Stack<BindPoint>();

    private Vector3 retrieveTarget;
    private bool isTryingToSpin = false;

    void Start()
    {
        retrieveTarget = origin.position - Vector3.up * data.SpinLength;
        Idle();
    }

    void Update()
    {
        // point flag in direction of travel
        LookAtDir2D(flag, currentDirection);
    }

    // Input Disambiguation Methods

    public void HandleSpinRetrieveInput()
    {
        isTryingToSpin = true;

        if (currentState == RopeDartState.Idle)
        {
            StartSpin();
        }
        else if (currentState == RopeDartState.Extended || currentState == RopeDartState.Casting)
        {
            Retrieve();
        }
    }

    public void HandleSpinInputEnd()
    {
        isTryingToSpin = false;

        if (currentState == RopeDartState.Spinning)
        {
            StopSpin();
        }
    }

    // public void HandleCastRetrieveInput()
    // {
    //     if (currentState == RopeDartState.Spinning)
    //     {
    //         Cast();
    //     }
    //     else if (currentState == RopeDartState.Extended || currentState == RopeDartState.Casting)
    //     {
    //         Retrieve();
    //     }
    // }

    // State Control Methods

    public void StartSpin()
    {
        if (currentState != RopeDartState.Idle
                && currentState != RopeDartState.Retrieving)
        {
            return;
        }

        if (currentState == RopeDartState.Idle)
        {
            // default to a wheel plane down spin
            isClockwise = true;
        }

        // wrappedPoints.Push(null);

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

    private void OnEndRetrieve()
    {
        if (currentDirection.x < 0)
        {
            isClockwise = true;
        }
        else
        {
            isClockwise = false;
        }

        if (isTryingToSpin)
        {
            StartSpin();
        }
        else
        {
            Idle();
        }
    }

    private void Idle()
    {
        head.localPosition = retrieveTarget - origin.position;
        currentDirection = Vector3.up;
        LookAtDir2D(head, Vector3.down);
        currentVelocity = 0f;
        currentRadius = data.SpinLength;
        currentState = RopeDartState.Idle;
    }

    // Coroutines

    private IEnumerator SpinCoroutine()
    {
        currentVelocity = LinearToAngularSpeed(currentVelocity, data.SpinLength);

        while (true)
        {
            if (currentVelocity < data.BaseSpinSpeed)
            {
                currentVelocity += data.SpinAcceleration * Time.deltaTime;
                if (currentVelocity > data.BaseSpinSpeed) currentVelocity = data.BaseSpinSpeed;
            }

            head.RotateAround(origin.position, Vector3.forward, currentVelocity * Time.deltaTime * (isClockwise ? -1 : 1));
            LookAtPoint2D(head, origin);
            currentDirection = isClockwise ? -head.right : head.right;
            yield return null;
        }
    }

    private IEnumerator CastCoroutine()
    {
        currentVelocity = AngularToLinearSpeed(currentVelocity, data.SpinLength);

        while (Vector3.Distance(head.position, origin.position) < data.MaxLength)
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
        currentDirection = (retrieveTarget - head.position).normalized;
        // currentVelocity = 0f;

        while (Vector3.Distance(head.position, retrieveTarget) > data.RetrievalFinishThreshold)
        {
            if (currentVelocity < data.RetrievalSpeed)
            {
                currentVelocity += data.RetrievalAcceleration * Time.deltaTime;
                if (currentVelocity > data.RetrievalSpeed) currentVelocity = data.RetrievalSpeed;
            }

            head.position += currentDirection * currentVelocity * Time.deltaTime;
            LookAtDir2D(head, currentDirection);
            yield return null;
        }

        currentCoroutine = null;
        OnEndRetrieve();
    }

    // Helper Methods

    private float AngularToLinearSpeed(float angularSpeed, float radius)
    {
        return Mathf.Deg2Rad * angularSpeed * radius;
    }

    private float LinearToAngularSpeed(float linearSpeed, float radius)
    {
        return linearSpeed * Mathf.Rad2Deg / radius;
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
