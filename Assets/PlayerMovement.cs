using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public CharacterController controller;
    public Camera playerCamera;

    public GameObject setHitbox;
    public float walkSpeed = 5f;
    public float runSpeed = 10f;
    public float gravity = -9.81f;
    public float jumpForce = 5f;
    public float mancheteForce = 10f;
    public float maxHitPower = 20f;
    public float setForce = 6f;
    public float maxChargeTime = 2f;
    public float mouseSensitivity = 2f;
    public GameObject ballPrefab;

    private Vector3 velocity;
    private bool isGrounded;
    private float chargeTime = 0f;
    private float hitChargeTime = 0f;
    private float verticalRotation = 0f;
    private GameObject ball;
    private bool canSet = false;
    private bool canBump = false;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        HandleMovement();
        HandleJump();
        HandleCamera();
        HandleBallInteraction();

        if (Input.GetMouseButton(0)) // Segurar botão esquerdo do mouse
        {
            hitChargeTime += Time.deltaTime;
            hitChargeTime = Mathf.Clamp(hitChargeTime, 0, maxChargeTime);
        }

        if (Input.GetMouseButtonUp(0)) // Soltar botão esquerdo do mouse para bater
        {
            PerformManchete();
            hitChargeTime = 0f;
        }

        // Levantamento para frente
        if (Input.GetKeyDown(KeyCode.F))
        {
            PerformSet(Vector3.forward);
        }

        // Levantamento para trás
        if (Input.GetKeyDown(KeyCode.R))
        {
            PerformSet(Vector3.back);
        }
    }

    void HandleMovement()
    {
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");
        Vector3 move = transform.right * moveX + transform.forward * moveZ;

        float speed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;
        controller.Move(move * speed * Time.deltaTime);
    }

    void HandleJump()
    {
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
            velocity.y = -2f;

        if (Input.GetKey(KeyCode.Space))
        {
            chargeTime += Time.deltaTime;
            chargeTime = Mathf.Clamp(chargeTime, 0, maxChargeTime);
        }

        if (Input.GetKeyUp(KeyCode.Space) && isGrounded)
        {
            float jumpPower = jumpForce * (chargeTime / maxChargeTime + 0.5f);
            velocity.y = Mathf.Sqrt(jumpPower * -2f * gravity);
            chargeTime = 0f;
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    void PerformManchete()
    {
        if(!canBump) return;

        Debug.Log("Manchete realizada!");
        if (ball != null)
        {
            Rigidbody ballRb = ball.GetComponent<Rigidbody>();
            if (ballRb != null)
            {
                float hitPower = mancheteForce * (hitChargeTime / maxChargeTime + 0.3f); // Mínimo de 50% da força
                Vector3 direction = (ball.transform.position - transform.position).normalized + Vector3.up;
                ballRb.AddForce(direction * hitPower, ForceMode.Impulse);
            }
        }
    }

    void PerformSet(Vector3 direction)
    {
        if (!canSet) return;

        Debug.Log("Levantamento realizado!");
        if (ball != null)
        {
            Rigidbody ballRb = ball.GetComponent<Rigidbody>();
            if (ballRb != null)
            {
                Vector3 setDirection = transform.TransformDirection(direction + Vector3.up * 1.5f);
                ballRb.linearVelocity = setDirection * setForce;
            }
        }
    }

    void HandleCamera()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -90f, 90f);
        playerCamera.transform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
    }

    void HandleBallInteraction()
    {
        if (Input.GetKeyDown(KeyCode.B))
        {
            SpawnBall();
        }
    }

    void SpawnBall()
    {
        ball = Instantiate(ballPrefab, transform.position + Vector3.up * 10, Quaternion.identity);
        Debug.Log("Ball spawned!");
    }

    public void SetBall(GameObject detectedBall)
    {
        canSet = true;
        ball = detectedBall;
    }

    public void ClearBall()
    {
        canSet = false;
        ball = null;
    }
}
