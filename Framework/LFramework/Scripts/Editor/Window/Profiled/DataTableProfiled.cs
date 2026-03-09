using GameFramework;
using GameFramework.DataTable;
using LFramework.Runtime;
using UnityEditor;
using UnityGameFramework.Runtime;

namespace LFramework.Editor.Window
{
    internal sealed class DataTableProfiled : ProfiledBase
    {
        internal override bool CanDraw { get; } = true;

        private TableComponent _tableComponent;
      
        internal override void Draw()
        {
            
            GetComponent(ref _tableComponent);

            EditorGUILayout.LabelField("Data Table Count", _tableComponent.GetAllDataTables().Length.ToString());
            foreach (TableBase dataTable in _tableComponent.GetAllDataTables())
            {
                DrawDataTable(dataTable);
            }
            
        }

        private void DrawDataTable(TableBase dataTable)
        {
            EditorGUILayout.LabelField(dataTable.Name);
        }
    }
}