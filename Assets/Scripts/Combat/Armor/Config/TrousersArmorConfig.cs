using RPG.Inventories;
using UnityEngine;

namespace RPG.Combat
{
    /// <summary>
    /// A single "trousers" item that bundles all of the modular meshes that make up
    /// leg wear: the hips / pelvis plus the left and right shins (legs). Equipping
    /// this one item swaps every assigned region at once. Leave a field empty to
    /// keep the current mesh in that region.
    /// </summary>
    [CreateAssetMenu(fileName = "Trousers Armor", menuName = "RPG/ Trousers Armor/ New Trousers Armor", order = 7)]
    public class TrousersArmorConfig : StatsEquipableItem
    {
        [Header("Modular leg pieces (leave empty to keep the current mesh)")]
        [Tooltip("Hips / pelvis mesh.")]
        [SerializeField] private TrousersArmor _equippedPrefab;
        [Tooltip("Left shin / leg mesh.")]
        [SerializeField] private BootLeftArmor _bootLeftPrefab;
        [Tooltip("Right shin / leg mesh.")]
        [SerializeField] private BootRightArmor _bootRightPrefab;

        private const string HipsPartName = "Trousers Armor";
        private const string BootLeftPartName = "Boot Left Armor";
        private const string BootRightPartName = "Boot Right Armor";

        /// <summary>Mount transforms for every modular leg region on the character.</summary>
        public struct Mounts
        {
            public Transform Hips;
            public Transform LegLeft;
            public Transform LegRight;
        }

        /// <summary>
        /// Spawns every assigned leg piece onto its matching mount, rebinding each
        /// mesh to the character skeleton.
        /// </summary>
        public void Spawn(in Mounts mounts)
        {
            ModularArmorAttacher.Attach(_equippedPrefab, mounts.Hips, HipsPartName);
            ModularArmorAttacher.Attach(_bootLeftPrefab, mounts.LegLeft, BootLeftPartName);
            ModularArmorAttacher.Attach(_bootRightPrefab, mounts.LegRight, BootRightPartName);
        }
    }
}
