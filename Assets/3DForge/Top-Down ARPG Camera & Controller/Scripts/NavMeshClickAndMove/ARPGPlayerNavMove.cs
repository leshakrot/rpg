using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
/// <summary>
/// Player move.
/// Created By: Juandre Swart 
/// Email: info@3dforge.co.za
/// 
/// This script controls the player movement. Simple raycast gets the target location and ignore layers provided, then the player
/// moves to target location via NavMesh.
/// </summary>

// This script requires a character controller to be attached
[RequireComponent (typeof (UnityEngine.AI.NavMeshAgent))]
public class ARPGPlayerNavMove : MonoBehaviour {

	public int moveSpeed = 8; // Character movement speed.
	public int rotationSpeed = 300; // How quick the character rotate to target location.
	public int acceleration = 30; // How quick the character accelerates to target direction.
	public float distanceError = 0.5f; // The distance where you stop the character between the difference of target.position and character.position.
	public float gravity = 20.0f; // Gravity for the character.
	public float rayCastDisntance = 500.0f; // The ray casting distance of the mouse click.
	public LayerMask layerMask = 1<<7; // The layers the raycast should ignore.
	public Rect guiRect = new Rect(10, 170, 240, 160); // The HUD or GUI rectangle
	public bool guiRectYFromBottom = true; //If the HUD or GUI is calculated from Screen.heigt – y.
	
	private Camera myCamera;
	private Transform myTransform;	
	private Vector3 currentMoveToPos; // The position of the mouse click, the location where the character should go.
	private bool hasTargetPosition = false; // Tells us if there is a target to move to.
	private UnityEngine.AI.NavMeshAgent navMeshAgent;
	private Animator animator; // The animator for the toon. 
	private bool buttonDown = false;	// If player holds the mouse button down.	
	
	void Start()
	{		
		layerMask=~layerMask; // Get all the layers to raycast on, this will allow the raycast to ignore chosen layers.
		myCamera = Camera.main; // Get main camera as the camera will not always be a child GameObject.
		if(myCamera == null)
		{
			Debug.LogError("No main camera, please add camera or set camera to MainCamera in the tag option.");
		}
		myTransform = transform;				

		// Get the NavMeshAgent and set 
		navMeshAgent = myTransform.GetComponent<UnityEngine.AI.NavMeshAgent>();			
		navMeshAgent.speed = moveSpeed;
		navMeshAgent.angularSpeed = rotationSpeed;
		navMeshAgent.acceleration = acceleration;
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
		
		// Get the mouse pressed position in world.
		if ((Input.GetAxis("Fire1")>0 || Input.GetAxis("Fire2")>0) && !isMouseHovering(Input.mousePosition, guiRect, guiRectYFromBottom))
		{
			Ray ray = myCamera.ScreenPointToRay(Input.mousePosition);
			RaycastHit hit;
			if (Physics.Raycast(ray,out hit,rayCastDisntance, layerMask)) 
			{
				currentMoveToPos = hit.point;	
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
		
		// Was a successful move enabled.
		if(hasTargetPosition)
		{
			// Move to target location.
			navMeshAgent.SetDestination(currentMoveToPos);

			if(animator != null)
			{
				animator.SetBool("run", true);
			}

			// Calculate distance to target location and stop if in range.
			if(Vector3.Distance(myTransform.position, currentMoveToPos) <= distanceError && !buttonDown)
			{				
				hasTargetPosition = false;				
				if(animator != null)
				{
					animator.SetBool("run", false);
				}
				navMeshAgent.Stop();
			}
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
	public static bool isMouseHovering(Vector3 pos, Rect rect, bool yFromBottom)
	{
		float h = Screen.height;
		float x = pos.x;
		float y = h - pos.y;
		if(yFromBottom)
		{
			rect.y = h - rect.y;
		}
		return rect.Contains(new Vector2(x, y));  
	}
}
