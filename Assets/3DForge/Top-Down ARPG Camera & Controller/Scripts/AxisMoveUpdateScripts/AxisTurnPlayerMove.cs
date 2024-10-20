using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
/// <summary>
/// Axis Turn Player Move.
/// Created By: Juandre Swart 
/// Email: info@3dforge.co.za
/// 
/// This script controls the player movement. Use the Vertical axis to move forward and back.
/// Use the horizontal axis to turn player left and right.
/// </summary>

// This script requires a character controller to be attached
[RequireComponent (typeof (CharacterController))]
public class AxisTurnPlayerMove : MonoBehaviour {

	public float moveSpeed = 6.0f; // Character movement speed.
	public int rotationSpeed = 120; // How quick the character rotate to target location.
	public float gravity = 20.0f; // Gravity for the character.
	public float jumpSpeed = 8.0f; // The Jump speed
	
	private Transform myTransform;	
	private CharacterController controller;
	private Animator animator; // The animator for the toon. 
	
	private Vector3 moveDirection = Vector3.zero; // The move direction of the player.
	
	void Start()
	{			
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
		
		float xMovement = Input.GetAxis("Horizontal");// The horizontal movement.
		float zMovement = Input.GetAxis("Vertical");// The vertical movement.
		
		// Are whe grounded, yes then move.
		if (IsGrounded()) {	
			
	       	moveDirection = new Vector3(0, 0, zMovement);
	       	moveDirection = myTransform.TransformDirection(moveDirection);
	       	moveDirection *= moveSpeed;
			
			// Make the player jump.
	       	if (Input.GetButton("Jump"))
		   	{
	       		moveDirection.y = jumpSpeed;
				// TODO Add jump animation.
		   	}     			
			
       	}
		
		// Rotate player with horizontal movement.
		myTransform.localEulerAngles += Vector3.up * (xMovement * rotationSpeed *Time.deltaTime);
		
		// Apply gravity.
		moveDirection.y -= gravity * Time.deltaTime;        		
		
		// Are we moving.
		if(animator!=null)
		{
			animateCharacter(zMovement);		
		}
		// Move the player.	
		controller.Move(moveDirection * Time.deltaTime);					
	}
	
	// Check if the player is grounded.
	bool IsGrounded () {
		return controller.isGrounded;
	}
	
	/// <summary>
	/// Animates the character using Animator.
	/// Here you can set your animation variable.
	/// </summary>
	/// <param name='xMovement'>
	/// X axis movement.
	/// </param>
	/// <param name='zMovement'>
	/// Z axis movement.
	/// </param>
	private void animateCharacter(float zMovement)
	{
		animator.SetFloat("speed", zMovement);
	}
	
}
