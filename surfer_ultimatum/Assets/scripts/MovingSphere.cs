using UnityEngine;

public class MovingSphere : MonoBehaviour
{
    [HideInInspector] public Vector3 direction;
    [HideInInspector] public float speed;
    [HideInInspector] public float lifetime;
    [HideInInspector] public SphereSpawner spawner;

    [Header("Movement / Difficulty")]
    public float baseSpeed = 2f;
    public float accelerationPerSecond = 0.1f;
    public float initialSpeedBoost = 0.5f;
    public bool updateDirectionEachFrame = true;
    public string playerTag = "Player";

    [Header("Lane (preserve)")]
    public bool preserveLane = true;

    // distanța la care considerăm că "ajunge" la player (înainte de a decide dacă a fost colectat)
    public float reachDistance = 0.6f;
    // distanța în spatele player-ului unde sfera va continua drumul (nu mai urmărește jucătorul)
    public float passThroughDistance = 6f;

    private float spawnTime;
    private bool hasCollided = false;
    private Transform target;
    private float laneX;
    private float laneY;

    // stare: a trecut deja de jucător?
    private bool passedPlayer = false;
    // țintă fixă după ce s-a trecut de player
    private Vector3 exitTarget;

    void Start()
    {
        spawnTime = Time.time;

        if (Camera.main != null) target = Camera.main.transform;
        else
        {
            GameObject t = GameObject.FindGameObjectWithTag(playerTag);
            if (t != null) target = t.transform;
        }

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true;

        var collider = GetComponent<SphereCollider>();
        if (collider == null) collider = gameObject.AddComponent<SphereCollider>();
        collider.isTrigger = true; // IMPORTANT: trigger pentru a nu bloca fizic player-ul

        laneX = transform.position.x;
        laneY = transform.position.y;

        if (target != null)
            direction = (target.position - transform.position).normalized;

        if (speed <= 0f) speed = baseSpeed + initialSpeedBoost;
    }

    void Update()
    {
        if (target == null) return;

        float elapsed = Time.timeSinceLevelLoad;
        float currentSpeed = speed + accelerationPerSecond * elapsed;

        if (!passedPlayer)
        {
            // ținta curentă: pe banda inițială dar la Z jucătorului (atenție: doar până trece sfera)
            Vector3 targetPos = preserveLane
                ? new Vector3(laneX, laneY, target.position.z)
                : target.position;

            // mișcare cursivă către targetPos (MoveTowards evită overshoot)
            transform.position = Vector3.MoveTowards(transform.position, targetPos, currentSpeed * Time.deltaTime);

            // dacă sfera a trecut de jucător pe axa Z => considerăm că a trecut și o trimitem mai departe
            // presupunem că "înainte" pentru sferă în scenă este Z în scădere (dacă scena ta folosește semn opus,
            // inversează comparatia de mai jos)
            if (transform.position.z <= target.position.z)
            {
                passedPlayer = true;
                // fixează exit target în spatele jucătorului la un offset fix (nu mai urmărim jucătorul)
                float exitZ = target.position.z - Mathf.Abs(passThroughDistance);
                exitTarget = preserveLane
                    ? new Vector3(laneX, laneY, exitZ)
                    : new Vector3(target.position.x, target.position.y, exitZ);
            }

            // dacă s-a apropiat suficient (colectat) => tratează colectarea
            float distToPlayer = Vector3.Distance(transform.position, target.position);
            if (distToPlayer <= reachDistance)
            {
                // dacă player-ul a intrat în trigger-ul nostru, OnTriggerEnter ar trebui să gestioneze (preferat).
                // aici tratăm fallback în caz că nu s-a declanșat triggerul: considerăm că e colectată.
                HandleCollected();
                return;
            }
        }
        else
        {
            // sfera a trecut de jucător => mergem către exitTarget fix (nu îl mai urmăm)
            transform.position = Vector3.MoveTowards(transform.position, exitTarget, currentSpeed * Time.deltaTime);

            // când ajunge la exitTarget, distrugem-o (sau putem reutiliza/despawn)
            if (Vector3.Distance(transform.position, exitTarget) < 0.1f)
            {
                DestroySphere();
                return;
            }
        }

        // actualizăm direcția opțional
        if (updateDirectionEachFrame)
        {
            // target pentru calcule (folosim exitTarget dacă passedPlayer)
            Vector3 calcTarget = passedPlayer ? exitTarget : (preserveLane ? new Vector3(laneX, laneY, target.position.z) : target.position);
            if ((calcTarget - transform.position).sqrMagnitude > 0.0001f)
                direction = (calcTarget - transform.position).normalized;
        }

        // expirare normală
        if (Time.time - spawnTime > lifetime)
        {
            DestroySphere();
        }
    }

    private void HandleCollected()
    {
        if (hasCollided) return;
        hasCollided = true;
        if (spawner != null) spawner.OnSphereDestroyed();
        // Aici a fost colectată: poți rula efecte (sunet, particule).
        Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasCollided) return;

        // Powerup/shield logic (păstrează ce ai)
        XRCharacterPowerUpHandler powerUpHandler = other.GetComponent<XRCharacterPowerUpHandler>();
        if (powerUpHandler != null && powerUpHandler.HasShield())
        {
            hasCollided = true;
            DestroySphere();
            return;
        }

        XRCameraCollisionTracker cameraTracker = other.GetComponent<XRCameraCollisionTracker>();
        if (cameraTracker != null)
        {
            hasCollided = true;
            DestroySphere();
            return;
        }

        if (other.GetComponent<CharacterController>() != null)
        {
            hasCollided = true;
            DestroySphere();
        }
    }

    private void DestroySphere()
    {
        if (spawner != null) spawner.OnSphereDestroyed();
        Destroy(gameObject);
    }
}