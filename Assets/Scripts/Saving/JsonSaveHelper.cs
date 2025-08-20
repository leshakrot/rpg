using System;
using UnityEngine;

namespace GameDevTV.Saving
{
    /// <summary>
    /// Вспомогательный класс для безопасного приведения типов при загрузке JSON данных
    /// </summary>
    public static class JsonSaveHelper
    {
        /// <summary>
        /// Безопасно конвертирует объект в float
        /// JSON десериализует числа как double, этот метод обрабатывает различные типы
        /// </summary>
        public static float ToFloat(object value)
        {
            if (value == null) return 0f;
            
            if (value is double doubleValue)
                return (float)doubleValue;
            
            if (value is float floatValue)
                return floatValue;
            
            if (value is int intValue)
                return (float)intValue;
            
            if (value is long longValue)
                return (float)longValue;
            
            try
            {
                return Convert.ToSingle(value);
            }
            catch (Exception)
            {
                Debug.LogWarning($"Failed to convert {value} ({value.GetType()}) to float. Returning 0.");
                return 0f;
            }
        }

        /// <summary>
        /// Безопасно конвертирует объект в int
        /// </summary>
        public static int ToInt(object value)
        {
            if (value == null) return 0;
            
            if (value is double doubleValue)
                return (int)doubleValue;
            
            if (value is float floatValue)
                return (int)floatValue;
            
            if (value is int intValue)
                return intValue;
            
            if (value is long longValue)
                return (int)longValue;
            
            try
            {
                return Convert.ToInt32(value);
            }
            catch (Exception)
            {
                Debug.LogWarning($"Failed to convert {value} ({value.GetType()}) to int. Returning 0.");
                return 0;
            }
        }

        /// <summary>
        /// Безопасно конвертирует объект в bool
        /// </summary>
        public static bool ToBool(object value)
        {
            if (value == null) return false;
            
            if (value is bool boolValue)
                return boolValue;
            
            try
            {
                return Convert.ToBoolean(value);
            }
            catch (Exception)
            {
                Debug.LogWarning($"Failed to convert {value} ({value.GetType()}) to bool. Returning false.");
                return false;
            }
        }
    }
}