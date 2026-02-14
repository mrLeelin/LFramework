using System;
using System.Linq;
using UnityGameFramework.Runtime;

namespace LFramework.Runtime
{
    public static class TypeExtension
    {
        /// <summary>
        /// 获取类的接口类型
        /// </summary>
        /// <param name="type"></param>
        /// <param name="interfaceType"></param>
        /// <param name="ignoreTypes"></param>
        /// <returns></returns>
        public static Type GetDerivedInterfaces(this Type type, params Type[] ignoreTypes)
        {
            var typeInterfaces = type.GetInterfaces();
            var bindInterfaceAttribute = type.GetCustomAttribute<BindInterfaceAttribute>();
            foreach (var iInterface in typeInterfaces)
            {
                if (ignoreTypes.Contains(iInterface))
                {
                    continue;
                }

                if (bindInterfaceAttribute != null)
                {
                    if (iInterface == bindInterfaceAttribute.InterfaceType)
                    {
                        return iInterface;
                    }
                }
                else
                {
                    var attribute = iInterface.GetCustomAttribute<IgnoreInterfaceAttribute>();
                    if (attribute != null)
                    {
                        continue;
                    }

                    return iInterface;
                }
            }

            return null;
        }

        public static T GetCustomAttribute<T>(this System.Type type, bool inherit) where T : Attribute
        {
            object[] customAttributes = type.GetCustomAttributes(typeof(T), inherit);
            return customAttributes.Length == 0 ? default(T) : customAttributes[0] as T;
        }

        /// <summary>
        /// Returns the first found non-inherited custom attribute of type T on this type
        /// Returns null if none was found
        /// </summary>
        public static T GetCustomAttribute<T>(this System.Type type) where T : Attribute =>
            type.GetCustomAttribute<T>(false);
    }
}