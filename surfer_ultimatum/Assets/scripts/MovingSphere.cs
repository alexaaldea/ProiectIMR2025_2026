using UnityEngine;

public class MovingSphere : MonoBehaviour
{
    [HideInInspector] public Vector3 direction;
    [HideInInspector] public float speed;
    [HideInInspector] public float lifetime;
    [HideInInspector] public SphereSpawner spawner;

    private float spawnTime;
    private bool hasCollided = false;

    void Start()
    {
        spawnTime = Time.time;
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        rb.useGravity = false;
        rb.isKinematic = true;

        SphereCollider collider = GetComponent<SphereCollider>();
        if (collider == null)
        {
            collider = gameObject.AddComponent<SphereCollider>();
        }
        collider.isTrigger = true;
    }

    void Update()
    {
        transform.position += direction * speed * Time.deltaTime;

        if (Time.time - spawnTime > lifetime)
        {
            DestroySphere();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasCollided) return;

        // Check for shield on XR camera
        XRCharacterPowerUpHandler powerUpHandler = other.GetComponent<XRCharacterPowerUpHandler>();
        if (powerUpHandler != null && powerUpHandler.HasShield())
        {
            hasCollided = true;
            DestroySphere();
            return;
        }

        // Check for XR Camera collision tracker
        XRCameraCollisionTracker cameraTracker = other.GetComponent<XRCameraCollisionTracker>();
        if (cameraTracker != null)
        {
            hasCollided = true;
            ShowNotification("You were hit by a sphere!");
            DestroySphere();
            return;
        }

        // Legacy check for CharacterController (keep for backward compatibility)
        if (other.GetComponent<CharacterController>() != null)
        {
            hasCollided = true;
            ShowNotification("You were hit by a sphere!");
            DestroySphere();
        }
    }

    private void ShowNotification(string message)
    {
        Debug.LogWarning("*** " + message + " ***");
    }

    private void DestroySphere()
    {
        if (spawner != null)
        {
            spawner.OnSphereDestroyed();
        }
        Destroy(gameObject);
    }
}