using UnityEngine;
using Newtonsoft.Json;

namespace GameDevTV.Saving
{
    /// <summary>
    /// A `System.Serializable` wrapper for the `Vector3` class that works with JSON.
    /// </summary>
    [System.Serializable]
    public class SerializableVector3
    {
        [JsonProperty] public float x;
        [JsonProperty] public float y;
        [JsonProperty] public float z;

        public SerializableVector3() { }

        /// <summary>
        /// Copy over the state from an existing Vector3.
        /// </summary>
        public SerializableVector3(Vector3 vector)
        {
            x = vector.x;
            y = vector.y;
            z = vector.z;
        }

        /// <summary>
        /// Create a Vector3 from this class' state.
        /// </summary>
        /// <returns></returns>
        public Vector3 ToVector()
        {
            return new Vector3(x, y, z);
        }
    }
}