using UnityEngine;

public enum PowerUpType
{
    Shield,
    SlowTime
}

public class PowerUp : MonoBehaviour
{
    public PowerUpType powerUpType;
    public float duration = 5f; 

    private void OnTriggerEnter(Collider other)
    {
        XRCharacterPowerUpHandler handler = other.GetComponent<XRCharacterPowerUpHandler>();
        if (handler != null)
        {
            handler.ActivatePowerUp(powerUpType, duration);
            Destroy(gameObject); 
        }
    }
}
