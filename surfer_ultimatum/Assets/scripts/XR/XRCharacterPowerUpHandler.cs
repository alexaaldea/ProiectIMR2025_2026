using System;
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class XRCharacterPowerUpHandler : MonoBehaviour
{
    [Header("Visuals / Settings")]
    [SerializeField] private GameObject shieldVisual;
    [SerializeField] private float slowTimeScale = 0.5f;

    [Header("Lives")]
    [Tooltip("Numărul inițial de vieți consumabile (pe player).")]
    [SerializeField] private int extraLives = 1; // START WITH 1 LIFE
    [SerializeField] private int maxExtraLives = 99;

    // State
    private bool isShieldActive = false;
    private bool isSlowTimeActive = false;

    // Properties
    public int ExtraLives => extraLives;
    public bool HasShieldActive => isShieldActive;
    public bool HasSlowTimeActive => isSlowTimeActive;

    // Event: notifică schimbarea vieților (GameManager / UI se vor abona)
    public event Action<int> OnLivesChanged;

    // Public API
    public void ActivatePowerUp(PowerUpType type, float duration)
    {
        Debug.Log($"[PowerUpHandler] ActivatePowerUp called: {type} duration={duration} on {gameObject.name}");

        switch (type)
        {
            case PowerUpType.Shield:
                AddExtraLife(1);
                if (!isShieldActive) StartCoroutine(ShieldCoroutine(duration));
                break;

            case PowerUpType.SlowTime:
                if (!isSlowTimeActive) StartCoroutine(SlowTimeCoroutine(duration));
                break;
        }
    }

    private IEnumerator ShieldCoroutine(float duration)
    {
        isShieldActive = true;
        if (shieldVisual != null) shieldVisual.SetActive(true);
        Debug.Log($"[PowerUpHandler] Shield enabled for {duration} seconds.");

        if (duration > 0f) yield return new WaitForSeconds(duration);
        else yield return null;

        if (shieldVisual != null) shieldVisual.SetActive(false);
        isShieldActive = false;
        Debug.Log("[PowerUpHandler] Shield expired.");
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

    // Extra lives API
    public void AddExtraLife(int amount = 1)
    {
        int old = extraLives;
        extraLives = Mathf.Clamp(extraLives + amount, 0, maxExtraLives);
        Debug.Log($"[PowerUpHandler] Extra lives: {old} -> {extraLives}");
        OnLivesChanged?.Invoke(extraLives);
    }

    public bool HasExtraLife()
    {
        return extraLives > 0;
    }

    // Consumă o viață; returnează true dacă s-a consumat una
    public bool ConsumeExtraLife()
    {
        if (extraLives > 0)
        {
            int old = extraLives;
            extraLives--;
            Debug.Log($"[PowerUpHandler] Extra life consumed: {old} -> {extraLives}");
            OnLivesChanged?.Invoke(extraLives);
            return true;
        }
        return false;
    }

    // Helper
    public bool HasShield()
    {
        return isShieldActive;
    }
}