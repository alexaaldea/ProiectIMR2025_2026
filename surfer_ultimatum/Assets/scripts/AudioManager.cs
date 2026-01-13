using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource sfxSource;

    [Header("Sound Effects")]
    [SerializeField] private AudioClip coinCollectSound;
    [SerializeField] private AudioClip deathSound;
    [SerializeField] private AudioClip powerupSound;

    [Header("Volume Settings")]
    [SerializeField][Range(0f, 1f)] private float masterSfxVolume = 1f;
    [SerializeField][Range(0f, 1f)] private float coinSoundVolume = 0.4f;  // Lower for coin
    [SerializeField][Range(0f, 1f)] private float deathSoundVolume = 0.8f; // Higher for death
    [SerializeField][Range(0f, 1f)] private float powerupSoundVolume = 0.7f; // Powerup volume

    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Create AudioSource if not assigned
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 0f; // 2D sound
        }
    }

    public void PlayCoinSound()
    {
        if (coinCollectSound != null)
        {
            float finalVolume = masterSfxVolume * coinSoundVolume;
            sfxSource.PlayOneShot(coinCollectSound, finalVolume);
            Debug.Log($"Playing coin collect sound at volume: {finalVolume}");
        }
        else
        {
            Debug.LogWarning("Coin collect sound is not assigned in AudioManager!");
        }
    }

    public void PlayDeathSound()
    {
        if (deathSound != null)
        {
            float finalVolume = masterSfxVolume * deathSoundVolume;
            sfxSource.PlayOneShot(deathSound, finalVolume);
            Debug.Log($"Playing death sound at volume: {finalVolume}");
        }
        else
        {
            Debug.LogWarning("Death sound is not assigned in AudioManager!");
        }
    }

    public void PlayPowerupSound()
    {
        if (powerupSound != null)
        {
            float finalVolume = masterSfxVolume * powerupSoundVolume;
            sfxSource.PlayOneShot(powerupSound, finalVolume);
            Debug.Log($"Playing powerup sound at volume: {finalVolume}");
        }
        else
        {
            Debug.LogWarning("Powerup sound is not assigned in AudioManager!");
        }
    }

    // Optional: Method to play any sound effect with custom volume
    public void PlaySound(AudioClip clip, float volumeScale = 1f)
    {
        if (clip != null)
        {
            sfxSource.PlayOneShot(clip, volumeScale * masterSfxVolume);
        }
    }

    // Optional: Set volumes at runtime
    public void SetMasterVolume(float volume)
    {
        masterSfxVolume = Mathf.Clamp01(volume);
    }

    public void SetCoinVolume(float volume)
    {
        coinSoundVolume = Mathf.Clamp01(volume);
    }

    public void SetDeathVolume(float volume)
    {
        deathSoundVolume = Mathf.Clamp01(volume);
    }

    public void SetPowerupVolume(float volume)
    {
        powerupSoundVolume = Mathf.Clamp01(volume);
    }
}