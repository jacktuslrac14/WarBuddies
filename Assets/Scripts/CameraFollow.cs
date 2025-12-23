using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float smoothSpeed = 0.125f;
    public Vector3 offset;

    // Idagdag itong mga variables para sa boundaries
    public float minX = -5f; // Palitan base sa sinulat mong coordinate
    public float maxX = 15f; // Palitan base sa sinulat mong coordinate

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = target.position + offset;
        
        // Dito natin lilitiman ang galaw ng camera
        float clampedX = Mathf.Clamp(desiredPosition.x, minX, maxX);
        
        // Panatilihin ang Y at Z base sa desiredPosition o offset
        Vector3 boundPosition = new Vector3(clampedX, desiredPosition.y, desiredPosition.z);

        Vector3 smoothedPosition = Vector3.Lerp(transform.position, boundPosition, smoothSpeed);
        transform.position = smoothedPosition;
    }
}