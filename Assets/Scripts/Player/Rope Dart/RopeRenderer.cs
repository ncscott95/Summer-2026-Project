using UnityEngine;

public class RopeRenderer : Singleton<RopeRenderer>
{
    [SerializeField] private LineRenderer lineRenderer;

    void Start()
    {
        Reset();
    }

    void Update()
    {
        lineRenderer.SetPosition(lineRenderer.positionCount - 1, RopeDartManager.Instance.GetHeadPosition());
    }
    
    public void Reset()
    {
        lineRenderer.positionCount = 1;
        lineRenderer.SetPosition(0, RopeDartManager.Instance.GetHeadPosition());
        lineRenderer.enabled = true;
    }

    public void AddPointBeforeHead(BindPointObject point)
    {
        lineRenderer.positionCount += 1;
        // use count-2 to adjust so that the last point is always the head position
        lineRenderer.SetPosition(lineRenderer.positionCount - 2, point.Position);
    }

    public void RemoveTopNonHeadPoint()
    {
        if (lineRenderer.positionCount > 0)
        {
            lineRenderer.positionCount -= 1;
            lineRenderer.SetPosition(lineRenderer.positionCount - 1, RopeDartManager.Instance.GetHeadPosition());
        }
    }
}
