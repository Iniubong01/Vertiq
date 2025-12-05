using UnityEngine;

public class JoystickPlayerExample : MonoBehaviour
{
    public float speed = 5f;
    public float rotationSpeed = 10f;
    public VariableJoystick variableJoystick;
    public Rigidbody2D rbb;

    private void FixedUpdate()
    {
        // Use X and Y for 2D movement, not forward/right (that’s 3D)
        Vector2 direction = new Vector2(variableJoystick.Horizontal, variableJoystick.Vertical);

        // Add force for movement
        rbb.AddForce(direction * speed * Time.fixedDeltaTime, ForceMode2D.Force);

        // Rotate toward the movement direction (only if joystick is being moved)
        if (direction.sqrMagnitude > 0.001f)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f; 
            Quaternion targetRotation = Quaternion.Euler(0, 0, angle);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
        }
    }
}
