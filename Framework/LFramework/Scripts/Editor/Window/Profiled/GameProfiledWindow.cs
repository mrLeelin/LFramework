using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using UnityGameFramework.Runtime;
using Type = UnityGameFramework.Editor.Type;

namespace LFramework.Editor.Window
{
    public class GameProfiledWindow : OdinEditorWindow
    {
        private ProfiledBase[] _allProfiled;

        [MenuItem("LFramework/GameProfiled")]
        private static void OpenWindow()
        {
            var window = GetWindow<GameProfiledWindow>();
            window.position = GUIHelper.GetEditorWindowRect().AlignCenter(800, 600);
        }


        protected override void OnEnable()
        {
            base.OnEnable();
            if (_allProfiled == null)
            {
                var profiledBaseTypes = Type.GetRuntimeOrEditorTypes(typeof(ProfiledBase));
                _allProfiled = new ProfiledBase[profiledBaseTypes.Length];
                for (var i = 0; i < profiledBaseTypes.Length; i++)
                {
                    var t = profiledBaseTypes[i];
                    var instance = Activator.CreateInstance(t) as ProfiledBase;
                    if (instance == null)
                    {
                        continue;
                    }

                    _allProfiled[i] = instance;
                }
            }
        }


        protected override IEnumerable<object> GetTargets()
        {
            if (!EditorApplication.isPlaying)
            {
                yield return this;
            }
            else
            {
                foreach (var profiledBase in _allProfiled)
                {
                    if (profiledBase.CanDraw)
                    {
                        yield return profiledBase;
                    }
                }
            }
        }

        protected override void DrawEditor(int index)
        {
            var currentDrawingEditor = this.CurrentDrawingTargets[index];

            if (currentDrawingEditor is ProfiledBase profiledBase)
            {
                SirenixEditorGUI.Title(
                    title: string.IsNullOrEmpty(profiledBase.Title)
                        ? currentDrawingEditor.ToString()
                        : profiledBase.Title,
                    subtitle: string.IsNullOrEmpty(profiledBase.SubTitle)
                        ? currentDrawingEditor.GetType().GetNiceFullName()
                        : profiledBase.SubTitle,
                    textAlignment: TextAlignment.Left,
                    horizontalLine: true
                );

                profiledBase.Draw();
            }
            else
            {
                SirenixEditorGUI.Title("Available during runtime only.", "", TextAlignment.Left, true);
            }

            base.DrawEditor(index);
        }
    }
}