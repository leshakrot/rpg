using System;
using UnityEngine;

namespace GameDevTV.Saving
{
    /// <summary>
    /// Compatibility helpers used by existing ISaveable implementations and
    /// by the saving system for values that came through JSON as another
    /// numeric CLR type.
    /// </summary>
    public static class JsonSaveHelper
    {
        public static float ToFloat(object value)
        {
            if (value == null) return 0f;

            if (value is double d) return (float)d;
            if (value is float f) return f;
            if (value is decimal m) return (float)m;
            if (value is int i) return i;
            if (value is long l) return l;

            try
            {
                return Convert.ToSingle(value);
            }
            catch (Exception)
            {
                Debug.LogWarning(
                    $"[Saving] Failed to convert {value} ({value.GetType()}) to float. Returning 0.");
                return 0f;
            }
        }

        public static int ToInt(object value)
        {
            if (value == null) return 0;

            if (value is double d) return (int)d;
            if (value is float f) return (int)f;
            if (value is decimal m) return (int)m;
            if (value is int i) return i;
            if (value is long l) return (int)l;

            try
            {
                return Convert.ToInt32(value);
            }
            catch (Exception)
            {
                Debug.LogWarning(
                    $"[Saving] Failed to convert {value} ({value.GetType()}) to int. Returning 0.");
                return 0;
            }
        }

        public static bool ToBool(object value)
        {
            if (value == null) return false;
            if (value is bool b) return b;

            try
            {
                return Convert.ToBoolean(value);
            }
            catch (Exception)
            {
                Debug.LogWarning(
                    $"[Saving] Failed to convert {value} ({value.GetType()}) to bool. Returning false.");
                return false;
            }
        }
    }
}
