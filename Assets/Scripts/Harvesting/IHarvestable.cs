using System;
using UnityEngine;
using RPG.Control;

namespace RPG.Harvesting
{
    public interface IHarvestable
    {
        bool CanStartHarvesting(int playerLevel);
        void StartHarvesting(PlayerController player);
        void StopHarvesting();
        bool IsHarvesting { get; }
        bool IsDepeleted { get; }
        HarvestableResource GetResource();
        int GetRemainingResources(); // ДОБАВЛЕНО: для получения оставшихся ресурсов

        event Action<float> OnHarvestProgress; // 0-1 прогресс
        event Action<HarvestableResource, int> OnHarvestComplete; // ресурс и количество
        event Action OnResourceDepleted; // ресурс истощён
    }
} 