using GameDevTV.Inventories;
using RPG.Stats;
using RPG.Core;
using UnityEngine;
using UnityEngine.AI;

namespace RPG.Inventories
{
    public class RandomDropper : ItemDropper
    {
        [Tooltip("How far can the pickups be scattered from the dropper.")]
        [SerializeField] private float _scatterDistance = 1;
        [SerializeField] private DropLibrary _dropLibrary;
        
        [Header("Защита от интерактивных объектов")]
        [Tooltip("Минимальное расстояние от интерактивных объектов")]
        [SerializeField] private float _minDistanceFromInteractables = 1.5f;
        [Tooltip("Радиус проверки наличия интерактивных объектов")]
        [SerializeField] private float _interactableCheckRadius = 2f;

        const int ATTEMPTS = 30;

        public void RandomDrop()
        {
            var baseStats = GetComponent<BaseStats>();
            var drops = _dropLibrary.GetRandomDrops(baseStats.GetLevel());
            foreach (var drop in drops)
            {
                DropItem(drop.item, drop.number);
            }        
        }

        protected override Vector3 GetDropLocation()
        {
            for(int i = 0; i < ATTEMPTS; i++)
            {
                Vector3 randomPoint = transform.position + Random.insideUnitSphere * _scatterDistance;
                NavMeshHit hit;
                if (NavMesh.SamplePosition(randomPoint, out hit, 0.1f, NavMesh.AllAreas))
                {
                    if (IsValidDropLocation(hit.position))
                    {
                        return hit.position;
                    }
                }
            }
            return transform.position;
        }

        private bool IsValidDropLocation(Vector3 position)
        {
            float nearestDistSqr = InteractableRegistry.Instance.GetNearestInteractableDistanceSqr(position, _interactableCheckRadius);
            return nearestDistSqr >= _minDistanceFromInteractables * _minDistanceFromInteractables;
        }
    }
}
