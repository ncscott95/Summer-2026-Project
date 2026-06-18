using UnityEngine;

[CreateAssetMenu(fileName = "RopeDartData", menuName = "ScriptableObjects/RopeDartData")]
public class RopeDartData : ScriptableObject
{

    [Header("Basic Settings")]
    public float SpinLength;
    public float MaxLength;

    [Header("Gravity Settings")]
    public float Gravity;

    [Header("Spin Settings")]
    public float SpinAcceleration;
    public float SpinLinearSpeed;
    public float SpinDeceleration;

    [Header("Retrieval Settings")]
    public float RetrievalSpeed;
    public float RetrievalAcceleration;
    public float RetrievalFinishThreshold;
}
