using UnityEngine;

namespace FrustumCullingSpace
{
    [AddComponentMenu("Frustum Culling/Frustum Culling Object")]
    public class FrustumCullingObject : MonoBehaviour
    {   
        public Renderer renderer { get; private set; }
        public bool turnedOff { get; private set; }

        public Transform[] edges = new Transform[4];

        [Tooltip("Shows the edges in scene view as yellow wire spheres.")]
        public bool showEdges = true;
        [Tooltip("Change the radius size of the shown edges to make it easier to be seen.")]
        public float edgesRadius = 0.1f;

        [Tooltip("Enabling this, you can disable the parent object which in turn will disable the entire gameobject and all of it's children. The root will be disabled on cull and re-enabled when in view.")]
        public bool disableParent;
        [Tooltip("Set the parent transform you want to disable/enable on culling.")]
        public Transform parentTransform;


        #region UNITY METHODS

        void Start()
        {
            if (FrustumCulling.instance == null) {
                Debug.LogWarning("No Frustum Culling instance has been added to the scene");
                return;
            }
            
            renderer = GetComponent<Renderer>();
            if (renderer) {
                FrustumCulling.instance.Add(gameObject);
            }
            else {
                Debug.LogWarning($"No renderer found on gameobject: {gameObject.name}. You must put this script on an object with a renderer.");
            }
            

            if (edges.Length == 0) {
                print($"No edges set on gameobject {gameObject.name}. You need to Build Edges from the FrustumCullingObject component.");
                return;
            }


            foreach(var edge in edges) {
                if (edge == null) {
                    print($"There seems to be missing edges on the gameobject {gameObject.name}. Make sure to Build Edges from the FrustumCullingObject component.");
                }
            }
        }

        void OnDrawGizmosSelected() 
        {
            if (!showEdges) {
                return;
            }

            // render the built edges as yellow wire spheres
            for (int i=0; i<edges.Length; i++) {
                if (edges[i] == null) {
                    continue;
                }

                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(edges[i].position, edgesRadius);
            }
        }

        #endregion

        #region SYSTEM METHODS

        // disable object
        public void DisableObject()
        {
            if (turnedOff) return;

            turnedOff = true;

            if (disableParent) {
                ParentTransformState(false);
                return;
            }
            
            gameObject.SetActive(false);
        }

        // enable object 
        public void EnableObject()
        {
            if (!turnedOff) return;

            turnedOff = false;

            if (disableParent) {
                ParentTransformState(true);
                return;
            }
            
            gameObject.SetActive(true);
        }

        void ParentTransformState(bool isEnable)
        {
            if (!isEnable)
            {
                if (parentTransform == null) {
                    Debug.LogWarning($"GameObject name {gameObject.name} is set to cull and disable parent but parent transform is empty. Please set a parent to complete the culling.");    
                    gameObject.SetActive(false);
                    return;
                }

                parentTransform.gameObject.SetActive(false);
                return;
            }

            if (parentTransform == null) {
                Debug.LogWarning($"GameObject name {gameObject.name} is set to re-enable but parent transform property is empty. Please set a parent to complete the re-enabling.");    
                gameObject.SetActive(true);
                return;
            }

            parentTransform.gameObject.SetActive(true);
        }

        // changes the structure of this gameobject and builds edges to work with culling calculations
        public void BuildEdges()
        {
            MeshFilter mf = GetComponent<MeshFilter>();

            if (mf == null) {
                Debug.LogWarning("The game object needs to have a mesh filter.");
                return;
            }

            
            Vector3 chosenPoint = Vector3.zero;

            float highestPointCompare = -Mathf.Infinity;
            float lowestPointCompare = Mathf.Infinity;
            float rightMostPointCompare = -Mathf.Infinity;
            float leftMostPointCompare = Mathf.Infinity;

            Vector3 highestPoint = Vector3.zero;
            Vector3 lowestPoint = Vector3.zero;
            Vector3 rightPoint = Vector3.zero;
            Vector3 leftPoint = Vector3.zero;

            int max = mf.sharedMesh.vertices.Length - 1;

            // get top and bottom of mesh
            for (int i=0; i<max; i++) {
                Vector3 item = mf.sharedMesh.vertices[i];

                // get highest point
                if (item.y > highestPointCompare) {
                    highestPointCompare = item.y;
                    highestPoint = new Vector3(0, item.y, 0);
                    continue;
                }


                // get lowest point
                if (item.y < lowestPointCompare) {
                    lowestPointCompare = item.y;
                    lowestPoint = new Vector3(0, item.y, 0);
                }
            }


            // get right & left most points of mesh
            for (int i=0; i<max; i++) {
                Vector3 item = mf.sharedMesh.vertices[i];
            
                // get right point
                if (item.x > rightMostPointCompare) {
                    rightMostPointCompare = item.x;

                    if (highestPoint.y <= 0.5f) {
                        rightPoint = new Vector3(item.x, 0, 0);
                        continue;
                    }

                    rightPoint = new Vector3(item.x, Mathf.Clamp(highestPoint.y/2, 0, item.y), 0);
                }


                // get left point
                if (item.x < leftMostPointCompare) {
                    leftMostPointCompare = item.x;

                    if (highestPoint.y <= 0.5f) {
                        leftPoint = new Vector3(item.x, 0, 0);
                        continue;
                    }

                    leftPoint = new Vector3(item.x, Mathf.Clamp(highestPoint.y/2, 0, item.y), 0);
                }
            }

            
            // force edges length
            edges = new Transform[4];

            
            // set the edges
            for (int i=0; i<4; i++) {
                GameObject go = new GameObject();
                go.transform.parent = transform;
                go.AddComponent(typeof(FrustumCullingEdge));
                edges[i] = go.transform;
                
                if (i == 0) {
                    go.name = "TopEdge";
                    go.transform.localPosition = highestPoint;
                    continue;
                }

                if (i == 1) {
                    go.name = "BottomEdge";
                    go.transform.localPosition = lowestPoint;
                    continue;
                }

                if (i == 2) {
                    go.name = "RightEdge";
                    go.transform.localPosition = rightPoint;
                    continue;
                }

                if (i == 3) {
                    go.name = "LeftEdge";
                    go.transform.localPosition = leftPoint;
                    continue;
                }
            }
        }

        // return the edges transforms
        public Transform[] GetEdges()
        {
            return edges;
        }

        public bool CheckIfEdgesBuilt()
        {
            if (edges.Length < 4) return false;
            
            for (int i=0; i<4; i++) {
                if (edges[i] != null) {
                    return true;
                }
            }

            return false;
        }

        #endregion
    }
}