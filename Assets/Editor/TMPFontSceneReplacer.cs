using UnityEditor;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class TMPFontSceneReplacer : EditorWindow
{
    private TMP_FontAsset targetFont;

    [MenuItem("Tools/Replace TMP Fonts In Scene")]
    public static void ShowWindow()
    {
        GetWindow<TMPFontSceneReplacer>("TMP Font Scene Replacer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Replace TMP Fonts in Current Scene", EditorStyles.boldLabel);
        targetFont = (TMP_FontAsset)EditorGUILayout.ObjectField("Target Font", targetFont, typeof(TMP_FontAsset), false);

        if (GUILayout.Button("Replace Fonts in Scene"))
        {
            ReplaceFontsInScene(targetFont);
        }
    }

    private static void ReplaceFontsInScene(TMP_FontAsset targetFont)
    {
        if (targetFont == null)
        {
            Debug.LogError("Target font is not assigned.");
            return;
        }

        int replacedCount = 0;
        TextMeshProUGUI[] allTMPs = GameObject.FindObjectsOfType<TextMeshProUGUI>(true);

        foreach (var tmp in allTMPs)
        {
            if (tmp.font != targetFont)
            {
                Undo.RecordObject(tmp, "Replace TMP Font");
                tmp.font = targetFont;
                EditorUtility.SetDirty(tmp);
                replacedCount++;
            }
        }

        Debug.Log($"✅ Replaced fonts on {replacedCount} TMP components in the current scene.");

        // Mark the scene as dirty so the user can save changes
        if (replacedCount > 0)
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }
    }
}
