using UnityEngine;

public class RopeDartVisualManager : Singleton<RopeDartVisualManager>
{
    [Header("Object References")]
    [SerializeField] private Transform head;
    [SerializeField] private Transform startOrigin;
    [SerializeField] private Transform flag;

    public Vector3 CurrentDirection { get; private set; } = Vector3.zero;
    public float CurrentSpeed { get; private set; } = 0f;
    private Vector3 currentVelocity = Vector3.zero;
    public Transform CurrentOrigin { get; private set; }
    public float CurrentRadius { get; private set; } = 0f;
    private Vector3 retrieveTarget => CurrentOrigin.position - Vector3.up * CurrentRadius;

    public void Initialize()
    {
        CurrentOrigin = startOrigin;
        CurrentRadius = RopeDartManagerNew.Instance.Data.SpinLength;
    }

    void Update()
    {
        // apply acceleration based on current state
        if (RopeDartManagerNew.Instance.CurrentState == RopeDartState.Spinning)
        {
            ApplySpinMotion(true);
        }
        else if (RopeDartManagerNew.Instance.CurrentState == RopeDartState.Stalling)
        {
            ApplySpinMotion(false);
        }
        else
        {
            if (RopeDartManagerNew.Instance.CurrentState == RopeDartState.Casting) currentVelocity += CalculateGravityAcceleration();
            head.position += currentVelocity * Time.deltaTime;
        }

        // check for state transitions
        if (IsMaxLengthExceeded())
        {
            if (RopeDartManagerNew.Instance.CurrentState == RopeDartState.Casting) RopeDartManagerNew.Instance.OnEndCast();
            else RopeDartManagerNew.Instance.OnMaxLength();
        }

        if (RopeDartManagerNew.Instance.CurrentState == RopeDartState.Retrieving && IsRetrievalFinished())
        {
            RopeDartManagerNew.Instance.OnEndRetrieve();
        }

        // point head and flag in direction of travel
        CurrentDirection = currentVelocity.normalized;
        if (CurrentDirection == Vector3.zero) CurrentDirection = Vector3.up;
        LookAtDir2D(head, CurrentDirection);
        LookAtDir2D(flag, CurrentDirection);
    }

    private void ApplySpinMotion(bool isPowered)
    {
        if (isPowered && CurrentSpeed < RopeDartManagerNew.Instance.Data.SpinLinearSpeed)
        {
            CurrentSpeed += RopeDartManagerNew.Instance.Data.SpinAcceleration * Time.deltaTime;
            if (CurrentSpeed > RopeDartManagerNew.Instance.Data.SpinLinearSpeed) CurrentSpeed = RopeDartManagerNew.Instance.Data.SpinLinearSpeed;
        }
        else if (!isPowered && CurrentSpeed > 0)
        {
            CurrentSpeed -= RopeDartManagerNew.Instance.Data.SpinDeceleration * Time.deltaTime;
            if (CurrentSpeed <= 0) CurrentSpeed = 0f;
        }

        float angularSpeed = LinearToAngularSpeed(CurrentSpeed, CurrentRadius);
        head.RotateAround(CurrentOrigin.position, Vector3.forward, (RopeDartManagerNew.Instance.IsClockwise ? -1 : 1) * angularSpeed * Time.deltaTime);
        LookAtPoint2D(head, CurrentOrigin.position);
        CurrentDirection = RopeDartManagerNew.Instance.IsClockwise ? -head.right : head.right;
        currentVelocity = CurrentDirection * CurrentSpeed;

        if (CurrentSpeed <= 0)
        {
            currentVelocity = Vector3.zero;
            RopeDartManagerNew.Instance.Reset();
        }
    }

    public void OnRetrieve()
    {
        Vector3 toTarget = (retrieveTarget - head.position).normalized;
        currentVelocity = toTarget * RopeDartManagerNew.Instance.Data.RetrievalSpeed;
        CurrentSpeed = RopeDartManagerNew.Instance.Data.RetrievalSpeed;
    }

    public void OnReset()
    {
        head.position = retrieveTarget;
        CurrentDirection = Vector3.up;
        LookAtDir2D(head, Vector3.down);
        CurrentSpeed = 0f;
        currentVelocity = Vector3.zero;
        CurrentRadius = RopeDartManagerNew.Instance.Data.SpinLength;
    }

    public void OnCollideWithGround()
    {
        CurrentDirection = Vector3.up;
        CurrentSpeed = 0f;
        currentVelocity = Vector3.zero;
    }

    public void ResetOriginAndRadius()
    {
        SetOrigin(startOrigin);
        SetRadius(RopeDartManagerNew.Instance.Data.SpinLength);
    }

    public void SetOrigin(Transform origin)
    {
        CurrentOrigin = origin;
    }

    public void SetRadius(float radius)
    {
        if (radius < 0) radius = 0;
        CurrentRadius = radius;
    }

    public void SetVelocity(Vector2 velocity)
    {
        currentVelocity = velocity;
    }

    private Vector3 CalculateGravityAcceleration()
    {
        if (RopeDartManagerNew.Instance.CurrentState == RopeDartState.Idle
                || RopeDartManagerNew.Instance.CurrentState == RopeDartState.Spinning
                || RopeDartManagerNew.Instance.CurrentState == RopeDartState.Retrieving)
            return Vector3.zero;

        return RopeDartManagerNew.Instance.Data.Gravity * Time.deltaTime * Vector3.down;
    }

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
        return Vector3.Distance(head.position, CurrentOrigin.position) >= RopeDartManagerNew.Instance.Data.MaxLength;
    }

    private bool IsRetrievalFinished()
    {
        return Vector3.Distance(head.position, retrieveTarget) <= RopeDartManagerNew.Instance.Data.RetrievalFinishThreshold;
    }

    public Vector3 GetHeadPosition()
    {
        return head.position;
    }
}
