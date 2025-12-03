using UnityEngine;
using System.Collections;

public class Boid : MonoBehaviour
{
    // --- Input Values (Configurable in Inspector) ---
    public float minSpeed = 20.0f;
    public float turnSpeed = 20.0f;
    public float randomFreq = 20.0f;
    public float randomForce = 20.0f;

    [Header("Alignment Variables")]
    public float toOriginForce = 50.0f;
    public float toOriginRange = 100.0f;
    public float gravity = 2.0f;

    [Header("Separation Variables")]
    public float avoidanceRadius = 50.0f;
    public float avoidanceForce = 20.0f;

    [Header("Cohesion Variables")]
    public float followVelocity = 4.0f;
    public float followRadius = 40.0f;

    [Header("Movement Variables")]

    // --- Private Internal Variables ---
    private Transform origin;
    private Vector3 velocity;
    [HideInInspector] public Vector3 normalizedVelocity;
    private Vector3 randomPush;
    private Vector3 originPush;
    private Transform[] objects;
    private Boid[] otherBoids;
    private Transform transformComponent;
    private float randomFreqInterval;

    void Start()
    {
        randomFreqInterval = 1.0f / randomFreq;
        origin = transform.parent; // Assign the parent as origin (FlockController)
        transformComponent = transform;

        // Get all the boids from the parent transform
        Component[] tempBoids = transform.parent.GetComponentsInChildren<Boid>();

        // Assign and store all the flock objects in this group
        objects = new Transform[tempBoids.Length];
        otherBoids = new Boid[tempBoids.Length];

        for (int i = 0; i < tempBoids.Length; i++)
        {
            objects[i] = tempBoids[i].transform;
            otherBoids[i] = (Boid)tempBoids[i];
        }

        // Null Parent: The boid should now move independently, using the stored 'origin' as its leader target
        transform.parent = null;

        // Start the coroutine for random push updates
        StartCoroutine(UpdateRandom());
    }

    // Coroutine to update randomPush periodically (Coroutines are explained in your Lab instructions)
    IEnumerator UpdateRandom()
    {
        while (true)
        {
            // Random.insideUnitSphere gives a Vector3 with random X, Y, Z values
            randomPush = Random.insideUnitSphere * randomForce;

            // Wait for a random interval before updating the push again
            yield return new WaitForSeconds(randomFreqInterval +
                Random.Range(-randomFreqInterval / 2.0f, randomFreqInterval / 2.0f));
        }
    }

    void Update()
    {
        // Internal variables
        float speed = velocity.magnitude;
        Vector3 avgVelocity = Vector3.zero;
        Vector3 avgPosition = Vector3.zero;
        int count = 0;
        Vector3 myPosition = transformComponent.position;
        Vector3 forceV;
        Vector3 toAvg;

        // --- Loop through all nearby boids to calculate rules ---
        for (int i = 0; i < objects.Length; i++)
        {
            Transform boidTransform = objects[i];

            if (boidTransform != transformComponent) // Don't check against self
            {
                Vector3 otherPosition = boidTransform.position;

                // 1. Cohesion/Alignment Calculation
                avgPosition += otherPosition;
                count++;

                // Directional vector from other flock to this flock
                forceV = myPosition - otherPosition;
                float directionMagnitude = forceV.magnitude;
                float forceMagnitude = 0.0f;

                // 2. Separation Rule
                if (directionMagnitude < avoidanceRadius)
                {
                    forceMagnitude = 1.0f - (directionMagnitude / avoidanceRadius);
                    // Add separation force to velocity
                    avgVelocity += (forceV / directionMagnitude) * forceMagnitude * avoidanceForce;
                }

                // 3. Alignment Rule (Velocity Matching)
                if (directionMagnitude > 0)
                {
                    forceMagnitude = directionMagnitude / followRadius;
                    Boid tempOtherBoid = otherBoids[i];
                    // Add alignment force to velocity
                    avgVelocity += followVelocity * forceMagnitude * tempOtherBoid.normalizedVelocity;
                }
            }
        }

        // --- Apply Rules and Final Movement ---

        if (count > 0)
        {
            // Calculate the average flock velocity (Alignment)
            avgVelocity /= count;

            // Calculate Center value of the flock (Cohesion)
            toAvg = (avgPosition / count) - myPosition;
        }
        else
        {
            toAvg = Vector3.zero;
        }

        // Directional Vector to the leader (FlockController)
        forceV = origin.position - myPosition;
        float leaderDirectionMagnitude = forceV.magnitude;
        float leaderForceMagnitude = leaderDirectionMagnitude / toOriginRange;

        // Calculate the velocity of the flock to the leader
        if (leaderDirectionMagnitude > 0)
            originPush = leaderForceMagnitude * toOriginForce * (forceV / leaderDirectionMagnitude);

        // Enforce minimum speed
        if (speed < minSpeed && speed > 0)
        {
            velocity = (velocity / speed) * minSpeed;
        }

        Vector3 wantedVel = velocity;

        // Calculate final velocity by combining all forces
        wantedVel -= wantedVel * Time.deltaTime; // Deceleration/Damping
        wantedVel += randomPush * Time.deltaTime; // Randomness
        wantedVel += originPush * Time.deltaTime; // Follow Leader
        wantedVel += avgVelocity * Time.deltaTime; // Separation/Alignment forces
        wantedVel += gravity * Time.deltaTime * toAvg.normalized; // Cohesion (Move to center)

        // Smoothly rotate the current velocity toward the wanted velocity
        velocity = Vector3.RotateTowards(velocity, wantedVel, turnSpeed * Time.deltaTime, 100.00f);

        // Update rotation to face the new velocity vector
        transformComponent.rotation = Quaternion.LookRotation(velocity);

        // Move the boid based on the calculated velocity (in World space)
        transformComponent.Translate(velocity * Time.deltaTime, Space.World);

        // Store normalized velocity for other boids (Alignment)
        normalizedVelocity = velocity.normalized;
    }
}
