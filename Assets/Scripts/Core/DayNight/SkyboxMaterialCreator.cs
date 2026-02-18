using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR
/// <summary>
/// Создаёт готовые Skybox-материалы для стиля Cozy Lowpoly Dark Fantasy.
/// Tools → Skybox Creator (Cozy Dark Fantasy)
/// </summary>
public class SkyboxMaterialCreator : EditorWindow
{
    private Material lastCreatedMaterial;
    private bool     useMobileShader = false;

    [MenuItem("Tools/Skybox Creator (Cozy Dark Fantasy)")]
    public static void ShowWindow()
    {
        var w = GetWindow<SkyboxMaterialCreator>("Skybox Creator");
        w.minSize = new Vector2(320, 280);
    }

    private void OnGUI()
    {
        GUILayout.Label("Skybox Creator — Cozy Dark Fantasy", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Создаёт материал скайбокса с предустановленными значениями\n" +
            "под стиль Cozy Lowpoly Dark Fantasy.\n\n" +
            "Mobile  → AtmosphericSkyboxMobile  (target 2.0)\n" +
            "Desktop → ProceduralSkybox  (Млечный путь, хроматика)",
            MessageType.Info);

        EditorGUILayout.Space(6);

        useMobileShader = EditorGUILayout.Toggle("Мобильная версия", useMobileShader);

        EditorGUILayout.Space(4);

        if (GUILayout.Button("Создать материал скайбокса", GUILayout.Height(32)))
            CreateMaterial();

        if (lastCreatedMaterial != null)
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.ObjectField("Материал:", lastCreatedMaterial, typeof(Material), false);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Применить к текущей сцене"))
                RenderSettings.skybox = lastCreatedMaterial;
            if (GUILayout.Button("Выбрать в Project"))
            {
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = lastCreatedMaterial;
            }
            EditorGUILayout.EndHorizontal();
        }
    }

    private void CreateMaterial()
    {
        string shaderName = useMobileShader
            ? "Custom/AtmosphericSkyboxMobile"
            : "Custom/ProceduralSkybox";

        Shader shader = Shader.Find(shaderName);
        if (shader == null)
        {
            EditorUtility.DisplayDialog("Ошибка",
                $"Шейдер '{shaderName}' не найден.\n" +
                "Убедитесь, что .shader файлы добавлены в проект.", "OK");
            return;
        }

        var mat = new Material(shader);
        mat.name = useMobileShader ? "CozyDarkFantasy_Mobile" : "CozyDarkFantasy_PC";

        ApplyCozyCorrectedDefaults(mat, useMobileShader);

        // Сохраняем
        const string folder = "Assets/Materials";
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets", "Materials");

        string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{mat.name}.mat");
        AssetDatabase.CreateAsset(mat, path);
        AssetDatabase.SaveAssets();

        lastCreatedMaterial = mat;
        Debug.Log($"[SkyboxCreator] Создан материал: {path}");
    }

    // ─── Дефолтные значения под Cozy Lowpoly Dark Fantasy ───────────────
    // Стиль: тёплый молочно-бежевый день, глубокий фиолетово-синий ночью.
    // Горизонт янтарный — убирает "серость" переходов.
    private static void ApplyCozyCorrectedDefaults(Material mat, bool mobile)
    {
        // --- Небо (устанавливается DayNightSystem через градиент, это начальные) ---
        mat.SetColor("_SkyColor",      new Color(0.32f, 0.52f, 0.82f, 1f));  // мягкий синий
        mat.SetColor("_HorizonColor",  new Color(0.82f, 0.72f, 0.55f, 1f));  // янтарный
        mat.SetColor("_GroundColor",   new Color(0.10f, 0.10f, 0.15f, 1f));  // тёмный индиго

        if (!mobile)
            mat.SetColor("_MidSkyColor", new Color(0.38f, 0.55f, 0.82f, 1f));

        // --- Атмосфера ---
        mat.SetFloat("_HorizonBlend",    mobile ? 2.5f : 3.0f);
        mat.SetFloat("_AtmospherePower", mobile ? 0.85f : 0.75f);
        mat.SetFloat("_HorizonGlow",     mobile ? 0.28f : 0.35f);
        mat.SetFloat("_WarmthFactor",    0.08f);

        // --- Звёзды ---
        mat.SetFloat("_StarBrightness", 0.90f);
        mat.SetFloat("_StarDensity",    mobile ? 55f : 75f);
        mat.SetFloat("_StarSize",       mobile ? 0.018f : 0.022f);
        mat.SetFloat("_StarTwinkle",    mobile ? 0.25f : 0.35f);
        if (!mobile) mat.SetFloat("_StarColorShift", 0.30f);

        // --- Ночь ---
        mat.SetFloat("_NightSkyIntensity",  mobile ? 1.15f : 1.10f);
        mat.SetColor("_NightTint",          new Color(0.55f, 0.60f, 1.0f, 1f)); // фиолетово-синий
        mat.SetFloat("_NightTintStrength",  0.18f);
        if (mobile) mat.SetFloat("_NightSkyDesaturation", 0.25f);

        // --- Млечный путь (только PC) ---
        if (!mobile)
        {
            mat.SetFloat("_MilkyWayIntensity", 0.28f);
            mat.SetFloat("_MilkyWayRotation",  0.5f);
        }
    }
}
#endif
