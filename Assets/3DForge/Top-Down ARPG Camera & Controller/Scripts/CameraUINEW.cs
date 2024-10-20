using UnityEngine;
using UnityEngine.UI;
using System.Collections;
/// <summary>
/// Camera UI.
/// Player move.
/// Created By: Juandre Swart 
/// Email: info@3dforge.co.za
/// 
/// This script helps with UI functions that controls which 
/// camera script is active and displays descriptions on how to use the camera.
/// It is only used for the demo scenes.
/// </summary>
public class CameraUINEW : MonoBehaviour {

	public Transform camWithBothScriptsTransform;
	
	public ARPGCameraController myARPGCameraController;
	public ARPGFollowCameraController myARPGFollowCameraController;

	public Slider rdSlider;
	public Text labelChange;

	// Use this for initialization
	void Start () {
		if (camWithBothScriptsTransform != null) {
			myARPGCameraController = camWithBothScriptsTransform.GetComponent<ARPGCameraController>();
			myARPGFollowCameraController = camWithBothScriptsTransform.GetComponent<ARPGFollowCameraController>();
			myARPGFollowCameraController.enabled = false;
		}
		if (myARPGFollowCameraController != null) {
			rdSlider.value = myARPGFollowCameraController.rotationDamping;
		}
		rdSlider.gameObject.SetActive(false);

	}

	public void OnRotationDampingChange(){
		myARPGFollowCameraController.rotationDamping = rdSlider.value;
	}

	public void OnFollowCamChecked(){
		myARPGCameraController.enabled = false;
		myARPGFollowCameraController.enabled = true;
		rdSlider.gameObject.SetActive(true);
		labelChange.text = "- Adjust the camera rotation dampening speed:";
	}

	public void OnNormalARPGCamChecked(){
		myARPGFollowCameraController.enabled = false;
		myARPGCameraController.enabled = true;
		rdSlider.gameObject.SetActive(false);
		labelChange.text = "- Adjust Camera y angle by holding down middle mouse and moving mouse left/right.";
	}

}
