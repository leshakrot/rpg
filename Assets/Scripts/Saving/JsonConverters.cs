using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace GameDevTV.Saving
{
    /// <summary>
    /// JSON converters used by both local and Yandex saves.
    /// The format intentionally remains compatible with the existing project:
    /// { "x": ..., "y": ..., "z": ... } etc.
    /// </summary>
    public sealed class Vector3JsonConverter : JsonConverter<Vector3>
    {
        public override void WriteJson(JsonWriter writer, Vector3 value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("x");
            writer.WriteValue(value.x);
            writer.WritePropertyName("y");
            writer.WriteValue(value.y);
            writer.WritePropertyName("z");
            writer.WriteValue(value.z);
            writer.WriteEndObject();
        }

        public override Vector3 ReadJson(
            JsonReader reader,
            Type objectType,
            Vector3 existingValue,
            bool hasExistingValue,
            JsonSerializer serializer)
        {
            JObject json = JObject.Load(reader);

            return new Vector3(
                ReadFloat(json, "x"),
                ReadFloat(json, "y"),
                ReadFloat(json, "z"));
        }

        private static float ReadFloat(JObject json, string name)
        {
            JToken token = json[name];
            return token == null ? 0f : token.Value<float>();
        }
    }

    public sealed class QuaternionJsonConverter : JsonConverter<Quaternion>
    {
        public override void WriteJson(JsonWriter writer, Quaternion value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("x");
            writer.WriteValue(value.x);
            writer.WritePropertyName("y");
            writer.WriteValue(value.y);
            writer.WritePropertyName("z");
            writer.WriteValue(value.z);
            writer.WritePropertyName("w");
            writer.WriteValue(value.w);
            writer.WriteEndObject();
        }

        public override Quaternion ReadJson(
            JsonReader reader,
            Type objectType,
            Quaternion existingValue,
            bool hasExistingValue,
            JsonSerializer serializer)
        {
            JObject json = JObject.Load(reader);

            return new Quaternion(
                ReadFloat(json, "x"),
                ReadFloat(json, "y"),
                ReadFloat(json, "z"),
                ReadFloat(json, "w", 1f));
        }

        private static float ReadFloat(JObject json, string name, float fallback = 0f)
        {
            JToken token = json[name];
            return token == null ? fallback : token.Value<float>();
        }
    }

    public sealed class ColorJsonConverter : JsonConverter<Color>
    {
        public override void WriteJson(JsonWriter writer, Color value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("r");
            writer.WriteValue(value.r);
            writer.WritePropertyName("g");
            writer.WriteValue(value.g);
            writer.WritePropertyName("b");
            writer.WriteValue(value.b);
            writer.WritePropertyName("a");
            writer.WriteValue(value.a);
            writer.WriteEndObject();
        }

        public override Color ReadJson(
            JsonReader reader,
            Type objectType,
            Color existingValue,
            bool hasExistingValue,
            JsonSerializer serializer)
        {
            JObject json = JObject.Load(reader);

            return new Color(
                ReadFloat(json, "r"),
                ReadFloat(json, "g"),
                ReadFloat(json, "b"),
                ReadFloat(json, "a", 1f));
        }

        private static float ReadFloat(JObject json, string name, float fallback = 0f)
        {
            JToken token = json[name];
            return token == null ? fallback : token.Value<float>();
        }
    }
}
