using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using GameFramework;
using GameFramework.ObjectPool;
using LFramework.Editor;
using UnityEditor;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace LFramework.Editor.Window
{
    internal sealed class ObjectPoolProfiled : ProfiledBase
    {
        private readonly HashSet<string> m_OpenedItems = new HashSet<string>();

        internal override bool CanDraw { get; } = true;

        private ObjectPoolComponent _objectPoolComponent;

        internal override void Draw()
        {
            GetComponent(ref _objectPoolComponent);
            if (_objectPoolComponent == null)
            {
                EditorGUILayout.HelpBox("ObjectPoolComponent is unavailable in the current runtime state.", MessageType.Info);
                return;
            }

            ObjectPoolBase[] objectPools = _objectPoolComponent.GetAllObjectPools(true);

            GameWindowChrome.DrawCompactHeader("Overview", "Expand a pool to inspect objects, release actions, and export tools.");
            EditorGUILayout.LabelField("Object Pool Count", _objectPoolComponent.Count.ToString());
            EditorGUILayout.LabelField("Visible Pools", objectPools.Length.ToString());

            if (objectPools.Length == 0)
            {
                EditorGUILayout.HelpBox("No object pools are currently registered.", MessageType.Info);
                return;
            }

            GUILayout.Space(6f);
            GameWindowChrome.DrawCompactHeader("Pools");
            foreach (ObjectPoolBase objectPool in objectPools)
            {
                DrawObjectPool(objectPool);
            }
        }

        private void DrawObjectPool(ObjectPoolBase objectPool)
        {
            bool lastState = m_OpenedItems.Contains(objectPool.FullName);
            string label = Utility.Text.Format("{0}  ({1}/{2})", objectPool.FullName, objectPool.Count, objectPool.Capacity);
            bool currentState = EditorGUILayout.Foldout(lastState, label, true);
            if (currentState != lastState)
            {
                if (currentState)
                {
                    m_OpenedItems.Add(objectPool.FullName);
                }
                else
                {
                    m_OpenedItems.Remove(objectPool.FullName);
                }
            }

            if (!currentState)
            {
                return;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Name", objectPool.Name);
            EditorGUILayout.LabelField("Type", objectPool.ObjectType.FullName);
            EditorGUILayout.LabelField("Auto Release Interval", objectPool.AutoReleaseInterval.ToString());
            EditorGUILayout.LabelField("Capacity", objectPool.Capacity.ToString());
            EditorGUILayout.LabelField("Used Count", objectPool.Count.ToString());
            EditorGUILayout.LabelField("Can Release Count", objectPool.CanReleaseCount.ToString());
            EditorGUILayout.LabelField("Expire Time", objectPool.ExpireTime.ToString());
            EditorGUILayout.LabelField("Priority", objectPool.Priority.ToString());

            ObjectInfo[] objectInfos = objectPool.GetAllObjectInfos();
            if (objectInfos.Length > 0)
            {
                GUILayout.Space(4f);
                GameWindowChrome.DrawCompactHeader(
                    "Objects",
                    objectPool.AllowMultiSpawn
                        ? "Locked state, spawn count, release flag, priority, and last use time."
                        : "Locked state, in-use flag, release flag, priority, and last use time.");

                foreach (ObjectInfo objectInfo in objectInfos)
                {
                    DrawObjectInfo(objectPool, objectInfo);
                }

                GUILayout.Space(4f);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Release"))
                {
                    objectPool.Release();
                }

                if (GUILayout.Button("Release All Unused"))
                {
                    objectPool.ReleaseAllUnused();
                }

                if (GUILayout.Button("Export CSV Data"))
                {
                    ExportCsv(objectPool, objectInfos);
                }

                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.HelpBox("This object pool is currently empty.", MessageType.None);
            }

            EditorGUILayout.EndVertical();
            GUILayout.Space(4f);
        }

        private void DrawObjectInfo(ObjectPoolBase objectPool, ObjectInfo objectInfo)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(string.IsNullOrEmpty(objectInfo.Name) ? "<None>" : objectInfo.Name, EditorStyles.boldLabel);
            string summary = objectPool.AllowMultiSpawn
                ? Utility.Text.Format(
                    "Locked {0} | Count {1} | Flag {2} | Priority {3} | Last Use {4:yyyy-MM-dd HH:mm:ss}",
                    objectInfo.Locked,
                    objectInfo.SpawnCount,
                    objectInfo.CustomCanReleaseFlag,
                    objectInfo.Priority,
                    objectInfo.LastUseTime.ToLocalTime())
                : Utility.Text.Format(
                    "Locked {0} | In Use {1} | Flag {2} | Priority {3} | Last Use {4:yyyy-MM-dd HH:mm:ss}",
                    objectInfo.Locked,
                    objectInfo.IsInUse,
                    objectInfo.CustomCanReleaseFlag,
                    objectInfo.Priority,
                    objectInfo.LastUseTime.ToLocalTime());
            EditorGUILayout.LabelField(summary, EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.EndVertical();
        }

        private static void ExportCsv(ObjectPoolBase objectPool, ObjectInfo[] objectInfos)
        {
            string exportFileName = EditorUtility.SaveFilePanel(
                "Export CSV Data",
                string.Empty,
                Utility.Text.Format("Object Pool Data - {0}.csv", objectPool.Name),
                string.Empty);
            if (string.IsNullOrEmpty(exportFileName))
            {
                return;
            }

            try
            {
                int index = 0;
                string[] data = new string[objectInfos.Length + 1];
                data[index++] = Utility.Text.Format(
                    "Name,Locked,{0},Custom Can Release Flag,Priority,Last Use Time",
                    objectPool.AllowMultiSpawn ? "Count" : "In Use");
                foreach (ObjectInfo objectInfo in objectInfos)
                {
                    data[index++] = objectPool.AllowMultiSpawn
                        ? Utility.Text.Format(
                            "{0},{1},{2},{3},{4},{5:yyyy-MM-dd HH:mm:ss}",
                            objectInfo.Name,
                            objectInfo.Locked,
                            objectInfo.SpawnCount,
                            objectInfo.CustomCanReleaseFlag,
                            objectInfo.Priority,
                            objectInfo.LastUseTime.ToLocalTime())
                        : Utility.Text.Format(
                            "{0},{1},{2},{3},{4},{5:yyyy-MM-dd HH:mm:ss}",
                            objectInfo.Name,
                            objectInfo.Locked,
                            objectInfo.IsInUse,
                            objectInfo.CustomCanReleaseFlag,
                            objectInfo.Priority,
                            objectInfo.LastUseTime.ToLocalTime());
                }

                File.WriteAllLines(exportFileName, data, Encoding.UTF8);
                Debug.Log(Utility.Text.Format("Export object pool CSV data to '{0}' success.", exportFileName));
            }
            catch (Exception exception)
            {
                Debug.LogError(Utility.Text.Format("Export object pool CSV data to '{0}' failure, exception is '{1}'.", exportFileName, exception));
            }
        }
    }
}
