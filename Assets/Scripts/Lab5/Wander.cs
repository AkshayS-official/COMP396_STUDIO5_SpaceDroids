using UnityEngine;
using System.Collections;

public class Wander : MonoBehaviour
{
    private Vector3 tarPos;
    [SerializeField] private float movementSpeed = 5.0f;
    [SerializeField] private float rotSpeed = 2.0f;
    [SerializeField] private float minX = -45.0f;
    [SerializeField] private float maxX = 45.0f;
    [SerializeField] private float minZ = -45.0f;
    [SerializeField] private float maxZ = 45.0f;
    [SerializeField] private float targetReactionRadius = 5.0f;
    [SerializeField] private float targetVerticaloffset = 0.5f;

    void Start()
    {
        // Get Wander Position
        GetNextPosition();
    }

    void Update()
    {
        // Check if we're near the destination position
        if (Vector3.Distance(tarPos, transform.position) < targetReactionRadius)
            GetNextPosition(); // generate new random position

        // Set up quaternion for rotation toward destination
        Quaternion tarRot = Quaternion.LookRotation(tarPos - transform.position);

        // Update rotation and translation
        transform.rotation = Quaternion.Slerp(transform.rotation, tarRot, rotSpeed * Time.deltaTime);
        transform.Translate(new Vector3(0, 0, movementSpeed * Time.deltaTime));
    }

    void GetNextPosition()
    {
        tarPos = new Vector3(Random.Range(minX, maxX), targetVerticaloffset, Random.Range(minZ, maxZ));
    }
}
