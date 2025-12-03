using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FlockController2 : MonoBehaviour
{
    // Settings
    public float minVelocity = 1;
    public float maxVelocity = 8;
    public int flockSize = 20;

    // Weights for Flocking Rules
    public float centerWeight = 1;
    public float velocityWeight = 1;
    public float separationWeight = 1;
    public float followWeight = 1;
    public float randomizeWeight = 1;

    // Prefab and Target
    public Boid2 boid2Prefab;
    public Transform target; // The moving leader target (Target Sphere)

    // Global Flock State
    internal Vector3 flockCenter;
    internal Vector3 flockVelocity;
    internal ArrayList flockList = new ArrayList();

    void Start()
    {
        for (int i = 0; i < flockSize; i++)
        {
            // Instantiate boids and set up their controller reference
            Boid2 boid2 = Instantiate(boid2Prefab, transform.position, transform.rotation) as Boid2;
            boid2.transform.parent = transform;
            boid2.controller = this;
            flockList.Add(boid2);
        }
    }

    void Update()
    {
        // Calculate the Center and Velocity of the whole flock group
        Vector3 center = Vector3.zero;
        Vector3 velocity = Vector3.zero;

        foreach (Boid2 boid2 in flockList)
        {
            center += boid2.transform.localPosition;
            velocity += boid2.GetComponent<Rigidbody>().linearVelocity;
        }

        flockCenter = center / flockSize;
        flockVelocity = velocity / flockSize;
    }
}
