using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class XRCharacterCollisionHandler : MonoBehaviour
{
    [Header("Optional (dacă vrei să setezi manual)")]
    [SerializeField] private XRCharacterPowerUpHandler powerUpHandler;

    private bool isDead = false;

    private void Reset()
    {
        // Setări utile când atașezi componenta prima dată
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = false;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = false;
        }
    }

    private void Awake()
    {
        ResolvePowerUpHandlerIfNeeded();
    }

    // În caz că referința lipsește, încercăm câteva variante și logăm
    private void ResolvePowerUpHandlerIfNeeded()
    {
        if (powerUpHandler == null)
        {
            powerUpHandler = GetComponent<XRCharacterPowerUpHandler>()
                           ?? GetComponentInParent<XRCharacterPowerUpHandler>()
                           ?? GetComponentInChildren<XRCharacterPowerUpHandler>()
                           ?? FindObjectOfType<XRCharacterPowerUpHandler>();

            Debug.Log($"[CollisionHandler] powerUpHandler resolved to: {(powerUpHandler == null ? "null" : powerUpHandler.gameObject.name)} on {gameObject.name}");
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isDead) return;

        GameObject hit = collision.gameObject;
        Debug.Log($"[CollisionHandler] Am lovit: {hit.name} (tag: {hit.tag})");

        // Mori doar dacă obiectul are tag "Obstacle" (conform codului tău)
        if (hit.CompareTag("Obstacle"))
        {
            TryKillPlayer($"Lovit de obstacle: {hit.name}");
        }
    }

    private void TryKillPlayer(string reason)
    {
        if (isDead) return;

        // Asigurăm referința
        ResolvePowerUpHandlerIfNeeded();

        if (powerUpHandler != null)
            Debug.Log($"[CollisionHandler] HasShield={powerUpHandler.HasShield()} ExtraLives={powerUpHandler.ExtraLives}");

        // 1) Dacă ai shield activ -> nu mori
        if (powerUpHandler != null && powerUpHandler.HasShield())
        {
            Debug.Log("[CollisionHandler] Protejat de shield activ, NU mori. Motiv: " + reason);
            return;
        }

        // 2) Dacă ai o viață extra, o consumăm și nu mori
        if (powerUpHandler != null && powerUpHandler.ConsumeExtraLife())
        {
            Debug.Log("[CollisionHandler] Extra life disponibilă — consumată. Continuăm jocul. Motiv: " + reason);
            // Poți adăuga efecte vizuale/sunet scurt aici
            return;
        }

        // 3) Altfel -> mori
        isDead = true;
        Debug.LogWarning("[CollisionHandler] PLAYER KILLED! Motiv: " + reason);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.GameOver();
        }
        else
        {
            Debug.LogError("[CollisionHandler] GameManager.Instance este null. Pune un GameManager în scenă.");
        }
    }
}