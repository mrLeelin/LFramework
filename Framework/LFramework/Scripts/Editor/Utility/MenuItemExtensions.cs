using System.Collections;
using System.Collections.Generic;
using LFramework.Runtime;
using UnityEditor;
using UnityEngine;

namespace LFramework.Editor
{
    public static class MenuItemExtensions
    {


        [MenuItem("GameObject/UI/CustomImage")]
        public static void CreateCustomImage()
        {
            var selected = Selection.activeGameObject;
            var newGameObject = new GameObject("CustomImage");
            newGameObject.AddComponent<CustomImage>();
            if (selected != null)
            {
                newGameObject.transform.SetParent(selected.transform);
                newGameObject.transform.localPosition = Vector3.zero;
                newGameObject.transform.rotation = Quaternion.identity;
                newGameObject.transform.localScale = Vector3.one;
            }
            Selection.activeGameObject = newGameObject;
        }
    }
 
}
