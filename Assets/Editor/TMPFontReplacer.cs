using UnityEditor;
using UnityEngine;
using TMPro;
using System.IO;

public class TMPFontReplacer : EditorWindow
{
    private TMP_FontAsset targetFont;

    [MenuItem("Tools/Replace TMP Fonts")]
    public static void ShowWindow()
    {
        GetWindow<TMPFontReplacer>("TMP Font Replacer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Replace All TMP Fonts in Prefabs", EditorStyles.boldLabel);
        targetFont = (TMP_FontAsset)EditorGUILayout.ObjectField("Target Font", targetFont, typeof(TMP_FontAsset), false);

        if (GUILayout.Button("Replace Fonts in Project"))
        {
            ReplaceFontsInProject(targetFont);
        }
    }

    private static void ReplaceFontsInProject(TMP_FontAsset targetFont)
    {
        if (targetFont == null)
        {
            Debug.LogError("Target font is not assigned.");
            return;
        }

        string[] prefabPaths = Directory.GetFiles("Assets", "*.prefab", SearchOption.AllDirectories);

        foreach (string path in prefabPaths)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            bool changed = false;
            var textComponents = prefab.GetComponentsInChildren<TextMeshProUGUI>(true);

            foreach (var tmp in textComponents)
            {
                if (tmp.font != targetFont)
                {
                    tmp.font = targetFont;
                    changed = true;
                }
            }

            if (changed)
            {
                EditorUtility.SetDirty(prefab);
                Debug.Log($"Updated font in prefab: {path}");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Font replacement completed.");
    }
}
