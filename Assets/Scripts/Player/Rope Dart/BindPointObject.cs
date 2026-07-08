using UnityEngine;

public class BindPointObject : MonoBehaviour
{
    public BindPointID ID;
    public Vector3 Position => transform.position;
}

public enum BindPointID
{
    Root,
    LeadHand,
    LeadShoulder,
    LeadArmpit,
    LeadElbow,
    LeadKnee,
    LeadFoot,
    AnchorHand,
    AnchorShoulder,
    AnchorArmpit,
    AnchorElbow,
    AnchorKnee,
    AnchorFoot,
    Neck
}
