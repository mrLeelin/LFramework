using System.IO;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Luban.Editor.PrimaryKey
{
    public static class LubanPrimaryKeyGenerateConfigRegister
    {
        private const string DefaultAssetPath = "Assets/Editor/LubanPrimaryKeyGenerateConfig.asset";
        private static LubanPrimaryKeyGenerateConfig _instance;

        public static LubanPrimaryKeyGenerateConfig Instance => _instance ??= GetOrCreate();

        public static LubanPrimaryKeyGenerateConfig GetOrCreate()
        {
            string[] guids = AssetDatabase.FindAssets("t:LubanPrimaryKeyGenerateConfig");
            if (guids.Length > 1)
            {
                Debug.LogWarning("Found multiple LubanPrimaryKeyGenerateConfig assets, using the first one");
            }

            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<LubanPrimaryKeyGenerateConfig>(path);
            }

            if (!Directory.Exists("Assets/Editor"))
            {
                Directory.CreateDirectory("Assets/Editor");
            }

            var config = ScriptableObject.CreateInstance<LubanPrimaryKeyGenerateConfig>();
            config.InitializeDefaults();
            AssetDatabase.CreateAsset(config, DefaultAssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return config;
        }
    }

    internal static class LubanPrimaryKeyGenerateConfigSettingsRegister
    {
        private static PropertyTree _property;

        [SettingsProvider]
        public static SettingsProvider Create()
        {
            return new SettingsProvider("Project/Luban Primary Keys", SettingsScope.Project)
            {
                label = "Luban Primary Keys",
                guiHandler = _ =>
                {
                    if (_property == null)
                    {
                        var config = LubanPrimaryKeyGenerateConfigRegister.GetOrCreate();
                        _property = PropertyTree.Create(new SerializedObject(config));
                    }

                    _property.Draw();
                }
            };
        }
    }
}
