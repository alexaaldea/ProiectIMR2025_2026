using UnityEngine;

public enum PowerUpType
{
    Shield,
    SlowTime
}

[RequireComponent(typeof(Collider))]
public class PowerUp : MonoBehaviour
{
    public PowerUpType powerUpType = PowerUpType.Shield;
    public float duration = 5f;

    private void Reset()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true; // Trigger for pickup
        if (CompareTag("Untagged"))
            gameObject.tag = "PowerUp";
    }

    private void OnTriggerEnter(Collider other)
    {
        XRCharacterPowerUpHandler handler = other.GetComponent<XRCharacterPowerUpHandler>();
        if (handler == null)
            handler = other.GetComponentInParent<XRCharacterPowerUpHandler>();

        if (handler != null)
        {
            handler.ActivatePowerUp(powerUpType, duration);
            Destroy(gameObject);
        }
    }
}
