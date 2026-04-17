using System;
using System.Collections.Generic;
using UnityEngine;

namespace LFramework.Runtime
{
    [Serializable]
    public class RouteIndexEntry
    {
        public string address;
        public string packageId;
    }

    [CreateAssetMenu(fileName = "RouteIndexAsset", menuName = "LFramework/YooAsset/Route Index")]
    public class RouteIndexAsset : ScriptableObject
    {
        public List<RouteIndexEntry> entries = new List<RouteIndexEntry>();
    }
}
