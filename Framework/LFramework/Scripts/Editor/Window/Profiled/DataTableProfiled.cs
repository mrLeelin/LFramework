using GameFramework;
using GameFramework.DataTable;
using UnityEditor;
using UnityGameFramework.Runtime;

namespace LFramework.Editor.Window
{
    internal sealed class DataTableProfiled : ProfiledBase
    {
        internal override bool CanDraw { get; } = true;

      
        internal override void Draw()
        {
            /*
            GetComponent(ref _dataTableComponent);

            EditorGUILayout.LabelField("Data Table Count", _dataTableComponent.Count.ToString());
            EditorGUILayout.LabelField("Cached Bytes Size", _dataTableComponent.CachedBytesSize.ToString());
            DataTableBase[] dataTables = _dataTableComponent.GetAllDataTables();
            foreach (DataTableBase dataTable in dataTables)
            {
                DrawDataTable(dataTable);
            }
            */
        }

        private void DrawDataTable(DataTableBase dataTable)
        {
            EditorGUILayout.LabelField(dataTable.FullName, Utility.Text.Format("{0} Rows", dataTable.Count));
        }
    }
}