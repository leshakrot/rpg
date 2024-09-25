using GameDevTV.Inventories;
using RPG.Attributes;
using RPG.Stats;
using System.Collections.Generic;
using UnityEngine;

namespace RPG.Combat
{
    [CreateAssetMenu(fileName = "LowerArm Armor", menuName = "RPG/ LowerArm Armor/ New LowerArm Armor", order = 3)]
    public class LowerArmArmorConfig : EquipableItem/*, IModifierProvider*/
    {
        [SerializeField] private LowerArmArmor _equippedPrefab;
        [SerializeField] private bool _isSingle;
        [SerializeField] private bool _isLeft;
        [SerializeField] private EquipLocation _equipLocation;
        //[SerializeField] private float _weaponDamage = 5f;
        //[SerializeField] private float _percantageBonus = 0;
        //[SerializeField] private float _weaponRange = 2f;
        //[SerializeField] private bool _isRightHanded = true;
        //[SerializeField] private Projectile _projectile;

        private const string armorName = "LowerArm Armor";

        public LowerArmArmor Spawn(Transform equipSingle, Transform equipLeft, Transform equipRight)
        {
            DestroyOldArmor(equipSingle, equipLeft, equipRight);

            LowerArmArmor armor = null;
            if(_equippedPrefab != null)
            {
                Transform equipTransform = GetTransform(equipSingle, equipLeft, equipRight);
                armor = Instantiate(_equippedPrefab, equipTransform);
                armor.gameObject.name = armorName;
            }

            return armor;
        }

        private void DestroyOldArmor(Transform equipTransform, Transform equipLeftTransform, Transform equipRightTransform)
        {
            Transform oldArmor = equipTransform.Find(armorName);
            if (oldArmor == null)
            {
                oldArmor = equipLeftTransform.Find(armorName);
            }

            if (oldArmor == null)
            {
                oldArmor = equipRightTransform.Find(armorName);
            }

            if (oldArmor == null) return;

            oldArmor.name = "Destroying";
            Destroy(oldArmor.gameObject);
        }

        private Transform GetTransform(Transform equipSingle, Transform equipLeft, Transform equipRight)
        {
            Transform equipTransform;
            if (_isSingle) equipTransform = equipSingle;
            else if(_isLeft) equipTransform = equipLeft;
            else equipTransform = equipRight;
            return equipTransform;
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
