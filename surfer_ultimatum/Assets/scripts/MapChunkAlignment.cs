using UnityEngine;

public class MapChunkAlignment : MonoBehaviour
{
    [Header("Chunk Alignment Settings")]
    public float chunkLength = 50f;
    public Vector3 startConnectionPoint = Vector3.zero;
    public Vector3 endConnectionPoint = Vector3.forward * 50f;

    [Header("Gizmo Settings")]
    public bool alwaysShowGizmos = true;
    public float gizmoSize = 1f;

    void OnDrawGizmos()
    {
        if (alwaysShowGizmos)
        {
            DrawConnectionGizmos();
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!alwaysShowGizmos)
        {
            DrawConnectionGizmos();
        }
    }

    void DrawConnectionGizmos()
    {
        // Visualize connection points
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.TransformPoint(startConnectionPoint), gizmoSize);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.TransformPoint(endConnectionPoint), gizmoSize);

        // Draw line showing chunk direction
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(
            transform.TransformPoint(startConnectionPoint),
            transform.TransformPoint(endConnectionPoint)
        );

#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.TransformPoint(startConnectionPoint) + Vector3.up * gizmoSize, "START");
        UnityEditor.Handles.Label(transform.TransformPoint(endConnectionPoint) + Vector3.up * gizmoSize, "END");
#endif
    }

    public Vector3 GetWorldStartPoint()
    {
        return transform.TransformPoint(startConnectionPoint);
    }

    public Vector3 GetWorldEndPoint()
    {
        return transform.TransformPoint(endConnectionPoint);
    }
}