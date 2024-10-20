using UnityEngine;
using UnityEngine.UI;
using System.Collections;
/// <summary>
/// Camera UI.
/// Player move.
/// Created By: Juandre Swart 
/// Email: info@3dforge.co.za
/// 
/// This script helps with UI functions.
/// It is only used for the demo scenes.
/// </summary>
public class MobileCameraUINEW : MonoBehaviour {

	public MobileARPGFollowCameraController myMobileARPGFollowCameraController; // Follow camera script

	public Slider rdSlider; // Slider in canvas to change rotationDamoing value

	// Use this for initialization
	void Start () {
		if(myMobileARPGFollowCameraController != null)
		{
			rdSlider.value = myMobileARPGFollowCameraController.rotationDamping;
		}
	}

	// Method called when slider value changes
	public void OnRotationDampingChange(){
		myMobileARPGFollowCameraController.rotationDamping = rdSlider.value;
	}
}
