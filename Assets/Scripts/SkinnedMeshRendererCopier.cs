using UnityEngine;

public static class SkinnedMeshRendererCopier
{
	/// <summary>
	/// Копирует SkinnedMeshRenderer с одного объекта на другой.
	/// </summary>
	/// <param name="source">Исходный SkinnedMeshRenderer.</param>
	/// <param name="target">Целевой SkinnedMeshRenderer.</param>
	public static void CopySkinnedMeshRenderer(SkinnedMeshRenderer source, SkinnedMeshRenderer target)
	{
		// Проверяем, что оба компонента заданы
		if (source == null || target == null)
		{
			Debug.LogError("Source или Target SkinnedMeshRenderer не задан!");
			return;
		}

		// Копируем основные параметры SkinnedMeshRenderer
		target.sharedMesh = source.sharedMesh;
		target.rootBone = source.rootBone;
		target.bones = source.bones;
		target.materials = source.materials;
		target.sharedMaterials = source.sharedMaterials;
		target.quality = source.quality;
		target.updateWhenOffscreen = source.updateWhenOffscreen;
		target.skinnedMotionVectors = source.skinnedMotionVectors;
		target.receiveShadows = source.receiveShadows;
		target.shadowCastingMode = source.shadowCastingMode;
		target.lightProbeUsage = source.lightProbeUsage;
		target.reflectionProbeUsage = source.reflectionProbeUsage;
		target.probeAnchor = source.probeAnchor;
		target.allowOcclusionWhenDynamic = source.allowOcclusionWhenDynamic;
		target.motionVectorGenerationMode = source.motionVectorGenerationMode;

		//Debug.Log("SkinnedMeshRenderer успешно скопирован!");
	}
}