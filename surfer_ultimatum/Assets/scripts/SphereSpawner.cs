using UnityEngine;
using System.Threading;

public class SphereSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject[] prefabs;
    [SerializeField] private float spawnInterval = 2f;
    [SerializeField] private int maxSpheres = 10;
    [SerializeField] private float startDelay = 0f;

    [Header("Initial probabilistic distribution")]
    [SerializeField] private int obstacle = 2;
    [SerializeField] private int first_powerup = 4;
    [SerializeField] private int second_powerup = 4;

    [Header("Distance scaling")]
    [SerializeField] private float metersStep = 100f;
    [SerializeField] private float distanceForObstacle4 = 550f;
    [SerializeField] private float distanceForObstacle6 = 1000f;
    [SerializeField] private VRMapController mapController;

    [Header("Spawn Area")]
    [SerializeField] private Vector3 spawnAreaSize = new Vector3(10f, 2f, 10f);
    [SerializeField] private bool spawnAtRandomHeight = false;

    [Header("Movement")]
    [SerializeField] private float sphereSpeed = 5f;
    [SerializeField] private bool moveTowardsPlayer = true;
    [SerializeField] private Vector3 fixedDirection = Vector3.forward;

    [Header("Sphere Properties")]
    [SerializeField] private float sphereSize = 0.5f;
    [SerializeField] private Color sphereColor = Color.red;
    [SerializeField] private float sphereLifetime = 10f;

    private Transform playerTransform;
    private Camera xrCamera;
    private float nextSpawnTime;
    private int currentSphereCount = 0;

    private int lastStepIndex = 0;

    void Start()
    {
        if (obstacle + first_powerup + second_powerup != 10)
        {
            Debug.LogError("Inserted probabilities don t add up to 100 ! (ex: 1,6,3)");
        }

        GameObject xrOrigin = GameObject.Find("XR Origin (XR Rig)");
        if (xrOrigin != null)
        {
            playerTransform = xrOrigin.transform;

            xrCamera = xrOrigin.GetComponentInChildren<Camera>();
            if (xrCamera != null)
            {
                Debug.Log("Found XR Camera: " + xrCamera.name);
            }
            else
            {
                Debug.LogWarning("XR Origin found but no camera detected!");
            }
        }
        else
        {
            Debug.LogError("Could not find XR Origin! Make sure it's named 'XR Origin (XR Rig)'");
        }

        if (mapController == null)
        {
            mapController = FindObjectOfType<VRMapController>();
        }

        if (mapController != null && metersStep > 0f)
        {
            lastStepIndex = Mathf.FloorToInt(mapController.distanceTraveled / metersStep);
            ApplyWeightsAtDistance(lastStepIndex * metersStep);
        }

        nextSpawnTime = Time.time + spawnInterval + startDelay;
    }

    public void ResetSpawner()
    {
        if (this.GetType().GetField("timer") != null)
        {
            this.GetType().GetField("timer").SetValue(this, 0f);
        }

        this.enabled = true;

        if (mapController != null && metersStep > 0f)
        {
            lastStepIndex = Mathf.FloorToInt(mapController.distanceTraveled / metersStep);
            ApplyWeightsAtDistance(lastStepIndex * metersStep);
        }

        Debug.Log("SphereSpawner reset.");
    }

    void Update()
    {
        if (mapController != null && metersStep > 0f)
        {
            int stepIndex = Mathf.FloorToInt(mapController.distanceTraveled / metersStep);
            while (stepIndex > lastStepIndex)
            {
                lastStepIndex++;
                ApplyWeightsAtDistance(lastStepIndex * metersStep);
            }
        }

        if (Time.time >= nextSpawnTime && currentSphereCount < maxSpheres)
        {
            SpawnSphere();
            nextSpawnTime = Time.time + spawnInterval;
        }
    }

    void ApplyWeightsAtDistance(float d)
    {
        int targetObstacle = 2;

        if (d >= distanceForObstacle6)
        {
            targetObstacle = 6;
        }
        else if (d >= distanceForObstacle4)
        {
            targetObstacle = 4;
        }

        if (targetObstacle != obstacle)
        {
            obstacle = targetObstacle;
            int p = (10 - obstacle) / 2;
            first_powerup = p;
            second_powerup = p;

            Debug.Log($"Distance {d:F0}m -> Weights: obstacle={obstacle}, powerup1={first_powerup}, powerup2={second_powerup} (total={obstacle + first_powerup + second_powerup})");
        }
    }

    void SpawnSphere()
    {
        Vector3 randomOffset = new Vector3(
            Random.Range(-spawnAreaSize.x / 2, spawnAreaSize.x / 2),
            spawnAtRandomHeight ? Random.Range(-spawnAreaSize.y / 2, spawnAreaSize.y / 2) : 0,
            Random.Range(-spawnAreaSize.z / 2, spawnAreaSize.z / 2)
        );

        Vector3 spawnPosition = transform.position + randomOffset;

        GameObject sphere;
        if (prefabs != null && prefabs.Length > 0)
        {
            int total = obstacle + first_powerup + second_powerup;
            if (total <= 0) total = 1;

            int prob = Random.Range(0, total);
            int rand;

            if (prob < obstacle)
            {
                rand = 0;
            }
            else if (prob < obstacle + first_powerup)
            {
                rand = 1;
            }
            else
            {
                rand = 2;
            }

            rand = Mathf.Clamp(rand, 0, prefabs.Length - 1);
            sphere = Instantiate(prefabs[rand], spawnPosition, Quaternion.identity);
        }
        else
        {
            sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.position = spawnPosition;
            sphere.transform.localScale = Vector3.one * sphereSize;

            Renderer renderer = sphere.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = sphereColor;
            }
        }

        sphere.name = "Moving Sphere";

        MovingSphere movingSphere = sphere.AddComponent<MovingSphere>();
        movingSphere.speed = sphereSpeed;
        movingSphere.lifetime = sphereLifetime;
        movingSphere.spawner = this;

        if (moveTowardsPlayer)
        {
            Transform targetTransform = xrCamera != null ? xrCamera.transform : playerTransform;

            if (targetTransform != null)
            {
                Vector3 directionToPlayer = (targetTransform.position - spawnPosition).normalized;
                movingSphere.direction = directionToPlayer;
            }
            else
            {
                movingSphere.direction = fixedDirection.normalized;
            }
        }
        else
        {
            movingSphere.direction = fixedDirection.normalized;
        }

        currentSphereCount++;
    }

    public void OnSphereDestroyed()
    {
        currentSphereCount--;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, spawnAreaSize);
    }
}