using UnityEngine;
using UnityEditor;

namespace FrustumCullingSpace
{
    [CustomEditor(typeof(FrustumCullingObject))]
    [CanEditMultipleObjects]
    public class FrustumCullingObjectCustomInspector : Editor
    {
        SerializedProperty edges,
        showEdges,
        edgesRadius,
        disableParent,
        parentTransform;

        FrustumCullingObject[] scripts;
        FrustumCullingObject script;

        void OnEnable()
        {
            edges = serializedObject.FindProperty("edges");
            showEdges = serializedObject.FindProperty("showEdges");
            edgesRadius = serializedObject.FindProperty("edgesRadius");
            disableParent = serializedObject.FindProperty("disableParent");
            parentTransform = serializedObject.FindProperty("parentTransform");
        }

        public override void OnInspectorGUI()
        {
            script = (FrustumCullingObject) target;
            var button = GUILayout.Button("Build Edges", GUILayout.Height(40));
            
            // clicking on button
            if (button) {
                Object[] monoObjects = targets;
                
                scripts = new FrustumCullingObject[monoObjects.Length];
                for (int i = 0; i < monoObjects.Length; i++) {
                    scripts[i] = monoObjects[i] as FrustumCullingObject;

                    // check if edges already built
                    if (scripts[i].CheckIfEdgesBuilt()) {
                        // warn user about rebuilding edges
                        if (!EditorUtility.DisplayDialog("Warning!", "The system has detected that you have already built the edges. Are you sure you want to rebuild with the current structure? This may lead to unexpected behaviour.",
                            "Build Edges", "Cancel")) 
                        {
                            return;
                        }
                    }
                    
                    if (scripts[i].edges.Length < 4) {
                        scripts[i].edges = new Transform[4];
                    }

                    scripts[i].BuildEdges();
                    EditorUtility.SetDirty(scripts[i]);
                }
            }
            EditorGUILayout.Space(5);

            EditorGUILayout.PropertyField(edges);
            EditorGUILayout.Space(5);
            
            EditorGUILayout.LabelField("Edge Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(showEdges);
            EditorGUILayout.PropertyField(edgesRadius);
            EditorGUILayout.Space(5);

            EditorGUILayout.LabelField("Parent Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(disableParent);
            if (script.disableParent) {
                EditorGUILayout.PropertyField(parentTransform);
            }
            EditorGUILayout.Space(5);
            
            serializedObject.ApplyModifiedProperties();
        }
    }
}
