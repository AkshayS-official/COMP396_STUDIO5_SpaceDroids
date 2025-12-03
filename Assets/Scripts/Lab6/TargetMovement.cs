using UnityEngine;
using System.Collections;

public class TargetMovement : MonoBehaviour
{
    public Vector3 bound = new Vector3(40, 40, 70);
    public float speed = 100.0f;
    public float targetReachRadius = 10.0f;

    private Vector3 initialPosition;
    private Vector3 nextMovementPoint;

    void Start()
    {
        initialPosition = transform.position;
        CalculateNextMovementPoint();
    }

    void Update()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime);

        Quaternion targetRot = Quaternion.LookRotation(nextMovementPoint - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime);

        if (Vector3.Distance(nextMovementPoint, transform.position) <= targetReachRadius)
            CalculateNextMovementPoint();
    }

    void CalculateNextMovementPoint()
    {
        float posX = Random.Range(initialPosition.x - bound.x, initialPosition.x + bound.x);
        float posY = Random.Range(initialPosition.y - bound.y, initialPosition.y + bound.y);
        float posZ = Random.Range(initialPosition.z - bound.z, initialPosition.z + bound.z);

        nextMovementPoint = new Vector3(posX, posY, posZ);
    }
}
