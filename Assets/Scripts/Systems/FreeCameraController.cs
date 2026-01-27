using UnityEngine;

public class FreeCameraController : MonoBehaviour
{
    public float moveSpeed = 10f;
    public float fastMultiplier = 3f;
    public float mouseSensitivity = 3f;

    float rotationX = 0f;

    void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * 100f * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * 100f * Time.deltaTime;

        rotationX -= mouseY;
        rotationX = Mathf.Clamp(rotationX, -90f, 90f);

        transform.localRotation = Quaternion.Euler(rotationX, 0f, 0f);
        transform.parent.Rotate(Vector3.up * mouseX);

        float speed = moveSpeed;
        if (Input.GetKey(KeyCode.LeftShift))
            speed *= fastMultiplier;

        Vector3 direction =
            transform.parent.forward * Input.GetAxis("Vertical") +
            transform.parent.right * Input.GetAxis("Horizontal");

        if (Input.GetKey(KeyCode.E))
            direction += Vector3.up;
        if (Input.GetKey(KeyCode.Q))
            direction += Vector3.down;

        transform.parent.position += direction * speed * Time.deltaTime;
    }
}
