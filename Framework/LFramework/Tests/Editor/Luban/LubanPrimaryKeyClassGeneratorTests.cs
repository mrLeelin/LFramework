using System.Collections.Generic;
using NUnit.Framework;

namespace LFramework.Editor.Tests.Luban
{
    public class LubanPrimaryKeyClassGeneratorTests
    {
        [Test]
        public void GenerateCode_ShouldUseHeaderOrder_WhenMultipleCommentFieldsSelected()
        {
            var config = new global::Luban.Editor.PrimaryKey.LubanPrimaryKeyGenerateConfig
            {
                Namespace = "MagicWarrior.Hotfix",
                OutputDir = "Assets/MagicWarrior/Script/Generated"
            };
            var rule = new global::Luban.Editor.PrimaryKey.LubanPrimaryKeyGenerateRule
            {
                Enable = true,
                TableName = "UiFormConfig",
                PrimaryKeyField = "Id",
                CommentFields = new List<string> { "UiGroupName", "AssetsName" }
            };
            var headers = new[] { "Id", "AssetsName", "UiGroupName" };
            var rows = new List<Dictionary<string, string>>
            {
                new()
                {
                    ["Id"] = "Login",
                    ["AssetsName"] = "登录界面",
                    ["UiGroupName"] = "PopUp"
                }
            };

            string code = global::Luban.Editor.PrimaryKey.LubanPrimaryKeyClassGenerator.GenerateCode(config, rule, headers, rows);

            Assert.That(code, Does.Contain("public static class UiFormConfigSerialID"));
            Assert.That(code, Does.Contain("/// 登录界面"));
            Assert.That(code, Does.Contain("/// PopUp"));
            Assert.That(code.IndexOf("/// 登录界面"), Is.LessThan(code.IndexOf("/// PopUp")));
        }

        [Test]
        public void GenerateCode_ShouldSkipEmptyCommentValues()
        {
            var config = new global::Luban.Editor.PrimaryKey.LubanPrimaryKeyGenerateConfig
            {
                Namespace = "MagicWarrior.Hotfix",
                OutputDir = "Assets/MagicWarrior/Script/Generated"
            };
            var rule = new global::Luban.Editor.PrimaryKey.LubanPrimaryKeyGenerateRule
            {
                Enable = true,
                TableName = "UiFormConfig",
                PrimaryKeyField = "Id",
                CommentFields = new List<string> { "AssetsName", "UiGroupName" }
            };
            var headers = new[] { "Id", "AssetsName", "UiGroupName" };
            var rows = new List<Dictionary<string, string>>
            {
                new()
                {
                    ["Id"] = "Login",
                    ["AssetsName"] = "登录界面",
                    ["UiGroupName"] = string.Empty
                }
            };

            string code = global::Luban.Editor.PrimaryKey.LubanPrimaryKeyClassGenerator.GenerateCode(config, rule, headers, rows);

            Assert.That(code, Does.Contain("/// 登录界面"));
            Assert.That(code, Does.Not.Contain("/// "));
            Assert.That(code, Does.Contain("public const string Login = \"Login\";"));
        }

        [Test]
        public void GenerateCode_ShouldOmitComments_WhenCommentFieldsEmpty()
        {
            var config = new global::Luban.Editor.PrimaryKey.LubanPrimaryKeyGenerateConfig
            {
                Namespace = "MagicWarrior.Hotfix",
                OutputDir = "Assets/MagicWarrior/Script/Generated"
            };
            var rule = new global::Luban.Editor.PrimaryKey.LubanPrimaryKeyGenerateRule
            {
                Enable = true,
                TableName = "UiFormConfig",
                PrimaryKeyField = "Id",
                CommentFields = new List<string>()
            };
            var headers = new[] { "Id", "AssetsName" };
            var rows = new List<Dictionary<string, string>>
            {
                new()
                {
                    ["Id"] = "Setting"
                }
            };

            string code = global::Luban.Editor.PrimaryKey.LubanPrimaryKeyClassGenerator.GenerateCode(config, rule, headers, rows);

            Assert.That(code, Does.Contain("public const string Setting = \"Setting\";"));
            Assert.That(code, Does.Not.Contain("<summary>"));
        }

        [Test]
        public void GenerateCode_ShouldSkipRows_WhenPrimaryKeyValueEmpty()
        {
            var config = new global::Luban.Editor.PrimaryKey.LubanPrimaryKeyGenerateConfig
            {
                Namespace = "MagicWarrior.Hotfix",
                OutputDir = "Assets/MagicWarrior/Script/Generated"
            };
            var rule = new global::Luban.Editor.PrimaryKey.LubanPrimaryKeyGenerateRule
            {
                Enable = true,
                TableName = "UiFormConfig",
                PrimaryKeyField = "Id",
                CommentFields = new List<string> { "AssetsName" }
            };
            var headers = new[] { "Id", "AssetsName" };
            var rows = new List<Dictionary<string, string>>
            {
                new()
                {
                    ["Id"] = string.Empty,
                    ["AssetsName"] = "ShouldSkip"
                },
                new()
                {
                    ["Id"] = "Login",
                    ["AssetsName"] = "登录界面"
                }
            };

            string code = global::Luban.Editor.PrimaryKey.LubanPrimaryKeyClassGenerator.GenerateCode(config, rule, headers, rows);

            Assert.That(code, Does.Not.Contain("ShouldSkip"));
            Assert.That(code, Does.Contain("public const string Login = \"Login\";"));
        }

        [Test]
        public void GenerateCode_ShouldThrow_WhenPrimaryKeyValuesDuplicate()
        {
            var config = new global::Luban.Editor.PrimaryKey.LubanPrimaryKeyGenerateConfig();
            var rule = new global::Luban.Editor.PrimaryKey.LubanPrimaryKeyGenerateRule
            {
                Enable = true,
                TableName = "UiFormConfig",
                PrimaryKeyField = "Id"
            };
            var headers = new[] { "Id" };
            var rows = new List<Dictionary<string, string>>
            {
                new() { ["Id"] = "Login" },
                new() { ["Id"] = "Login" }
            };

            var exception = Assert.Throws<System.InvalidOperationException>(() =>
                global::Luban.Editor.PrimaryKey.LubanPrimaryKeyClassGenerator.GenerateCode(config, rule, headers, rows));

            Assert.That(exception!.Message, Does.Contain("Login"));
        }

        [Test]
        public void GenerateCode_ShouldThrow_WhenSanitizedNamesDuplicate()
        {
            var config = new global::Luban.Editor.PrimaryKey.LubanPrimaryKeyGenerateConfig();
            var rule = new global::Luban.Editor.PrimaryKey.LubanPrimaryKeyGenerateRule
            {
                Enable = true,
                TableName = "UiFormConfig",
                PrimaryKeyField = "Id"
            };
            var headers = new[] { "Id" };
            var rows = new List<Dictionary<string, string>>
            {
                new() { ["Id"] = "main-panel" },
                new() { ["Id"] = "main panel" }
            };

            var exception = Assert.Throws<System.InvalidOperationException>(() =>
                global::Luban.Editor.PrimaryKey.LubanPrimaryKeyClassGenerator.GenerateCode(config, rule, headers, rows));

            Assert.That(exception!.Message, Does.Contain("main_panel"));
        }

        [Test]
        public void ResolveOutputClassName_ShouldAlwaysUseTableNameSerialId()
        {
            var rule = new global::Luban.Editor.PrimaryKey.LubanPrimaryKeyGenerateRule
            {
                TableName = "UiFormConfig"
            };

            string outputClassName = global::Luban.Editor.PrimaryKey.LubanPrimaryKeyClassGenerator.ResolveOutputClassName(rule);

            Assert.That(outputClassName, Is.EqualTo("UiFormConfigSerialID"));
        }
    }
}
