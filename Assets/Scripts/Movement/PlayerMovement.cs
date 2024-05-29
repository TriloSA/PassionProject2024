using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Variables")]
    // Movespeed is mainly used to cap the movement if it exceeds too much (under normal conditions).
    [SerializeField] private float cappedSpeed;
    [SerializeField] private float walkSpeed;
    [SerializeField] private float sprintSpeed;
    // Prevents slipperiness.
    [SerializeField] private float groundDrag;

    [Header("Jumping Variables")]
    [SerializeField] private float jumpForce;
    [SerializeField] private float jumpCooldown;
    [SerializeField] private bool canJump;
    // While in air, this is a multiplier to make sure momentum is saved, I think?
    [SerializeField] private float airMultiplier;

    [Header("Crouching Variables")]
    [SerializeField] private float crouchSpeed;
    [SerializeField] private float crouchYScale;
    private float originalYScale;

    [Header("Keybinds")]
    // NOTE: Needs to changed to new input system later. Affects lines 18-20, 74, etc.
    [SerializeField] private KeyCode jumpKey = KeyCode.Space;
    [SerializeField] private KeyCode sprintKey = KeyCode.LeftShift;
    [SerializeField] private KeyCode crouchKey = KeyCode.LeftControl;

    [Header("Ground Check")]
    [SerializeField] private LayerMask groundDefinition;
    [SerializeField] private bool grounded;
    // VITAL. Grabs the player's distance from their center point and subtracts a
    // y-value to reach their toes so collision is properly analyzed by the raycast.
    [SerializeField] private float playerHeight;

    [Header("Slope Handling")]
    [SerializeField] private float maxSlopeAngle;
    [SerializeField] private RaycastHit slopeHit;

    [Header("Raycasting")]
    [SerializeField] private float rayLength;

    // Reference to the game object tracking what way the player is facing.
    public Transform orientation;

    // The various inputs.
    private float horizontalInput;
    private float verticalInput;

    // A vector that holds the direction value the player is moving / should be moving towards.
    Vector3 moveDirection;
    
    // Rigidbody lmao.
    Rigidbody rb;

    // References the enum as variable mState.
    public MovementState movementState;

    public enum MovementState
    {
        walking,
        sprinting,
        crouching,
        air
    }

    ////////////////////////

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        canJump = true;
        originalYScale = transform.localScale.y;
    }

    private void Update()
    {
        // Checks if grounded via raycast downwards.
        // Raycasts (simplified) are essentially invisible lasers that can be used for detection and stuff when it comes to layers.
        // rayOrigin just gets a point where the ray starts and where it should end.
        // The notable part is transform.position.y - (playerHeight * transform.localScale.y). This is because when you are crouched, the
        // raycast point would be underneath the ground making you technically airborne, but now it scales with the player so the point is always at the feet.
        Vector3 rayOrigin = new Vector3(transform.position.x, transform.position.y - (playerHeight * transform.localScale.y), transform.position.z);
        // r makes a ray from the rayOrigin point and the direction (it needs to go down).
        Ray r = new Ray(rayOrigin, Vector3.down);
        // Visualization. Ignore. Hence the debug.
        Debug.DrawRay(r.origin, r.direction * rayLength, Color.red);

        grounded = Physics.Raycast(r, rayLength, groundDefinition);

        PlayerInput();
        SpeedControl();
        StateHandler();

        // Handles drag / friction.
        if (grounded)
        {
            rb.drag = groundDrag;
        }
        else
        {
            rb.drag = 0;
        }
    }

    private void FixedUpdate()
    {
        MovePlayer();
    }

    private void PlayerInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        if (Input.GetKey(jumpKey) && canJump && grounded)
        {
            canJump = false;
            Jump();
            // Invokes the method by the "name of" "ResetJump" to set the canJump bool to true after jumpCooldown is done.
            Invoke(nameof(ResetJump), jumpCooldown);
        }

        if (Input.GetKeyDown(crouchKey))
        {
            transform.localScale = new Vector3(transform.localScale.x, crouchYScale, transform.localScale.z);
            rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);
        }

        if (Input.GetKeyUp(crouchKey))
        {
            transform.localScale = new Vector3(transform.localScale.x, originalYScale, transform.localScale.z);
        }
    }

    private void StateHandler()
    {
        if (Input.GetKey(crouchKey))
        {
            movementState = MovementState.crouching;
            cappedSpeed = crouchSpeed;
        }
        // If you're sprinting...
        if (grounded && Input.GetKey(sprintKey))
        {
            movementState = MovementState.sprinting;
            cappedSpeed = sprintSpeed;
        }
        // If you're walking...
        else if (grounded)
        {
            movementState = MovementState.walking;
            cappedSpeed = walkSpeed;
        }
        // If you're airborne...
        else
        {
            movementState = MovementState.air;
        }
    }

    private void MovePlayer()
    {
        // Calculates movement direction.
        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;

        // Adds a force to the Rigidbody. If your grounded, your speed isn't affected by airMultiplier.
        if (grounded)
        {
            rb.AddForce(moveDirection.normalized * cappedSpeed * 10f, ForceMode.Force);
        }
        else if (!grounded)
        {
            rb.AddForce(moveDirection.normalized * cappedSpeed * 10f * airMultiplier, ForceMode.Force);
        }
    }

    private void SpeedControl()
    {
        Vector3 flatVelocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        // Limit the velocity if needed. Basically, if you go faster than your movement speed...
        if (flatVelocity.magnitude > cappedSpeed)
        {
            // You will calculate what your maximum velocity WOULD be...
            Vector3 limitedVelocity = flatVelocity.normalized * cappedSpeed;

            // and then apply it.
            rb.velocity = new Vector3(limitedVelocity.x, rb.velocity.y, limitedVelocity.z);
        }
    }

    private void Jump()
    {
        // Reset y velocity.
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        // Adds force. Uses ForceMode.Impusle because the force is only applied once.
        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }

    private void ResetJump()
    {
        canJump = true;
    }
}
