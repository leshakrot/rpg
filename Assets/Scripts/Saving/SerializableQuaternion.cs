using UnityEngine;
using Newtonsoft.Json;

namespace GameDevTV.Saving
{
    /// <summary>
    /// A `System.Serializable` wrapper for the `Quaternion` class that works with JSON.
    /// </summary>
    [System.Serializable]
    public class SerializableQuaternion
    {
        [JsonProperty] public float x;
        [JsonProperty] public float y;
        [JsonProperty] public float z;
        [JsonProperty] public float w;

        public SerializableQuaternion() { }

        /// <summary>
        /// Copy over the state from an existing Quaternion.
        /// </summary>
        public SerializableQuaternion(Quaternion quaternion)
        {
            x = quaternion.x;
            y = quaternion.y;
            z = quaternion.z;
            w = quaternion.w;
        }

        /// <summary>
        /// Create a Quaternion from this class' state.
        /// </summary>
        /// <returns></returns>
        public Quaternion ToQuaternion()
        {
            return new Quaternion(x, y, z, w);
        }
    }
}