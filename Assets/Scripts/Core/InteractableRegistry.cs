using System.Collections.Generic;
using UnityEngine;

namespace RPG.Core
{
    public class InteractableRegistry : MonoBehaviour
    {
        private static InteractableRegistry instance;
        public static InteractableRegistry Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("InteractableRegistry");
                    instance = go.AddComponent<InteractableRegistry>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        private List<MonoBehaviour> registeredObjects = new List<MonoBehaviour>();

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
        }

        public void Register(MonoBehaviour obj)
        {
            if (!registeredObjects.Contains(obj))
            {
                registeredObjects.Add(obj);
            }
        }

        public void Unregister(MonoBehaviour obj)
        {
            registeredObjects.Remove(obj);
        }

        public bool IsNearInteractable(Vector3 position, float checkRadius)
        {
            float checkRadiusSqr = checkRadius * checkRadius;
            
            for (int i = registeredObjects.Count - 1; i >= 0; i--)
            {
                if (registeredObjects[i] == null)
                {
                    registeredObjects.RemoveAt(i);
                    continue;
                }

                if ((registeredObjects[i].transform.position - position).sqrMagnitude <= checkRadiusSqr)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Возвращает расстояние до ближайшего интерактивного объекта в радиусе searchRadius.
        /// Возвращает float.MaxValue если ничего не найдено.
        /// Единственный проход по реестру — используй вместо пары IsNearInteractable + GetNearestInteractablePosition.
        /// </summary>
        public float GetNearestInteractableDistanceSqr(Vector3 position, float searchRadius)
        {
            float nearestDistSqr = float.MaxValue;
            float searchRadiusSqr = searchRadius * searchRadius;

            for (int i = registeredObjects.Count - 1; i >= 0; i--)
            {
                if (registeredObjects[i] == null)
                {
                    registeredObjects.RemoveAt(i);
                    continue;
                }

                float distSqr = (registeredObjects[i].transform.position - position).sqrMagnitude;
                if (distSqr <= searchRadiusSqr && distSqr < nearestDistSqr)
                    nearestDistSqr = distSqr;
            }

            return nearestDistSqr;
        }

        // Оставлен для обратной совместимости
        public Vector3 GetNearestInteractablePosition(Vector3 position, float searchRadius)
        {
            float nearestDistSqr = float.MaxValue;
            Vector3 nearestPos = Vector3.zero;
            bool found = false;
            float searchRadiusSqr = searchRadius * searchRadius;

            for (int i = registeredObjects.Count - 1; i >= 0; i--)
            {
                if (registeredObjects[i] == null)
                {
                    registeredObjects.RemoveAt(i);
                    continue;
                }

                float distanceSqr = (registeredObjects[i].transform.position - position).sqrMagnitude;
                if (distanceSqr <= searchRadiusSqr && distanceSqr < nearestDistSqr)
                {
                    nearestDistSqr = distanceSqr;
                    nearestPos = registeredObjects[i].transform.position;
                    found = true;
                }
            }

            return found ? nearestPos : Vector3.zero;
        }
    }
}
