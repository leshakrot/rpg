using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
/// <summary>
/// Mobile ARPG Player move.
/// Created By: Juandre Swart 
/// Email: info@3dforge.co.za
/// 
/// This script controls the player movement. Simple raycast gets the target location and ignore layers provided, then the player
/// moves to target location stopping if it hits something. Can be easily modified to work with Unity Pro Nav Mesh.
/// </summary>

// This script requires a character controller to be attached
[RequireComponent (typeof (CharacterController))]
public class MobileARPGPlayerMove : MonoBehaviour {

	public int moveSpeed = 8; // Character movement speed.
	public int rotationSpeed = 8; // How quick the character rotate to target location.
	public float distanceError = 0.5f; // The distance where you stop the character between the difference of target.position and character.position.
	public float gravity = 0.4f; // Gravity for the character.
	public float rayCastDisntance = 500.0f; // The ray casting distance of the mouse click.
	public LayerMask layerMask = 1<<7; // The layers the raycast should ignore.
	public Rect[] guiPositions; // The position of the GUI, so that the touch ignore and player dont move.
	public bool[] guiPositionsXFromRight; // If quiPosition i(any qui position) calculates from screen width set i to true.
	public bool[] guiPositionsYFromBottom; // If quiPosition i(any qui position) calculates from screen height set i to true.
	public float minMultiInputTouchWaitTime = 0.8f; // Stop player from moving when user zoomed in with pinch for duration.
	
	
	private Camera myCamera;
	private Transform myTransform;	
	private Vector3 currentMoveToPos; // The position of the mouse click, the location where the character should go.
	private bool hasTargetPosition = false; // Tells us if there is a target to move to.
	private CharacterController controller;
	private Animator animator; // The animator for the toon. 
	private CollisionFlags collisionFlags; 	
	private bool buttonDown = false;	// If player holds the mouse button down.	
	private float verticalSpeed = 0.0f; // The current vertical speed.		
			
	
	private float lastMultiInputTouch = 0;	
	
	void Start()
	{		
		layerMask=~layerMask; // Get all the layers to raycast on, this will allow the raycast to ignore chosen layers.
		myCamera = Camera.main; // Get main camera as the camera will not always be a child GameObject.
		if(myCamera == null)
		{
			Debug.LogError("No main camera, please add camera or set camera to MainCamera in the tag option.");
		}
		myTransform = transform;		
		// Get the player character controller.
		controller = myTransform.GetComponent<CharacterController>();			

		// Get the player animator in child.
		try
		{
			animator = myTransform.GetComponentInChildren<Animator>();						
		}
		catch(Exception e)
		{
			Debug.LogWarning("No animator attached to character." + e.Message);
		}	
		
		if(guiPositions == null)
		{
			guiPositions = new Rect[0];
		}
		
		if(guiPositions.Length != guiPositionsXFromRight.Length)
		{
			Debug.LogWarning("QUI Position lenght does not equal QUI Positions From Right or Bottom lenght. Setting QUI Positions From Right or Bottom lenght to QUI Position lenght.");
			guiPositionsXFromRight = new bool[guiPositions.Length];			
		}
		
		if(guiPositions.Length != guiPositionsYFromBottom.Length)
		{
			Debug.LogWarning("QUI Position lenght does not equal QUI Positions From Right or Bottom lenght. Setting QUI Positions From Right or Bottom lenght to QUI Position lenght.");
			guiPositionsYFromBottom = new bool[guiPositions.Length];			
		}
		
	}
	
	public void Update()
	{			
		
		// Get the mouse pressed position in world.
		int tc = Input.touchCount;
		if (tc == 1 && !isTouchInGUI(Input.GetTouch(0).position, guiPositions, guiPositionsXFromRight, guiPositionsYFromBottom) && (Time.time - lastMultiInputTouch > minMultiInputTouchWaitTime) )
		{
			Ray ray = myCamera.ScreenPointToRay(Input.GetTouch(0).position);
			RaycastHit hit;
			if (Physics.Raycast(ray,out hit,rayCastDisntance, layerMask)) 
			{
				currentMoveToPos = hit.point;	
				// Keep target height same as player height for accuracy.
				currentMoveToPos.y = myTransform.position.y;
				if(Vector3.Distance(myTransform.position, currentMoveToPos) > distanceError)
				{
					hasTargetPosition = true;
				}				
			}
			buttonDown = true;
		}
		else if(tc>=2)
		{
			lastMultiInputTouch = Time.time;
			buttonDown = false;
		}
		else
		{
			buttonDown = false;
		}
		
		// Make sure the character stays on the ground.
		ApplyGravity ();	
		
		// Keep target height same as player height for accuracy.
		currentMoveToPos.y = myTransform.position.y;
		
		// Was a successful move enabled.
		if(hasTargetPosition)
		{													
			// Look at target.
			myTransform.rotation = Quaternion.Slerp(myTransform.rotation, Quaternion.LookRotation(currentMoveToPos - myTransform.position), rotationSpeed * Time.deltaTime);		
			// Move to target location.
			collisionFlags = controller.Move((myTransform.forward * moveSpeed * Time.deltaTime)+( new Vector3 (0, verticalSpeed, 0)));
			if(animator != null)
			{
				animator.SetBool("run", true);
			}			
			// Check if side was hit and stop character.
			if(collisionFlags.ToString().Equals("5"))
			{
				hasTargetPosition = false;
				// Set character to previous position so animation of running/walking happens next.
				collisionFlags = controller.Move((myTransform.forward * -1 * moveSpeed * Time.deltaTime)+( new Vector3 (0, verticalSpeed, 0)));
				if(animator != null)
				{
					animator.SetBool("run", false);
				}
			}
			// Calculate distance to target location and stop if in range.
			if(Vector3.Distance(myTransform.position, currentMoveToPos) <= distanceError && !buttonDown)
			{				
				hasTargetPosition = false;				
				if(animator != null)
				{
					animator.SetBool("run", false);
				}
			}
		}
		else if(verticalSpeed != 0.0f)
		{
			controller.Move(new Vector3 (0, verticalSpeed, 0));
		}
		
	}
	
	// Check if the player is grounded.
	bool IsGrounded () {
		return controller.isGrounded;
	}	
	
	void ApplyGravity ()
	{	
		if (IsGrounded ())
		{
			verticalSpeed = 0.0f;
		}
		else
		{
			verticalSpeed -= gravity * Time.deltaTime;	
		}
	}
	
	// Check all qui positions
	public static bool isTouchInGUI(Vector3 pos, Rect[] guiPositions, bool[] fromRight, bool[] fromBottom)
	{
		for(int i = 0; i < guiPositions.Length ; i++)
		{
			if(isTouchHovering(pos, guiPositions[i], fromRight[i], fromBottom[i]))
			{
				return true;
			}
		}		
		
		return false;
	}
	
	/// <summary>
	/// Static method that can be called from any script to check if script is running.
	/// </summary>
	/// <returns>
	/// Boolean if pos(mouse) is in rectangle.
	/// </returns>
	/// <param name='pos'>
	/// The position of the mouse
	/// </param>
	/// <param name='rect'>
	/// The rectangle you want to test in.
	/// </param>
	/// <param name='yFromBottom'>
	/// If set to true then rectangle(UI) at the bottom of the screen and the y is set to offsett.
	/// </param>
	private static bool isTouchHovering(Vector3 pos, Rect rect, bool fromRight, bool fromBottom)
	{	
		float w = Screen.width;
		float h = Screen.height;
		float x = pos.x;
	    float y = h - pos.y;
		if(fromRight)
		{
			rect.x = w - rect.x;
		}
		if(fromBottom)
		{
			rect.y = h - rect.y;
		}
		return rect.Contains(new Vector2(x, y));  
	}
	
}