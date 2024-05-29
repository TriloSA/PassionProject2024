using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Values")]
    // Need I explain more?
    [SerializeField] private float moveSpeed;
    [SerializeField] private float jumpForce;
    [SerializeField] private float jumpCooldown;
    [SerializeField] private bool canJump;
    // While in air, this is a multiplier to make sure momentum is saved, I think?
    [SerializeField] private float airMultiplier;
    // Prevents slipperiness.
    [SerializeField] private float groundDrag;

    [Header("Keybinds")]
    // NOTE: Needs to changed to new input system later. Affects lines 18-20, 74, etc.
    [SerializeField] private KeyCode jumpKey = KeyCode.Space;

    [Header("Is on the Ground?")]
    [SerializeField] private LayerMask groundDefinition;
    [SerializeField] private bool grounded;
    // VITAL. Grabs the player's distance from their center point and subtracts a
    // y-value to reach their toes so collision is properly analyzed by the raycast.
    [SerializeField] private float playerHeight;

    [Header("ray????")]
    [SerializeField] private float rayLength;

    public Transform orientation;

    float horizontalInput;
    float verticalInput;

    Vector3 moveDirection;

    Rigidbody rb;

    ////////////////////////

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        canJump = true;
    }

    private void Update()
    {
        // Checks if grounded via raycast downwards.
        // Raycasts (simplified) are essentially invisible lasers that can be used for detection and stuff when it comes to layers.
        Vector3 rayOrigin = new Vector3(transform.position.x, transform.position.y - playerHeight, transform.position.z);
        Ray r = new Ray(rayOrigin, Vector3.down);
        // Visualization. Ignore. Hence the debug.
        Debug.DrawRay(r.origin, r.direction * rayLength, Color.red);

        grounded = Physics.Raycast(r, rayLength, groundDefinition);

        PlayerInput();
        SpeedControl();

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
    }

    private void MovePlayer()
    {
        // Calculates movement direction.
        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;

        // Adds a force to the Rigidbody. If your grounded, your speed isn't affected by airMultiplier.
        if (grounded)
        {
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);
        }
        else if (!grounded)
        {
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f * airMultiplier, ForceMode.Force);
        }
    }

    private void SpeedControl()
    {
        Vector3 flatVelocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        // Limit the velocity if needed. Basically, if you go faster than your movement speed...
        if (flatVelocity.magnitude > moveSpeed)
        {
            // You will calculate what your maximum velocity WOULD be...
            Vector3 limitedVelocity = flatVelocity.normalized * moveSpeed;

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
