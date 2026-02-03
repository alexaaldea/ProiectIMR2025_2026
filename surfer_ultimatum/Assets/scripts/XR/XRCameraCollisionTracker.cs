using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public class XRCameraCollisionTracker : MonoBehaviour
{
    [Header("Collision Settings")]
    [SerializeField] private bool logCollisions = true;
    [SerializeField] private LayerMask collisionLayers = ~0;

    [Header("Sphere/Obstacle Detection")]
    [Tooltip("Tags considered obstacles (example: RedSphere, Obstacle, Wall)")]
    [SerializeField] private string[] obstacleTags = { "RedSphere", "Obstacle" };

    [Tooltip("Layers considered obstacles (useful to mark walls)")]
    [SerializeField] private LayerMask obstacleLayers = 0;

    [Tooltip("If true, any collider hit will be treated as obstacle (use with caution)")]
    [SerializeField] private bool treatAllCollisionsAsObstacles = false;

    [SerializeField] private bool detectSpheres = true;

    [Header("Tracking Settings")]
    [SerializeField] private bool trackPosition = false;
    [SerializeField] private bool trackRotation = false;
    [SerializeField] private float trackingUpdateRate = 0.1f;

    [Header("References (optional)")]
    [SerializeField] private XRCharacterPowerUpHandler powerUpHandler;

    [Header("Hit / invulnerability")]
    [SerializeField] private float invulnerabilityDuration = 0.5f; // seconds to ignore collisions after consume
    private bool isInvulnerable = false;

    [SerializeField] private float perObjectHitCooldown = 0.6f; // seconds realtime
    private Dictionary<int, float> lastHitRealtime = new Dictionary<int, float>();

    // If true, bypass the per-object cooldown when a shield is active so hits are processed instantly
    [Header("Instant hit options")]
    [SerializeField] private bool bypassCooldownWhenShielded = true;

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

    // Event invoked only on actual death (no-shield, no-extra life)
    public delegate void SphereHitEventHandler(GameObject sphere);
    public event SphereHitEventHandler OnRedSphereHit;

    private void Start()
    {
        if (logCollisions) Debug.Log("=== XRCameraCollisionTracker Starting Setup ===");

        // Ensure collider + rb
        SphereCollider sphereCollider = GetComponent<SphereCollider>();
        if (sphereCollider == null)
        {
            sphereCollider = gameObject.AddComponent<SphereCollider>();
            if (logCollisions) Debug.Log(" Added SphereCollider to XR Camera");
        }

        sphereCollider.isTrigger = false;
        sphereCollider.radius = 0.15f;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            if (logCollisions) Debug.Log(" Added Rigidbody to XR Camera");
        }

        rb.isKinematic = false;
        rb.useGravity = false;
        rb.mass = 1f;
        rb.drag = 0f;
        rb.angularDrag = 0.05f;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        lastPosition = transform.position;
        lastRotation = transform.rotation;

        if (powerUpHandler == null)
            ResolvePowerUpHandler();

        if (logCollisions) Debug.Log($"[CameraTracker] powerUpHandler={(powerUpHandler == null ? "null" : powerUpHandler.gameObject.name)}");
        if (logCollisions) Debug.Log("=== XR Camera Setup Complete ===");
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
        if (trackPosition) lastPosition = transform.position;
        if (trackRotation) lastRotation = transform.rotation;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsInLayerMask(collision.gameObject, collisionLayers)) return;

        if (isInvulnerable)
        {
            if (logCollisions) Debug.Log("[CameraTracker] Ignored collision due to invulnerability window.");
            return;
        }

        currentCollisionCount++;
        if (logCollisions) Debug.LogWarning($"*** CAMERA HIT: {collision.gameObject.name}");

        if (detectSpheres && IsObstacleCandidate(collision.gameObject))
        {
            bool bypass = bypassCooldownWhenShielded && powerUpHandler != null && powerUpHandler.HasShield();
            ProcessObstacleHitWithCooldown(collision.gameObject, bypass);
            return; // don't call generic event for obstacle hits
        }

        OnCameraCollisionEnter?.Invoke(collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        if (!IsInLayerMask(collision.gameObject, collisionLayers)) return;
        OnCameraCollisionStay?.Invoke(collision);
    }

    private void OnCollisionExit(Collision collision)
    {
        if (!IsInLayerMask(collision.gameObject, collisionLayers)) return;

        currentCollisionCount = Mathf.Max(0, currentCollisionCount - 1);
        if (logCollisions) Debug.Log($"Stopped colliding with: {collision.gameObject.name}");
        OnCameraCollisionExit?.Invoke(collision);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsInLayerMask(other.gameObject, collisionLayers)) return;

        if (isInvulnerable)
        {
            if (logCollisions) Debug.Log("[CameraTracker] Ignored trigger due to invulnerability window.");
            return;
        }

        if (logCollisions) Debug.Log($"Camera entered trigger: {other.gameObject.name}");

        if (detectSpheres && IsObstacleCandidate(other.gameObject))
        {
            bool bypass = bypassCooldownWhenShielded && powerUpHandler != null && powerUpHandler.HasShield();
            ProcessObstacleHitWithCooldown(other.gameObject, bypass);
            return;
        }

        OnCameraTriggerEnter?.Invoke(other);
    }

    private void OnTriggerStay(Collider other)
    {
        if (!IsInLayerMask(other.gameObject, collisionLayers)) return;
        OnCameraTriggerStay?.Invoke(other);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsInLayerMask(other.gameObject, collisionLayers)) return;
        if (logCollisions) Debug.Log($"Camera exited trigger: {other.gameObject.name}");
        OnCameraTriggerExit?.Invoke(other);
    }

    // --- obstacle processing with per-object cooldown
    // Added bypassCooldown parameter to allow immediate repeated hits (useful when shield should be consumed and damage applied instantly)
    private void ProcessObstacleHitWithCooldown(GameObject obstacle, bool bypassCooldown = false)
    {
        int id = obstacle.GetInstanceID();
        float now = Time.realtimeSinceStartup;

        if (!bypassCooldown)
        {
            if (lastHitRealtime.TryGetValue(id, out float lastTime))
            {
                if (now - lastTime < perObjectHitCooldown)
                {
                    if (logCollisions) Debug.Log($"[CameraTracker] Ignored repeated hit on {obstacle.name} (cooldown).");
                    return;
                }
            }
        }

        lastHitRealtime[id] = now;
        HandleObstacleHit(obstacle);
    }

    private void HandleObstacleHit(GameObject obj)
    {
        if (logCollisions) Debug.Log($"[CameraTracker] PROCESS HIT (obstacle): {obj.name}");

        if (powerUpHandler == null)
            ResolvePowerUpHandler();

        // 1) If slow-time is active, IGNORE the hit but give immediate feedback (so player "se vede" hit-ul)
        if (IsSlowTimeActive())
        {
            if (logCollisions) Debug.Log("[CameraTracker] Slow-time active -> hit ignored (feedback shown).");
            ShowNotification("Hit ignored (slow-time).");
            // Optionally fire an event or play a sound here
            return;
        }

        // 2) If shield is active, consume/disable the shield and give feedback (do NOT decrease life)
        if (powerUpHandler != null && powerUpHandler.HasShield())
        {
            bool consumed = TryConsumeShieldViaReflection();
            if (logCollisions) Debug.Log($"[CameraTracker] Shield active -> {(consumed ? "consumed" : "could not be consumed (no method found)")}. Hit {(consumed ? "absorbed" : "NOT absorbed, falling through")} (feedback shown).");
            ShowNotification(consumed ? "Shield absorbed the hit!" : "Shield unavailable, hit applied!");
            if (consumed)
            {
                // important: do not continue to extra-life / death handling
                return;
            }
            // if we couldn't consume the shield (reflection failed), fall through and continue
            // to extra life / death handling so the hit is applied.
        }

        // 3) consume extra life if any (this preserves existing API if present)
        if (powerUpHandler != null && powerUpHandler.ConsumeExtraLife())
        {
            if (logCollisions) Debug.Log($"[CameraTracker] Extra life consumed. Remaining: {powerUpHandler.ExtraLives}");
            ShowNotification("Extra life used - you survived!");
            // instant handling, no invulnerability started
            return;
        }

        // 4) no protection -> death (instant)
        if (AudioManager.Instance != null) AudioManager.Instance.PlayDeathSound();
        string hitMessage = obj.CompareTag("RedSphere") ? "You were hit by a red sphere!" : "You hit an obstacle!";
        ShowNotification(hitMessage);

        OnRedSphereHit?.Invoke(obj);
        if (GameManager.Instance != null) GameManager.Instance.GameOver();
        else Debug.LogError("[CameraTracker] GameManager.Instance is null!");
    }

    // Try to consume the shield on the powerUpHandler using a few common method/property names via reflection.
    // Returns true if we successfully invoked something that looks like a "consume shield" action.
    private bool TryConsumeShieldViaReflection()
    {
        if (powerUpHandler == null) return false;

        var type = powerUpHandler.GetType();

        // common method names to try
        string[] methodNames = { "ConsumeShield", "UseShield", "DeactivateShield", "RemoveShield" };
        foreach (var mName in methodNames)
        {
            var m = type.GetMethod(mName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (m != null && m.GetParameters().Length == 0)
            {
                m.Invoke(powerUpHandler, null);
                return true;
            }
        }

        // fallback: look for a boolean property we can set to false
        string[] propNames = { "HasShieldActive", "ShieldActive", "HasShield" };
        foreach (var pName in propNames)
        {
            var p = type.GetProperty(pName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (p != null && p.PropertyType == typeof(bool) && p.CanWrite)
            {
                p.SetValue(powerUpHandler, false);
                return true;
            }
        }

        // last resort: look for integer shield count we can decrement
        string[] intNames = { "ShieldCount", "Shields" };
        foreach (var iName in intNames)
        {
            var pi = type.GetProperty(iName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (pi != null && (pi.PropertyType == typeof(int) || pi.PropertyType == typeof(byte)))
            {
                int current = (int)pi.GetValue(powerUpHandler);
                if (current > 0 && pi.CanWrite)
                {
                    pi.SetValue(powerUpHandler, current - 1);
                    return true;
                }
            }
        }

        return false;
    }

    // Detect if slow-time is active on the powerUpHandler (via common property names)
    private bool IsSlowTimeActive()
    {
        if (powerUpHandler == null) return false;

        var type = powerUpHandler.GetType();
        string[] propNames = { "HasSlowTimeActive", "SlowTimeActive", "IsSlowTimeActive", "HasSlowMotion", "SlowMotionActive" };
        foreach (var pName in propNames)
        {
            var p = type.GetProperty(pName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (p != null && p.PropertyType == typeof(bool))
            {
                return (bool)p.GetValue(powerUpHandler);
            }
        }

        // also try a method that reports slow state
        string[] methodNames = { "HasSlowTimeActive", "IsSlowTimeActive", "IsSlowMotionActive" };
        foreach (var mName in methodNames)
        {
            var m = type.GetMethod(mName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (m != null && m.ReturnType == typeof(bool) && m.GetParameters().Length == 0)
            {
                return (bool)m.Invoke(powerUpHandler, null);
            }
        }

        return false;
    }

    private IEnumerator StartInvulnerability()
    {
        isInvulnerable = true;
        if (logCollisions) Debug.Log($"[CameraTracker] Invulnerability ON for {invulnerabilityDuration} s");
        yield return new WaitForSecondsRealtime(invulnerabilityDuration);
        isInvulnerable = false;
        if (logCollisions) Debug.Log("[CameraTracker] Invulnerability OFF");
    }

    private IEnumerator RecoverAndIgnoreOverlap()
    {
        // preserve slow-time if active
        bool slowActive = false;
        if (powerUpHandler != null)
        {
            var prop = powerUpHandler.GetType().GetProperty("HasSlowTimeActive");
            if (prop != null && prop.PropertyType == typeof(bool))
                slowActive = (bool)prop.GetValue(powerUpHandler);
        }

        if (!slowActive && Time.timeScale != 1f)
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
            if (logCollisions) Debug.Log("[CameraTracker] Forced Time.timeScale = 1 to avoid freeze.");
        }

#if UNITY_EDITOR
        if (EditorApplication.isPaused)
        {
            EditorApplication.isPaused = false;
            if (logCollisions) Debug.Log("[CameraTracker] EditorApplication.isPaused = false");
        }
#endif

        var spawner = FindObjectOfType<SphereSpawner>();
        if (spawner != null && !spawner.enabled)
        {
            spawner.enabled = true;
            if (logCollisions) Debug.Log("[CameraTracker] Re-enabled SphereSpawner.");
        }

        var map = FindObjectOfType<VRMapController>();
        if (map != null && !((Behaviour)map).enabled)
        {
            ((Behaviour)map).enabled = true;
            if (logCollisions) Debug.Log("[CameraTracker] Enabled VRMapController component.");
        }

        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false;
            yield return new WaitForSecondsRealtime(0.25f);
            col.enabled = true;
            if (logCollisions) Debug.Log("[CameraTracker] Collider re-enabled after brief ignore window.");
        }
        else
        {
            yield return null;
        }
    }

    private void ShowNotification(string message)
    {
        Debug.LogWarning("*** " + message + " ***");
    }

    // decide if a GameObject should be treated as obstacle
    private bool IsObstacleCandidate(GameObject obj)
    {
        if (obj == null) return false;

        // 1) explicit tag match
        foreach (string t in obstacleTags)
            if (!string.IsNullOrEmpty(t) && obj.CompareTag(t)) return true;

        // 2) layer match against obstacleLayers
        if (obstacleLayers != 0 && ((1 << obj.layer) & obstacleLayers) != 0) return true;

        // 3) treat all collisions as obstacles (option)
        if (treatAllCollisionsAsObstacles) return true;

        // 4) heuristic checks: SphereCollider => likely a sphere; name contains "wall"/"sphere"
        if (obj.GetComponent<SphereCollider>() != null) return true;

        string lower = obj.name.ToLower();
        if (lower.Contains("wall") || lower.Contains("sphere") || lower.Contains("obstacle")) return true;

        return false;
    }

    private void ResolvePowerUpHandler()
    {
        powerUpHandler = GetComponent<XRCharacterPowerUpHandler>()
                       ?? GetComponentInParent<XRCharacterPowerUpHandler>()
                       ?? GetComponentInChildren<XRCharacterPowerUpHandler>()
                       ?? FindObjectOfType<XRCharacterPowerUpHandler>();
    }

    private bool IsInLayerMask(GameObject obj, LayerMask layerMask)
    {
        return ((1 << obj.layer) & layerMask) != 0;
    }

    public int GetCurrentCollisionCount() => currentCollisionCount;

    public bool IsObstacle(Collider collider)
    {
        return IsObstacleCandidate(collider.gameObject);
    }

    public bool IsObstacle(GameObject obj)
    {
        return IsObstacleCandidate(obj);
    }
}