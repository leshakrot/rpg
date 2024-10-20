using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Axis Turn Player Move.
/// Created By: Juandre Swart 
/// Email: info@3dforge.co.za
/// 
/// This script controls the player movement. Use the joystick to move forward and back.
/// Use the joystick to turn player left and right.
/// </summary>

// This script requires a character controller to be attached
[RequireComponent(typeof(CharacterController))]
public class MobileAxisTurnPlayerMove : MonoBehaviour
{
    public float moveSpeed = 6.0f; // Character movement speed.
    public int rotationSpeed = 120; // How quick the character rotates to target location.
    public float gravity = 20.0f; // Gravity for the character.
    public float jumpSpeed = 8.0f; // The Jump speed
    public MyJoystick myJoystick; // The mobile joystick
    public MobileARPGBaseCameraController mobileCameraController;
    public Image jumpButton;

    private Transform myTransform;
    private CharacterController controller;
    private Animator animator; // The animator for the toon. 

    private Vector3 moveDirection = Vector3.zero; // The move direction of the player.

    void Start()
    {
        myTransform = transform;
        controller = myTransform.GetComponent<CharacterController>();

        animator = myTransform.GetComponentInChildren<Animator>();
        if (animator == null)
        {
            Debug.LogWarning("No animator attached to character.");
        }
    }

    public void Update()
    {
        float xMovement = myJoystick.position.x; // The horizontal movement
        float zMovement = myJoystick.position.y; // The vertical movement

        // Are we grounded? If yes, then move.
        if (IsGrounded())
        {
            moveDirection = new Vector3(0, 0, zMovement);
            moveDirection = myTransform.TransformDirection(moveDirection) * moveSpeed;

            // Check for jump input.
            if (jumpButton != null && IsJumpButtonPressed())
            {
                moveDirection.y = jumpSpeed;
                // TODO: Add jump animation if needed.
            }
        }

        // Rotate player with horizontal movement.
        myTransform.localEulerAngles += Vector3.up * (xMovement * rotationSpeed * Time.deltaTime);

        // Apply gravity.
        moveDirection.y -= gravity * Time.deltaTime;

        // Animate character if animator is present.
        if (animator != null)
        {
            animateCharacter(zMovement, xMovement);
        }

        // Move the player.	
        controller.Move(moveDirection * Time.deltaTime);
    }

    // Check if the player is grounded.
    bool IsGrounded()
    {
        return controller.isGrounded;
    }

    // Check if jump button is pressed.
    private bool IsJumpButtonPressed()
    {
        for (int i = 0; i < Input.touchCount; i++)
        {
            if (jumpButton != null)
            {
                Vector2 touchPosition = Input.GetTouch(i).position;
                if (RectTransformUtility.RectangleContainsScreenPoint(jumpButton.rectTransform, touchPosition, Camera.main))
                {
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Animates the character using Animator.
    /// Here you can set your animation variable.
    /// </summary>
    /// <param name='zMovement'>Z axis movement.</param>
    /// <param name='xMovement'>X axis movement.</param>
    private void animateCharacter(float zMovement, float xMovement)
    {
        mobileCameraController.pinchZoom = (zMovement == 0 && xMovement == 0);
        animator.SetFloat("speed", zMovement);
    }
}
