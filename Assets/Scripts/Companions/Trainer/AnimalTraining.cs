using System;
using System.Collections.Generic;
using UnityEngine;
using RPG.Stats;

namespace RPG.Companions
{
    /// <summary>
    /// «Сторона животного» системы дрессировки. Вешается на префаб пса-компаньона
    /// (рядом с BaseStats, CompanionController, CompanionRecruiter).
    ///
    /// Аналог TraitStore, но:
    ///  • очки навыков покупаются у AnimalTrainer за деньги, а не выдаются за уровень;
    ///  • данные хранит CompanionManager (HiredCompanionInfo) — они переживают респавн и загрузку,
    ///    даже если сам префаб не сохраняется системой сохранений.
    ///
    /// ВАЖНО: на BaseStats пса должна быть включена галочка «Should Use Modifiers»,
    /// иначе бонусы навыков не будут учитываться в GetStat().
    /// </summary>
    [RequireComponent(typeof(BaseStats))]
    public class AnimalTraining : MonoBehaviour, IModifierProvider
    {
        [Tooltip("Тот же CompanionData, что и у CompanionRecruiter на этом животном (по нему ищутся сохранённые данные).")]
        [SerializeField] private CompanionData companionData;

        /// <summary>
        /// Как одно очко навыка (Trait) влияет на параметр (Stat). Формат как в TraitStore.
        /// ПРИМЕРЫ:
        ///  • Constitution → Health,          additive = 10   (+10 здоровья за очко)
        ///  • Strength     → Damage,          additive = 1.5  (+1.5 урона за очко)
        ///  • Dexterity    → MovementSpeed,   percentage = 4  (+4% скорости за очко)
        ///  • Intelligence → GuideMoveSpeed,  percentage = 6  (+6% скорости к GuideTarget за очко)
        /// </summary>
        [Serializable]
        private class TraitBonus
        {
            public Trait trait;
            public Stat stat;
            public float additiveBonusPerPoint = 0f;
            public float percentageBonusPerPoint = 0f;
        }

        [SerializeField] private TraitBonus[] bonusConfig = new TraitBonus[0];

        private BaseStats _stats;

        public BaseStats Stats => _stats;
        public CompanionData Data => companionData;
        public string CompanionID => companionData != null ? companionData.CompanionID : null;
        public string DisplayName => companionData != null ? companionData.CompanionName : name;

        // ── lifecycle ────────────────────────────────────────────────────

        private void Awake()
        {
            _stats = GetComponent<BaseStats>();
            SyncFromManager();
        }

        private void Start()
        {
            // Вторая попытка: на случай, если на Awake менеджер ещё не успел восстановить данные.
            SyncFromManager();
        }

        // ── публичный API ─────────────────────────────────────────────────

        /// <summary>
        /// Подтягивает купленные уровни из CompanionManager в BaseStats (тихо, без регенерации здоровья).
        /// Вызывается автоматически; повторный вызов безопасен.
        /// </summary>
        public void SyncFromManager()
        {
            if (_stats == null) _stats = GetComponent<BaseStats>();

            string id = CompanionID;
            if (_stats == null || string.IsNullOrEmpty(id)) return;

            CompanionManager manager = CompanionManager.Instance;
            if (manager == null) return;

            _stats.SetLevelBonus(manager.GetTrainedLevels(id));
        }

        public int GetPoints(Trait trait)
        {
            string id = CompanionID;
            CompanionManager manager = CompanionManager.Instance;
            if (string.IsNullOrEmpty(id) || manager == null) return 0;
            return manager.GetTrainedPoints(id, trait);
        }

        public int GetTrainedLevels()
        {
            string id = CompanionID;
            CompanionManager manager = CompanionManager.Instance;
            if (string.IsNullOrEmpty(id) || manager == null) return 0;
            return manager.GetTrainedLevels(id);
        }

        /// <summary>Есть ли у навыка хоть какой-то эффект (настроен в bonusConfig этого животного).</summary>
        public bool IsTraitConfigured(Trait trait)
        {
            if (bonusConfig == null) return false;
            foreach (TraitBonus bonus in bonusConfig)
            {
                if (bonus.trait != trait) continue;
                if (bonus.additiveBonusPerPoint != 0f || bonus.percentageBonusPerPoint != 0f) return true;
            }
            return false;
        }

        /// <summary>Добавляет очки навыка. Деньги списывает AnimalTrainer, здесь только применение.</summary>
        public bool AddPoints(Trait trait, int points = 1)
        {
            string id = CompanionID;
            CompanionManager manager = CompanionManager.Instance;
            if (string.IsNullOrEmpty(id) || manager == null) return false;
            if (!manager.AddTrainedPoints(id, trait, points)) return false;

            _stats.RefreshStats(); // обновить UI (здоровье не трогаем)
            return true;
        }

        /// <summary>Повышает общий уровень животного. Как настоящий левел-ап: эффект + регенерация здоровья.</summary>
        public bool AddLevels(int levels = 1)
        {
            string id = CompanionID;
            CompanionManager manager = CompanionManager.Instance;
            if (string.IsNullOrEmpty(id) || manager == null) return false;
            if (!manager.AddTrainedLevels(id, levels)) return false;

            _stats.SetLevelBonus(manager.GetTrainedLevels(id), asLevelUp: true);
            return true;
        }

        /// <summary>
        /// Скорость движения к GuideTarget. Значение = Progression (класс Dog, Stat.GuideMoveSpeed)
        /// + бонусы навыков. Если для стата нет данных (получилось 0) — вернёт fallback.
        /// Вызывать из CompanionGuide при выставлении NavMeshAgent.speed.
        /// </summary>
        public float GetGuideMoveSpeed(float fallback)
        {
            if (_stats == null) return fallback;
            float speed = _stats.GetStat(Stat.GuideMoveSpeed);
            return speed > 0f ? speed : fallback;
        }

        // ── IModifierProvider ─────────────────────────────────────────────

        public IEnumerable<float> GetAdditiveModifiers(Stat stat)
        {
            if (bonusConfig == null) yield break;

            foreach (TraitBonus bonus in bonusConfig)
            {
                if (bonus.stat != stat || bonus.additiveBonusPerPoint == 0f) continue;
                yield return bonus.additiveBonusPerPoint * GetPoints(bonus.trait);
            }
        }

        public IEnumerable<float> GetPercentageModifiers(Stat stat)
        {
            if (bonusConfig == null) yield break;

            foreach (TraitBonus bonus in bonusConfig)
            {
                if (bonus.stat != stat || bonus.percentageBonusPerPoint == 0f) continue;
                yield return bonus.percentageBonusPerPoint * GetPoints(bonus.trait);
            }
        }
    }
}
