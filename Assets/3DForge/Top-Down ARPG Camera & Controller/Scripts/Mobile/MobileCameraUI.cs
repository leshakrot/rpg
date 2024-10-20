using UnityEngine;
using System;
using System.Collections;
/// <summary>
/// Camera UI.
/// Player move.
/// Created By: Juandre Swart 
/// Email: info@3dforge.co.za
/// 
/// This script creates a GUI that displays controls.
/// It is only used for the demo scene.
/// </summary>
public class MobileCameraUI : MonoBehaviour {
	
	public MobileARPGFollowCameraController myMobileARPGFollowCameraController;		
	public int selGridInt = 0; // The current checked radio button
	
	public int currentDescription = 0;
    private string[] selStrings = new string[] {"Normal top-down ARPG camera", "Follow Camera"};// Strings for camera radio buttons.
		
	//private string[] buttonText = new string[] {"ARPG click movement", "Keys and mouse movement", "Keys and mouse position movement", "Key movement", "4.AxisTurnScene"};
	private string[] descriptionText = new string[]{"Move player by touching the desired position on screen.",
													"Move player with left joystick. Rotate player with right joystick.",
													"Move player with the left joystick.",
													"Move player with the left joystick."};		
	
	public float rotationDamping;
	
	void Start()
	{
		if(myMobileARPGFollowCameraController != null)
		{
			rotationDamping = myMobileARPGFollowCameraController.rotationDamping;
		}
	}
	
	void Update()
	{		
		if(myMobileARPGFollowCameraController != null)
		{			
			myMobileARPGFollowCameraController.rotationDamping = rotationDamping;
		}
		
	}
	
	//Create UI in bottom left corner
	void OnGUI()
	{		
				
		drawCamUI();							
		
	}
	
	private void drawCamUI()
	{
		GUI.BeginGroup(new Rect(10, Screen.height-140, 240, 130));
		GUI.Box(new Rect(0,0,240,130),"");
		
		GUI.Label(new Rect(10, 10, 220, 30),selStrings[selGridInt]);
		
		GUI.Label(new Rect(10, 30, 230, 30),"- Zoom using pinch.");			
		
		if(selGridInt == 1)
		{
			GUI.Label(new Rect(10, 50, 230, 20),"- Adjust the camera rotation dampening speed:");
			rotationDamping = GUI.HorizontalSlider(new Rect(10, 70, 100, 30), rotationDamping, 0.5f, 20.0f);
			GUI.Label(new Rect(10, 85, 230, 50),"- " + descriptionText[currentDescription]);
		}
		else
		{
			GUI.Label(new Rect(10, 50, 230, 50),"- " + descriptionText[currentDescription]);
		}
		GUI.EndGroup();		
	}
	
}
