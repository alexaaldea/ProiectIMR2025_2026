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
        col.isTrigger = true;
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
            // Play powerup sound
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayPowerupSound();
            }
            else
            {
                Debug.LogWarning("AudioManager.Instance not found!");
            }

            // Activate the powerup
            handler.ActivatePowerUp(powerUpType, duration);

            // Destroy the powerup object
            Destroy(gameObject);
        }
    }
}