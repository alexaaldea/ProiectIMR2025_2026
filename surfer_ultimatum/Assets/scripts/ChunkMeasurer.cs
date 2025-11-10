using UnityEngine;

public class ChunkMeasurer : MonoBehaviour
{
    void Start()
    {
        // Get all child objects
        Renderer[] renderers = GetComponentsInChildren<Renderer>();

        if (renderers.Length > 0)
        {
            Bounds totalBounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                totalBounds.Encapsulate(renderers[i].bounds);
            }

            Debug.Log($"Chunk Size: {totalBounds.size}");
            Debug.Log($"Chunk Length (Z): {totalBounds.size.z}");
            Debug.Log($"Start: {totalBounds.min}, End: {totalBounds.max}");
        }
    }

    void OnDrawGizmosSelected()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();

        if (renderers.Length > 0)
        {
            Bounds totalBounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                totalBounds.Encapsulate(renderers[i].bounds);
            }

            // Draw the bounds in Scene view
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(totalBounds.center, totalBounds.size);

            // Draw start and end points
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(totalBounds.min, 0.2f);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(totalBounds.max, 0.2f);

#if UNITY_EDITOR
            UnityEditor.Handles.Label(totalBounds.min, "START");
            UnityEditor.Handles.Label(totalBounds.max, "END");
#endif
        }
    }
}