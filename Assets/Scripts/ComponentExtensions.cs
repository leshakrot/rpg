using System;
using UnityEngine;
using System.Reflection;

public static class ComponentExtensions
{
	public static T GetCopyOf<T>(this T comp, T other) where T : Component
	{
		Type type = comp.GetType();
		Type othersType = other.GetType();

		if (type != othersType)
		{
			Debug.LogError($"The type \"{type.AssemblyQualifiedName}\" of \"{comp}\" does not match the type \"{othersType.AssemblyQualifiedName}\" of \"{other}\"!");
			return null;
		}

		BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Default;
		PropertyInfo[] pinfos = type.GetProperties(flags);

		foreach (var pinfo in pinfos)
		{
			if (pinfo.CanWrite)
			{
				try
				{
					// Проверяем, является ли свойство "material" или "materials"
					if (pinfo.Name == "material")
					{
						// Используем sharedMaterial вместо material
						var sharedMaterialValue = typeof(Renderer).GetProperty("sharedMaterial", flags)?.GetValue(other, null);
						typeof(Renderer).GetProperty("sharedMaterial", flags)?.SetValue(comp, sharedMaterialValue);
					}
					else if (pinfo.Name == "materials")
					{
						// Используем sharedMaterials вместо materials
						var sharedMaterialsValue = typeof(Renderer).GetProperty("sharedMaterials", flags)?.GetValue(other, null);
						typeof(Renderer).GetProperty("sharedMaterials", flags)?.SetValue(comp, sharedMaterialsValue);
					}
					else
					{
						// Копируем другие свойства как обычно
						pinfo.SetValue(comp, pinfo.GetValue(other, null), null);
					}
				}
					catch (Exception e)
					{
						Debug.LogWarning($"Failed to copy property {pinfo.Name}: {e.Message}");
					}
			}
		}

		FieldInfo[] finfos = type.GetFields(flags);

		foreach (var finfo in finfos)
		{
			try
			{
				finfo.SetValue(comp, finfo.GetValue(other));
			}
				catch (Exception e)
				{
					Debug.LogWarning($"Failed to copy field {finfo.Name}: {e.Message}");
				}
		}

		return comp as T;
	}
}