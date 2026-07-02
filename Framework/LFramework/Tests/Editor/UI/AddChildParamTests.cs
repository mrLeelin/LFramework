using LFramework.Runtime;
using LFramework.Runtime.Settings;
using NUnit.Framework;
using UnityEngine;

namespace LFramework.Editor.Tests.UI
{
    public sealed class AddChildParamTests
    {
        [Test]
        public void TryCreateEntityData_CopiesAllChildOptions()
        {
            var parent = new GameObject("ChildParent").transform;
            var userData = new object();
            var position = new Vector3(10f, 20f, 30f);

            try
            {
                var param = AddChildParam.Create(parent, "Assets/UI/Child.prefab")
                    .SetUserData(userData)
                    .SetDependOn(1001)
                    .SetPosition(position)
                    .SetSize(2f);

                Assert.That(param.TryCreateEntityData(out var data), Is.True);
                Assert.That(data, Is.Not.Null);
                Assert.That(data.EntityAssetsPath, Is.EqualTo("Assets/UI/Child.prefab"));
                Assert.That(data.Parent, Is.SameAs(parent));
                Assert.That(data.UserData, Is.SameAs(userData));
                Assert.That(data.DependOn, Is.EqualTo(1001));
                Assert.That(data.Position, Is.EqualTo(position));
                Assert.That(data.Size, Is.EqualTo(Vector3.one * 2f));
            }
            finally
            {
                Object.DestroyImmediate(parent.gameObject);
            }
        }

        [Test]
        public void TryCreateEntityData_ReturnsFalseForInvalidParam()
        {
            var param = AddChildParam.Create(null, string.Empty);

            Assert.That(param.TryCreateEntityData(out var data), Is.False);
            Assert.That(data, Is.Null);
        }

        [Test]
        public void UIComponentSetting_ChildEntityGroupName_DefaultsToEmptyString()
        {
            var setting = ScriptableObject.CreateInstance<UIComponentSetting>();

            try
            {
                Assert.That(setting.ChildEntityGroupName, Is.EqualTo(string.Empty));
            }
            finally
            {
                Object.DestroyImmediate(setting);
            }
        }
    }
}
