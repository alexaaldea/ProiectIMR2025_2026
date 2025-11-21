using UnityEngine;
using System.Collections;

public class XRCharacterPowerUpHandler : MonoBehaviour
{
    private bool isShieldActive = false;
    private bool isSlowTimeActive = false;

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
        Debug.Log("Shield Activated!");
        // TODO: Add shield visual here

        yield return new WaitForSeconds(duration);

        isShieldActive = false;
        Debug.Log("Shield Ended!");
    }

    private IEnumerator SlowTimeCoroutine(float duration)
    {
        isSlowTimeActive = true;
        Time.timeScale = 0.5f; // slow down the game
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        Debug.Log("Slow Time Activated!");

        yield return new WaitForSecondsRealtime(duration); // use real time because timeScale changed

        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        isSlowTimeActive = false;
        Debug.Log("Slow Time Ended!");
    }

    public bool HasShield()
    {
        return isShieldActive;
    }
}
