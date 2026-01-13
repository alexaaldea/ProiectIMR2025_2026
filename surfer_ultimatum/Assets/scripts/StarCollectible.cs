using UnityEngine;

public class StarCollectible : MonoBehaviour
{
    [Header("Vizual")]
    public float rotateSpeed = 90f;
    public AnimationCurve collectScaleCurve = AnimationCurve.EaseInOut(0, 1, 0.2f, 0);
    public float collectAnimDuration = 0.2f;

    [Header("Feedback")]
    public AudioClip pickupClip;
    public ParticleSystem pickupParticles;

    [Header("Audio Settings")]
    [Tooltip("Use AudioManager for sound (recommended) or local AudioSource")]
    public bool useAudioManager = true;

    [Header("Intern")]
    public bool isActive = true;

    private AudioSource _audioSource;
    private float _collectAnimTime;
    private bool _isCollecting;
    private Vector3 _initialScale;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();

        // If no AudioSource and not using AudioManager, add one
        if (!useAudioManager && _audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 1f; // 3D sound
        }

        _initialScale = transform.localScale;
    }

    private void OnEnable()
    {
        isActive = true;
        _isCollecting = false;
        _collectAnimTime = 0f;
        transform.localScale = _initialScale;
    }

    private void Update()
    {
        if (isActive && !_isCollecting)
            transform.Rotate(Vector3.up * rotateSpeed * Time.deltaTime, Space.World);

        if (_isCollecting)
        {
            _collectAnimTime += Time.deltaTime;
            float t = _collectAnimTime / collectAnimDuration;
            float scale = collectScaleCurve.Evaluate(t);
            transform.localScale = _initialScale * scale;

            if (t >= 1f)
            {
                CoinPool.Instance.ReturnToPool(this);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isActive) return;

        // Check for XR Camera collision tracker
        XRCameraCollisionTracker cameraTracker = other.GetComponent<XRCameraCollisionTracker>();
        if (cameraTracker != null)
        {
            Collect();
            return;
        }

        // Legacy check for Player tag (keep for backward compatibility)
        if (other.CompareTag("Player"))
        {
            Collect();
        }
    }

    public void Collect()
    {
        if (!isActive) return;

        isActive = false;

        // Add coin to score
        CoinGameManager.Instance.AddCoin(1);

        // Spawn particle effects
        if (pickupParticles != null)
        {
            var p = Instantiate(pickupParticles, transform.position, Quaternion.identity);
            p.Play();
            Destroy(p.gameObject, 2f);
        }

        // Play sound effect
        PlayCollectionSound();

        // Start collection animation
        _isCollecting = true;
        _collectAnimTime = 0f;
    }

    private void PlayCollectionSound()
    {
        if (pickupClip == null)
        {
            Debug.LogWarning("Pickup clip is not assigned on " + gameObject.name);
            return;
        }

        if (useAudioManager)
        {
            // Use centralized AudioManager (recommended for consistent volume)
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayCoinSound();
            }
            else
            {
                Debug.LogWarning("AudioManager not found! Falling back to local AudioSource");
                PlayLocalSound();
            }
        }
        else
        {
            // Use local AudioSource (for 3D positional audio)
            PlayLocalSound();
        }
    }

    private void PlayLocalSound()
    {
        if (_audioSource != null && pickupClip != null)
        {
            _audioSource.PlayOneShot(pickupClip);
        }
    }
}