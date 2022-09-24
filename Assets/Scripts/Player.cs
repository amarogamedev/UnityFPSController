using UnityEngine;

public class Player : MonoBehaviour
{
    #region Variables

    [Header("Movement Settings")]
    public float walkSpeed; //speed that the player will walk
    public float runSpeed; //speed that the player will run
    public float sprintVelocity; //time that the player takes to transition between running and walking
    [Range(0, 0.99f)] public float crouchSpeedMultiplier; //multiplier of the speed when crouching
    float speed; //current player speed
    [Range(0, 20)] public float movementSmoothness; //smoothness of starting and stopping walking
    float moveX, moveY; //player input (WASD or keys)
    public float crouchingHeight; //height that the player will have when crouching
    float standingHeight; //height that the player will have when standing
    Vector3 moveDirection; //the direction which the player is moving
    Vector3 finalMoveDirection; //smoothed version of the moveDirection

    [Header("Jumping Settings")]
    public float jumpForce; //force of the jump
    public float gravity; //force of gravity (only acts on the player)
    Vector3 gravityDirection; //direction of gravity (down)
    [Range(0.1f, 0.5f)] public float groundCheckRadius; //radius of the sphere that is used to check if the player is on the ground
    public LayerMask groundMask; //mask of layers that are considered ground
    public Transform foot; //position of the player's foot
    float timeSinceLastJump; //time since the player jumped

    [Header("Looking Settings")]
    [Range(100, 500)] public float mouseSensitivity; //sensitivity of camera movement
    public Vector2 minMaxFovPunch; //minimum and maximum amount of field of view
    float fov; //current camera field of view
    public Vector2 minMaxClampXRotation; //minimum and maximum values of the X rotation (up and down)
    float xAxisRotation; //current values of the X rotation (up and down)
    public Transform lookRotationPoint; //position of the eyes (camera)

    [HideInInspector]public bool paused;
    CharacterController characterController;
    Camera cam;

    #endregion

    private void Start()
    {
        GetComponents();
        LockCursor();
        SetStandingHeight();
        SetDefaultFOV();
    }

    void Update()
    {
        ApplyGravity();

        if(paused)
            return;

        LookAround();
        MoveAround();

        //stores the time since the last jump
        timeSinceLastJump += Time.deltaTime;
    }

    #region Starting methods (Only runs once at the start)

    void GetComponents()
    {
        //assigns the components needed to the variables
        characterController = GetComponent<CharacterController>();
        cam = GetComponentInChildren<Camera>();
    }

    public void LockCursor()
    {
        //locks the cursor on the center of the screen
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void UnlockCursor()
    {
        //unlocks the cursor
        Cursor.lockState = CursorLockMode.None;
    }

    void SetStandingHeight()
    {
        standingHeight = characterController.height;
    }

    void SetDefaultFOV()
    {
        fov = minMaxFovPunch.x;
    }

    #endregion

    #region Update methods (Runs every frame)

    void LookAround()
    {
        //get input from the mouse, multiply by sensitivity and framerate
        float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensitivity * Time.smoothDeltaTime;
        float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensitivity * Time.smoothDeltaTime;

        //rotate the player body based on the mouse X input
        transform.Rotate(Vector3.up * mouseX);

        //clamp the input and rotate the camera based on the mouse Y input
        xAxisRotation -= mouseY;
        xAxisRotation = Mathf.Clamp(xAxisRotation, minMaxClampXRotation.x, minMaxClampXRotation.y);
        lookRotationPoint.transform.localRotation = Quaternion.Euler(xAxisRotation, 0, 0);
    }

    void MoveAround()
    {
        //get input from the WASD or arrow keys only if we're grounded
        moveX = Input.GetAxisRaw("Horizontal");
        moveY = Input.GetAxisRaw("Vertical");

        //check if the player is crouching
        if (crouching())
        {
            CalculateSpeed(-sprintVelocity);
            CalculateFov(minMaxFovPunch.x);

            //reduces speed and the size if the player is crouching
            speed *= crouchSpeedMultiplier;
            SetCharacterControllerHeight(crouchingHeight);
        }
        else
        {
            //checks if the player is trying to run and sets variables to running state
            if (Input.GetKey(KeyCode.LeftShift) && moveY > 0)
            {
                CalculateSpeed(sprintVelocity);
                CalculateFov(minMaxFovPunch.y);
            }
            else
            {
                CalculateSpeed(-sprintVelocity);
                CalculateFov(minMaxFovPunch.x);
            }

            SetCharacterControllerHeight(standingHeight);
        }

        //checks if the player can jump
        if (Input.GetKeyDown(KeyCode.Space) && grounded())
        {
            Jump();
        }

        //recenters the character controller and camera (changes when crouching or standing up)
        characterController.center = new Vector3(0, 0.5f + characterController.height / 4, 0);
        lookRotationPoint.localPosition = new Vector3(0, characterController.height * 0.875f, 0);

        //creates the direction vector, normalizes it and smoothes it
        moveDirection = transform.right * moveX + transform.forward * moveY;
        moveDirection.Normalize();
        finalMoveDirection = Vector3.Lerp(finalMoveDirection , moveDirection , movementSmoothness * Time.deltaTime);

        //apply the movement based on the direction vector
        characterController.Move(finalMoveDirection * speed * Time.deltaTime);
    }

    void CalculateSpeed(float sprintVelocity)
    {
        speed = Mathf.Clamp(speed += sprintVelocity * Time.deltaTime, walkSpeed, runSpeed);
    }

    void CalculateFov(float fov)
    {
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, fov, sprintVelocity * Time.deltaTime);
    }

    void SetCharacterControllerHeight(float height)
    {
        characterController.height = Mathf.Lerp(characterController.height, height, movementSmoothness * Time.deltaTime);
    }

    void ApplyGravity()
    {
        //checks if the player is grounded
        if(grounded() && timeSinceLastJump > 0.5f)
        {
            //resets the gravity and reset back the slope limit
            gravityDirection.y = -9.8f;
            characterController.slopeLimit = 45;
        }
        else
        {
            //increases gravity over time and increase slope limit to prevent jitter when jumping next to a wall
            gravityDirection.y += gravity * Time.deltaTime;
            characterController.slopeLimit = 90;
        }

        //moves the player down based on the gravity
        characterController.Move(gravityDirection * Time.deltaTime);
    }

    void Jump()
    {
        //applies upwards force
        gravityDirection.y = Mathf.Sqrt(jumpForce * -2 * gravity);
        timeSinceLastJump = 0;
    }

    bool grounded()
    {
        return Physics.CheckSphere(foot.transform.position, groundCheckRadius, groundMask);
    }

    bool crouching()
    {
        if(Input.GetKey(KeyCode.LeftControl) && grounded())
        {
            return true;
        }
        else if(hittingHead())
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    bool hittingHead()
    {
        float x = transform.position.x;
        float y = transform.position.y + characterController.stepOffset;
        float z = transform.position.z;

        //do multiple raycasts to check if the player is hitting his head using the radius of the character controller collider
        if ((Physics.Raycast(new Vector3(x + characterController.radius, y, z), Vector3.up, standingHeight - characterController.stepOffset))
            || (Physics.Raycast(new Vector3(x - characterController.radius, y, z), Vector3.up, standingHeight - characterController.stepOffset))
            || (Physics.Raycast(new Vector3(x, y, z + characterController.radius), Vector3.up, standingHeight - characterController.stepOffset))
            || (Physics.Raycast(new Vector3(x, y, z - characterController.radius), Vector3.up, standingHeight - characterController.stepOffset)))
        {
            return true;
        }

        return false;
    }

    #endregion
}
