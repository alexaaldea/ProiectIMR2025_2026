using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class XRCharacterCollisionHandler : MonoBehaviour
{
    private XRCharacterPowerUpHandler powerUpHandler;
    private bool isDead = false;

    private void Awake()
    {
        powerUpHandler = GetComponent<XRCharacterPowerUpHandler>();
        if (powerUpHandler == null)
            powerUpHandler = GetComponentInParent<XRCharacterPowerUpHandler>();
    }

    private void Reset()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = false;

        Rigidbody rb = GetComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.useGravity = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isDead) return;

        GameObject hit = collision.gameObject;

        // Check if obstacle
        if (hit.CompareTag("Obstacle") || hit.CompareTag("RedSphere"))
        {
            // Shield protects only against RedSphere
            if (hit.CompareTag("RedSphere") && powerUpHandler != null && powerUpHandler.HasShield())
            {
                Debug.Log("Shield protected from sphere hit!");
                return;
            }

            // Player dies
            isDead = true;
            Debug.LogWarning("PLAYER HIT! " + hit.name);

            if (GameManager.Instance != null)
                GameManager.Instance.GameOver();
        }
    }
}
