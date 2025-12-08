using UnityEngine;
using UnityEngine.XR;

public class VRPlayerLateralMovement : MonoBehaviour
{
    [Header("References")]
    public Transform xrOrigin;  // The parent XR Rig
    public Transform vrHead;    // The VR headset (Camera)

    [Header("Movement Settings")]
    public float lateralSpeed = 5f; // Multiplier for how far headset movement moves player

    private float initialX;

    private void Start()
    {
        if (xrOrigin == null)
        {
            xrOrigin = transform; // Default to this object
        }

        if (vrHead == null)
        {
            vrHead = Camera.main.transform;
        }

        // Record initial headset local X position
        initialX = vrHead.localPosition.x;
    }

    private void Update()
    {
        // Calculate lateral offset from start
        float offsetX = vrHead.localPosition.x - initialX;

        // Apply movement to XR Origin along world X axis
        Vector3 newPosition = xrOrigin.position;
        newPosition.x = offsetX * lateralSpeed;
        xrOrigin.position = newPosition;
    }
}
