#if YOOASSET_SUPPORT
using System;
using YooAsset.Editor;

namespace LFramework.Editor
{
    [DisplayName("定位地址: MigrationUserData")]
    public class AddressByMigrationUserData : IAddressRule
    {
        public string GetAssetAddress(AddressRuleData data)
        {
            if (AddressableYooMigrationUserDataUtility.TryDeserialize(data.UserData, out var payload))
            {
                return payload.ExactAddress;
            }

            throw new Exception($"Invalid migration user data for address rule. CollectPath={data.CollectPath}");
        }
    }

    [DisplayName("资源包名: MigrationUserData")]
    public class PackByMigrationUserData : IPackRule
    {
        public PackRuleResult GetPackRuleResult(PackRuleData data)
        {
            if (AddressableYooMigrationUserDataUtility.TryDeserialize(data.UserData, out var payload))
            {
                return new PackRuleResult(payload.BundleName, DefaultPackRule.AssetBundleFileExtension);
            }

            throw new Exception($"Invalid migration user data for pack rule. CollectPath={data.CollectPath}");
        }
    }
}
#endif
