using UnityEngine;

public class Target : MonoBehaviour
{
    [SerializeField] private float hoffset = 0.2f;

    void Update()
    {
        int button = 0;

        // Get the point of the hit position when the mouse is being clicked
        if (Input.GetMouseButtonDown(button))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hitInfo;

            if (Physics.Raycast(ray.origin, ray.direction, out hitInfo))
            {
                Vector3 targetPosition = hitInfo.point;
                // Move the Target marker slightly above the ground
                transform.position = targetPosition + new Vector3(0.0f, hoffset, 0.0f);
            }
        }
    }
}
