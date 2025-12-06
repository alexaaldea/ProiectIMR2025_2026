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

    [Header("Intern")]
    public bool isActive = true;

    private AudioSource _audioSource;
    private float _collectAnimTime;
    private bool _isCollecting;
    private Vector3 _initialScale;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
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
        CoinGameManager.Instance.AddCoin(1);

        if (pickupParticles != null)
        {
            var p = Instantiate(pickupParticles, transform.position, Quaternion.identity);
            p.Play();
            Destroy(p.gameObject, 2f);
        }

        if (pickupClip != null && _audioSource != null)
            _audioSource.PlayOneShot(pickupClip);

        _isCollecting = true;
        _collectAnimTime = 0f;
    }
}