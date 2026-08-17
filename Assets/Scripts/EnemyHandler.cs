using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Audio;
using UnityEngine.UI;

public class EnemyHandler : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public Transform[] waypoints;
    public float velocityDampening = 100f;

    [SerializeField] private NavMeshAgent navMeshAgent;
    [SerializeField] private ParticleSystem enemyDeathEffectPrefab;
    [SerializeField] private ParticleSystem smashEffect;
    [SerializeField] private GameObject nonModelContents; // All contents of the enemy that aren't the enemy (e.g., Particles or Health Ui)
    [SerializeField] private ParticleSystem hit1; // Particle effect if enemy gets hit
    [SerializeField] private ParticleSystem hit2; // Particle effect if enemy gets hit
    [SerializeField] private ParticleSystem hit3; // Particle effect if enemy gets hit
    [SerializeField] private ParticleSystem hit4; // Particle effect if enemy gets hit
    [SerializeField] private ParticleSystem hit6; // Particle effect if enemy gets hit
    [SerializeField] private ParticleSystem hit7;
    [SerializeField] private LayerMask playerMask;
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private LayerMask rayMask;
    [SerializeField] private Collider attackHitbox;
    [SerializeField] private RawImage healthBar;
    [SerializeField] private float health;
    [SerializeField] private Animator animator;

    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip attackAudio1;
    [SerializeField] private AudioClip attackAudio2;

    private Rigidbody rigidBody;
    private float baseHealthBarScaleX;

    private const float startWaitTime = 2;
    private const float rotateTime = 0.2f;
    private const float walkSpeed = 5;
    private const float runSpeed = 10;
    private const float viewRadius = 14;
    private const float viewAngle = 85;
    private const float meshResolution = 1f;
    private const int edgeIterations = 4;
    private const float edgeDistance = 0.5f;
    private const float chaseDefaultWait = 2;
    private const float attackDefaultWait = 2f;

    private int currentWayPointIndex;

    Vector3 playerLastPosition = Vector3.zero; // Last position of player
    Vector3 playerPosition;

    Vector3 stunDirection = Vector3.zero;
    private float stun = 0f;
    private float attackDebounce = 0; // Used for both transition to attack state and transition out
    private float waitTime; // In patrol determines speed of wait between points, in chase it determines the amount of time player is out of reach
    private float timeToRotate;
    private bool playerInBounds;
    private bool playerNear;
    private string[] states = { "Patrol", "Chasing", "Attacking", "Dead", "Stun" }; // All possible enemy states
    private string currentState = "Patrol"; // Current state of the enemy - state list: "Patrol", "Chasing", "Attacking", "Dead", "Stun"

    void Start()
    {
        navMeshAgent.updateRotation = false;
        playerPosition = Vector3.zero;
        playerInBounds = false;
        waitTime = startWaitTime;
        timeToRotate = rotateTime;

        currentWayPointIndex = 0;
        navMeshAgent = GetComponent<NavMeshAgent>();
        rigidBody = GetComponent<Rigidbody>();

        navMeshAgent.isStopped = false;
        navMeshAgent.speed = walkSpeed;
        navMeshAgent.SetDestination(waypoints[currentWayPointIndex].position);
        navMeshAgent.stoppingDistance = 2.35f;

        baseHealthBarScaleX = healthBar.rectTransform.sizeDelta.x / health;
    }

    public bool HealthChanger(float num, bool altEffects, Vector3 direction, float stunLength = 0.2f) // Public method for setting health
    {
        if (health > 0)
        {
            health = math.clamp(health += num, 0, 500);
            hit3.Emit(1);
            if (direction != Vector3.zero)
            {
                stun = stunLength;
                attackDebounce = attackDefaultWait;
                currentState = "Stun";
                navMeshAgent.enabled = false;
                rigidBody.linearVelocity = direction;
                //rigidBody.linearDamping = 3f;
                velocityDampening = 30f;
            }
            if (altEffects == true)
            {
                hit1.Emit(1);
                hit7.Emit(1);
                hit4.Emit(4);
            }
            else
            {
                hit1.Emit(1);
                hit2.Emit(1);
                hit4.Emit(2);
                hit6.Emit(1);
            }
            if (health <= 0)
            {
                ParticleSystem deathParticle = Instantiate(enemyDeathEffectPrefab, transform.position, transform.rotation);
                deathParticle.Emit(1);
                Destroy(deathParticle, 0.5f);
                Destroy(transform.parent.gameObject, 0.05f); // Destroy parent because it contains other things like enemy paths, etc. We don't need that when the enemy is dead lol
                currentState = "Dead";
            }
            float healthBarFormula = baseHealthBarScaleX * health;
            healthBar.rectTransform.sizeDelta = new Vector2(healthBarFormula, healthBar.rectTransform.sizeDelta.y);
            return true;
        }
        else
        {
            return false;
        }
    }

    public void Hitbox(Collider collider) // Collision ONLY for attack hitboxes! Not regular collision
    {
        smashEffect.Emit(1);
        audioSource.PlayOneShot(attackAudio2, 0.4f);
        Collider[] colliderOverlap = Physics.OverlapBox(collider.bounds.center, collider.bounds.extents, collider.transform.rotation, LayerMask.GetMask("Player"));
        foreach (Collider c in colliderOverlap)
        {
            if (c.tag == "Player")
            {
                PlayerController playerController = c.transform.parent.gameObject.GetComponent<PlayerController>();
                playerController.HealthChange(-25, "Add", transform.position);
            }
        }
    }

    float Decelerate(float velocityOnAxis, float decelRate) // Self explanatory - decelerates a specific vector passed as an argument like vector3.x -- copied over from my player script because we want to replace lineardamping because it actually sucks!!!!!!!!! UNITY IT IS STUPIDLY EASY FOR YOU TO MAKE DAMPENING ON AXIS AN OPTION!!!!!!!!!
    {
        if (velocityOnAxis < 0) // if we are decelerating up to zero like velocity = -4, we want to add to get closer to 0.
        {
            float velocityMinusDecel = velocityOnAxis + decelRate;
            if (velocityMinusDecel > 0) // If velocity - deceleration is less than 0 we set it to zero to prevent decelerating past 0
            {
                return 0;
            }
            return velocityMinusDecel; // Otherwise decelerated velocity will be returned
        }
        else if (velocityOnAxis > 0) // if we are decelerating down to zero e.g., velocity = 4, we want to subtract to get closer to 0.
        {
            float velocityMinusDecel = velocityOnAxis - decelRate;
            if (velocityMinusDecel < 0) // If velocity + deceleration is greater than 0 we set it to zero to prevent decelerating past 0
            {
                return 0;
            }
            return velocityMinusDecel; // Otherwise, decelerated velocity will be returned
        }
        else
        {
            return velocityOnAxis;
        }
    }

    // Update is called once per frame
    void Update()
    {
        bool raycastCollided = true;
        rigidBody.linearVelocity = new Vector3(Decelerate(rigidBody.linearVelocity.x, velocityDampening * Time.deltaTime), rigidBody.linearVelocity.y, Decelerate(rigidBody.linearVelocity.z, velocityDampening * Time.deltaTime));

        if (stun <= 0f && currentState == "Stun")
        {
            RaycastHit hit;
            raycastCollided = Physics.Raycast(transform.position, Vector3.down, out hit, 1.5f * 0.75f, rayMask); // Raycast at ground
            if (raycastCollided == true)
            {
                navMeshAgent.enabled = true;
                velocityDampening = 100f;
                currentState = "Chase";
            }


        }
        else if (stun > 0 && currentState == "Stun")
        {
            stun = Mathf.Clamp(stun - Time.deltaTime, 0f, 10f);
            if (raycastCollided == false)
            {
                rigidBody.linearVelocity = new Vector3(rigidBody.linearVelocity.x, rigidBody.linearVelocity.y - (25f * Time.deltaTime), rigidBody.linearVelocity.z);
            }
        }
        FaceTarget();
        switch (currentState) // Run code depending on what state enemy is in
        {
            case "Patrol":
                EnvironmentView();
                Patroling();
                break;
            case "Chase":
                EnvironmentView();
                Chasing();
                break;
            case "Attack":
                EnvironmentView();
                attackDebounce -= Time.deltaTime;
                if (attackDebounce <= 0)
                {
                    waitTime = chaseDefaultWait;
                    attackDebounce = attackDefaultWait;
                    currentState = "Chase";
                }
                break;
            default:
                break;
        }
    }
    private void FaceTarget()
    {
        var turnTowardNavSteeringTarget = navMeshAgent.steeringTarget;
        Vector3 direction = (turnTowardNavSteeringTarget - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5);
    }



    private void Chasing() // Enemy is in the chase state
    {
        playerNear = false;
        playerLastPosition = Vector3.zero;
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        float distFromPlayer = Vector3.Distance(transform.position, player.transform.position);
        navMeshAgent.SetDestination(player.transform.position);
        if (navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance) // If player is in range and in range for long enough to attack
        {
            if(navMeshAgent.speed != 0)
            {
               Stop();
            }
            attackDebounce -= Time.deltaTime * 1f;
            if (attackDebounce <= 0)
            {
                animator.Play("SlimeAttack");
                //Hitbox(attackHitbox);
                audioSource.PlayOneShot(attackAudio1, 0.2f);
                attackDebounce = attackDefaultWait;
                currentState = "Attack";
            }
        }
        else
        {
            print("Outofrange");
            attackDebounce = 0.5f; // Reset attack timer because player isn't close enough
            Move(runSpeed);
            if (distFromPlayer >= 5f)
            {
                waitTime -= Time.deltaTime * 2;
            }
        }
        if (waitTime <= 0)
        {
            currentState = "Patrol";
            playerNear = false;
            Move(walkSpeed);
            timeToRotate = rotateTime;
            waitTime = startWaitTime;
            navMeshAgent.SetDestination(waypoints[currentWayPointIndex].position);
        }
    }

    private void Patroling() // Method for patrol state, move between patrol points while searching for player
    {
        if (playerNear)
        {
            if (timeToRotate <= 0)
            {
                Move(walkSpeed);
                LookingPlayer(playerLastPosition);
            }
            else
            {
                Stop();
                timeToRotate -= Time.deltaTime;
            }
        }
        else
        {
            playerNear = false;
            playerLastPosition = Vector3.zero;
            navMeshAgent.SetDestination(waypoints[currentWayPointIndex].position);
            if (navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance)
            {
                if (waitTime <= 0)
                {
                    NextPoint(); // Move to next waypoint
                    Move(walkSpeed); // Move at default walkspeed
                    waitTime = startWaitTime;
                }
                else
                {
                    Stop();
                    waitTime -= Time.deltaTime;
                }
            }
        }
    }

    void Move(float speed) // Move at specified speed
    {
        navMeshAgent.isStopped = false;
        navMeshAgent.speed = speed;

    }

    void Stop() // Stop enemy
    {
        navMeshAgent.isStopped = true;
        navMeshAgent.speed = 0;
        animator.CrossFade("Empty", 0.05f, 0);
    }

    public void NextPoint() // Go to next of the enemy patrol waypoints
    {
        currentWayPointIndex = (currentWayPointIndex + 1) % waypoints.Length;
        navMeshAgent.SetDestination(waypoints[currentWayPointIndex].position); // Set out destination to next waypoint
    }

    void LookingPlayer(Vector3 player)
    {
        navMeshAgent.SetDestination(player);
        if (Vector3.Distance(transform.position, player) <= 0.3f) // If player is close
        {
            if (waitTime <= 0)
            {
                playerNear = false;
                Move(walkSpeed);
                navMeshAgent.SetDestination(waypoints[currentWayPointIndex].position);
                waitTime = startWaitTime;
                timeToRotate = rotateTime;
            }
            else
            {
                Stop();
                waitTime -= Time.deltaTime;
            }
        }
    }

    void EnvironmentView()
    {
        Collider[] playerInRange = Physics.OverlapSphere(transform.position, viewRadius, playerMask); // Check in bounds of sphere arround enemy
        for (int i = 0; i < playerInRange.Length; i++)
        {
            Transform player = playerInRange[i].transform;
            Vector3 dirToPlayer = (player.position - transform.position).normalized;
            if (Vector3.Angle(transform.forward, dirToPlayer) < viewAngle / 2) //Check if enemy should spot player based on view angle
            {
                float distToPlayer = Vector3.Distance(transform.position, player.position); // Get distance between player and enemy pos
                if (!Physics.Raycast(transform.position, dirToPlayer, distToPlayer, obstacleMask)) // Raycast at player if they are within view angle
                {
                    playerInBounds = true; // If raycast hit, player is in bounds
                    if (currentState != "Chase")
                    {
                        waitTime = chaseDefaultWait;
                        attackDebounce = 0f;
                        currentState = "Chase"; // Set enemy state to chase player
                    }
                }
                else
                {
                    playerInBounds = false;
                }
            }
            if (Vector3.Distance(transform.position, player.position) > viewRadius) // If player is too far away, they are not in bounds of enemy
            {
                playerInBounds = false;
            }
            if (playerInBounds)
            {
                playerPosition = player.transform.position;
            }
        }
    }
}
