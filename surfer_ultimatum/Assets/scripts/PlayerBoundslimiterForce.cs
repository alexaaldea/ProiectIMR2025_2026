using UnityEngine;
using System.Linq;

/// <summary>
/// Versiune robustă care:
/// - caută wall-urile cu tag-ul dat,
/// - recalculează limitele dacă walls se schimbă,
/// - aplică clamp FORȚAT în LateUpdate (override poziție) astfel încât niciun alt script
///   care modifică transformul mai devreme să nu te lase să treci de perete.
/// </summary>
[DisallowMultipleComponent]
public class PlayerBoundsLimiterForce : MonoBehaviour
{
    [Tooltip("Tag folosit pe obiectele wall.")]
    public string wallTag = "Wall";
    [Tooltip("Margină internă față de perete.")]
    public float innerMargin = 0.05f;
    [Tooltip("Dacă true afișează Debug.Log când se aplică clamp.")]
    public bool debugLogs = false;

    private float leftLimitX = float.NegativeInfinity;
    private float rightLimitX = float.PositiveInfinity;

    private int lastWallsCount = -1;
    private GameObject[] cachedWalls = new GameObject[0];

    void Start()
    {
        RecalculateLimits();
    }

    // Recalculează limitele (apelează după ce spawn-ezi chunk-uri noi)
    public void RecalculateLimits()
    {
        var walls = GameObject.FindGameObjectsWithTag(wallTag);
        if (walls == null || walls.Length == 0)
        {
            if (debugLogs) Debug.LogWarning("[PlayerBoundsLimiterForce] Nu am găsit walls cu tag='" + wallTag + "'");
            return;
        }

        // salvăm cache pentru a detecta schimbările
        cachedWalls = walls;
        lastWallsCount = walls.Length;

        float minX = float.PositiveInfinity;
        float maxX = float.NegativeInfinity;
        foreach (var w in walls)
        {
            var col = w.GetComponent<Collider>();
            if (col != null)
            {
                var b = col.bounds;
                minX = Mathf.Min(minX, b.min.x);
                maxX = Mathf.Max(maxX, b.max.x);
            }
            else
            {
                float wx = w.transform.position.x;
                minX = Mathf.Min(minX, wx);
                maxX = Mathf.Max(maxX, wx);
            }
        }

        leftLimitX = minX + innerMargin;
        rightLimitX = maxX - innerMargin;

        if (leftLimitX >= rightLimitX)
            Debug.LogWarning("[PlayerBoundsLimiterForce] Limitele calculate sunt invalide. Verifică wall-urile.");

        if (debugLogs) Debug.Log($"[PlayerBoundsLimiterForce] Limits calculated: left={leftLimitX:F3} right={rightLimitX:F3} (walls={walls.Length})");
    }

    void LateUpdate()
    {
        // detectăm schimbări în scena (dacă walls sunt instanțiate dinamically)
        var wallsNow = GameObject.FindGameObjectsWithTag(wallTag);
        if (wallsNow.Length != lastWallsCount)
        {
            RecalculateLimits();
        }

        // aplicăm clamp FORȚAT (override)
        Vector3 pos = transform.position;
        float clampedX = Mathf.Clamp(pos.x, leftLimitX, rightLimitX);

        if (clampedX != pos.x)
        {
            Vector3 desired = new Vector3(clampedX, pos.y, pos.z);

            // FORȚĂ OVERRIDE: setăm transform.position direct (asta anulează orice alt script
            // care a făcut transform.position mai devreme în frame)
            transform.position = desired;

            if (debugLogs)
                Debug.Log($"[PlayerBoundsLimiterForce] Clamped player X from {pos.x:F3} to {clampedX:F3}");
        }
    }

    // util în editor: forțează recalculare manuală
    [ContextMenu("Recalculate Limits")]
    public void EditorRecalculate() => RecalculateLimits();

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        if (leftLimitX != float.NegativeInfinity && rightLimitX != float.PositiveInfinity)
        {
            float h = 2f;
            Gizmos.DrawLine(new Vector3(leftLimitX, transform.position.y - h, transform.position.z - 50),
                            new Vector3(leftLimitX, transform.position.y + h, transform.position.z + 50));
            Gizmos.DrawLine(new Vector3(rightLimitX, transform.position.y - h, transform.position.z - 50),
                            new Vector3(rightLimitX, transform.position.y + h, transform.position.z + 50));
        }
    }
}