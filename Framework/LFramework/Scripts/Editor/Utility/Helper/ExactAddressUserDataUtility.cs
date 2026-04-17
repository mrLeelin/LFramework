using System;
using UnityEngine;

namespace LFramework.Editor
{
    /// <summary>
    /// Serialized payload for exact-address YooAsset collectors.
    /// </summary>
    [Serializable]
    public sealed class ExactAddressUserData
    {
        /// <summary>
        /// The exact address resolved at runtime.
        /// </summary>
        public string ExactAddress;
    }

    /// <summary>
    /// Serializes and deserializes exact-address collector payloads.
    /// </summary>
    public static class ExactAddressUserDataUtility
    {
        /// <summary>
        /// Serializes the exact address payload.
        /// </summary>
        public static string Serialize(string exactAddress)
        {
            return JsonUtility.ToJson(new ExactAddressUserData
            {
                ExactAddress = exactAddress
            });
        }

        /// <summary>
        /// Attempts to deserialize the exact address payload.
        /// </summary>
        public static bool TryDeserialize(string userData, out ExactAddressUserData payload)
        {
            payload = null;
            if (string.IsNullOrWhiteSpace(userData))
            {
                return false;
            }

            try
            {
                payload = JsonUtility.FromJson<ExactAddressUserData>(userData);
                return payload != null && !string.IsNullOrWhiteSpace(payload.ExactAddress);
            }
            catch
            {
                payload = null;
                return false;
            }
        }
    }
}
