using UnityEngine;

namespace RPG.Combat
{
    /// <summary>
    /// Attaches modular armor meshes (Polygon Synty style) onto a character that
    /// shares a single skeleton. Every body region has a mount <see cref="Transform"/>
    /// that always holds exactly one skinned mesh child bound to the character
    /// skeleton. When a new part is attached, the bone binding is copied from the
    /// existing child so the freshly instantiated mesh deforms with the same
    /// skeleton, and only then the previous child is removed.
    /// </summary>
    public static class ModularArmorAttacher
    {
        private const string ReplacedSuffix = " (Replaced)";

        /// <summary>
        /// Swaps the skinned mesh under <paramref name="mount"/> for a new instance of
        /// <paramref name="partPrefab"/> and rebinds it to the character skeleton.
        /// Passing a <c>null</c> prefab leaves the currently attached part untouched,
        /// which lets a piece of armor cover only some body regions.
        /// </summary>
        /// <param name="partPrefab">Prefab holding the SkinnedMeshRenderer to attach, or null to keep the current part.</param>
        /// <param name="mount">Body region parent that owns the skinned mesh child.</param>
        /// <param name="partName">Stable child name so the part can be found and replaced on the next swap.</param>
        /// <returns>The newly attached <see cref="SkinnedMeshRenderer"/>, or null when nothing was spawned.</returns>
        public static SkinnedMeshRenderer Attach(Component partPrefab, Transform mount, string partName)
        {
            if (mount == null)
            {
                Debug.LogWarning($"ModularArmorAttacher: mount for '{partName}' is not assigned.");
                return null;
            }

            // No prefab means "do not change this region" - keep whatever is on the body.
            if (partPrefab == null) return null;

            SkinnedMeshRenderer boneReference = FindBoneReference(mount, partName);

            SkinnedMeshRenderer newPart = InstantiatePart(partPrefab, mount, partName);
            if (newPart == null) return null;

            if (boneReference != null)
            {
                RebindToSkeleton(newPart, boneReference);
            }
            else
            {
                Debug.LogWarning($"ModularArmorAttacher: no bound reference '{partName}' under '{mount.name}'. The new part may not follow the skeleton.");
            }

            RemoveReplacedParts(mount, partName, newPart);
            return newPart;
        }

        // Finds the currently attached (bone-bound) part so its skeleton binding can be reused.
        private static SkinnedMeshRenderer FindBoneReference(Transform mount, string partName)
        {
            Transform current = mount.Find(partName);
            if (current == null) return null;

            current.TryGetComponent(out SkinnedMeshRenderer reference);
            return reference;
        }

        private static SkinnedMeshRenderer InstantiatePart(Component partPrefab, Transform mount, string partName)
        {
            Component instance = Object.Instantiate(partPrefab, mount);
            instance.gameObject.name = partName;

            if (!instance.TryGetComponent(out SkinnedMeshRenderer skinnedMesh))
            {
                Debug.LogError($"ModularArmorAttacher: prefab '{partPrefab.name}' has no SkinnedMeshRenderer.");
                Object.Destroy(instance.gameObject);
                return null;
            }

            return skinnedMesh;
        }

        // Copies bone binding from an already-bound sibling so the new mesh shares the character skeleton.
        private static void RebindToSkeleton(SkinnedMeshRenderer target, SkinnedMeshRenderer boneReference)
        {
            target.bones = boneReference.bones;
            target.rootBone = boneReference.rootBone;
        }

        // Removes every previous part under the mount, keeping only the freshly attached one.
        private static void RemoveReplacedParts(Transform mount, string partName, SkinnedMeshRenderer keep)
        {
            for (int i = mount.childCount - 1; i >= 0; i--)
            {
                Transform child = mount.GetChild(i);
                if (child == keep.transform) continue;
                if (child.name != partName) continue;

                child.name = partName + ReplacedSuffix;
                Object.Destroy(child.gameObject);
            }
        }
    }
}
