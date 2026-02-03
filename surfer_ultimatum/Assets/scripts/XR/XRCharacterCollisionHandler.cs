using UnityEngine;

/// <summary>
/// Detectează două ziduri (tag = "Wall") din aceea��i "bandă" și blochează poziția jucătorului
/// între limitele laterale. Funcționează cu transform, CharacterController sau Rigidbody.
/// </summary>
[DisallowMultipleComponent]
public class PlayerBoundsLimiterAuto : MonoBehaviour
{
    [Tooltip("Tag folosit pe obiectele wall. Asigură-te că Left/Right walls au acest tag.")]
    public string wallTag = "Wall";

    [Tooltip("Margină internă față de perete (în metri) — jucătorul nu va atinge direct peretele.")]
    public float innerMargin = 0.05f;

    [Tooltip("Dacă true, scriptul corectează poziția folosind CharacterController.Move când e disponibil.")]
    public bool useCharacterControllerIfPresent = true;

    private float leftLimitX = float.NegativeInfinity;
    private float rightLimitX = float.PositiveInfinity;
    private CharacterController cc;
    private Rigidbody rb;
    private bool initialized = false;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        rb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        InitializeLimits();
    }

    // Caută wall‑urile din scenă și calculează min/max X
    public void InitializeLimits()
    {
        GameObject[] walls = GameObject.FindGameObjectsWithTag(wallTag);
        if (walls == null || walls.Length == 0)
        {
            Debug.LogWarning("[PlayerBoundsLimiterAuto] Nu am găsit walls cu tag='" + wallTag + "'. Setează tagul sau adaugă wall-urile.");
            return;
        }

        float minX = float.PositiveInfinity;
        float maxX = float.NegativeInfinity;
        foreach (var w in walls)
        {
            // Folosim bounding box-ul (Collider) dacă există, altfel transform.position
            Collider col = w.GetComponent<Collider>();
            if (col != null)
            {
                Bounds b = col.bounds;
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

        // Estimăm limitele pistei între cele două ziduri: leftLimit = minX, rightLimit = maxX
        leftLimitX = minX + innerMargin;
        rightLimitX = maxX - innerMargin;

        if (leftLimitX >= rightLimitX)
            Debug.LogWarning("[PlayerBoundsLimiterAuto] Limitele calculate sunt invalide. Verifică poziția/fizica wall-urilor.");

        initialized = true;
    }

    void Update()
    {
        if (!initialized) InitializeLimits();
        if (!initialized) return;

        Vector3 pos = transform.position;
        float clampedX = Mathf.Clamp(pos.x, leftLimitX, rightLimitX);
        if (Mathf.Approximately(clampedX, pos.x)) return; // în interior, nu corectăm

        Vector3 desired = new Vector3(clampedX, pos.y, pos.z);
        Vector3 delta = desired - pos;

        // Preferăm CharacterController.Move dacă există (nu modificăm direct transform în cazul CC)
        if (useCharacterControllerIfPresent && cc != null)
        {
            // cc.Move așteaptă un delta în world space
            cc.Move(delta);
        }
        else if (rb != null && !rb.isKinematic)
        {
            // Folosim MovePosition pentru Rigidbody din FixedUpdate — dar putem aplica imediat aici:
            // (scurt fallback) setăm poziția fizică, dar păstrăm integritatea fizicii
            rb.position = desired;
            rb.velocity = Vector3.zero;
        }
        else
        {
            // fallback: setăm transform direct
            transform.position = desired;
        }
    }

    // Editor gizmo: afișăm limitele
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