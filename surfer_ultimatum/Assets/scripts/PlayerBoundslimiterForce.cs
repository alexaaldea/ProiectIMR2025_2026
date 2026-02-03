using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class RigidbodyPlayerMover : MonoBehaviour
{
    public float speed = 3f;
    Rigidbody rb;

    void Awake() { rb = GetComponent<Rigidbody>(); }

    void FixedUpdate()
    {
        // exemplu: input simplu (înlocuiește cu input-ul tău VR)
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        Vector3 dir = transform.forward * v + transform.right * h;
        Vector3 targetPos = rb.position + dir * speed * Time.fixedDeltaTime;

        rb.MovePosition(targetPos); // folosește MovePosition -> coliziuni fizice corecte
    }
}