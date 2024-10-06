using GameDevTV.Inventories;
using RPG.Attributes;
using RPG.Stats;
using System.Collections.Generic;
using UnityEngine;

namespace RPG.Combat
{
    [CreateAssetMenu(fileName = "Glove Right Armor", menuName = "RPG/ Glove Right Armor/ New Glove Right Armor", order = 5)]
    public class GloveRightArmorConfig: EquipableItem/*, IModifierProvider*/
    {
        [SerializeField] private GloveRightArmor _equippedPrefab;
        [SerializeField] private EquipLocation _equipLocation;
        //[SerializeField] private float _weaponDamage = 5f;
        //[SerializeField] private float _percantageBonus = 0;
        //[SerializeField] private float _weaponRange = 2f;
        //[SerializeField] private bool _isRightHanded = true;
        //[SerializeField] private Projectile _projectile;

        private const string armorName = "Glove Right Armor";

        public GloveRightArmor Spawn(Transform equipTransform)
        {
            DestroyOldArmor(equipTransform);

            GloveRightArmor armor = null;
            if(_equippedPrefab != null)
            {
                armor = Instantiate(_equippedPrefab, equipTransform);
                armor.gameObject.name = armorName;
            }

            return armor;
        }

        private void DestroyOldArmor(Transform equipTransform)
        {
            Transform oldArmor = equipTransform.Find(armorName);

            if (oldArmor == null) return;

            oldArmor.name = "Destroying";
            Destroy(oldArmor.gameObject);
        }

        public EquipLocation GetEquipLocation()
        {
            return _equipLocation;
        }

        public void SetupEquipLocation(EquipLocation equipLocation)
        {
            _equipLocation = equipLocation;
        }

        //public float GetDamage()
        //{
        //    return _weaponDamage;
        //}

        //public float GetPercentageBonus()
        //{
        //    return _percantageBonus;
        //}

        //public float GetRange()
        //{
        //    return _weaponRange;
        //}

        //public IEnumerable<float> GetAdditiveModifiers(Stat stat)
        //{
        //    if(stat == Stat.Damage)
        //    {
        //        yield return _weaponDamage;
        //    }
        //}

        //public IEnumerable<float> GetPercentageModifiers(Stat stat)
        //{
        //    if (stat == Stat.Damage)
        //    {
        //        yield return _percantageBonus;
        //    }
        //}
    }
}
