using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace GameDevTV.Saving
{
    /// <summary>
    /// Single JSON configuration for the existing Dictionary<string, object>
    /// save format. This deliberately keeps the old root structure intact.
    /// </summary>
    public static class SaveJson
    {
        private static JsonSerializerSettings CreateSettings(Formatting formatting)
        {
            return new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto,
                Formatting = formatting,
                Converters = new JsonConverter[]
                {
                    new Vector3JsonConverter(),
                    new QuaternionJsonConverter(),
                    new ColorJsonConverter()
                }
            };
        }

        public static string Serialize(Dictionary<string, object> state, bool pretty = false)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            return JsonConvert.SerializeObject(
                state,
                CreateSettings(pretty ? Formatting.Indented : Formatting.None));
        }

        public static Dictionary<string, object> Deserialize(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new Dictionary<string, object>();

            Dictionary<string, object> state =
                JsonConvert.DeserializeObject<Dictionary<string, object>>(
                    json,
                    CreateSettings(Formatting.None));

            return state ?? new Dictionary<string, object>();
        }
    }
}
