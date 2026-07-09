using System.Collections.Generic;
using UnityEngine;

public class RopeRenderer : Singleton<RopeRenderer>
{
    [SerializeField] private LineRenderer lineRenderer;

    private List<BindPointObject> currentBindPoints = new List<BindPointObject>();

    void Update()
    {
        for (int i = 0; i < currentBindPoints.Count; i++)
        {
            lineRenderer.SetPosition(i, currentBindPoints[i].Position);
        }

        lineRenderer.SetPosition(lineRenderer.positionCount - 1, RopeDartManager.Instance.GetHeadPosition());
    }

    public void Reset()
    {
        currentBindPoints.Clear();

        lineRenderer.positionCount = 1;
        lineRenderer.SetPosition(0, RopeDartManager.Instance.GetHeadPosition());
        lineRenderer.enabled = true;
    }

    public void AddPointsBeforeHead(List<BindPointObject> points)
    {
        if (points == null || points.Count == 0)
            return;

        foreach (BindPointObject point in points)
        {
            AddPointBeforeHead(point);
        }
    }

    public void AddPointBeforeHead(BindPointObject point)
    {
        if (lineRenderer.positionCount == 0) Reset();

        currentBindPoints.Add(point);

        lineRenderer.positionCount += 1;

        lineRenderer.SetPosition(lineRenderer.positionCount - 1, RopeDartManager.Instance.GetHeadPosition());
        // use count-2 to adjust so that the last point is always the head position
        lineRenderer.SetPosition(lineRenderer.positionCount - 2, point.Position);
    }

    public void RemoveTopNonHeadPoint()
    {
        if (lineRenderer.positionCount > 0)
        {
            currentBindPoints.RemoveAt(currentBindPoints.Count - 1);

            lineRenderer.positionCount -= 1;
            lineRenderer.SetPosition(lineRenderer.positionCount - 1, RopeDartManager.Instance.GetHeadPosition());
        }
    }
}
