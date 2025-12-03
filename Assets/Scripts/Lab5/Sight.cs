using UnityEngine;

public class Sight : Sense
{
    public int FieldOfView = 45; // Half angle for FOV (90 degrees total)
    public int ViewDistance = 100;

    private Transform playerTrans;
    private Vector3 rayDirection;

    protected override void Initialize()
    {
        // Find player position using the required tag
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            playerTrans = playerObject.transform;
        }
        else
        {
            Debug.LogError("Sight.cs: Player object not found. Ensure it is tagged 'Player'.");
        }
    }

    protected override void UpdateSense()
    {
        if (playerTrans == null) return;

        elapsedTime += Time.deltaTime;

        // Detect perspective sense if within the detection rate
        if (elapsedTime > detectionRate)
        {
            DetectAspect();
            elapsedTime = 0.0f;
        }
    }

    void DetectAspect()
    {
        // Direction from current position to player position
        rayDirection = (playerTrans.position - transform.position).normalized;

        // Check the angle between the AI character's forward vector and the direction vector
        if ((Vector3.Angle(rayDirection, transform.forward)) < FieldOfView)
        {
            RaycastHit hit;

            // Perform a Raycast to see if the target is visible (not behind an obstacle)
            if (Physics.Raycast(transform.position, rayDirection, out hit, ViewDistance))
            {
                Aspect aspect = hit.collider.GetComponent<Aspect>();

                if (aspect != null)
                {
                    // Check the affiliation (e.g., if the hit object is a Player)
                    if (aspect.affiliation == targetAffiliation)
                    {
                        print("Enemy Detected"); // Log the detection
                    }
                }
            }
        }
    }

    // Draws visualization of the sensor in the editor
    void OnDrawGizmos()
    {
        if (!Application.isEditor || playerTrans == null) return;

        // Draw line to target (red)
        Debug.DrawLine(transform.position, playerTrans.position, Color.red);

        // Approximate perspective visualization (green)
        Vector3 frontRayPoint = transform.position + (transform.forward * ViewDistance);

        Quaternion leftRotation = Quaternion.Euler(0, -FieldOfView * 0.5f, 0);
        Vector3 leftRayPoint = leftRotation * transform.forward * ViewDistance + transform.position;

        Quaternion rightRotation = Quaternion.Euler(0, FieldOfView * 0.5f, 0);
        Vector3 rightRayPoint = rightRotation * transform.forward * ViewDistance + transform.position;

        Debug.DrawLine(transform.position, frontRayPoint, Color.green);
        Debug.DrawLine(transform.position, leftRayPoint, Color.yellow);
        Debug.DrawLine(transform.position, rightRayPoint, Color.yellow);
    }
}
