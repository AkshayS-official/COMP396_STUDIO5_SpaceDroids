using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq; // Needed for Sum() on lists

public class Boid2 : MonoBehaviour
{
    internal FlockController2 controller;
    private new Rigidbody rigidbody;

    private void Start()
    {
        rigidbody = GetComponent<Rigidbody>();

        // Find the FlockController2 GameObject and get its script component
        GameObject controllerObject = GameObject.FindGameObjectWithTag("FlockController2");
        if (controllerObject != null)
        {
            controller = controllerObject.GetComponent<FlockController2>();
        }
        else
        {
            Debug.LogError("Boid2: Cannot find GameObject tagged 'FlockController2'. Check your tag setup!");
        }
    }

    void Update()
    {
        if (controller)
        {
            Vector3 relativePos = Steer() * Time.deltaTime;

            if (relativePos != Vector3.zero)
                rigidbody.linearVelocity = relativePos;

            // Enforce minimum and maximum speeds for the boids
            float speed = rigidbody.linearVelocity.magnitude;

            if (speed > controller.maxVelocity)
            {
                rigidbody.linearVelocity = rigidbody.linearVelocity.normalized * controller.maxVelocity;
            }
            else if (speed < controller.minVelocity)
            {
                rigidbody.linearVelocity = rigidbody.linearVelocity.normalized * controller.minVelocity;
            }

            // Rotation: Face the direction of movement
            if (rigidbody.linearVelocity.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(rigidbody.linearVelocity);
            }
        }
    }

    private Vector3 Steer()
    {
        // Cohesion: Move toward the center of the flock
        Vector3 center = controller.flockCenter - transform.localPosition;

        // Alignment: Match the average velocity of the flock
        Vector3 velocity = controller.flockVelocity - rigidbody.linearVelocity;

        // Following the Leader
        Vector3 follow = controller.target.localPosition - transform.localPosition;

        // Separation: Avoid nearby boids
        Vector3 separation = Vector3.zero;
        foreach (Boid2 boid2 in controller.flockList)
        {
            if (boid2 != this)
            {
                Vector3 relativePos = transform.localPosition - boid2.transform.localPosition;
                // Accumulate separation force based on relative position
                separation += relativePos.normalized;
            }
        }

        // Randomize: Add some noise to the movement
        Vector3 randomize = new Vector3(Random.value * 2 - 1, Random.value * 2 - 1, Random.value * 2 - 1);
        randomize.Normalize();

        // Combine all forces with their weights
        return (
            controller.centerWeight * center             // Cohesion
            + controller.velocityWeight * velocity         // Alignment
            + controller.separationWeight * separation     // Separation
            + controller.followWeight * follow             // Follow Leader
            + controller.randomizeWeight * randomize        // Randomness
        );
    }
}
