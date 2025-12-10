using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class XRCharacterCollisionHandler : MonoBehaviour
{
    [Header("Optional (doar dacă ai shield)")]
    [SerializeField] private XRCharacterPowerUpHandler powerUpHandler;

    private bool isDead = false;

    private void Reset()
    {
        // Se cheamă când atașezi scriptul prima dată
        Collider col = GetComponent<Collider>();
        col.isTrigger = false;      // vrem coliziune fizică

        Rigidbody rb = GetComponent<Rigidbody>();
        rb.isKinematic = false;    // obligatoriu pentru OnCollisionEnter
        rb.useGravity = false;     // pentru VR, să nu cadă camera
    }

    private void Awake()
    {
        // Dacă nu e setat din Inspector, încercăm să-l căutăm pe același obiect
        if (powerUpHandler == null)
            powerUpHandler = GetComponent<XRCharacterPowerUpHandler>();
    }

    // SE APELEAZĂ DOAR LA COLIZIUNI FIZICE (NU TRIGGER)
    private void OnCollisionEnter(Collision collision)
    {
        if (isDead) return;

        GameObject hit = collision.gameObject;
        Debug.Log("Am lovit: " + hit.name + " (tag: " + hit.tag + ")");

        // Mori DOAR dacă obiectul are tag "Obstacle"
        if (hit.CompareTag("Obstacle"))
        {
            TryKillPlayer("Lovit de obstacle: " + hit.name);
        }
    }

    private void TryKillPlayer(string reason)
    {
        // Dacă ai shield activ, nu mori
        if (powerUpHandler != null && powerUpHandler.HasShield())
        {
            Debug.Log("Protejat de shield, NU mori. Motiv: " + reason);
            return;
        }

        if (isDead) return;
        isDead = true;

        Debug.LogWarning("PLAYER KILLED! Motiv: " + reason);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.GameOver();
        }
        else
        {
            Debug.LogError("GameManager.Instance este null. Pune un GameManager în scenă și asigură-te că nu este dezactivat.");
        }
    }
}