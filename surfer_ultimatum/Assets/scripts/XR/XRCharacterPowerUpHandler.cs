using UnityEngine;
using System.Collections;

public class XRCharacterPowerUpHandler : MonoBehaviour
{
    private bool isShieldActive = false;
    private bool isSlowTimeActive = false;

    [SerializeField] private GameObject shieldVisual;
    [SerializeField] private float slowTimeScale = 0.5f;

    public void ActivatePowerUp(PowerUpType type, float duration)
    {
        switch (type)
        {
            case PowerUpType.Shield:
                if (!isShieldActive)
                    StartCoroutine(ShieldCoroutine(duration));
                break;

            case PowerUpType.SlowTime:
                if (!isSlowTimeActive)
                    StartCoroutine(SlowTimeCoroutine(duration));
                break;
        }
    }

    private IEnumerator ShieldCoroutine(float duration)
    {
        isShieldActive = true;
        if (shieldVisual != null) shieldVisual.SetActive(true);

        yield return new WaitForSeconds(duration);

        if (shieldVisual != null) shieldVisual.SetActive(false);
        isShieldActive = false;
    }

    private IEnumerator SlowTimeCoroutine(float duration)
    {
        isSlowTimeActive = true;

        float originalTimeScale = Time.timeScale;
        float originalFixedDeltaTime = Time.fixedDeltaTime;

        Time.timeScale = slowTimeScale;
        Time.fixedDeltaTime = originalFixedDeltaTime * Time.timeScale;

        yield return new WaitForSecondsRealtime(duration);

        Time.timeScale = originalTimeScale;
        Time.fixedDeltaTime = originalFixedDeltaTime;
        isSlowTimeActive = false;
    }

    public bool HasShield()
    {
        return isShieldActive;
    }
}