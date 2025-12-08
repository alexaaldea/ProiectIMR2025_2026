using UnityEngine;
using UnityEngine.InputSystem;

public class XRCharacterMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float gravityMultiplier = 2f;

    private CharacterController characterController;
    private Vector3 velocity;
    private Transform cameraTransform;

    void Start()
    {
        characterController = GetComponent<CharacterController>();

        if (Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
        else
        {
            Debug.LogError("No main camera found! Make sure your camera has the 'MainCamera' tag.");
        }
    }

    void Update()
    {
        if (characterController == null || cameraTransform == null) return;

        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;

        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        Vector3 moveDirection = (forward * vertical + right * horizontal) * moveSpeed;

        if (characterController.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }
        else
        {
            velocity.y += gravity * gravityMultiplier * Time.deltaTime;
        }

        moveDirection.y = velocity.y;

        characterController.Move(moveDirection * Time.deltaTime);
    }

    private void OnDrawGizmos()
    {
        if (Application.isPlaying && characterController != null)
        {
            Gizmos.color = characterController.isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }
    }
}