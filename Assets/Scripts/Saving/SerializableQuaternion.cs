using UnityEngine;
using Newtonsoft.Json;

namespace GameDevTV.Saving
{
    [System.Serializable]
    public class SerializableQuaternion
    {
        [JsonProperty] public float x;
        [JsonProperty] public float y;
        [JsonProperty] public float z;
        [JsonProperty] public float w;

        public SerializableQuaternion() { }

        public SerializableQuaternion(Quaternion quaternion)
        {
            x = quaternion.x;
            y = quaternion.y;
            z = quaternion.z;
            w = quaternion.w;
        }

        public Quaternion ToQuaternion()
        {
            return new Quaternion(x, y, z, w);
        }
    }
}
