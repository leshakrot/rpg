using System;
using UnityEngine;

namespace RPG.Stats
{
    public enum CurveMode
    {
        /// <summary>Значения по уровням задаются руками (так хранились старые данные).</summary>
        Table,
        /// <summary>Равномерный рост: одинаковая прибавка каждый уровень.</summary>
        Linear,
        /// <summary>Рост start → end с изгибом: shape &lt; 1 быстрее в начале, shape &gt; 1 быстрее в конце.</summary>
        Power,
        /// <summary>Одинаковый процентный рост каждый уровень (start → end). Лучше всего ощущается для HP и урона.</summary>
        Geometric,
        /// <summary>Форма кривой рисуется вручную (AnimationCurve 0..1), масштабируется на start → end.</summary>
        Curve
    }

    public enum ValueRounding { None, Integer, OneDecimal, TwoDecimals }

    /// <summary>
    /// Одна кривая роста: (класс, характеристика) → значение на каждом уровне.
    /// Имена полей characterClass / stat / values сохранены, поэтому старый Progression.asset
    /// продолжает читаться (как Table).
    /// </summary>
    [Serializable]
    public class ProgressionEntry
    {
        public CharacterClass characterClass;
        public Stat stat;
        public CurveMode mode = CurveMode.Table;

        [Tooltip("Значение на 1 уровне")]
        public float start = 1f;
        [Tooltip("Значение на максимальном уровне")]
        public float end = 1f;
        [Tooltip("Power: степень изгиба. 1 = линейно, <1 = быстрый старт, >1 = медленный старт")]
        public float shape = 1f;
        public AnimationCurve curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        public ValueRounding rounding = ValueRounding.None;

        [Tooltip("Только для режима Table")]
        public float[] values = new float[0];

        /// <summary>Считает значения для всех уровней (индекс 0 = уровень 1).</summary>
        public float[] Bake(int maxLevel)
        {
            if (mode == CurveMode.Table)
            {
                int length = values != null ? values.Length : 0;
                var copy = new float[length];
                if (length > 0) Array.Copy(values, copy, length);
                return copy;
            }

            int n = Mathf.Max(1, maxLevel);
            var result = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = n > 1 ? i / (float)(n - 1) : 0f;
                result[i] = Round(Evaluate(t), rounding);
            }
            return result;
        }

        /// <summary>Значение кривой в точке t (0 = 1 уровень, 1 = максимальный). Без округления.</summary>
        public float Evaluate(float t)
        {
            switch (mode)
            {
                case CurveMode.Linear:
                    return Mathf.Lerp(start, end, t);

                case CurveMode.Power:
                    return Mathf.Lerp(start, end, Mathf.Pow(t, Mathf.Max(0.01f, shape)));

                case CurveMode.Geometric:
                    if (start > 0f && end > 0f)
                        return start * Mathf.Pow(end / start, t);
                    return Mathf.Lerp(start, end, t);

                case CurveMode.Curve:
                    return Mathf.LerpUnclamped(start, end, curve != null ? curve.Evaluate(t) : t);

                default:
                    return start;
            }
        }

        public static float Round(float value, ValueRounding rounding)
        {
            switch (rounding)
            {
                case ValueRounding.Integer: return Mathf.Round(value);
                case ValueRounding.OneDecimal: return Mathf.Round(value * 10f) / 10f;
                case ValueRounding.TwoDecimals: return Mathf.Round(value * 100f) / 100f;
                default: return value;
            }
        }
    }
}
