using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace LFramework.Runtime
{
    public static class UIComponentExtension
    {
        public static Camera GetCanvasCamera(this UIComponent uiComponent)
        {
            return uiComponent.CanvasRoot.GetComponent<Canvas>().worldCamera;
        }

        private static UniTask<int?> _defaultReturn = new UniTask<int?>(0);


        /// <summary>
        /// 获取Ui上挂载的脚本
        /// </summary>
        /// <param name="uiComponent"></param>
        /// <param name="id"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T GetUiBehaviour<T>(this UIComponent uiComponent, int id)
            where T : UIBehaviour
        {
            var uiForm = uiComponent.GetUIForm(id);
            if (uiForm == null)
            {
                return null;
            }

            var selfUiForm = uiForm as UIForm;
            if (selfUiForm == null)
            {
                return null;
            }

            return selfUiForm.UIBehaviour as T;
        }


        /// <summary>
        /// 打开Ui界面
        /// </summary>
        /// <param name="uiComponent"></param>
        /// <param name="assetName"></param>
        /// <param name="allowMultiInstance"></param>
        /// <param name="uiGroupName"></param>
        /// <param name="pauseCoveredUiForm"></param>
        /// <param name="userData"></param>
        /// <returns></returns>
        public static int? OpenUIForm(this UIComponent uiComponent,
            string assetName,
            bool allowMultiInstance,
            string uiGroupName,
            bool pauseCoveredUiForm,
            object userData = null)
        {
            if (!allowMultiInstance)
            {
                if (uiComponent.IsLoadingUIForm(assetName))
                {
                    return null;
                }

                if (uiComponent.HasUIForm(assetName))
                {
                    return null;
                }
            }

            return uiComponent.OpenUIForm(assetName,
                uiGroupName,
                Constant.AssetPriority.UIFormAsset,
                pauseCoveredUiForm,
                userData);
        }


        /// <summary>
        /// 打开Ui界面
        /// </summary>
        /// <param name="uiComponent"></param>
        /// <param name="assetName"></param>
        /// <param name="allowMultiInstance"></param>
        /// <param name="uiGroupName"></param>
        /// <param name="pauseCoveredUiForm"></param>
        /// <param name="userData"></param>
        /// <returns></returns>
        public static UniTask<int?> OpenUIFormAsync(this UIComponent uiComponent,
            string assetName,
            bool allowMultiInstance,
            string uiGroupName,
            bool pauseCoveredUiForm,
            object userData = null)
        {
            if (!allowMultiInstance)
            {
                if (uiComponent.IsLoadingUIForm(assetName))
                {
                    return _defaultReturn;
                }

                if (uiComponent.HasUIForm(assetName))
                {
                    return _defaultReturn;
                }
            }

            return uiComponent.OpenUIFormAsync(assetName,
                uiGroupName,
                Constant.AssetPriority.UIFormAsset,
                pauseCoveredUiForm,
                userData);
        }


        /// <summary>
        /// 存在激活的界面在组中
        /// </summary>
        /// <param name="uiComponent"></param>
        /// <param name="uiFormGroups"></param>
        /// <returns></returns>
        public static bool HasActiveViewInGroup(this UIComponent uiComponent, List<string> uiFormGroups)
        {
            if (uiFormGroups == null || uiFormGroups.Count == 0)
            {
                return false;
            }

            foreach (var groupName in uiFormGroups)
            {
                var group = uiComponent.GetUIGroup(groupName);
                if (group == null)
                {
                    continue;
                }

                if (group.UIFormCount > 0)
                {
                    return true;
                }
            }

            return false;
        }
    }
}