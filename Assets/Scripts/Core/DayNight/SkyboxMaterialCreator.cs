using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR
public class SkyboxMaterialCreator : EditorWindow
{
    private Material skyboxMaterial;

    [MenuItem("Tools/Create Procedural Skybox")]
    public static void ShowWindow()
    {
        GetWindow<SkyboxMaterialCreator>("Создать скайбокс");
    }

    private void OnGUI()
    {
        GUILayout.Label("Создать процедурный скайбокс", EditorStyles.boldLabel);

        if (GUILayout.Button("Создать новый материал скайбокса"))
        {
            CreateSkyboxMaterial();
        }

        if (skyboxMaterial != null)
        {
            GUILayout.Label("Материал создан: " + skyboxMaterial.name);

            if (GUILayout.Button("Применить к текущей сцене"))
            {
                RenderSettings.skybox = skyboxMaterial;
            }
        }
    }

    private void CreateSkyboxMaterial()
    {
        // Проверка, существует ли шейдер
        Shader skyboxShader = Shader.Find("Custom/ProceduralSkybox");
        if (skyboxShader == null)
        {
            Debug.LogError("Шейдер 'Custom/ProceduralSkybox' не найден. Убедитесь, что вы создали шейдер правильно.");
            return;
        }

        // Создаем новый материал
        skyboxMaterial = new Material(skyboxShader);

        // Задаем базовые значения
        skyboxMaterial.SetColor("_SkyColor", new Color(0.4f, 0.6f, 0.9f, 1.0f));
        skyboxMaterial.SetColor("_HorizonColor", new Color(0.9f, 0.85f, 0.8f, 1.0f));
        skyboxMaterial.SetColor("_GroundColor", new Color(0.3f, 0.25f, 0.2f, 1.0f));
        skyboxMaterial.SetFloat("_StarBrightness", 0.5f);
        skyboxMaterial.SetFloat("_StarDensity", 100.0f);
        skyboxMaterial.SetFloat("_HorizonBlend", 1.0f);

        // Сохраняем материал в проекте
        string path = "Assets/Materials";

        // Создаем папку Materials, если она не существует
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder("Assets", "Materials");
        }

        AssetDatabase.CreateAsset(skyboxMaterial, path + "/ProceduralSkybox.mat");
        AssetDatabase.SaveAssets();

        EditorUtility.FocusProjectWindow();
        Selection.activeObject = skyboxMaterial;
    }
}
#endif