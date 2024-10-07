using UnityEngine;
using UnityEditor;

namespace FrustumCullingSpace
{
    [CustomEditor(typeof(FrustumCulling))]
    public class FrustumCullingCustomInspector : Editor
    {
        SerializedProperty autoCatchCamera,
        mainCam,
        gameView,
        activationDirection,
        xRangeTwoD,
        
        runEveryFrames,
        restingFrames,
        
        cullInScene,
        preventCullingCloseObjects,
        closeObjectsDistance,

        distanceCulling,
        distanceToCull,
        prioritizeDistanceCulling,
        distanceCullingOnly;

        FrustumCulling script;
        

        void OnEnable()
        {
            autoCatchCamera = serializedObject.FindProperty("autoCatchCamera");
            mainCam = serializedObject.FindProperty("mainCam");
            gameView = serializedObject.FindProperty("gameView");
            activationDirection = serializedObject.FindProperty("activationDirection");
            xRangeTwoD = serializedObject.FindProperty("xRangeTwoD");

            runEveryFrames = serializedObject.FindProperty("runEveryFrames");
            restingFrames = serializedObject.FindProperty("restingFrames");

            cullInScene = serializedObject.FindProperty("cullInScene");
            preventCullingCloseObjects = serializedObject.FindProperty("preventCullingCloseObjects");
            closeObjectsDistance = serializedObject.FindProperty("closeObjectsDistance");
            
            distanceCulling = serializedObject.FindProperty("distanceCulling");
            distanceToCull = serializedObject.FindProperty("distanceToCull");
            prioritizeDistanceCulling = serializedObject.FindProperty("prioritizeDistanceCulling");
            distanceCullingOnly = serializedObject.FindProperty("distanceCullingOnly");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update ();

            var button = GUILayout.Button("Click for more tools");
            if (button) Application.OpenURL("https://bit.ly/3CyjBzT");
            EditorGUILayout.Space(10);

            FrustumCulling script = (FrustumCulling) target;
            
            
            EditorGUILayout.LabelField("Camera Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(autoCatchCamera);
            if (script.autoCatchCamera == false) {
                EditorGUILayout.PropertyField(mainCam);
            }
            EditorGUILayout.PropertyField(gameView);
            EditorGUILayout.PropertyField(activationDirection);
            if (script.gameView == FrustumCulling.GameViewOption.TwoD) {
                EditorGUILayout.PropertyField(xRangeTwoD);
            }
            EditorGUILayout.Space(10);


            EditorGUILayout.LabelField("Performance Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(runEveryFrames);
            EditorGUILayout.PropertyField(restingFrames);
            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("Objects Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(cullInScene);
            EditorGUILayout.PropertyField(preventCullingCloseObjects);
            if (script.preventCullingCloseObjects) {
                EditorGUILayout.PropertyField(closeObjectsDistance);
            }
            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("Distance Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(distanceCulling);
            
            EditorGUI.BeginDisabledGroup(script.distanceCulling == false);
                EditorGUILayout.PropertyField(distanceToCull);
                
                EditorGUI.BeginDisabledGroup(script.distanceCullingOnly == true);
                    EditorGUILayout.PropertyField(prioritizeDistanceCulling);
                EditorGUI.EndDisabledGroup ();
                
                EditorGUILayout.PropertyField(distanceCullingOnly);
            EditorGUI.EndDisabledGroup ();


            serializedObject.ApplyModifiedProperties();
        }
    }
}
