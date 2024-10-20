using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Mobile ARPG follow camera controller.
/// Created By: Juandre Swart 
/// Email: info@3dforge.co.za
/// 
/// A Script for a follow style camera that allows zoom with pinch.
/// It contains code that makes objects transparent if they are between the camera and target.
/// The objects material needs to be a transparent shader so we can change the alpha value.
/// </summary>
public class MobileARPGFollowCameraController : MobileARPGBaseCameraController {
	
	public float rotationDamping = 3.0f; // How fast it should rotate to target angles.
		
	private Transform myTransform;
	private Transform prevHit;
	
	private float prevDistance = 0.0f;
	
	void Start () {
		myTransform = transform;		
		myTransform.position = target.position;			
		
		if(target == null)
		{			
			Debug.LogWarning("No taget added, please add target Game object ");
		}
		
	}
	
	void LateUpdate  () {
		
		if(target == null)
		{			
			return;
		}
		
		// Zoom Camera and keep the distance between [minDistance, maxDistance].
		if(Input.touchCount == 2 && pinchZoom)
		{
			Vector2 touch0 = Input.GetTouch(0).position;
			Vector2 touch1 = Input.GetTouch(1).position;
			
			float distance = Vector2.Distance(touch0, touch1);
			
			// Check prev distance and zoom in or out.
			if(prevDistance != 0)
			{			
				if(prevDistance - distance > 0)
				{
					startingDistance+=Time.deltaTime*zoomSpeed;
					if(startingDistance>maxDistance)
						startingDistance=maxDistance;
				}
				else if(prevDistance - distance < 0)
				{
					startingDistance-=Time.deltaTime*zoomSpeed;
					if(startingDistance<minDistance)
						startingDistance=minDistance;
				}
			}
			
			prevDistance = distance;
			
		}
		else
		{
			prevDistance = 0;
		}
		
		// Calculate the current rotation angles
		float wantedRotationAngle = target.eulerAngles.y;			
		float currentRotationAngle = myTransform.eulerAngles.y;		
		// Damp the rotation around the y-axis
		currentRotationAngle = Mathf.LerpAngle (currentRotationAngle, wantedRotationAngle, rotationDamping * Time.deltaTime);	
		// Convert the angle into a rotation
		Quaternion currentRotation = Quaternion.Euler (camXAngle, currentRotationAngle, 0);
		
		// Position Camera.
		myTransform.position = target.position;
		myTransform.position -= currentRotation * Vector3.forward * startingDistance + new Vector3(0, -1 * targetHeight, 0);
		
		Vector3 targetToLookAt = target.position;
		targetToLookAt.y += targetHeight;
		myTransform.LookAt (targetToLookAt);
			
		//Start checking if object between camera and target
		if(fadeObjects)
		{		
			// Cast ray from camera.position to target.position and check if the specified layers are between them.
			Ray ray = new Ray(myTransform.position, (target.position - myTransform.position).normalized);
			RaycastHit hit;
			if (Physics.Raycast(ray, out hit, maxDistance)) {
			    Transform objectHit = hit.transform;
				if(layersToTransparent.Contains(objectHit.gameObject.layer))
				{
					if(prevHit!=null)
					{
						prevHit.GetComponent<Renderer>().material.color = new Color(1,1,1,1);
					}
					if(objectHit.GetComponent<Renderer>() != null)
					{
						prevHit = objectHit;
						// Can only apply alpha if this material shader is transparent.
						prevHit.GetComponent<Renderer>().material.color = new Color(1,1,1,alpha);
					}
				}		    
				else if(prevHit != null)
				{
					prevHit.GetComponent<Renderer>().material.color = new Color(1,1,1,1);
					prevHit = null;
				}
			}
		}

		
	}
}
