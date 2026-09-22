using UnityEngine;
using Newtonsoft.Json;

namespace GameDevTV.Saving
{
    [System.Serializable]
    public class SerializableVector3
    {
        [JsonProperty] public float x;
        [JsonProperty] public float y;
        [JsonProperty] public float z;

        public SerializableVector3() { }

        public SerializableVector3(Vector3 vector)
        {
            x = vector.x;
            y = vector.y;
            z = vector.z;
        }

        public Vector3 ToVector()
        {
            return new Vector3(x, y, z);
        }
    }
}
