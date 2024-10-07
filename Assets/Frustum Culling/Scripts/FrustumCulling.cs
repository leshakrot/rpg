using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using FrustumCullingSpace;

[DisallowMultipleComponent]
[AddComponentMenu("Frustum Culling/Frustum Culling")]
public class FrustumCulling : MonoBehaviour
{
    [Tooltip("Automatically get the main active camera on startup. Your camera needs to have the [MainCamera] tag.")]
    public bool autoCatchCamera = true;
    [Tooltip("Manually drag and drop the game camera here, better for performance on game start.")]                                 
    public Camera mainCam;
    [Tooltip("The type of game view of your game. (Two D) includes 2D and 2.5D game views. While (Three D) is first person and third person games.")]
    public GameViewOption gameView = GameViewOption.ThreeD; 
    [Range(0, 2), Tooltip("Enable/disable object when a ledge screen point is bigger or equals to this value. Better to leave this property alone unless objects don't re-enable on correct angles.")]
    public float activationDirection = 1;
    [Min(-1), Tooltip("This will only be considered if the [Game View] property is set to (Two D). The two values is the range of each edge in X axis it needs to be within to be activated. Better to leave this property alone unless you're having culling problems.")]
    public Vector2 xRangeTwoD = new Vector2(-0.1f, 1.1f); 

    [Range(0, 30), Tooltip("Run the logic and checks every (this set) frames. The larger the number, the better the performance, but may cause inaccuracies. Suggested from 5-7. Depends on the pace of your game.")]       
    public int runEveryFrames = 5;
    [Range(0, 10), Tooltip("The amount of frames to rest before looping to the next item in the list. This helps spread the list functionality of culling across several frames which improves performance.")]
    public int restingFrames = 2;

    [Tooltip("Show the culling in scene view. This may decrease precision. It may disable the object before it gets out of view. This property is editor only and on game build the system automatically falls back to max precision by not taking this into account.")]
    public bool cullInScene = true;
    [Tooltip("If enabled, the system will prevent culling any object that is too close by a set distance.")]
    public bool preventCullingCloseObjects = true;
    [Min(0), Tooltip("Set the distance that is considered too close. If the distance between the camera to an object is equal or less to this value then culling will be prevented.")]
    public float closeObjectsDistance = 5;
    
    [Tooltip("Check whether culling should take distance into consideration. Distance culling only happens when object is outside view.")]
    public bool distanceCulling;
    [Tooltip("The distance if exceeded the object will always be culled.")]                                 
    public float distanceToCull = 0f;
    [Tooltip("If distance exceeded the object will instantly be turned off and not wait to be out of view first.")]                                
    public bool prioritizeDistanceCulling;
    [Tooltip("Only distance culling will be applied no camera culling will occur.")]
    public bool distanceCullingOnly;


    #region SYSTEM VARIABLES

    public static FrustumCulling instance;
    List<FrustumCullingObject> objectsList = new List<FrustumCullingObject>();

    int framesElapsed = 0;
    int restFramesElapsed = 0;
    
    public enum GameViewOption {
        ThreeD,
        TwoD
    }

    bool isLoopingCull;

    #endregion

    #region UNITY METHODS

    void Awake()
    {
        if (autoCatchCamera) mainCam = Camera.main;

        if (!FrustumCulling.instance) {
            instance = this;
        }
    }

    void Update()
    {   
        // run every 5 frames (for performance)
        if (framesElapsed < runEveryFrames) {
            framesElapsed++;
            return;
        }

        framesElapsed = 0;

        // catch camera during runtime if auto catch is set
        if (autoCatchCamera && mainCam == null) {
            mainCam = Camera.main;
            
            if (mainCam == null) {
                Debug.LogWarning("No camera found. Make sure your camera has the MainCamera tag.");
                return;
            }
        }

        // if no auto catch and no set camera
        if (mainCam == null) {
            Debug.LogWarning("No camera set or found.");
            return;
        }

        // if distance culling only set
        if (distanceCulling && distanceCullingOnly) {
            DistanceCulling();
            return;
        }

        // Frustum and distance culling
        if (!isLoopingCull) {
            StartCoroutine("CameraCulling");
        }
    }

    #endregion
    
    #region MAIN METHODS
    
    // cull the objects when they're out of view
    IEnumerator CameraCulling()
    {
        isLoopingCull = true;

        for (int i=0; i<objectsList.Count; i++) 
        {
            if (i > 0) {
                while (restFramesElapsed < restingFrames) {
                    restFramesElapsed++;
                    yield return null;
                }
            }

            restFramesElapsed = 0;

            FrustumCullingObject script = objectsList[i];
            if (script == null) {
                objectsList.RemoveAt(i);
                continue;
            }

            bool distanceOk = true;

            if (distanceCulling) 
            {
                float distance = Vector3.Distance(script.transform.position, mainCam.transform.position);

                if (distance > distanceToCull) distanceOk = false;
                else distanceOk = true;

                if (prioritizeDistanceCulling) 
                {
                    if (!distanceOk) {
                        script.DisableObject();
                        continue;
                    }
                }
            }

            // increase precision by not disabling object if renderer is visible (in game build OR if option set)
            if (!Application.isEditor || !cullInScene) {
                if (script.renderer.isVisible) {
                    continue;
                }
            }


            Transform[] edges = script.GetEdges();
            bool isCornerVisible = false;

            // check if any edge is nearing camera view port
            for (int x=0; x<4; x++) 
            {
                if (edges[x] == null) continue;
                
                if (preventCullingCloseObjects) {
                    float dist = Vector3.Distance(script.transform.position, mainCam.transform.position);
                    if (dist <= closeObjectsDistance) {
                        isCornerVisible = true;
                        break;
                    }
                }
                

                // change edge to view port position and check whether it's about to enter camera frame or not
                Vector3 screenPoint = mainCam.WorldToViewportPoint(edges[x].position);
                
                if (gameView == GameViewOption.ThreeD) 
                {
                    if (screenPoint.z >= activationDirection) {
                        isCornerVisible = true;
                        break;
                    }

                    continue;
                }

                if (screenPoint.z >= activationDirection) 
                {
                    if (screenPoint.x >= xRangeTwoD.x && screenPoint.x <= xRangeTwoD.y) {
                        isCornerVisible = true;
                        break;
                    }
                }               
            }


            // enable/disable game object depending on results
            if (isCornerVisible) {
                if (distanceOk) {
                    script.EnableObject();
                }
            }
            else {
                script.DisableObject();
            }
        }

        isLoopingCull = false;
    }

    // cull the objects when they're too far from set distance
    void DistanceCulling()
    {
        for (int i=0; i<objectsList.Count; i++) 
        {
            FrustumCullingObject script = objectsList[i];
            if (script == null) {
                objectsList.RemoveAt(i);
                continue;
            }

            float distance = Vector3.Distance(script.transform.position, mainCam.transform.position);
            if (distance > distanceToCull) {
                script.DisableObject();
                continue;
            }

            script.EnableObject();
        }
    }

    #endregion

    #region PUBLIC APIS
    
    // add a gameobject to the culling -> must have the FrustumCullingObject script
    public void Add(GameObject go)
    {
        FrustumCullingObject objectScript = go.GetComponent<FrustumCullingObject>();

        // check gameobject has script
        if (!objectScript) {
            Debug.LogWarning($"Gameobject: {go.name} doesn't have FrustumCullingObject script.");
            return;
        }

        // check if script is already added to list
        if (objectsList.Contains(objectScript)) {
            return;
        }

        // add to list
        objectsList.Add(objectScript);
    }

    // remove certain game object from culling list -> must have the FrustumCullingObject script
    public void Remove(GameObject go) 
    {
        FrustumCullingObject script = go.GetComponent<FrustumCullingObject>();

        if (script == null) {
            Debug.LogWarning($"The passed object {go.name} doesn't have a FrustumCullingObject component attached thus nothing can be removed from the Frustum Culling system.");
            return;
        }

        if (objectsList.Contains(script)) {
            objectsList.Remove(script);
        }
    }
    
    #endregion
}