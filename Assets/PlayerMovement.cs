using UnityEngine;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    public CharacterController controller;
    public Camera playerCamera;

    public GameObject setHitbox;
    public float walkSpeed = 5f;
    public float runSpeed = 10f;
    public float gravity = -9.81f;
    public float jumpForce = 1f;
    public float jumpChargeSpeed = 4.5f;
    public float mancheteForce = 10f;
    public float maxHitPower = 20f;
    public float setForce = 10f;
    public float setChargeSpeed = 3.0f;
    public float maxChargeTime = 0.001f;
    public float mouseSensitivity = 2f;
    public GameObject ballPrefab;
    public Slider powerSlider;
    public Slider jumpSlider;

    private Vector3 velocity;
    private float chargeTime = 0f;
    private float chargeTimeReverse = 0f;
    private float hitChargeTime = 0f;
    private float verticalRotation = 0f;
    private GameObject ball;
    private bool canSet = false;
    private bool canBump = false;
    private bool stopJump = false;


    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        powerSlider.minValue = 0;
        powerSlider.maxValue = 100;
        powerSlider.value = 0;

        jumpSlider.minValue = 0;
        jumpSlider.maxValue = 100;
        jumpSlider.value = 0;



        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Ball"));
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
        if (Input.GetKey(KeyCode.F)) // Charge the set
        {
            hitChargeTime += Time.deltaTime * setChargeSpeed;
            hitChargeTime = Mathf.Clamp(hitChargeTime, 0, maxChargeTime);

            float powerPercent = (hitChargeTime / maxChargeTime) * 100f;
            powerSlider.value = powerPercent;
        }

        if (Input.GetKeyUp(KeyCode.F)) // Set in the direction you're looking
        {
            PerformSet(1);
            hitChargeTime = 0f;

            powerSlider.value = 0;
        }

        if (Input.GetKey(KeyCode.R)) // Charge the set
        {
            hitChargeTime += Time.deltaTime;
            hitChargeTime = Mathf.Clamp(hitChargeTime, 0, maxChargeTime);

            float powerPercent = (hitChargeTime / maxChargeTime) * 100f;
            powerSlider.value = powerPercent;
        }

        if (Input.GetKeyUp(KeyCode.R)) // Set in the direction you're looking
        {
            PerformSet(-1);
            hitChargeTime = 0f;

            powerSlider.value = 0;
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
        if (Input.GetKey(KeyCode.Space) && GetIsGrounded() && !stopJump)
        {
            if (chargeTimeReverse > 0)
            {
                chargeTime -= chargeTimeReverse;
                chargeTimeReverse = Time.deltaTime * jumpChargeSpeed;
                if (chargeTime <= 0)
                {
                    stopJump = true;
                    chargeTime = 0f;
                    chargeTimeReverse = 0f;
                    jumpSlider.value = 0;
                }
            }
            else
            {
                chargeTime += Time.deltaTime * jumpChargeSpeed;
                if (chargeTime >= maxChargeTime)
                {
                    chargeTimeReverse = Time.deltaTime * jumpChargeSpeed;
                }
            }

            float jumpPercent = (chargeTime / Mathf.Abs(maxChargeTime)) * 100f;
            jumpSlider.value = jumpPercent;
        }

        if (Input.GetKeyUp(KeyCode.Space) && GetIsGrounded())
        {
            if (!stopJump)
            {
                float jumpPower = jumpForce * (chargeTime / maxChargeTime + 0.5f);
                velocity.y = Mathf.Sqrt(jumpPower * -2f * gravity);
                chargeTime = 0f;
                chargeTimeReverse = 0f;
                jumpSlider.value = 0;
            }
            stopJump = false;
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    private bool GetIsGrounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 1f);
    }

    void PerformManchete()
    {
        if (!canBump) return;

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

    void PerformSet(int directionMultiplier)
    {
        if (!canSet) return;

        Debug.Log("Levantamento realizado!");
        if (ball != null)
        {
            Rigidbody ballRb = ball.GetComponent<Rigidbody>();
            if (ballRb != null)
            {
                // Use camera forward direction
                Vector3 setDirection = playerCamera.transform.forward * directionMultiplier;
                setDirection.y = Mathf.Abs(setDirection.y) + 0.5f; // Ensure it goes upwards

                // Control power based on charge time
                float setPower = Mathf.Lerp(setForce * 0.5f, setForce * 2f, hitChargeTime / maxChargeTime);

                // Apply force
                ballRb.linearVelocity = setDirection.normalized * setPower;
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
