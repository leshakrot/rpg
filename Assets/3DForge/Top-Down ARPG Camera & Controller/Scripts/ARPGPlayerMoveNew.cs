using UnityEngine;
using UnityEngine.EventSystems;
using System;
using System.Collections;
using System.Collections.Generic;
/// <summary>
/// Player move.
/// Created By: Juandre Swart 
/// Email: info@3dforge.co.za
/// 
/// This script controls the player movement. Simple raycast gets the target location and ignore layers provided, then the player
/// moves to target location stopping if it hits something. Can be easily modified to work with Unity Pro Nav Mesh.
/// From version 1.1 added 2 methods as suggested:
/// MoveToPosition(Vector3 target) , public method that can be called.(Can use for AI)
/// TeleportToPosition(Vector3 target), public method.
/// </summary>

// This script requires a character controller to be attached
[RequireComponent (typeof (CharacterController))]
public class ARPGPlayerMoveNew : MonoBehaviour {

	public int moveSpeed = 8; // Character movement speed.
	public int rotationSpeed = 8; // How quick the character rotate to target location.
	public float distanceError = 0.5f; // The distance where you stop the character between the difference of target.position and character.position.
	public float gravity = 0.4f; // Gravity for the character.
	public float rayCastDisntance = 500.0f; // The ray casting distance of the mouse click.
	public LayerMask layerMask = 1<<7; // The layers the raycast should ignore.
	
	private Camera myCamera;
	private Transform myTransform;	
	private Vector3 currentMoveToPos; // The position of the mouse click, the location where the character should go.
	private bool hasTargetPosition = false; // Tells us if there is a target to move to.
	private CharacterController controller;
	private Animator animator; // The animator for the toon. 
	private CollisionFlags collisionFlags; 	
	private bool buttonDown = false;	// If player holds the mouse button down.	
	private float verticalSpeed = 0.0f; // The current vertical speed.		
	
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
	}
	
	public void Update()
	{		
	
		// Get the mouse pressed position in world and check if mouse pressed in UI(Ignore if true).
		// Ignores UI if mouse clicked is hold down.
		if ((Input.GetAxis("Fire1")>0 || Input.GetAxis("Fire2")>0) && (buttonDown || !EventSystem.current.IsPointerOverGameObject()))
		{
			Ray ray = myCamera.ScreenPointToRay(Input.mousePosition);
			RaycastHit hit;
			if (Physics.Raycast(ray,out hit,rayCastDisntance, layerMask)) 
			{
				currentMoveToPos = hit.point;	
				// Keep target height same as player height for accuracy.
				currentMoveToPos.y = myTransform.position.y;
				//Check if character not already at target position then move
				if(Vector3.Distance(myTransform.position, currentMoveToPos) > distanceError)
				{
					hasTargetPosition = true;
				}				
			}
			buttonDown = true;
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
			verticalSpeed -= gravity* Time.deltaTime ;	
		}
	}
	
	/// <summary>
	/// Moves to target position given.
	/// </summary>
	/// <param name='target'>
	/// The target position the character should move to.
	/// </param>
	public void MoveToPosition(Vector3 target)
	{
		currentMoveToPos = target;
		//Check if character not already at target position then move
		if(Vector3.Distance(myTransform.position, currentMoveToPos) > distanceError)
		{
			hasTargetPosition = true;
		}					
	}
	
	/// <summary>
	/// Teleports to target position given.
	/// </summary>
	/// <param name='target'>
	/// The target position the character should move to immediately.
	/// </param>
	public void TeleportToPosition(Vector3 target)
	{
		myTransform.position = target;
	}	

}
