using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using GameFramework;
using LFramework.Editor.Builder;
using LFramework.Runtime;
using LFramework.Runtime.Settings;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using Type = UnityGameFramework.Editor.Type;

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

        private List<ProfiledBase> _allProfiled;

        protected override void OnEnable()
        {
            base.OnEnable();
            if (_allProfiled == null)
            {
                _allProfiled = new List<ProfiledBase>();
                var profiledBaseTypes = Type.GetRuntimeOrEditorTypes(typeof(ProfiledBase));
                foreach (var type in profiledBaseTypes)
                {
                    if (type.IsAbstract || type.IsInterface)
                        continue;

                    try
                    {
                        var instance = Activator.CreateInstance(type) as ProfiledBase;
                        if (instance != null)
                        {
                            _allProfiled.Add(instance);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"Failed to create ProfiledBase instance: {type.Name}, Error: {e.Message}");
                    }
                }
            }
        }

        protected override OdinMenuTree BuildMenuTree()
        {
            var tree = new OdinMenuTree(supportsMultiSelect: true)
            {
                { "Home", this, EditorIcons.House },
            };
            
            //All Component Setting
            AddAllAssetsAtType<ComponentSetting>(tree, "Framework Setting")
                .AddIcons(EditorIcons.SettingsCog);
            

            // Framework Profiled 子菜单
            tree.Add("Framework Profiled", null, EditorIcons.Car);
            if (_allProfiled != null)
            {
                foreach (var profiled in _allProfiled)
                {
                    if (profiled == null) continue;
                    var name = profiled.GetType().Name;
                    if (name.EndsWith("Profiled"))
                        name = name.Substring(0, name.Length - "Profiled".Length);
                    tree.AddObjectAtPath("Framework Profiled/" + name, profiled);
                }
            }
            
            // Setting Selector - 新的配置管理系统
            AddAllAssetsAtType<SettingSelector>(tree, "Game Setting/Setting Selector")
                .AddIcons(EditorIcons.SettingsCog);

            // 所有 GameSetting 实例
            AddAllAssetsAtType<BaseSetting>(tree, "Game Setting/GameSettings")
                .AddIcons(EditorIcons.SettingsCog);
            
            tree.Add("打包", null, EditorIcons.Airplane);
            tree.AddObjectAtPath("打包/打包资源", new BuildResourcesData()).AddIcon(EditorIcons.SettingsCog);
            tree.AddObjectAtPath("打包/打包App", new BuildPackageWindow()).AddIcon(EditorIcons.SettingsCog);
            tree.AddObjectAtPath("打包/上传版本文件", new BuildVersionWindow()).AddIcon(EditorIcons.Car);
            tree.AddObjectAtPath("Utility/OpenFolder", new OpenFolderInspector()).AddIcon(EditorIcons.ShoppingCart);
            tree.Add("游戏扩展", null, EditorIcons.SettingsCog);
            AddAllExtendItems(tree);
            //tree.SortMenuItemsByName();
            return tree;
        }

        protected override void DrawEditors()
        {
            var selected = this.MenuTree?.Selection?.SelectedValue;
            if (selected is ProfiledBase profiledBase)
            {
                GUILayout.BeginVertical();

                if (!EditorApplication.isPlaying)
                {
                    SirenixEditorGUI.Title("Runtime Only", "此面板仅在 Play Mode 下可用", TextAlignment.Left, true);
                    GUILayout.EndVertical();
                    return;
                }

                if (!profiledBase.CanDraw)
                {
                    SirenixEditorGUI.Title("Not Available", "当前监控组件不可用", TextAlignment.Left, true);
                    GUILayout.EndVertical();
                    return;
                }

                SirenixEditorGUI.Title(
                    title: string.IsNullOrEmpty(profiledBase.Title)
                        ? profiledBase.GetType().Name
                        : profiledBase.Title,
                    subtitle: string.IsNullOrEmpty(profiledBase.SubTitle)
                        ? profiledBase.GetType().GetNiceFullName()
                        : profiledBase.SubTitle,
                    textAlignment: TextAlignment.Left,
                    horizontalLine: true
                );

                GUILayout.Space(10);
                profiledBase.Draw();
                GUILayout.EndVertical();
                Repaint();
            }
            else
            {
                base.DrawEditors();
            }
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