using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
/// <summary>
/// Mobile ARPG base camera controller.
/// This is just the super class for the Camera Controllers.
/// </summary>
public class MobileARPGBaseCameraController : MonoBehaviour {
	
	public Transform target;
	public float startingDistance = 10f; // Distance the camera starts from target object.
	public float maxDistance = 20f; // Max distance the camera can be from target object.
	public float minDistance = 3f; // Min distance the camera can be from target object.
	public float zoomSpeed = 20f; // The speed the camera zooms in.
	public float camXAngle = 45.0f; // The camera x euler angle.
	public bool fadeObjects = false; // Enable objects of a certain layer to be faded.
	public List<int> layersToTransparent = new List<int>();	// The layers where we will allow transparency.
	public float alpha = 0.3f; // The alpha value of the material when player behind object.
	public float targetHeight = 2.0f; // The amount from the target object pivot the camera should look at.
	public bool pinchZoom = true; // If the camera is allowed to check for pichZoom.
	
}
