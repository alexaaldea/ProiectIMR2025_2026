using UnityEngine;

public class SphereSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject[] prefabs;
    [SerializeField] private float spawnInterval = 2f;
    [SerializeField] private int maxSpheres = 10;

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

    void Start()
    {
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

        nextSpawnTime = Time.time + spawnInterval;
    }

    void Update()
    {
        if (Time.time >= nextSpawnTime && currentSphereCount < maxSpheres)
        {
            SpawnSphere();
            nextSpawnTime = Time.time + spawnInterval;
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
        if (prefabs != null && prefabs.Length > 0) // Fixed: added null check
        {
            int rand = Random.Range(0, prefabs.Length); // Fixed: was prefabs.Length-1, should be prefabs.Length
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

        // Modified: Prioritize moving towards XR camera if available, otherwise XR Origin
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