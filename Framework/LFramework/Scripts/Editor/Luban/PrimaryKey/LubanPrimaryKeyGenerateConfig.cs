using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Luban.Editor.PrimaryKey
{
    [CreateAssetMenu(fileName = "LubanPrimaryKeyGenerateConfig", menuName = "Luban/PrimaryKeyGenerateConfig")]
    public sealed class LubanPrimaryKeyGenerateConfig : ScriptableObject
    {
        [LabelText("命名空间")]
        public string Namespace = "MagicWarrior.Hotfix";

        [Required]
        [LabelText("输出目录")]
        [FolderPath]
        public string OutputDir = "Assets/MagicWarrior/Script/Generated";

        [TableList(ShowIndexLabels = true, AlwaysExpanded = true)]
        [HideLabel]
        public List<LubanPrimaryKeyGenerateRule> Rules = new();

        internal void InitializeDefaults()
        {
            Rules ??= new List<LubanPrimaryKeyGenerateRule>();
            if (Rules.Count > 0)
            {
                return;
            }

            Rules.Add(new LubanPrimaryKeyGenerateRule
            {
                Enable = true,
                TableName = "UiFormConfig",
                PrimaryKeyField = "Id",
                CommentFields = new List<string> { "AssetsName" }
            });
        }
    }
}
