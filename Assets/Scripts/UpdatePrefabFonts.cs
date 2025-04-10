#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using TMPro;

public class UpdatePrefabFonts : EditorWindow
{
	private TMP_FontAsset newFont; // Поле для выбора шрифта

	[MenuItem("Tools/Update TMP Fonts in Prefabs")]
	public static void ShowWindow()
	{
		// Создаем окно редактора
		GetWindow<UpdatePrefabFonts>("Update TMP Fonts");
	}

	private void OnGUI()
	{
		// Заголовок окна
		GUILayout.Label("Обновление шрифтов в префабах", EditorStyles.boldLabel);

		// Поле для выбора шрифта
		newFont = (TMP_FontAsset)EditorGUILayout.ObjectField("Новый шрифт", newFont, typeof(TMP_FontAsset), false);

		// Кнопка для запуска обновления
		if (GUILayout.Button("Обновить шрифты"))
		{
			if (newFont == null)
			{
				Debug.LogError("Шрифт не назначен! Пожалуйста, выберите шрифт.");
				return;
			}

			UpdateFontsInPrefabs(newFont);
		}
	}

	private void UpdateFontsInPrefabs(TMP_FontAsset font)
	{
		string[] prefabPaths = AssetDatabase.FindAssets("t:prefab");
		int updatedCount = 0;

		foreach (string prefabGuid in prefabPaths)
		{
			string path = AssetDatabase.GUIDToAssetPath(prefabGuid);
			GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

			if (prefab != null)
			{
				TMP_Text[] texts = prefab.GetComponentsInChildren<TMP_Text>(true);
				foreach (var text in texts)
				{
					text.font = font; // Устанавливаем новый шрифт
					updatedCount++;
				}

				EditorUtility.SetDirty(prefab); // Помечаем префаб как измененный
			}
		}

		AssetDatabase.SaveAssets();
		Debug.Log($"Обновлено {updatedCount} текстовых объектов в префабах.");
	}
}
#endif