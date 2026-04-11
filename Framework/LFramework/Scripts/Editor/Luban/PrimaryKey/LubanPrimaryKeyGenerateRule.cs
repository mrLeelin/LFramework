using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;

namespace Luban.Editor.PrimaryKey
{
    [Serializable]
    public sealed class LubanPrimaryKeyGenerateRule
    {
        [LabelText("启用")]
        public bool Enable = true;

        [Required]
        [LabelText("表名")]
        public string TableName;

        [Required]
        [LabelText("主键字段")]
        public string PrimaryKeyField;

        [LabelText("注释字段")]
        public List<string> CommentFields = new();
    }
}
