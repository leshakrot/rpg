using GameDevTV.Inventories;
using RPG.Attributes;
using RPG.Inventories;
using RPG.Stats;
using System.Collections.Generic;
using UnityEngine;

namespace RPG.Combat
{
    [CreateAssetMenu(fileName = "LowerArm Left Armor", menuName = "RPG/ LowerArm Left Armor/ New LowerArm Left Armor", order = 6)]
    public class LowerArmLeftArmorConfig : StatsEquipableItem // EquipableItem/*, IModifierProvider*/
    {
        [SerializeField] private LowerArmLeftArmor _equippedPrefab;
        [SerializeField] private EquipLocation _equipLocation;
        //[SerializeField] private float _weaponDamage = 5f;
        //[SerializeField] private float _percantageBonus = 0;
        //[SerializeField] private float _weaponRange = 2f;
        //[SerializeField] private bool _isRightHanded = true;
        //[SerializeField] private Projectile _projectile;

        private const string armorName = "LowerArm Left Armor";

        public LowerArmLeftArmor Spawn(Transform equipTransform)
        {
            DestroyOldArmor(equipTransform);

            LowerArmLeftArmor armor = null;
            if(_equippedPrefab != null)
            {
                armor = Instantiate(_equippedPrefab, equipTransform);
                armor.gameObject.name = armorName;
            }

            return armor;
        }

        private void DestroyOldArmor(Transform equipTransform)
        {
            // Находим старую броню по имени
            Transform oldArmor = equipTransform.Find(armorName);

            if (oldArmor == null) return;

            // Получаем SkinnedMeshRenderer у старой брони
            if (!oldArmor.TryGetComponent(out SkinnedMeshRenderer oldSMR))
            {
                Debug.LogError("Старая броня не содержит SkinnedMeshRenderer!");
                return;
            }

            // Проверяем, есть ли SkinnedMeshRenderer у нового префаба
            if (!_equippedPrefab.TryGetComponent(out SkinnedMeshRenderer newSMR))
            {
                Debug.LogError("Новый префаб не содержит SkinnedMeshRenderer!");
                return;
            }

            // Сохраняем новый меш и материалы из нового префаба
            Mesh newArmorMesh = newSMR.sharedMesh;
            Material[] newMaterials = newSMR.sharedMaterials;

            // Копируем данные из старого SkinnedMeshRenderer в новый
            SkinnedMeshRendererCopier.CopySkinnedMeshRenderer(oldSMR, newSMR);

            // Восстанавливаем новый меш и материалы после копирования
            newSMR.sharedMesh = newArmorMesh;
            newSMR.sharedMaterials = newMaterials;

            // Переименовываем и уничтожаем старую броню
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
