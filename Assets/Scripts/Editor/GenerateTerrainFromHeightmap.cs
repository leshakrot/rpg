// =====================================================================
//  GenerateTerrainFromHeightmap.cs
//  Place this file in any Editor/ folder in your Unity project.
//  Then: Tools > RPG Terrain > Generate Terrain from Heightmap
// =====================================================================
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

public class GenerateTerrainFromHeightmap : EditorWindow
{
    // ---- SETTINGS (edit freely) -----
    Texture2D heightmapTexture;
    float terrainWidth  = 500f;
    float terrainLength = 500f;
    float terrainHeight = 120f;   // max elevation in world units
    string assetSavePath = "Assets/Terrain/TerrainData_WC3.asset";
    // ---------------------------------

    [MenuItem("Tools/RPG Terrain/Generate Terrain from Heightmap")]
    public static void ShowWindow()
    {
        GetWindow<GenerateTerrainFromHeightmap>("RPG Terrain Generator");
    }

    void OnGUI()
    {
        GUILayout.Label("WC3-Style Terrain Generator", EditorStyles.boldLabel);
        EditorGUILayout.Space(6);

        heightmapTexture = (Texture2D)EditorGUILayout.ObjectField(
            "Heightmap PNG", heightmapTexture, typeof(Texture2D), false);

        terrainWidth  = EditorGUILayout.FloatField("Width  (X)", terrainWidth);
        terrainLength = EditorGUILayout.FloatField("Length (Z)", terrainLength);
        terrainHeight = EditorGUILayout.FloatField("Max Height (Y)", terrainHeight);
        assetSavePath = EditorGUILayout.TextField("Save Path", assetSavePath);

        EditorGUILayout.Space(10);

        if (GUILayout.Button("Generate Terrain Asset", GUILayout.Height(36)))
        {
            if (heightmapTexture == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign a Heightmap PNG first.", "OK");
                return;
            }
            Generate();
        }

        EditorGUILayout.Space(6);
        EditorGUILayout.HelpBox(
            "1. Import terrain_heightmap_16bit.png into your project.\n" +
            "2. Set Texture Type = 'Single Channel', Format = R16.\n" +
            "3. Disable sRGB, enable Read/Write. Apply.\n" +
            "4. Assign it above and click Generate.",
            MessageType.Info);
    }

    void Generate()
    {
        // Make sure output directory exists
        string dir = Path.GetDirectoryName(assetSavePath);
        if (!AssetDatabase.IsValidFolder(dir))
        {
            Directory.CreateDirectory(Application.dataPath + "/../" + dir);
            AssetDatabase.Refresh();
        }

        // Ensure heightmap texture is readable
        string texPath = AssetDatabase.GetAssetPath(heightmapTexture);
        TextureImporter ti = (TextureImporter)AssetImporter.GetAtPath(texPath);
        if (!ti.isReadable)
        {
            ti.isReadable = true;
            ti.SaveAndReimport();
        }

        int res = Mathf.ClosestPowerOfTwo(heightmapTexture.width) + 1; // Unity needs 2^n+1
        res = Mathf.Clamp(res, 33, 1025);

        // ---- Build TerrainData ----
        TerrainData td = new TerrainData();
        td.heightmapResolution = res;
        td.size = new Vector3(terrainWidth, terrainHeight, terrainLength);

        float[,] heights = SampleHeightmap(heightmapTexture, res);
        td.SetHeights(0, 0, heights);

        // Basic detail & tree settings
        td.SetDetailResolution(512, 8);

        // Save asset
        if (File.Exists(Application.dataPath + "/../" + assetSavePath))
            AssetDatabase.DeleteAsset(assetSavePath);

        AssetDatabase.CreateAsset(td, assetSavePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // ---- Place Terrain in scene ----
        GameObject go = Terrain.CreateTerrainGameObject(td);
        go.name = "WC3_Terrain";
        // Center it at origin
        go.transform.position = new Vector3(-terrainWidth / 2f, 0f, -terrainLength / 2f);

        Selection.activeGameObject = go;
        EditorUtility.DisplayDialog("Done!",
            $"TerrainData saved to:\n{assetSavePath}\n\nTerrain placed in scene. Paint textures in the Terrain Inspector!",
            "Great!");

        Debug.Log($"[RPG Terrain] Generated {res}x{res} heightmap terrain → {assetSavePath}");
    }

    static float[,] SampleHeightmap(Texture2D tex, int res)
    {
        float[,] h = new float[res, res];
        int w = tex.width;
        int ht = tex.height;

        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                float u = (float)x / (res - 1);
                float v = (float)y / (res - 1);

                // Bilinear sample
                float px = u * (w - 1);
                float py = v * (ht - 1);
                int x0 = Mathf.FloorToInt(px), x1 = Mathf.Min(x0 + 1, w - 1);
                int y0 = Mathf.FloorToInt(py), y1 = Mathf.Min(y0 + 1, ht - 1);
                float fx = px - x0, fy = py - y0;

                float c00 = tex.GetPixel(x0, y0).r;
                float c10 = tex.GetPixel(x1, y0).r;
                float c01 = tex.GetPixel(x0, y1).r;
                float c11 = tex.GetPixel(x1, y1).r;

                h[y, x] = Mathf.Lerp(
                    Mathf.Lerp(c00, c10, fx),
                    Mathf.Lerp(c01, c11, fx), fy);
            }
        }
        return h;
    }
}
#endif
