using UnityEngine;
using System.Collections.Generic;

public class CircleGestureDetector : MonoBehaviour
{
    [Header("Tracking Input")]
    public Vector3 fingertipPosition;

    [Header("Detection Settings")]
    [SerializeField] private int maxTrailPoints = 60;
    [SerializeField] private float minPointDistance = 0.02f;
    [SerializeField] private float circleThreshold = 0.55f;
    [SerializeField] private float minRadius = 0.1f;
    [SerializeField] private float maxRadius = 1.0f;
    [SerializeField] private float gestureTimeout = 0.4f;

    [Header("VFX Connection")]
    [SerializeField] private PortalVFXController vfxController;

    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = true;

    private List<Vector3> trailPoints = new List<Vector3>();
    private Vector3 lastPosition;
    private float lastMoveTime;
    private bool isDrawing = false;

    public struct CircleData
    {
        public Vector3 center;
        public float radius;
        public float confidence;
    }

    void Start()
    {
        lastPosition = fingertipPosition;
        lastMoveTime = Time.time;
    }

    void Update()
    {
        float moveDist = Vector3.Distance(fingertipPosition, lastPosition);

        if (moveDist > minPointDistance)
        {
            trailPoints.Add(fingertipPosition);

            if (trailPoints.Count > maxTrailPoints)
                trailPoints.RemoveAt(0);

            lastMoveTime = Time.time;
            isDrawing = true;

            lastPosition = fingertipPosition;
        }

        if (isDrawing && Time.time - lastMoveTime > gestureTimeout)
        {
            TryDetectCircle();
            ClearTrail();
            isDrawing = false;
        }
    }

    private void TryDetectCircle()
    {
        if (trailPoints.Count < 20)
        {
            Debug.Log($"❌ Not enough points: {trailPoints.Count}");
            return;
        }

        CircleData circle = FitCircle(trailPoints);

        Debug.Log($"📊 Circle Analysis: Radius={circle.radius:F3}m, Confidence={circle.confidence:F2}");

        if (circle.confidence > circleThreshold &&
            circle.radius >= minRadius &&
            circle.radius <= maxRadius)
        {
            Debug.Log($"✅ CIRCLE DETECTED! Spawning portal...");
            vfxController.SpawnPortal(circle);
        }
        else
        {
            if (circle.confidence <= circleThreshold)
                Debug.Log($"❌ Too irregular (threshold: {circleThreshold:F2})");
            if (circle.radius < minRadius)
                Debug.Log($"❌ Too small (min: {minRadius:F2}m)");
            if (circle.radius > maxRadius)
                Debug.Log($"❌ Too large (max: {maxRadius:F2}m)");
        }
    }

    private CircleData FitCircle(List<Vector3> points)
    {
        CircleData result = new CircleData();

        Vector3 centroid = Vector3.zero;
        foreach (var p in points)
            centroid += p;
        centroid /= points.Count;
        result.center = centroid;

        float radiusSum = 0f;
        foreach (var p in points)
            radiusSum += Vector3.Distance(p, centroid);
        result.radius = radiusSum / points.Count;

        float errorSum = 0f;
        foreach (var p in points)
        {
            float dist = Vector3.Distance(p, centroid);
            float error = Mathf.Abs(dist - result.radius);
            errorSum += error;
        }

        float avgError = errorSum / points.Count;
        float normalizedError = avgError / result.radius;
        result.confidence = Mathf.Clamp01(1.0f - normalizedError);

        return result;
    }

    private void ClearTrail()
    {
        trailPoints.Clear();
    }

    void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;

        if (trailPoints.Count >= 2)
        {
            Gizmos.color = Color.yellow;
            for (int i = 0; i < trailPoints.Count - 1; i++)
                Gizmos.DrawLine(trailPoints[i], trailPoints[i + 1]);
        }

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(fingertipPosition, 0.02f);
    }
}