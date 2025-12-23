using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float smoothSpeed = 0.125f;
    public Vector3 offset;

    [Header("Boundary Settings")]
    public float minX = -19f; 
    public float maxX = 19f; 

    void LateUpdate()
{
    // FIX: Hangga't wala pang target o kung naka-pause (Menu), i-lock sa Y = 0
    if (target == null || Time.timeScale == 0)
    {
        transform.position = new Vector3(transform.position.x, 0, -10);
        return;
    }

    // Dito lang susunod ang camera kapag nag-resume na ang laro
    Vector3 desiredPosition = target.position + offset;
    float clampedX = Mathf.Clamp(desiredPosition.x, minX, maxX);
    
    // I-lock pa rin ang Y sa 0 kung gusto mong steady lang ang taas
    Vector3 boundPosition = new Vector3(clampedX, 0, -10); 

    transform.position = Vector3.Lerp(transform.position, boundPosition, smoothSpeed);
}
}