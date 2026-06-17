using UnityEngine;

[CreateAssetMenu(fileName = "RopeDartData", menuName = "ScriptableObjects/RopeDartData")]
public class RopeDartData : ScriptableObject
{

    [Header("Basic Settings")]
    public float SpinLength;
    public float MaxLength;

    [Header("Spin Settings")]
    public float BaseSpinSpeed;
    public float SpinAcceleration;

    [Header("Retrieval Settings")]
    public float RetrievalSpeed;
    public float RetrievalAcceleration;
    public float RetrievalFinishThreshold;
}
