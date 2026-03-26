using System;
using GameFramework.Resource;
using LFramework.Runtime.Settings;
using UnityEditor;
using UnityEngine;
using UnityGameFramework.Editor;
using UnityGameFramework.Runtime;

namespace LFramework.Editor.Inspector
{
    [CustomEditor(typeof(ResourceComponentSetting))]
    internal sealed class ResourceComponentInspector : ComponentSettingInspector
    {
        private static readonly Color AccentColor = new Color(0.18f, 0.67f, 0.94f);
        private static readonly Color CardLight = new Color(0.96f, 0.97f, 0.99f, 1f);
        private static readonly Color CardDark = new Color(0.20f, 0.22f, 0.25f, 1f);
        private static readonly Color MutedTextLight = new Color(0.35f, 0.39f, 0.44f);
        private static readonly Color MutedTextDark = new Color(0.67f, 0.71f, 0.76f);

        private SerializedProperty m_ResourceMode;
        private SerializedProperty m_MinUnloadInterval;
        private SerializedProperty m_MaxUnloadInterval;
        private SerializedProperty m_YooAssetPackageName;
        private SerializedProperty m_YooAssetPlayMode;
        private SerializedProperty m_AddressableHotfixProfileName;

        private readonly HelperInfo<ResourceHelperBase> m_ResourceHelperInfo = new HelperInfo<ResourceHelperBase>("Resource");

        private GUIStyle _cardTitleStyle;
        private GUIStyle _cardValueStyle;
        private GUIStyle _cardDetailStyle;
        private GUIStyle _sectionHeaderStyle;
        private GUIStyle _sectionDetailStyle;
        private GUIStyle _buttonStyle;
        private bool _stylesInitialized;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();
            EnsureStyles();

            using (new EditorGUI.DisabledGroupScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                DrawOverviewCard();
                GUILayout.Space(8f);

                DrawSection(
                    "Pipeline",
                    "Select the active resource backend and the helper implementation that will bridge GameFramework and the chosen pipeline.",
                    () =>
                    {
                        EditorGUILayout.PropertyField(m_ResourceMode);
                        m_ResourceHelperInfo.Draw();
                    });

                DrawSection(
                    "Release Strategy",
                    "Tune the unload cadence that controls how aggressively unused assets are released back to the runtime.",
                    () =>
                    {
                        EditorGUILayout.PropertyField(m_MinUnloadInterval);
                        EditorGUILayout.PropertyField(m_MaxUnloadInterval);
                    });

                if (m_ResourceMode.enumValueIndex == (int)ResourceMode.YooAsset)
                {
                    DrawSection(
                        "YooAsset Runtime",
                        "Configure the package name and play mode used by the YooAsset backend in editor and runtime flows.",
                        () =>
                        {
                            EditorGUILayout.PropertyField(m_YooAssetPackageName);
                            EditorGUILayout.PropertyField(m_YooAssetPlayMode);
                        });
                }
                else
                {
                    DrawSection(
                        "Addressables Runtime",
                        "Select the Addressables hotfix profile that should be used when the pipeline switches away from YooAsset.",
                        () =>
                        {
                            EditorGUILayout.PropertyField(m_AddressableHotfixProfileName);
                        });
                }

                DrawMigrationSection();
            }

            serializedObject.ApplyModifiedProperties();
            Repaint();
        }

        protected override void OnCompileComplete()
        {
            base.OnCompileComplete();
            RefreshInspectorState();
        }

        protected override void OnEnable()
        {
            m_ResourceMode = serializedObject.FindProperty("_resourceMode");
            m_MinUnloadInterval = serializedObject.FindProperty("_minUnloadInterval");
            m_MaxUnloadInterval = serializedObject.FindProperty("_maxUnloadInterval");
            m_YooAssetPackageName = serializedObject.FindProperty("_yooAssetPackageName");
            m_YooAssetPlayMode = serializedObject.FindProperty("_yooAssetPlayMode");
            m_AddressableHotfixProfileName = serializedObject.FindProperty("_hotfixProfileName");

            m_ResourceHelperInfo.Init(serializedObject);
            RefreshInspectorState();
        }

        private void DrawOverviewCard()
        {
            Rect rect = GUILayoutUtility.GetRect(0f, 104f, GUILayout.ExpandWidth(true));
            DrawCard(rect);

            GUI.Label(new Rect(rect.x + 16f, rect.y + 10f, rect.width - 32f, 18f), "Resource Pipeline Summary", _cardTitleStyle);
            GUI.Label(new Rect(rect.x + 16f, rect.y + 30f, rect.width - 32f, 24f), GetModeTitle(), _cardValueStyle);
            GUI.Label(new Rect(rect.x + 16f, rect.y + 56f, rect.width - 32f, 18f), GetModeDetail(), _cardDetailStyle);
            GUI.Label(new Rect(rect.x + 16f, rect.y + 76f, rect.width - 32f, 18f), GetReleaseDetail(), _sectionDetailStyle);
        }

        private void DrawSection(string title, string detail, Action drawContent)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(title, _sectionHeaderStyle);
                EditorGUILayout.LabelField(detail, _sectionDetailStyle);
                GUILayout.Space(6f);
                drawContent?.Invoke();
            }

            GUILayout.Space(6f);
        }

        private void DrawMigrationSection()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Resource Migration", _sectionHeaderStyle);
                EditorGUILayout.LabelField(
                    "Rebuild the generated pipeline configuration when you need to switch resource ownership between YooAsset and Addressables.",
                    _sectionDetailStyle);
                EditorGUILayout.HelpBox(
                    "Main thread handles Unity APIs. Worker threads handle validation, conflict checks, and migration planning.",
                    MessageType.Info);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (DrawActionButton("YooAssets -> Addressables"))
                    {
                        ExecuteMigration(
                            "This will rebuild generated Addressable groups and move matching entries. Continue?",
                            ResourceConfigMigrationHelper.ConvertYooAssetsToAddressables);
                    }

                    if (DrawActionButton("Addressables -> YooAssets"))
                    {
                        ExecuteMigration(
                            "This will rebuild the target YooAssets package collectors. Continue?",
                            ResourceConfigMigrationHelper.ConvertAddressablesToYooAssets);
                    }
                }
            }
        }

        private bool DrawActionButton(string label)
        {
            Color previousColor = GUI.backgroundColor;
            GUI.backgroundColor = AccentColor;
            bool clicked = GUILayout.Button(label, _buttonStyle, GUILayout.Height(30f));
            GUI.backgroundColor = previousColor;
            return clicked;
        }

        private void RefreshInspectorState()
        {
            m_ResourceHelperInfo.Refresh();
        }

        private string GetModeTitle()
        {
            return m_ResourceMode.enumValueIndex == (int)ResourceMode.YooAsset
                ? "YooAsset Delivery"
                : "Addressables Delivery";
        }

        private string GetModeDetail()
        {
            if (m_ResourceMode.enumValueIndex == (int)ResourceMode.YooAsset)
            {
                string packageName = string.IsNullOrWhiteSpace(m_YooAssetPackageName.stringValue)
                    ? "DefaultPackage"
                    : m_YooAssetPackageName.stringValue;
                string playMode = m_YooAssetPlayMode.enumDisplayNames[m_YooAssetPlayMode.enumValueIndex];
                return $"Package {packageName} · Play Mode {playMode}";
            }

            string profileName = string.IsNullOrWhiteSpace(m_AddressableHotfixProfileName.stringValue)
                ? "No hotfix profile configured"
                : m_AddressableHotfixProfileName.stringValue;
            return $"Profile {profileName}";
        }

        private string GetReleaseDetail()
        {
            return $"Unload cadence {m_MinUnloadInterval.floatValue:F0}s to {m_MaxUnloadInterval.floatValue:F0}s";
        }

        private void ExecuteMigration(
            string confirmationMessage,
            Func<ResourceComponentSetting, ResourceConfigMigrationHelper.ResourceConfigMigrationResult> action)
        {
            if (!EditorUtility.DisplayDialog("Resource Migration", confirmationMessage, "Continue", "Cancel"))
            {
                return;
            }

            var result = action((ResourceComponentSetting)target);
            var dialogTitle = result.Success ? "Migration Success" : "Migration Failed";
            var dialogBody = $"{result.Summary}\nReport: {result.ReportPath}";
            EditorUtility.DisplayDialog(dialogTitle, dialogBody, "OK");
        }

        private void EnsureStyles()
        {
            if (_stylesInitialized)
            {
                return;
            }

            _stylesInitialized = true;

            _cardTitleStyle = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleLeft,
            };

            _cardValueStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleLeft,
            };

            _cardDetailStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleLeft,
            };
            _cardDetailStyle.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : new Color(0.16f, 0.18f, 0.21f);

            _sectionHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleLeft,
            };

            _sectionDetailStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                wordWrap = true,
            };
            _sectionDetailStyle.normal.textColor = EditorGUIUtility.isProSkin ? MutedTextDark : MutedTextLight;

            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 11,
                padding = new RectOffset(12, 12, 6, 6),
            };
        }

        private void DrawCard(Rect rect)
        {
            EditorGUI.DrawRect(rect, EditorGUIUtility.isProSkin ? CardDark : CardLight);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 4f, rect.height), AccentColor);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 2f), new Color(AccentColor.r, AccentColor.g, AccentColor.b, 0.45f));
        }
    }
}
