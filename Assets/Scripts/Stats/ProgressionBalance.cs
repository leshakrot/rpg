using System.Collections.Generic;
using UnityEngine;
using RPG.Stats;

namespace RPG.EditorTools
{
    /// <summary>Как Defence уменьшает входящий урон. Выберите то, что реально делает ваш Health.TakeDamage.</summary>
    public enum DefenceModel
    {
        Percent,
        Flat,
        Ignore
    }

    /// <summary>
    /// Дизайн-пресет и расчёт баланса. Единственный источник чисел для пресета.
    ///
    /// Идея: сначала задаётся игрок (HP, урон, защита), а враги описываются не числами,
    /// а ощущениями при бое с игроком того же уровня:
    ///   • сколько ударов игрока нужно, чтобы убить врага;
    ///   • сколько ударов врага нужно, чтобы убить игрока.
    /// Из этого выводятся HP и урон врагов, поэтому баланс не разъезжается при смене цифр игрока.
    /// </summary>
    public static class ProgressionBalance
    {
        public static readonly string[] DefenceModelLabels =
        {
            "Процент: урон × 100 / (100 + Defence)",
            "Вычитание: урон − Defence (минимум 10%)",
            "Игнорировать Defence"
        };

        // ---------- Игрок (start → end) ----------
        private const float PlayerHpStart = 280f, PlayerHpEnd = 1400f;      // ×5, +10% за уровень
        private const float PlayerDamageStart = 35f, PlayerDamageEnd = 150f; // ×4.3, +9% за уровень
        private const float PlayerDefenceStart = 15f, PlayerDefenceEnd = 55f;

        // ---------- Опыт ----------
        private const float GruntXpStart = 25f;       // награда за Grunt на 1 уровне
        private const float XpRewardGrowth = 1.10f;   // рост награды за уровень (+10%)
        private const float KillsPerLevelStart = 6f;  // убийств "равного" врага на 1 уровне
        private const float KillsPerLevelStep = 0.6f; // +0.6 убийства на каждый следующий уровень

        private class Archetype
        {
            public CharacterClass characterClass;
            // Ударов игрока для убийства врага того же уровня: на 1 уровне → на максимальном
            public float hitsToKillStart, hitsToKillEnd;
            // Ударов врага для убийства игрока того же уровня
            public float hitsToDieStart, hitsToDieEnd;
            public float defenceStart, defenceEnd;
            public float speed;
            public float xpMultiplier;
            public bool usesMana;
        }

        private static readonly Archetype[] Enemies =
        {
            new Archetype { characterClass = CharacterClass.Grunt,  hitsToKillStart = 4f,   hitsToKillEnd = 3f,   hitsToDieStart = 14f, hitsToDieEnd = 16f, defenceStart = 8f,  defenceEnd = 32f, speed = 4.5f, xpMultiplier = 1.0f },
            new Archetype { characterClass = CharacterClass.Mage,   hitsToKillStart = 2.5f, hitsToKillEnd = 2f,   hitsToDieStart = 9f,  hitsToDieEnd = 10f, defenceStart = 5f,  defenceEnd = 18f, speed = 4.8f, xpMultiplier = 1.4f, usesMana = true },
            new Archetype { characterClass = CharacterClass.Archer, hitsToKillStart = 3f,   hitsToKillEnd = 2.5f, hitsToDieStart = 12f, hitsToDieEnd = 13f, defenceStart = 7f,  defenceEnd = 26f, speed = 5.2f, xpMultiplier = 1.15f },
            new Archetype { characterClass = CharacterClass.Orc,    hitsToKillStart = 8f,   hitsToKillEnd = 6f,   hitsToDieStart = 8f,  hitsToDieEnd = 9f,  defenceStart = 18f, defenceEnd = 62f, speed = 4.0f, xpMultiplier = 2.0f },
            new Archetype { characterClass = CharacterClass.Wolf,   hitsToKillStart = 2f,   hitsToKillEnd = 1.6f, hitsToDieStart = 20f, hitsToDieEnd = 22f, defenceStart = 4f,  defenceEnd = 16f, speed = 6.0f, xpMultiplier = 0.6f },
            new Archetype { characterClass = CharacterClass.Boar,   hitsToKillStart = 3f,   hitsToKillEnd = 2.4f, hitsToDieStart = 16f, hitsToDieEnd = 17f, defenceStart = 6f,  defenceEnd = 24f, speed = 4.3f, xpMultiplier = 0.8f },
            new Archetype { characterClass = CharacterClass.Spider, hitsToKillStart = 1.8f, hitsToKillEnd = 1.5f, hitsToDieStart = 20f, hitsToDieEnd = 22f, defenceStart = 4f,  defenceEnd = 16f, speed = 5.5f, xpMultiplier = 0.7f },
        };

        #region Модель защиты

        public static float Effective(float damage, float defence, DefenceModel model)
        {
            switch (model)
            {
                case DefenceModel.Percent: return damage * 100f / (100f + Mathf.Max(0f, defence));
                case DefenceModel.Flat: return Mathf.Max(damage * 0.1f, damage - defence);
                default: return damage;
            }
        }

        /// <summary>Обратное к Effective: какой "сырой" урон нужен, чтобы после защиты получилось effective.</summary>
        private static float RawForEffective(float effective, float defence, DefenceModel model)
        {
            switch (model)
            {
                case DefenceModel.Percent: return effective * (100f + Mathf.Max(0f, defence)) / 100f;
                case DefenceModel.Flat: return effective + defence;
                default: return effective;
            }
        }

        #endregion

        #region Пресет

        public static List<ProgressionEntry> BuildPreset(int maxLevel, DefenceModel defenceModel)
        {
            int n = Mathf.Max(2, maxLevel);
            var list = new List<ProgressionEntry>();

            // ----- Игрок -----
            ProgressionEntry playerHp = Geometric(CharacterClass.Player, Stat.Health, PlayerHpStart, PlayerHpEnd);
            ProgressionEntry playerDamage = Geometric(CharacterClass.Player, Stat.Damage, PlayerDamageStart, PlayerDamageEnd);
            ProgressionEntry playerDefence = Linear(CharacterClass.Player, Stat.Defence, PlayerDefenceStart, PlayerDefenceEnd);

            list.Add(playerHp);
            list.Add(Linear(CharacterClass.Player, Stat.Mana, 100f, 300f));
            list.Add(Linear(CharacterClass.Player, Stat.ManaRegenRate, 2.5f, 6f, ValueRounding.OneDecimal));
            list.Add(playerDamage);
            list.Add(playerDefence);
            list.Add(BuildPlayerXpTable(n));
            list.Add(BuildTraitPointsTable(n));
            list.Add(Linear(CharacterClass.Player, Stat.BuyingDiscountPercentage, 0f, 17f));
            list.Add(Linear(CharacterClass.Player, Stat.MovementSpeed, 5.5f, 6.5f, ValueRounding.TwoDecimals));

            float pHp1 = playerHp.Evaluate(0f), pHpN = playerHp.Evaluate(1f);
            float pDmg1 = playerDamage.Evaluate(0f), pDmgN = playerDamage.Evaluate(1f);
            float pDef1 = playerDefence.Evaluate(0f), pDefN = playerDefence.Evaluate(1f);

            // ----- Враги -----
            foreach (Archetype a in Enemies)
            {
                float hp1 = a.hitsToKillStart * Effective(pDmg1, a.defenceStart, defenceModel);
                float hpN = a.hitsToKillEnd * Effective(pDmgN, a.defenceEnd, defenceModel);
                float dmg1 = RawForEffective(pHp1 / a.hitsToDieStart, pDef1, defenceModel);
                float dmgN = RawForEffective(pHpN / a.hitsToDieEnd, pDefN, defenceModel);

                list.Add(Geometric(a.characterClass, Stat.Health, hp1, hpN));
                list.Add(Geometric(a.characterClass, Stat.Damage, dmg1, dmgN));
                list.Add(Linear(a.characterClass, Stat.Defence, a.defenceStart, a.defenceEnd));
                list.Add(XpReward(a.characterClass, a.xpMultiplier, n));
                list.Add(Linear(a.characterClass, Stat.MovementSpeed, a.speed, a.speed, ValueRounding.OneDecimal));

                if (a.usesMana)
                {
                    list.Add(Linear(a.characterClass, Stat.Mana, 150f, 350f));
                    list.Add(Linear(a.characterClass, Stat.ManaRegenRate, 4f, 8f, ValueRounding.OneDecimal));
                }
            }

            // ----- Сундук: не боевой, но даёт опыт -----
            list.Add(Linear(CharacterClass.Chest, Stat.Health, 1f, 1f));
            list.Add(Linear(CharacterClass.Chest, Stat.Damage, 0f, 0f));
            list.Add(Linear(CharacterClass.Chest, Stat.MovementSpeed, 0f, 0f));
            list.Add(XpReward(CharacterClass.Chest, 1f, n));

            return list;
        }

        private static ProgressionEntry Linear(CharacterClass c, Stat s, float start, float end, ValueRounding r = ValueRounding.Integer)
        {
            return new ProgressionEntry { characterClass = c, stat = s, mode = CurveMode.Linear, start = start, end = end, rounding = r };
        }

        private static ProgressionEntry Geometric(CharacterClass c, Stat s, float start, float end)
        {
            return new ProgressionEntry { characterClass = c, stat = s, mode = CurveMode.Geometric, start = start, end = end, rounding = ValueRounding.Integer };
        }

        private static float XpRewardAt(int level, float multiplier)
        {
            return GruntXpStart * multiplier * Mathf.Pow(XpRewardGrowth, level - 1);
        }

        private static ProgressionEntry XpReward(CharacterClass c, float multiplier, int n)
        {
            return Geometric(c, Stat.ExperienceReward, XpRewardAt(1, multiplier), XpRewardAt(n, multiplier));
        }

        /// <summary>
        /// ExperienceToLevelUp[L] = СУММАРНЫЙ опыт, при достижении которого игрок выходит с уровня L
        /// (так его читает BaseStats.CalculateLevel). Записей на одну меньше, чем уровней:
        /// после последней записи игрок получает максимальный уровень, а не maxLevel+1.
        /// Разница между соседними значениями = (убийств равного врага) × (награда Grunt этого уровня):
        /// первые левелапы быстрые, дальше убийств нужно чуть больше.
        /// </summary>
        private static ProgressionEntry BuildPlayerXpTable(int n)
        {
            var values = new float[n - 1];
            float total = 0f;
            for (int level = 1; level < n; level++)
            {
                float kills = KillsPerLevelStart + KillsPerLevelStep * (level - 1);
                total += kills * XpRewardAt(level, 1f);
                values[level - 1] = Mathf.Round(total / 5f) * 5f;
            }
            return new ProgressionEntry { characterClass = CharacterClass.Player, stat = Stat.ExperienceToLevelUp, mode = CurveMode.Table, values = values };
        }

        /// <summary>
        /// TotalTraitPoints[L] = очки, получаемые НА ЭТОМ уровне (BaseStats суммирует значения 1..текущий).
        /// +1 за уровень и ещё +1 на каждом 5-м. Итого на 18 уровне: 21 очко.
        /// </summary>
        private static ProgressionEntry BuildTraitPointsTable(int n)
        {
            var values = new float[n];
            for (int level = 1; level <= n; level++)
                values[level - 1] = level % 5 == 0 ? 2f : 1f;
            return new ProgressionEntry { characterClass = CharacterClass.Player, stat = Stat.TotalTraitPoints, mode = CurveMode.Table, values = values };
        }

        #endregion

        #region Отчёт по бою

        public struct FightReport
        {
            public float hitsToKill;   // ударов игрока до смерти врага
            public float hitsToDie;    // ударов врага до смерти игрока
            public float killsToLevel; // сколько таких врагов нужно убить для следующего уровня (0 = неизвестно)
        }

        public static FightReport Analyze(Progression p, CharacterClass enemy, int playerLevel, int enemyLevel, DefenceModel model)
        {
            float pDmg = p.GetStat(Stat.Damage, CharacterClass.Player, playerLevel);
            float pHp = p.GetStat(Stat.Health, CharacterClass.Player, playerLevel);
            float pDef = p.GetStat(Stat.Defence, CharacterClass.Player, playerLevel);

            float eHp = p.GetStat(Stat.Health, enemy, enemyLevel);
            float eDmg = p.GetStat(Stat.Damage, enemy, enemyLevel);
            float eDef = p.GetStat(Stat.Defence, enemy, enemyLevel);

            float toKill = Effective(pDmg, eDef, model);
            float toDie = Effective(eDmg, pDef, model);

            var report = new FightReport
            {
                hitsToKill = toKill > 0f ? eHp / toKill : float.PositiveInfinity,
                hitsToDie = toDie > 0f ? pHp / toDie : float.PositiveInfinity
            };

            float reward = p.GetStat(Stat.ExperienceReward, enemy, enemyLevel);
            float need = p.GetStat(Stat.ExperienceToLevelUp, CharacterClass.Player, playerLevel);
            if (playerLevel > 1)
                need -= p.GetStat(Stat.ExperienceToLevelUp, CharacterClass.Player, playerLevel - 1);
            if (reward > 0f && need > 0f)
                report.killsToLevel = need / reward;

            return report;
        }

        #endregion
    }
}
