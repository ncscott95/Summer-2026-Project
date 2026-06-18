using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RopeDartController : Singleton<RopeDartController>
{
    private enum RopeDartState
    {
        Idle,
        Spinning,
        Stalling,
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

    private float currentSpeed = 0f;
    private Vector3 currentVelocity = Vector3.zero;
    private Vector3 currentDirection = Vector3.zero;
    private bool isClockwise = true;
    private RopeDartState currentState = RopeDartState.Idle;
    private float currentRadius = 0f;
    private Stack<BindPoint> wrappedPoints = new Stack<BindPoint>();

    private Vector3 retrieveTarget;
    private bool isTryingToSpin = false;

    void Start()
    {
        currentRadius = data.SpinLength;
        retrieveTarget = origin.position - Vector3.up * currentRadius;
        Reset();
    }

    void Update()
    {
        // apply acceleration based on current state
        if (currentState == RopeDartState.Spinning)
        {
            ApplySpinMotion(true);
        }
        else if (currentState == RopeDartState.Stalling)
        {
            ApplySpinMotion(false);
        }
        else
        {
            if (currentState == RopeDartState.Casting) currentVelocity += CalculateGravityAcceleration();
            head.position += currentVelocity * Time.deltaTime;
        }

        // check for state transitions
        if (IsMaxLengthExceeded())
        {
            if (currentState == RopeDartState.Casting) OnEndCast();
            else OnMaxLength();
        }

        if (currentState == RopeDartState.Retrieving && IsRetrievalFinished())
        {
            OnEndRetrieve();
        }

        // point head and flag in direction of travel
        currentDirection = currentVelocity.normalized;
        if (currentDirection == Vector3.zero) currentDirection = Vector3.up;
        LookAtDir2D(head, currentDirection);
        LookAtDir2D(flag, currentDirection);
    }

    private void ApplySpinMotion(bool isPowered)
    {
        if (isPowered && currentSpeed < data.SpinLinearSpeed)
        {
            currentSpeed += data.SpinAcceleration * Time.deltaTime;
            if (currentSpeed > data.SpinLinearSpeed) currentSpeed = data.SpinLinearSpeed;
        }
        else if (!isPowered && currentSpeed > 0)
        {
            currentSpeed -= data.SpinDeceleration * Time.deltaTime;
            if (currentSpeed <= 0) currentSpeed = 0f;
        }

        float angularSpeed = LinearToAngularSpeed(currentSpeed, currentRadius);
        head.RotateAround(origin.position, Vector3.forward, (isClockwise ? -1 : 1) * angularSpeed * Time.deltaTime);
        LookAtPoint2D(head, origin);
        currentDirection = isClockwise ? -head.right : head.right;
        currentVelocity = currentDirection * currentSpeed;

        if (currentSpeed <= 0)
        {
            currentVelocity = Vector3.zero;
            Reset();
        }
    }

    private Vector3 CalculateGravityAcceleration()
    {
        if (currentState == RopeDartState.Idle || currentState == RopeDartState.Spinning || currentState == RopeDartState.Retrieving)
            return Vector3.zero;

        return data.Gravity * Time.deltaTime * Vector3.down;
    }

    // Input Disambiguation Methods

    public void HandleSpinRetrieveInput()
    {
        isTryingToSpin = true;

        if (currentState == RopeDartState.Idle || currentState == RopeDartState.Stalling) StartSpin();
        else if (currentState == RopeDartState.Extended || currentState == RopeDartState.Casting) Retrieve();
    }

    public void HandleSpinInputEnd()
    {
        isTryingToSpin = false;

        if (currentState == RopeDartState.Spinning) StopSpin();
    }

    // State Control Methods

    public void StartSpin()
    {
        if (currentState != RopeDartState.Idle && currentState != RopeDartState.Retrieving && currentState != RopeDartState.Stalling)
            return;

        if (currentState == RopeDartState.Idle)
        {
            // default to a wheel plane down spin
            isClockwise = true;
        }
        else if (currentState == RopeDartState.Stalling)
        {
            // flip spin direction when starting a new spin during stall
            isClockwise = !isClockwise;
        }

        // wrappedPoints.Push(null);

        currentState = RopeDartState.Spinning;
    }

    public void StopSpin()
    {
        if (currentState != RopeDartState.Spinning)
            return;

        currentState = RopeDartState.Stalling;
    }

    public void Cast()
    {
        if (currentState != RopeDartState.Spinning)
            return;

        currentState = RopeDartState.Casting;
    }

    private void OnEndCast()
    {
        OnMaxLength();
    }

    private void OnMaxLength()
    {
        currentVelocity = Vector3.zero;
        currentState = RopeDartState.Extended;
    }

    public void Retrieve()
    {
        if (currentState != RopeDartState.Extended && currentState != RopeDartState.Casting)
            return;

        currentState = RopeDartState.Retrieving;

        Vector3 toTarget = (retrieveTarget - head.position).normalized;
        currentVelocity = toTarget * data.RetrievalSpeed;
        currentSpeed = data.RetrievalSpeed;
    }

    private void OnEndRetrieve()
    {
        isClockwise = currentDirection.x < 0;

        if (isTryingToSpin) StartSpin();
        else Reset();
    }

    private void Reset()
    {
        head.localPosition = retrieveTarget - origin.position;
        currentDirection = Vector3.up;
        LookAtDir2D(head, Vector3.down);
        currentSpeed = 0f;
        currentVelocity = Vector3.zero;
        currentRadius = data.SpinLength;
        currentState = RopeDartState.Idle;
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

    private bool IsMaxLengthExceeded()
    {
        return Vector3.Distance(head.position, origin.position) >= data.MaxLength;
    }

    private bool IsRetrievalFinished()
    {
        return Vector3.Distance(head.position, retrieveTarget) <= data.RetrievalFinishThreshold;
    }
}
