using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GameDevTV.Saving
{
    /// <summary>
    /// ЕДИНСТВЕННОЕ место, где настраивается JSON для сейвов.
    /// Настройки (TypeNameHandling.Auto + конвертеры Vector3/Quaternion/Color)
    /// идентичны тем, что использовались раньше, поэтому формат файлов не меняется.
    /// </summary>
    public static class SaveJson
    {
        public static JsonSerializerSettings CreateSettings(bool pretty = false)
        {
            return new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto,
                Formatting = pretty ? Formatting.Indented : Formatting.None,
                Converters = new JsonConverter[]
                {
                    new Vector3JsonConverter(),
                    new QuaternionJsonConverter(),
                    new ColorJsonConverter()
                }
            };
        }

        public static JsonSerializer CreateSerializer()
        {
            return JsonSerializer.Create(CreateSettings());
        }

        public static string Serialize(Dictionary<string, object> state, bool pretty = false)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            return JsonConvert.SerializeObject(state, CreateSettings(pretty));
        }

        public static Dictionary<string, object> Deserialize(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new Dictionary<string, object>();

            Dictionary<string, object> state =
                JsonConvert.DeserializeObject<Dictionary<string, object>>(
                    json,
                    CreateSettings());

            return state ?? new Dictionary<string, object>();
        }

        /// <summary>
        /// Безопасное приведение значения из десериализованного JSON к int
        /// (Json.NET отдаёт long / double / JValue / string в зависимости от пути).
        /// </summary>
        public static int ToInt(object value, int fallback = 0)
        {
            if (value == null)
                return fallback;

            try
            {
                if (value is JValue jValue)
                    value = jValue.Value;

                if (value == null)
                    return fallback;

                if (value is string text)
                {
                    return int.TryParse(
                        text,
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out int parsed)
                        ? parsed
                        : fallback;
                }

                return Convert.ToInt32(value, CultureInfo.InvariantCulture);
            }
            catch
            {
                return fallback;
            }
        }
    }
}
