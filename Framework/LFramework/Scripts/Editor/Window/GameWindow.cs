using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using GameFramework;
using LFramework.Editor.Builder;
using LFramework.Runtime;
using LFramework.Runtime.Settings;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace LFramework.Editor.Window
{
    public class GameWindow : OdinMenuEditorWindow
    {
        public Action<OdinMenuTree> BuildMenuTreeAction;
        private static List<IGameWindowExtend> _gameWindowExtends;


        [MenuItem("LFramework/GameSetting")]
        private static void OpenWindow()
        {
            var window = GetWindow<GameWindow>();
            window.position = GUIHelper.GetEditorWindowRect().AlignCenter(800, 600);
        }

        private GameProfiledWindow _gameProfiledWindow;

        protected override void OnEnable()
        {
            base.OnEnable();
            _gameProfiledWindow = CreateInstance<GameProfiledWindow>();
        }

        protected override OdinMenuTree BuildMenuTree()
        {
            var tree = new OdinMenuTree(supportsMultiSelect: true)
            {
                { "Home", this, EditorIcons.House },
                { "Utility", _gameProfiledWindow, EditorIcons.Car },
            };

            AddAllAssetsAtType<ComponentSetting>(tree, "Framework Setting")
                .AddIcons(EditorIcons.SettingsCog);
            AddAllAssetsAtType<HybridCLRSetting>(tree, "Game Setting").AddIcons(EditorIcons.SettingsCog);
            //AddAllAssetsAtType<LubanExportConfig>(tree, "Game Setting").AddIcons(EditorIcons.SettingsCog);
            AddAllAssetsAtType<GameSetting>(tree, "Game Setting").AddIcons(EditorIcons.SettingsCog);

            tree.Add("打包", null, EditorIcons.Airplane);
            tree.AddObjectAtPath("打包/打包资源", new BuildResourcesData()).AddIcon(EditorIcons.SettingsCog);
            tree.AddObjectAtPath("打包/打包App", new BuildPackageWindow()).AddIcon(EditorIcons.SettingsCog);
            tree.AddObjectAtPath("打包/上传版本文件", new BuildVersionWindow()).AddIcon(EditorIcons.Car);
            tree.AddObjectAtPath("Utility/OpenFolder", new OpenFolderInspector()).AddIcon(EditorIcons.ShoppingCart);
            tree.Add("游戏扩展", null, EditorIcons.SettingsCog);
            AddAllExtendItems(tree);
            tree.SortMenuItemsByName();
            return tree;
        }


        private static void AddAllExtendItems(OdinMenuTree tree)
        {
            AppendExtends();
            if (_gameWindowExtends == null || _gameWindowExtends.Count == 0)
            {
                return;
            }

            foreach (var extend in _gameWindowExtends)
            {
                var items = extend.Handle(tree);
                if (items == null)
                {
                    continue;
                }

                foreach (var item in items)
                {
                    if (item == null || item.Value == null)
                    {
                        continue;
                    }

                    tree.AddMenuItemAtPath("游戏扩展", item);
                }
            }
        }

        private static void AppendExtends()
        {
            if (_gameWindowExtends != null)
            {
                return;
            }
            
            _gameWindowExtends = new List<IGameWindowExtend>();
            var allTypes = Utility.Assembly.GetTypes();
            foreach (var type in allTypes)
            {
                if (!type.InheritsFrom<IGameWindowExtend>())
                {
                    continue;
                }

                if (type.IsAbstract || type.IsInterface)
                {
                    continue;
                }

                _gameWindowExtends.Add((IGameWindowExtend)Activator.CreateInstance(type));
            }
        }

        private static IEnumerable<OdinMenuItem> AddAllAssetsAtType<T>(OdinMenuTree tree, string menuPath)
            where T : ScriptableObject
        {
            var allSettings = AssetUtilities.GetAllAssetsOfType<T>();

            menuPath = menuPath ?? "";
            menuPath = menuPath.TrimStart('/');
            HashSet<OdinMenuItem> result = new HashSet<OdinMenuItem>();
            foreach (T setting in allSettings)
            {
                if (!(@setting == (UnityEngine.Object)null))
                {
                    var assetsPath = AssetDatabase.GetAssetPath(setting);
                    string withoutExtension = Path.GetFileNameWithoutExtension(assetsPath);
                    string path = menuPath;
                    path = path.Trim('/') + "/" + withoutExtension;
                    string name;
                    SplitMenuPath(path, out path, out name);
                    tree.AddMenuItemAtPath((ICollection<OdinMenuItem>)
                        result, path, new OdinMenuItem(tree, name, setting));
                }
            }

            return result;
        }

        private static void SplitMenuPath(string menuPath, out string path, out string name)
        {
            menuPath = menuPath.Trim('/');
            int length = menuPath.LastIndexOf('/');
            if (length == -1)
            {
                path = "";
                name = menuPath;
            }
            else
            {
                path = menuPath.Substring(0, length);
                name = menuPath.Substring(length + 1);
            }
        }
    }
}