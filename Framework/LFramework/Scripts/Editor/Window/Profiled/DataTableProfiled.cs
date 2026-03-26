using GameFramework.DataTable;
using LFramework.Editor;
using LFramework.Runtime;
using UnityEditor;

namespace LFramework.Editor.Window
{
    internal sealed class DataTableProfiled : ProfiledBase
    {
        internal override bool CanDraw { get; } = true;

        private TableComponent _tableComponent;

        internal override void Draw()
        {
            GetComponent(ref _tableComponent);
            if (_tableComponent == null)
            {
                EditorGUILayout.HelpBox("TableComponent is unavailable in the current runtime context.", MessageType.Info);
                return;
            }

            TableBase[] dataTables = _tableComponent.GetAllDataTables();
            GameWindowChrome.DrawCompactHeader("Data Table Overview");
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Data Table Count", dataTables.Length.ToString());
            EditorGUILayout.EndVertical();

            GameWindowChrome.DrawCompactHeader("Registered Data Tables");
            EditorGUILayout.BeginVertical("box");
            if (dataTables.Length == 0)
            {
                EditorGUILayout.LabelField("No data tables found.");
            }
            else
            {
                foreach (TableBase dataTable in dataTables)
                {
                    DrawDataTable(dataTable);
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawDataTable(TableBase dataTable)
        {
            EditorGUILayout.LabelField(dataTable.Name);
        }
    }
}
