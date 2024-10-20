using UnityEngine;
using System;
using System.Collections;
/// <summary>
/// Camera UI.
/// Player move.
/// Created By: Juandre Swart 
/// Email: info@3dforge.co.za
/// 
/// This script creates a GUI that controls which camera script is active and displays descriptions on how to use the camera.
/// Also contains a static method that checks if mouse position in a specific rect.
/// It is only used for the demo scene.
/// </summary>
public class CameraUI : MonoBehaviour {
	
	public Transform camWithBothScriptsTransform;
	
	public ARPGCameraController myARPGCameraController;
	public ARPGFollowCameraController myARPGFollowCameraController;
			
	public int selGridInt = 0; // The current checked radio button
	private int currentCameraSelected = 0; // Current camera script that should be enabled
    private string[] selStrings = new string[] {"Normal top-down ARPG camera", "Follow Camera"};// Strings for camera radio buttons.
	
	private string[] scenes = new string[] {"demo", "1.AxisMouseScene", "2.AxisTopDownMouseScene", "3.AxisScene", "4.AxisTurnScene"};//Scenes added in build settings
	//private string[] buttonText = new string[] {"ARPG click movement", "Keys and mouse movement", "Keys and mouse position movement", "Key movement", "4.AxisTurnScene"};
	private string[] descriptionText = new string[]{"Move player by clicking the mouse on the desired position",
													"Move player with W(up)-forward, S(down)-backwards, A(left)-strafe left, D(right)-starfe right. Rotate player with mouse.",
													"Move player with W(up)-forward, S(down)-backwards, A(left)-strafe left, D(right)-starfe right. Player will rotate to mouse position.",
													"Move player with W(up)-move up, S(down)-move down, A(left)-move left, D(right)-move right",
													"Move player with W(up)-forward, S(down)-backwards, A(left)-turn left, D(right)-turn right"};
	private int currScene = 0;
	
	public float rotationDamping;
	
	void Start()
	{
		//	Check if no camera added, yes then ignore drawCamUI
		if(camWithBothScriptsTransform != null)
		{
			myARPGCameraController = camWithBothScriptsTransform.GetComponent<ARPGCameraController>();
			myARPGFollowCameraController = camWithBothScriptsTransform.GetComponent<ARPGFollowCameraController>();
			myARPGFollowCameraController.enabled = false;
		}
		else if(myARPGFollowCameraController != null)
		{
			rotationDamping = myARPGFollowCameraController.rotationDamping;
		}
		// Get current scene.
		for(int i = 0; i < scenes.Length; i++)
		{
			if(scenes[i] == Application.loadedLevelName)
			{
				currScene = i;
			}
		}	
		
	}
	
	void Update()
	{
		if(currScene<3)
		{
			switch(selGridInt)
			{
				case 0 :
					// Check if new button checked and enable correct camera script
					if(selGridInt != currentCameraSelected)
					{
						currentCameraSelected = selGridInt;
						myARPGFollowCameraController.enabled = false;
						myARPGCameraController.enabled = true;
					
					}				
					break;
				case 1 :
					// Check if new button checked and enable correct camera script
					if(selGridInt != currentCameraSelected)
					{
						currentCameraSelected = selGridInt;
						myARPGCameraController.enabled = false;
						myARPGFollowCameraController.enabled = true;	
						rotationDamping = myARPGFollowCameraController.rotationDamping;
					}
					myARPGFollowCameraController.rotationDamping = rotationDamping;
					break;
			}
		}
		if(currScene == 4)
		{
			currentCameraSelected = selGridInt;
			myARPGFollowCameraController.rotationDamping = rotationDamping;
		}
		
	}
	
	//Create UI in bottom left corner
	void OnGUI()
	{		
				
		drawCamUI();					
		
		drawDescription();
	}
	
	private void drawCamUI()
	{
		GUI.BeginGroup(new Rect(10, Screen.height-170, 240, 160));
		GUI.Box(new Rect(0,0,240,160),"");
		
		if(currScene<3)
		{	
			selGridInt = GUI.SelectionGrid(new Rect(10, 10, 220, 40), selGridInt, selStrings, 1,"toggle");
		}
		GUI.Label(new Rect(10, 50, 230, 20),"- Zoom using mouse wheel.");
		GUI.Label(new Rect(10, 66, 230, 50),"- Adjust Camera x angle by holding down middle mouse and moving mouse up/down.");				
		switch(currentCameraSelected)
		{
			case 0 :
				GUI.Label(new Rect(10, 110, 230, 55),"- Adjust Camera y angle by holding down middle mouse and moving mouse left/right.");
				break;
			case 1 :
				GUI.Label(new Rect(10, 110, 230, 20),"- Adjust the camera rotation dampening speed:");
				rotationDamping = GUI.HorizontalSlider(new Rect(10, 130, 100, 30), rotationDamping, 0.5f, 20.0f);				
				break;
		}
		
		GUI.EndGroup();
	}
	
	private void drawDescription()
	{
		GUI.BeginGroup(new Rect(10, Screen.height - 170-90, 240, 160));
		GUI.Box(new Rect(0, 0, 240, 80), "");
		GUI.Label(new Rect(10,10,220,140), descriptionText[currScene]);
		GUI.EndGroup();
	}
	
	private void drawSceneButtons()
	{
		GUI.BeginGroup(new Rect(260, Screen.height-170, 400, 160));
		GUI.Box(new Rect(0, 0, 400, 160), "");
		GUI.EndGroup();
	}
	
}
