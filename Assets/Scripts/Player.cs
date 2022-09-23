using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    #region Variables

    [Header("Movement Settings")]
    public float walkSpeed; //speed that the player will walk
    public float runSpeed; //speed that the player will run
    [Range(0, 50)] public float movementSmoothness; //smoothness of starting and stopping walking
    float moveX, moveY; //player input (WASD or keys)
    bool grounded; //returns if the the player is currently grounded
    bool crouching; //returns if the player is currently crouching
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
    [Range(100,500)] public float mouseSensitivity; //sensitivity of camera movement
    public Vector2 minMaxFovPunch; //minimum and maximum amount of field of view
    public Vector2 minMaxClampXRotation; //minimum and maximum values of the X rotation (up and down)
    float xAxisRotation; //current values of the X rotation (up and down)
    public Transform lookRotationPoint; //position of the eyes (camera)
    public Animator headBobAnimator; //animator responsible for the camera bob movement

    [HideInInspector]public bool paused;
    CharacterController characterController;
    Camera cam;

    #endregion

    private void Start()
    {
        GetComponents();
        LockCursor();
        SetStandingHeight();
    }

    void Update()
    {
        ApplyGravity();

        if(paused)
            return;

        LookAround();
        MoveAround();
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

        //checks if the player is trying to crouch or can't get up
        crouching = Input.GetKey(KeyCode.LeftControl) && grounded || hittingHead();

        float speed = walkSpeed;
        float fov = minMaxFovPunch.x;

        //check if the player is running
        if (Input.GetKey(KeyCode.LeftShift))
        {
            //sets variables to running state
            if(!crouching && moveY > 0)
            {
                speed = runSpeed;
                fov = minMaxFovPunch.y;
            }
        }

        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, fov, movementSmoothness * Time.deltaTime);

        //checks if the player is crouching
        if(crouching)
        {
            //reduces speed and the size if the player is crouching
            speed /= 2;
            characterController.height = Mathf.Lerp(characterController.height, crouchingHeight, movementSmoothness * Time.deltaTime);
        }
        else
        {
            characterController.height = Mathf.Lerp(characterController.height, standingHeight, movementSmoothness * Time.deltaTime);
        }

        //feed the current speed and inputs to the head bob function
        if(paused)
        {
            AnimateHeadBob(0, new Vector2(moveX, moveY));
        }
        else
        {
            AnimateHeadBob(speed, new Vector2(moveX, moveY));
        }

        //recenters the character controller and camera
        characterController.center = new Vector3(0, 0.5f + characterController.height / 4, 0);
        lookRotationPoint.localPosition = new Vector3(0, characterController.height * 0.875f, 0);

        //creates the direction vector, normalizes it and smoothes it
        moveDirection = transform.right * moveX + transform.forward * moveY;
        moveDirection.Normalize();
        finalMoveDirection = Vector3.Lerp(finalMoveDirection , moveDirection , movementSmoothness * Time.deltaTime);

        //apply the movement based on the direction vector
        characterController.Move(finalMoveDirection * speed * Time.deltaTime);
    }

    void AnimateHeadBob(float speed, Vector2 inputs)
    {
        //checks if the player is grounded and pressing a key
        int animationSpeed;
        if (grounded && inputs.magnitude != 0)
        {
            //checks if the player is running or walking
            if (speed == runSpeed)
            {
                animationSpeed = 2;
            }
            else
            {
                animationSpeed = 1;
            }
        }
        else
        {
            animationSpeed = 0;
        }

        //sends animation speed info to the animator
        headBobAnimator.SetInteger("speed", animationSpeed);
    }

    void ApplyGravity()
    {
        //stores the time since the last jump
        timeSinceLastJump += Time.deltaTime;

        //checks if the player is grounded
        if(grounded)
        {
            //resets the gravity and reset back the slope limit
            gravityDirection.y = -3.5f;
            characterController.slopeLimit = 45;
        }
        else
        {
            //increases gravity over time and increase slope limit to prevent jitter when jumping next to a wall
            gravityDirection.y += gravity * Time.deltaTime;
            characterController.slopeLimit = 90;
        }

        //checks if the player hasn't jumped recently to prevent getting stuck to the ground
        if (timeSinceLastJump > 0.3f)
        {
            //updates grounded variable by testing if the player is colliding with something of the ground layer
            grounded = Physics.CheckSphere(foot.transform.position, groundCheckRadius, groundMask);
        }

        //checks if the player can jump
        if (Input.GetKeyDown(KeyCode.Space) && grounded)
        {
            Jump();
        }

        //moves the player down based on the gravity
        characterController.Move(gravityDirection * Time.deltaTime);
    }

    void Jump()
    {
        //applies upwards force
        grounded = false;
        gravityDirection.y = Mathf.Sqrt(jumpForce * -2 * gravity);
        timeSinceLastJump = 0;
    }

    bool hittingHead()
    {
        float x = transform.position.x;
        float y = transform.position.y + characterController.height;
        float z = transform.position.z;

        //do multiple raycasts to check if the player is hitting his head using the radius of the character controller collider
        if ((Physics.Raycast(new Vector3(x + characterController.radius, y, z), Vector3.up, 1) && crouching)
            || (Physics.Raycast(new Vector3(x - characterController.radius, y, z), Vector3.up, 1) && crouching)
            || (Physics.Raycast(new Vector3(x, y, z + characterController.radius), Vector3.up, 1) && crouching)
            || (Physics.Raycast(new Vector3(x, y, z - characterController.radius), Vector3.up, 1) && crouching))
        {
            return true;
        }

        return false;
    }

    #endregion
}
