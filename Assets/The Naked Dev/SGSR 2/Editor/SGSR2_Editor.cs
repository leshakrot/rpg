using UnityEngine;
using UnityEditor;

namespace TND.SGSR2
{
    [CustomEditor(typeof(SGSR2_Base), editorForChildClasses: true)]
    public class SGSR2_Editor : Editor
    {
        public override void OnInspectorGUI() {
#if !TND_HDRP_EDITEDSOURCE && UNITY_HDRP
            EditorGUILayout.LabelField("----- HDRP Upscaling requires Source File edits. Please read the 'Quick Start: HDRP' chapter in the documentation. ------", EditorStyles.boldLabel);
            if (GUILayout.Button("I have edited the source files!"))
            {
                PipelineDefines.AddDefine("TND_HDRP_EDITEDSOURCE");
                AssetDatabase.Refresh();
            }
#else
            SGSR2_Base sgsr2Script = target as SGSR2_Base;
            if (sgsr2Script == null)
                return;

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.LabelField("SGSR 2 Settings", EditorStyles.boldLabel);
            SGSR2_Variant sgsr2Variant = (SGSR2_Variant)EditorGUILayout.EnumPopup(Styles.variantText, sgsr2Script.variant);
            SGSR2_Quality sgsr2Quality = (SGSR2_Quality)EditorGUILayout.EnumPopup(Styles.qualityText, sgsr2Script.quality);
            float antiGhosting = EditorGUILayout.Slider(Styles.antiGhosting, sgsr2Script.antiGhosting, 0.0f, 1.0f);

            EditorGUILayout.Space();

#if UNITY_BIRP
            EditorGUILayout.LabelField("MipMap Settings", EditorStyles.boldLabel);
            bool autoTextureUpdate = EditorGUILayout.Toggle(Styles.autoTextureUpdateText, sgsr2Script.autoTextureUpdate);
            float mipMapUpdateFrequency = sgsr2Script.mipMapUpdateFrequency;
            if(sgsr2Script.autoTextureUpdate) {
                EditorGUI.indentLevel++;
                mipMapUpdateFrequency = EditorGUILayout.FloatField(Styles.autoUpdateFrequencyText, sgsr2Script.mipMapUpdateFrequency);
                EditorGUI.indentLevel--;
            }
            float mipmapBiasOverride = EditorGUILayout.Slider(Styles.mipmapBiasText, sgsr2Script.mipmapBiasOverride, 0.0f, 1.0f);
#endif

            if(EditorGUI.EndChangeCheck()) {
                EditorUtility.SetDirty(sgsr2Script);

                Undo.RecordObject(target, "Changed Area Of Effect");
                sgsr2Script.variant = sgsr2Variant;
                sgsr2Script.quality = sgsr2Quality;
                sgsr2Script.antiGhosting = antiGhosting;

#if UNITY_BIRP
                sgsr2Script.autoTextureUpdate = autoTextureUpdate;
                sgsr2Script.mipMapUpdateFrequency = mipMapUpdateFrequency;
                sgsr2Script.mipmapBiasOverride = mipmapBiasOverride;
#endif
            }
#endif
        }

        private static class Styles
        {
            public static readonly GUIContent variantText = new GUIContent("Variant", "Which SGSR2 algorithm to run, with a variety of performance, quality and compatibility.");
            public static readonly GUIContent qualityText = new GUIContent("Quality", "Ultra Quality 1.2, Quality 1.5, Balanced 1.7, Performance 2, Ultra Performance 3");
            public static readonly GUIContent antiGhosting = new GUIContent("Anti Ghosting", "The Anti Ghosting value between 0 and 1, where 0 is no Anti Ghosting and 1 is the maximum amount.");

            public static readonly GUIContent mipmapBiasText = new GUIContent("Mipmap Bias Override", "An extra mipmap bias override for if AMD's recommended MipMap Bias values give artifacts");
            public static readonly GUIContent autoTextureUpdateText = new GUIContent("Auto Texture Update", "Wether the mipmap biases of all textures in the scene should automatically be updated");
            public static readonly GUIContent autoUpdateFrequencyText = new GUIContent("Update Frequency", "Interval in seconds in which the mipmap biases should be updated");
            public static readonly GUIContent debugText = new GUIContent("Debug", "Enables debugging in the FSR algorithm, which can help catch certain errors.");
        }
    }
}
