#if YOOASSET_SUPPORT
using System;
using YooAsset.Editor;

namespace LFramework.Editor
{
    /// <summary>
    /// Resolves YooAsset addresses from serialized exact-address payloads.
    /// </summary>
    [DisplayName("定位地址: ExactAddressUserData")]
    public sealed class AddressByExactAddressUserData : IAddressRule
    {
        /// <summary>
        /// Gets the final asset address for the collector entry.
        /// </summary>
        public string GetAssetAddress(AddressRuleData data)
        {
            if (ExactAddressUserDataUtility.TryDeserialize(data.UserData, out ExactAddressUserData payload))
            {
                return payload.ExactAddress;
            }

            throw new Exception($"Invalid exact-address user data for address rule. CollectPath={data.CollectPath}");
        }
    }
}
#endif
