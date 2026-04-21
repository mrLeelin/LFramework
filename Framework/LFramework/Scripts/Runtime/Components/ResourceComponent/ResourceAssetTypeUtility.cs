using System;

namespace LFramework.Runtime
{
    internal static class ResourceAssetTypeUtility
    {
        public static Type[] GetLoadTypeFallbackChain(Type requestedType)
        {
            if (requestedType == null || requestedType == typeof(object))
            {
                return new[] { typeof(object) };
            }

            return new[] { requestedType, typeof(object) };
        }

        public static bool TryConvertLoadedObject(
            object loadedAsset,
            Type requestedType,
            string assetName,
            out object typedAsset,
            out string errorMessage)
        {
            if (requestedType != null && loadedAsset != null && requestedType.IsInstanceOfType(loadedAsset))
            {
                typedAsset = loadedAsset;
                errorMessage = null;
                return true;
            }

            typedAsset = null;
            string actualTypeName = loadedAsset?.GetType().FullName ?? "null";
            string requestedTypeName = requestedType?.FullName ?? "null";
            errorMessage =
                $"Load asset '{assetName}' returned type '{actualTypeName}', which cannot be assigned to '{requestedTypeName}'.";
            return false;
        }
    }
}
