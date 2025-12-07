using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI;

public enum CitizenState { Wander, Group, Alert, Evade, Watch }

public class CitizenNPC : MonoBehaviour
{
    [Header("State Settings")]
    public CitizenState currentState = CitizenState.Wander;
    public Aspect.Affiliation affiliation = Aspect.Affiliation.Civilian;

    [Header("Movement")]
    public float wanderSpeed = 2.0f;
    public float groupSpeed = 2.5f;
    public float evadeSpeed = 4.0f;
    public float rotationSpeed = 3.0f;
    public float minWanderDistance = 5f;
    public float maxWanderDistance = 15f;

    [Header("Group Behavior")]
    public float groupRadius = 10f;
    public float separationDistance = 2f;
    public float cohesionWeight = 1f;
    public float separationWeight = 1.5f;
    public float alignmentWeight = 1f;
    public List<CitizenNPC> nearbyCitizens = new List<CitizenNPC>();

    [Header("Alert System")]
    public float alertRadius = 20f;
    public float watchTime = 5f;
    private float alertTimer = 0f;
    public Material alertMaterial;
    public Material normalMaterial;
    private Renderer npcRenderer;

    [Header("Sensors")]
    public Sight sightSensor;
    public Touch touchSensor;
    private bool playerDetected = false;
    private bool patrolDetected = false;

    [Header("References")]
    private NavMeshAgent agent;
    private Animator animator;
    private Vector3 wanderTarget;
    private Vector3 evadeDirection;
    private Transform playerTransform;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        npcRenderer = GetComponent<Renderer>();
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;

        // Initialize sensors
        InitializeSensors();

        // Set initial wander target
        GetNewWanderTarget();

        // Set up aspect component for detection
        Aspect aspect = gameObject.AddComponent<Aspect>();
        aspect.affiliation = affiliation;
    }

    void InitializeSensors()
    {
        // Add and configure Sight sensor
        sightSensor = gameObject.AddComponent<Sight>();
        sightSensor.targetAffiliation = Aspect.Affiliation.Player;
        sightSensor.detectionRate = 0.5f;
        sightSensor.FieldOfView = 60;
        sightSensor.ViewDistance = 15;

        // Add and configure Touch sensor
        touchSensor = gameObject.AddComponent<Touch>();
        touchSensor.targetAffiliation = Aspect.Affiliation.Enemy;

        // Add SphereCollider for touch detection
        SphereCollider touchCollider = gameObject.AddComponent<SphereCollider>();
        touchCollider.radius = 2f;
        touchCollider.isTrigger = true;
    }

    void Update()
    {
        // Update sensor detection
        UpdateSensors();

        // Update nearby citizens list
        UpdateNearbyCitizens();

        // State machine
        switch (currentState)
        {
            case CitizenState.Wander:
                WanderBehavior();
                CheckForThreats();
                break;

            case CitizenState.Group:
                GroupBehavior();
                CheckForThreats();
                break;

            case CitizenState.Alert:
                AlertBehavior();
                break;

            case CitizenState.Evade:
                EvadeBehavior();
                break;

            case CitizenState.Watch:
                WatchBehavior();
                break;
        }

        UpdateAnimator();
        UpdateVisuals();
    }

    void UpdateSensors()
    {
        // Sensor updates are handled by the components themselves
        // We just check their effects
    }

    void UpdateNearbyCitizens()
    {
        nearbyCitizens.Clear();
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, groupRadius);

        foreach (Collider col in hitColliders)
        {
            CitizenNPC citizen = col.GetComponent<CitizenNPC>();
            if (citizen != null && citizen != this && citizen.affiliation == Aspect.Affiliation.Civilian)
            {
                nearbyCitizens.Add(citizen);
            }
        }

        // Transition to Group state if near other civilians
        if (nearbyCitizens.Count >= 2 && currentState == CitizenState.Wander)
        {
            currentState = CitizenState.Group;
        }
        else if (nearbyCitizens.Count == 0 && currentState == CitizenState.Group)
        {
            currentState = CitizenState.Wander;
        }
    }

    void WanderBehavior()
    {
        // Simple wandering with obstacle avoidance
        agent.speed = wanderSpeed;

        // Check if reached target
        if (Vector3.Distance(transform.position, wanderTarget) < 1f ||
            !agent.hasPath || agent.pathStatus != NavMeshPathStatus.PathComplete)
        {
            GetNewWanderTarget();
        }

        // Move towards wander target
        agent.SetDestination(wanderTarget);

        // Random chance to change direction
        if (Random.value < 0.01f)
        {
            GetNewWanderTarget();
        }
    }

    void GroupBehavior()
    {
        agent.speed = groupSpeed;

        // Flocking behavior: Cohesion, Separation, Alignment
        Vector3 flockDirection = Vector3.zero;

        if (nearbyCitizens.Count > 0)
        {
            Vector3 centerOfMass = Vector3.zero;
            Vector3 separationForce = Vector3.zero;
            Vector3 averageHeading = Vector3.zero;
            int tooCloseCount = 0;

            foreach (CitizenNPC citizen in nearbyCitizens)
            {
                centerOfMass += citizen.transform.position;
                averageHeading += citizen.transform.forward;

                // Separation
                float distance = Vector3.Distance(transform.position, citizen.transform.position);
                if (distance < separationDistance && distance > 0)
                {
                    separationForce += (transform.position - citizen.transform.position).normalized / distance;
                    tooCloseCount++;
                }
            }

            centerOfMass /= nearbyCitizens.Count;
            averageHeading /= nearbyCitizens.Count;

            // Cohesion: Move toward center of mass
            Vector3 cohesionForce = (centerOfMass - transform.position).normalized * cohesionWeight;

            // Alignment: Match average heading
            Vector3 alignmentForce = averageHeading.normalized * alignmentWeight;

            // Separation: Avoid crowding
            if (tooCloseCount > 0)
            {
                separationForce = separationForce.normalized * separationWeight;
            }

            flockDirection = (cohesionForce + separationForce + alignmentForce).normalized;

            // Combine flocking with wandering
            Vector3 groupTarget = transform.position + flockDirection * 5f;
            groupTarget = GetValidNavMeshPosition(groupTarget);
            agent.SetDestination(groupTarget);
        }
        else
        {
            // Fall back to wandering
            WanderBehavior();
        }
    }

    void AlertBehavior()
    {
        // Stop moving and look around
        agent.isStopped = true;
        alertTimer += Time.deltaTime;

        // Slowly rotate to look around
        transform.Rotate(0, 45 * Time.deltaTime, 0);

        // Return to previous state after watch time
        if (alertTimer >= watchTime)
        {
            alertTimer = 0f;
            if (nearbyCitizens.Count > 0)
                currentState = CitizenState.Group;
            else
                currentState = CitizenState.Wander;
        }
    }

    void EvadeBehavior()
    {
        agent.speed = evadeSpeed;
        agent.isStopped = false;

        // Calculate evade direction away from threat
        Vector3 threatPosition = GetNearestThreatPosition();
        if (threatPosition != Vector3.zero)
        {
            evadeDirection = (transform.position - threatPosition).normalized;
            Vector3 evadeTarget = transform.position + evadeDirection * 10f;
            evadeTarget = GetValidNavMeshPosition(evadeTarget);
            agent.SetDestination(evadeTarget);
        }

        // Return to Wander after escaping
        if (Vector3.Distance(transform.position, threatPosition) > alertRadius * 1.5f)
        {
            currentState = CitizenState.Wander;
        }
    }

    void WatchBehavior()
    {
        // Watch patrol droids from a safe distance
        agent.isStopped = true;

        // Look at the patrol droids
        Vector3 threatPosition = GetNearestThreatPosition();
        if (threatPosition != Vector3.zero)
        {
            Vector3 lookDirection = (threatPosition - transform.position).normalized;
            lookDirection.y = 0;
            if (lookDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }

        // Stay in watch mode for a while, then go to alert
        alertTimer += Time.deltaTime;
        if (alertTimer >= watchTime / 2)
        {
            alertTimer = 0f;
            currentState = CitizenState.Alert;
        }
    }

    void CheckForThreats()
    {
        // Check for patrol droids (Enemy affiliation)
        Collider[] threats = Physics.OverlapSphere(transform.position, alertRadius);
        bool patrolNearby = false;

        foreach (Collider col in threats)
        {
            Aspect aspect = col.GetComponent<Aspect>();
            if (aspect != null && aspect.affiliation == Aspect.Affiliation.Enemy)
            {
                patrolNearby = true;
                break;
            }
        }

        // State transitions based on threats
        if (patrolNearby)
        {
            if (currentState != CitizenState.Evade && currentState != CitizenState.Watch)
            {
                // If patrol is far, watch them. If close, evade.
                float distanceToThreat = Vector3.Distance(transform.position, GetNearestThreatPosition());
                if (distanceToThreat > 10f)
                {
                    currentState = CitizenState.Watch;
                }
                else
                {
                    currentState = CitizenState.Evade;
                }
                alertTimer = 0f;
            }
        }
    }

    Vector3 GetNearestThreatPosition()
    {
        Collider[] threats = Physics.OverlapSphere(transform.position, alertRadius);
        float closestDistance = Mathf.Infinity;
        Vector3 closestThreat = Vector3.zero;

        foreach (Collider col in threats)
        {
            Aspect aspect = col.GetComponent<Aspect>();
            if (aspect != null && aspect.affiliation == Aspect.Affiliation.Enemy)
            {
                float distance = Vector3.Distance(transform.position, col.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestThreat = col.transform.position;
                }
            }
        }

        return closestThreat;
    }

    void GetNewWanderTarget()
    {
        Vector3 randomDirection = Random.insideUnitSphere * Random.Range(minWanderDistance, maxWanderDistance);
        randomDirection += transform.position;
        wanderTarget = GetValidNavMeshPosition(randomDirection);
    }

    Vector3 GetValidNavMeshPosition(Vector3 target)
    {
        NavMeshHit hit;
        if (NavMesh.SamplePosition(target, out hit, 10f, NavMesh.AllAreas))
        {
            return hit.position;
        }
        return transform.position + transform.forward * 5f;
    }

    void UpdateAnimator()
    {
        if (animator == null) return;

        // Set animation parameters based on state
        float speed = 0f;
        switch (currentState)
        {
            case CitizenState.Wander: speed = 0.5f; break;
            case CitizenState.Group: speed = 0.7f; break;
            case CitizenState.Evade: speed = 1f; break;
            default: speed = 0f; break;
        }

        animator.SetFloat("Speed", speed);
        animator.SetBool("IsAlert", currentState == CitizenState.Alert || currentState == CitizenState.Watch);
    }

    void UpdateVisuals()
    {
        if (npcRenderer == null) return;

        // Change material based on state
        if (currentState == CitizenState.Alert || currentState == CitizenState.Watch)
        {
            if (alertMaterial != null)
                npcRenderer.material = alertMaterial;
        }
        else
        {
            if (normalMaterial != null)
                npcRenderer.material = normalMaterial;
        }
    }

    // Event handlers for sensors
    public void OnPlayerDetected()
    {
        playerDetected = true;
        if (currentState != CitizenState.Evade)
        {
            currentState = CitizenState.Alert;
        }
    }

    public void OnPatrolDetected()
    {
        patrolDetected = true;
        CheckForThreats();
    }

    void OnDrawGizmosSelected()
    {
        // Draw group radius
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, groupRadius);

        // Draw alert radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, alertRadius);

        // Draw separation distance
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, separationDistance);

        // Draw wander target
        if (Application.isPlaying)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(wanderTarget, 0.5f);
            Gizmos.DrawLine(transform.position, wanderTarget);
        }
    }
}