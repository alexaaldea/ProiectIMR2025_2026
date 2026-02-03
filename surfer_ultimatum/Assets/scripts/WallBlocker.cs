using UnityEngine;

/// <summary>
/// Pune acest script pe perete (BoxCollider/MeshCollider, IsTrigger = OFF).
/// Când un obiect cu tagul "Player" lovește peretele, îl împinge înapoi astfel
/// încât să nu treacă prin el. Funcționează cu Rigidbody (preferat) sau CharacterController.
/// </summary>
[DisallowMultipleComponent]
public class WallBlocker : MonoBehaviour
{
    [Tooltip("Tagul folosit pentru obiectul player (set în Inspector).")]
    public string playerTag = "Player";

    [Tooltip("Distanța minimă pe care playerul trebuie să fie față de perete (în metri).")]
    public float clearance = 0.05f;

    [Tooltip("Smooth push (te păzește de 'teleport' prea brusc). 0 = instant.")]
    [Range(0f, 1f)]
    public float smoothFactor = 0f;

    Collider wallCollider;

    void Awake()
    {
        wallCollider = GetComponent<Collider>();
        if (wallCollider == null)
            Debug.LogError("[WallBlocker] Nu am găsit Collider pe wall!");
        else if (wallCollider.isTrigger)
            Debug.LogWarning("[WallBlocker] Collider este setat IsTrigger = true. Setează-l OFF pentru blocare fizică.");
    }

    void OnCollisionStay(Collision collision)
    {
        // Luăm doar obiectele cu tagul player
        if (collision.gameObject.tag != playerTag) return;

        GameObject other = collision.gameObject;
        Collider otherCol = other.GetComponent<Collider>();

        if (wallCollider == null || otherCol == null) return;

        // Punctul cel mai apropiat de centrul jucătorului pe colliderul peretelui
        Vector3 playerCenter = GetColliderCenterWorld(otherCol, other.transform);
        Vector3 closestOnWall = wallCollider.ClosestPoint(playerCenter);

        // Direcția din punctul cel mai apropiat spre centru jucător
        Vector3 dir = playerCenter - closestOnWall;
        float dist = dir.magnitude;

        // dacă e practic în interiorul peretelui (dist mic), folosim normalul contactului
        if (dist <= 0.0001f && collision.contacts.Length > 0)
        {
            dir = collision.contacts[0].normal * -1f; // normalul contactului vine dinspre wall spre player, inversăm dacă e nevoie
            dist = 0f;
        }
        else if (dist > 0f)
        {
            dir /= dist; // normalizăm
        }

        // Aflăm cât spațiu trebuie să existe (jucător radius etc.)
        float needed = GetColliderRadius(otherCol) + clearance;

        // Dacă dist < needed -> trebuie să mutăm jucătorul afară
        if (dist < needed)
        {
            Vector3 targetPos = closestOnWall + dir * needed;

            // Păstrăm Y (altitudine) din poziția playerului (nu vrem să-l ridicăm/sac)
            targetPos.y = other.transform.position.y;

            // Aplicare: CharacterController sau Rigidbody sau transform direct
            CharacterController cc = other.GetComponent<CharacterController>();
            Rigidbody rb = other.GetComponent<Rigidbody>();

            if (cc != null)
            {
                // cc.Move folosește delta; calculăm delta
                Vector3 delta = targetPos - other.transform.position;
                if (smoothFactor > 0f)
                    cc.Move(delta * (1f - smoothFactor));
                else
                    cc.Move(delta);
            }
            else if (rb != null && !rb.isKinematic)
            {
                if (smoothFactor > 0f)
                {
                    Vector3 newPos = Vector3.Lerp(rb.position, targetPos, 1f - smoothFactor);
                    rb.MovePosition(newPos);
                }
                else
                {
                    rb.MovePosition(targetPos);
                }

                // Oprim viteza pe axa laterală ca să nu fie forțe care împing în continuare prin wall
                Vector3 v = rb.velocity;
                // Proiectăm viteza pentru a elimina componenta pe direcția spre/înspre wall
                Vector3 tangent = Vector3.ProjectOnPlane(v, dir);
                rb.velocity = tangent;
            }
            else
            {
                // fallback: set transform.position (ultimul resort)
                if (smoothFactor > 0f)
                    other.transform.position = Vector3.Lerp(other.transform.position, targetPos, 1f - smoothFactor);
                else
                    other.transform.position = targetPos;
            }
        }
    }

    // Returnează poziția centrului "logical" al colliderului în world space
    private Vector3 GetColliderCenterWorld(Collider col, Transform t)
    {
        if (col is CapsuleCollider c)
            return t.TransformPoint(c.center);
        if (col is SphereCollider s)
            return t.TransformPoint(s.center);
        if (col is BoxCollider b)
            return t.TransformPoint(b.center);
        // fallback: transform position
        return t.position;
    }

    // Estimează "raza" aproximativă a colliderului pe planul lateral (pentru clearance)
    private float GetColliderRadius(Collider col)
    {
        if (col is CapsuleCollider c)
        {
            float scale = Mathf.Max(c.transform.lossyScale.x, c.transform.lossyScale.z);
            return c.radius * scale;
        }
        if (col is SphereCollider s)
        {
            float scale = Mathf.Max(s.transform.lossyScale.x, s.transform.lossyScale.y);
            return s.radius * scale;
        }
        if (col is BoxCollider b)
        {
            // luăm jumătate din dimensiunea pe X (sau pe Y/Z), aproximativ
            Vector3 sizeWorld = Vector3.Scale(b.size, b.transform.lossyScale);
            return Mathf.Max(sizeWorld.x, sizeWorld.z) * 0.5f;
        }
        // fallback
        return 0.5f;
    }
}