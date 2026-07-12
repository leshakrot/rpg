using RPG.Inventories;
using UnityEngine;

namespace RPG.Combat
{
    /// <summary>
    /// A single "chest piece" that bundles all of the modular meshes that make up a
    /// jacket / cuirass: the torso plus the left and right shoulders (upper arms)
    /// and forearms (lower arms). Equipping this one item swaps every assigned
    /// region at once. Leave a field empty to keep the current mesh in that region.
    /// </summary>
    [CreateAssetMenu(fileName = "Body Armor", menuName = "RPG/ Body Armor/ New Body Armor", order = 1)]
    public class BodyArmorConfig : StatsEquipableItem
    {
        [Header("Modular chest pieces (leave empty to keep the current mesh)")]
        [Tooltip("Torso mesh.")]
        [SerializeField] private BodyArmor _equippedPrefab;
        [Tooltip("Left shoulder / upper arm mesh.")]
        [SerializeField] private UpperArmLeftArmor _upperArmLeftPrefab;
        [Tooltip("Right shoulder / upper arm mesh.")]
        [SerializeField] private UpperArmRightArmor _upperArmRightPrefab;
        [Tooltip("Left forearm / lower arm mesh.")]
        [SerializeField] private LowerArmLeftArmor _lowerArmLeftPrefab;
        [Tooltip("Right forearm / lower arm mesh.")]
        [SerializeField] private LowerArmRightArmor _lowerArmRightPrefab;

        private const string TorsoPartName = "Body Armor";
        private const string UpperArmLeftPartName = "UpperArm Left Armor";
        private const string UpperArmRightPartName = "UpperArm Right Armor";
        private const string LowerArmLeftPartName = "LowerArm Left Armor";
        private const string LowerArmRightPartName = "LowerArm Right Armor";

        /// <summary>Mount transforms for every modular chest region on the character.</summary>
        public struct Mounts
        {
            public Transform Torso;
            public Transform UpperArmLeft;
            public Transform UpperArmRight;
            public Transform LowerArmLeft;
            public Transform LowerArmRight;
        }

        /// <summary>
        /// Spawns every assigned chest piece onto its matching mount, rebinding each
        /// mesh to the character skeleton.
        /// </summary>
        public void Spawn(in Mounts mounts)
        {
            ModularArmorAttacher.Attach(_equippedPrefab, mounts.Torso, TorsoPartName);
            ModularArmorAttacher.Attach(_upperArmLeftPrefab, mounts.UpperArmLeft, UpperArmLeftPartName);
            ModularArmorAttacher.Attach(_upperArmRightPrefab, mounts.UpperArmRight, UpperArmRightPartName);
            ModularArmorAttacher.Attach(_lowerArmLeftPrefab, mounts.LowerArmLeft, LowerArmLeftPartName);
            ModularArmorAttacher.Attach(_lowerArmRightPrefab, mounts.LowerArmRight, LowerArmRightPartName);
        }
    }
}
