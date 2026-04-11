using UnityEngine;
using RPG.Companions;

namespace RPG.Combat
{
    /// <summary>
    /// Размещается только на "родной" сцене компаньона.
    /// Единственная ответственность: заспавнить NPC если компаньон ещё не нанят.
    /// Если нанят и активен — CompanionManager сам заспавнит его в своём Start().
    /// </summary>
    public class CompanionSpawner : MonoBehaviour
    {
        [Header("Данные компаньона")]
        [SerializeField] private CompanionData companionData;

        [Header("Префаб")]
        [SerializeField] private GameObject companionPrefab;

        [Header("Точка появления")]
        [SerializeField] private Transform spawnPoint;

        private void Start()
        {
            if (companionData == null || companionPrefab == null)
            {
                Debug.LogError("[CompanionSpawner] companionData или companionPrefab не задан!", this);
                return;
            }

            // Нанятым компаньоном управляет CompanionManager — не вмешиваемся
            if (CompanionManager.Instance.IsCompanionHired(companionData.CompanionID)) return;

            // Не нанят — спавним NPC для диалога
            Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position;
            var instance = Instantiate(companionPrefab, pos, Quaternion.identity);
            instance.name = companionData.CompanionName;
            // CompanionController стартует в Inactive — NPC просто стоит
        }
    }
}
