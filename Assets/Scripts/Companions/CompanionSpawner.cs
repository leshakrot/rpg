using UnityEngine;
using RPG.Companions;

namespace RPG.Combat
{
    /// <summary>
    /// Размещается только на "родной" сцене компаньона.
    /// Спавнит NPC если компаньон не нанят.
    /// Спавнит компаньона на базе если sentToBase = true.
    /// Активных компаньонов спавнит CompanionManager.
    /// </summary>
    public class CompanionSpawner : MonoBehaviour
    {
        [Header("Данные компаньона")]
        [SerializeField] private CompanionData companionData;

        [Header("Префаб")]
        [SerializeField] private GameObject companionPrefab;

        [Header("Точка появления")]
        [SerializeField] private Transform spawnPoint;

        private bool _spawned = false;

        private void Start()
        {
            SpawnIfNeeded();
        }

        /// <summary>Сбрасывает флаг и спавнит заново — для случая SendToBase на родной сцене.</summary>
        public void ResetAndSpawn()
        {
            _spawned = false;
            SpawnIfNeeded();
        }

        /// <summary>Вызывается также из CompanionManager.SpawnBasedCompanions().</summary>
        public void SpawnIfNeeded()
        {
            if (_spawned) return;
            if (companionData == null || companionPrefab == null) return;

            string id   = companionData.CompanionID;
            bool   hired = CompanionManager.Instance.IsCompanionHired(id);

            if (!hired)
            {
                // Обычный NPC — не нанят
                DoSpawn(GetSpawnPos());
            }
            else if (CompanionManager.Instance.IsCompanionOnBase(id))
            {
                // Отправлен на базу — появляется здесь как ждущий NPC
                DoSpawn(GetSpawnPos());
                // Не активируем контроллер — просто стоит, можно поговорить
            }
            // Активный или ждущий в другом месте — не наше дело
        }

        private void DoSpawn(Vector3 pos)
        {
            var instance = Instantiate(companionPrefab, pos, Quaternion.identity);
            instance.name = companionData.CompanionName;
            _spawned = true;
        }

        private Vector3 GetSpawnPos() =>
            spawnPoint != null ? spawnPoint.position : transform.position;
    }
}
