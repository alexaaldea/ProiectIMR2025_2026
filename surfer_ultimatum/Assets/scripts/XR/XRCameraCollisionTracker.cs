using UnityEngine;

public class XRCameraCollisionTracker : MonoBehaviour
{
    [Header("Collision Settings")]
    [SerializeField] private bool logCollisions = true;
    [SerializeField] private LayerMask collisionLayers = ~0;

    [Header("Sphere Detection")]
    [SerializeField] private string[] obstacleTags = { "RedSphere", "Obstacle" };
    [SerializeField] private bool detectSpheres = true;

    [Header("Tracking Settings")]
    [SerializeField] private bool trackPosition = false;
    [SerializeField] private bool trackRotation = false;
    [SerializeField] private float trackingUpdateRate = 0.1f;

    private int currentCollisionCount = 0;
    private Vector3 lastPosition;
    private Quaternion lastRotation;
    private float trackingTimer;

    public delegate void CollisionEventHandler(Collision collision);
    public event CollisionEventHandler OnCameraCollisionEnter;
    public event CollisionEventHandler OnCameraCollisionStay;
    public event CollisionEventHandler OnCameraCollisionExit;

    public delegate void TriggerEventHandler(Collider other);
    public event TriggerEventHandler OnCameraTriggerEnter;
    public event TriggerEventHandler OnCameraTriggerStay;
    public event TriggerEventHandler OnCameraTriggerExit;

    public delegate void SphereHitEventHandler(GameObject sphere);
    public event SphereHitEventHandler OnRedSphereHit;

    private void Start()
    {
        Debug.Log("=== XRCameraCollisionTracker Starting Setup ===");

        // Ensure we have a collider
        SphereCollider sphereCollider = GetComponent<SphereCollider>();
        if (sphereCollider == null)
        {
            sphereCollider = gameObject.AddComponent<SphereCollider>();
            Debug.Log(" Added SphereCollider to XR Camera");
        }

        sphereCollider.isTrigger = false; // NOT a trigger for solid collisions
        sphereCollider.radius = 0.15f;
        Debug.Log($" Collider configured - Radius: {sphereCollider.radius}, IsTrigger: {sphereCollider.isTrigger}");

        // Ensure we have a rigidbody
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            Debug.Log(" Added Rigidbody to XR Camera");
        }

        // CRITICAL: Must be NON-kinematic to collide with kinematic walls
        rb.isKinematic = false; // CHANGED FROM TRUE TO FALSE
        rb.useGravity = false;  // No gravity
        rb.mass = 1f;
        rb.drag = 0f;
        rb.angularDrag = 0.05f;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // Freeze rotation so camera doesn't tilt when hitting walls
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        Debug.Log($" Rigidbody configured - IsKinematic: {rb.isKinematic}, Gravity: {rb.useGravity}");
        Debug.Log($" Rotation constraints: {rb.constraints}");

        lastPosition = transform.position;
        lastRotation = transform.rotation;

        Debug.Log($"=== XR Camera Setup Complete ===");
    }

    private void Update()
    {
        if (trackPosition || trackRotation)
        {
            trackingTimer += Time.deltaTime;

            if (trackingTimer >= trackingUpdateRate)
            {
                TrackCameraMovement();
                trackingTimer = 0f;
            }
        }
    }

    private void TrackCameraMovement()
    {
        if (trackPosition)
        {
            Vector3 currentPosition = transform.position;
            lastPosition = currentPosition;
        }

        if (trackRotation)
        {
            Quaternion currentRotation = transform.rotation;
            lastRotation = currentRotation;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"!!! COLLISION ENTER !!! with: {collision.gameObject.name}");

        if (!IsInLayerMask(collision.gameObject, collisionLayers))
            return;

        currentCollisionCount++;

        if (logCollisions)
        {
            Debug.LogWarning($"*** CAMERA HIT WALL *** {collision.gameObject.name}");
        }

        // NEW: Check for obstacle tags
        if (detectSpheres)
        {
            foreach (string tag in obstacleTags)
            {
                if (collision.gameObject.CompareTag(tag))
                {
                    HandleRedSphereHit(collision.gameObject);
                    break; // Exit loop once we find a match
                }
            }
        }

        OnCameraCollisionEnter?.Invoke(collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        if (!IsInLayerMask(collision.gameObject, collisionLayers))
            return;

        OnCameraCollisionStay?.Invoke(collision);
    }

    private void OnCollisionExit(Collision collision)
    {
        Debug.Log($"!!! COLLISION EXIT !!! with: {collision.gameObject.name}");

        if (!IsInLayerMask(collision.gameObject, collisionLayers))
            return;

        currentCollisionCount = Mathf.Max(0, currentCollisionCount - 1);

        if (logCollisions)
        {
            Debug.Log($"Stopped colliding with: {collision.gameObject.name}");
        }

        OnCameraCollisionExit?.Invoke(collision);
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"!!! TRIGGER ENTER !!! with: {other.gameObject.name}");

        if (!IsInLayerMask(other.gameObject, collisionLayers))
            return;

        if (logCollisions)
        {
            Debug.Log($"Camera entered trigger: {other.gameObject.name}");
        }

        // NEW: Check for obstacle tags
        if (detectSpheres)
        {
            foreach (string tag in obstacleTags)
            {
                if (other.gameObject.CompareTag(tag))
                {
                    HandleRedSphereHit(other.gameObject);
                    break; // Exit loop once we find a match
                }
            }
        }

        OnCameraTriggerEnter?.Invoke(other);
    }

    private void OnTriggerStay(Collider other)
    {
        if (!IsInLayerMask(other.gameObject, collisionLayers))
            return;

        OnCameraTriggerStay?.Invoke(other);
    }

    private void OnTriggerExit(Collider other)
    {
        Debug.Log($"!!! TRIGGER EXIT !!! with: {other.gameObject.name}");

        if (!IsInLayerMask(other.gameObject, collisionLayers))
            return;

        if (logCollisions)
        {
            Debug.Log($"Camera exited trigger: {other.gameObject.name}");
        }

        OnCameraTriggerExit?.Invoke(other);
    }

    // NEW: Method to handle red sphere/obstacle hits
    private void HandleRedSphereHit(GameObject sphere)
    {
        Debug.LogError($"!!! HIT BY RED SPHERE OR OBSTACLE: {sphere.name} !!!");

        // Call the GameOver event
        OnRedSphereHit?.Invoke(sphere);

        // Check for shield protection (only if it's a red sphere, not an obstacle)
        if (sphere.CompareTag("RedSphere"))
        {
            XRCharacterPowerUpHandler powerUpHandler = GetComponent<XRCharacterPowerUpHandler>();
            if (powerUpHandler != null && powerUpHandler.HasShield())
            {
                Debug.Log("Shield protected from sphere hit!");
                return; // Don't proceed to GameOver if shielded
            }
        }

        // Play death sound
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayDeathSound();
        }

        // Game Over logic
        string hitMessage = sphere.CompareTag("RedSphere") ?
            "You were hit by a red sphere!" :
            "You hit an obstacle!";

        ShowNotification(hitMessage);

        // Call GameManager.Instance.GameOver()
        if (GameManager.Instance != null)
        {
            GameManager.Instance.GameOver();
        }
        else
        {
            Debug.LogError("GameManager.Instance is null!");
        }
    }

    private void ShowNotification(string message)
    {
        Debug.LogWarning("*** " + message + " ***");
        // You could also implement a UI notification system here
    }

    private bool IsInLayerMask(GameObject obj, LayerMask layerMask)
    {
        return ((1 << obj.layer) & layerMask) != 0;
    }

    public int GetCurrentCollisionCount()
    {
        return currentCollisionCount;
    }

    // Optional: Public method to check if a collider is in obstacle tags
    public bool IsObstacle(Collider collider)
    {
        foreach (string tag in obstacleTags)
        {
            if (collider.CompareTag(tag))
            {
                return true;
            }
        }
        return false;
    }

    public bool IsObstacle(GameObject obj)
    {
        foreach (string tag in obstacleTags)
        {
            if (obj.CompareTag(tag))
            {
                return true;
            }
        }
        return false;
    }
}