using System.Collections.Generic;
using UnityEngine;

public class RopeDartManager : Singleton<RopeDartManager>
{

    [Header("Object References")]
    [SerializeField] private Transform head;
    [SerializeField] private Transform startOrigin;
    [SerializeField] private Transform flag;

    [Header("Current Rope Data")]
    [SerializeField] private RopeDartData data;

    private float currentSpeed = 0f;
    private Vector3 currentVelocity = Vector3.zero;
    private Vector3 currentDirection = Vector3.zero;
    private bool isClockwise = true;
    public RopeDartState CurrentState { get; private set; } = RopeDartState.Idle;
    private Transform currentOrigin;
    private float currentRadius = 0f;

    private Vector3 retrieveTarget => currentOrigin.position - Vector3.up * currentRadius;
    private bool isTryingToSpin = false;
    private bool isBinding = false;

    void Start()
    {
        currentOrigin = startOrigin;
        currentRadius = data.SpinLength;
        Reset();
        ResetRopeToHands();
    }

    void Update()
    {
        // apply acceleration based on current state
        if (CurrentState == RopeDartState.Spinning)
        {
            ApplySpinMotion(true);
        }
        else if (CurrentState == RopeDartState.Stalling)
        {
            ApplySpinMotion(false);
        }
        else
        {
            if (CurrentState == RopeDartState.Casting) currentVelocity += CalculateGravityAcceleration();
            head.position += currentVelocity * Time.deltaTime;
        }

        // check for state transitions
        if (IsMaxLengthExceeded())
        {
            if (CurrentState == RopeDartState.Casting) OnEndCast();
            else OnMaxLength();
        }

        if (CurrentState == RopeDartState.Retrieving && IsRetrievalFinished())
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
        head.RotateAround(currentOrigin.position, Vector3.forward, (isClockwise ? -1 : 1) * angularSpeed * Time.deltaTime);
        LookAtPoint2D(head, currentOrigin.position);
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
        if (CurrentState == RopeDartState.Idle || CurrentState == RopeDartState.Spinning || CurrentState == RopeDartState.Retrieving)
            return Vector3.zero;

        return data.Gravity * Time.deltaTime * Vector3.down;
    }

    // State Control Methods

    public void StartSpin()
    {
        if (CurrentState != RopeDartState.Idle && CurrentState != RopeDartState.Retrieving && CurrentState != RopeDartState.Stalling)
            return;

        if (CurrentState == RopeDartState.Idle)
        {
            // default to a wheel plane down spin
            isClockwise = true;
        }
        else if (CurrentState == RopeDartState.Stalling)
        {
            // flip spin direction when starting a new spin during stall
            isClockwise = !isClockwise;
        }

        BindingStack.Instance.TryPushWrappedBinding("Spin " + (isClockwise ? "Down" : "Up"));

        CurrentState = RopeDartState.Spinning;
    }

    public void StopSpin()
    {
        if (CurrentState != RopeDartState.Spinning)
            return;
        
        BindingStack.Instance.PopWrappedBinding();

        CurrentState = RopeDartState.Stalling;
    }

    public void Cast()
    {
        if (CurrentState != RopeDartState.Spinning)
            return;
        
        if (!isBinding)
        {
            // if player is not binding, unravel all twined bind points and reset the origin to the start origin
            // TODO: if the player is holding a wrap, only reset to the last wrapped bind point instead of the start origin
            ResetRopeToHands();
            currentOrigin = startOrigin;
        }

        CurrentState = RopeDartState.Casting;
    }

    private void OnEndCast()
    {
        OnMaxLength();
    }

    public void TwineSimple()
    {
        // TODO: pick an elbow depending on which side the player is spinning on (lead/anchor)
        Twine("Bind Lead");
    }

    public void Twine(string bindingInput)
    {
        if (CurrentState != RopeDartState.Spinning)
            return;

        List<BindPointObject> points = BindingStack.Instance.TryPushWrappedBinding(bindingInput);
        if (points == null || points.Count == 0)
            return;

        BindPointObject point = points[points.Count - 1];
        Debug.Log($"Twining to {point.ID} at position {point.Position}");

        currentRadius = currentRadius - Vector3.Distance(point.Position, currentOrigin.position);
        if (currentRadius <= 0) currentRadius = 0;
        currentOrigin = point.transform;
        RopeRenderer.Instance.AddPointsBeforeHead(points);

        if (currentRadius == 0) Reset();
    }

    public void ShiftPlane(Vector2 direction)
    {
        // TODO
    }

    private void OnMaxLength()
    {
        currentVelocity = Vector3.zero;
        CurrentState = RopeDartState.Extended;
    }

    public void Retrieve()
    {
        if (CurrentState != RopeDartState.Extended && CurrentState != RopeDartState.Casting)
            return;

        CurrentState = RopeDartState.Retrieving;

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
        // TODO: maybe temp for testing twining
        // currentOrigin = startOrigin;
        head.position = retrieveTarget;
        currentDirection = Vector3.up;
        LookAtDir2D(head, Vector3.down);
        currentSpeed = 0f;
        currentVelocity = Vector3.zero;
        currentRadius = data.SpinLength;
        CurrentState = RopeDartState.Idle;
    }

    public void FailCombo()
    {
        Reset();
    }

    public void CollideWithGround()
    {
        currentDirection = Vector3.up;
        currentSpeed = 0f;
        currentVelocity = Vector3.zero;
        CurrentState = RopeDartState.Extended;
    }

    public void ToggleTryingToSpin(bool value)
    {
        isTryingToSpin = value;
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

    private void LookAtPoint2D(Transform from, Vector3 to)
    {
        LookAtDir2D(from, to - from.position);
    }

    private void LookAtDir2D(Transform from, Vector3 direction)
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        from.eulerAngles = new Vector3(0, 0, angle);
    }

    private bool IsMaxLengthExceeded()
    {
        return Vector3.Distance(head.position, currentOrigin.position) >= data.MaxLength;
    }

    private bool IsRetrievalFinished()
    {
        return Vector3.Distance(head.position, retrieveTarget) <= data.RetrievalFinishThreshold;
    }

    private void ResetRopeToHands()
    {
        RopeRenderer.Instance.Reset();
        // BindPointObject point = BindPointStack.Instance.TryPushWrappedPoint(BindPointID.AnchorHand);
        // RopeRenderer.Instance.AddPointBeforeHead(point);
        // point = BindPointStack.Instance.TryPushWrappedPoint(BindPointID.LeadHand);
        // RopeRenderer.Instance.AddPointBeforeHead(point);

        BindingStack.Instance.ClearWrappedBindings();
        List<BindPointObject> points = BindingStack.Instance.TryPushWrappedBinding("Idle");
        RopeRenderer.Instance.AddPointsBeforeHead(points);
    }

    public Vector3 GetHeadPosition()
    {
        return head.position;
    }
}

public enum RopeDartState
{
    Idle,
    Spinning,
    Stalling,
    Casting,
    Extended,
    Retrieving
}