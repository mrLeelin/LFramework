using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace LFramework.Editor.Tests.Luban
{
    public class LubanExportConfigMultiConfigTests
    {
        [Test]
        public void BuildExportConfig_ShouldMergePrimaryAndAdditionalSchemas()
        {
            var exportConfig = UnityEngine.ScriptableObject.CreateInstance<global::Luban.Editor.LubanExportConfig>();
            exportConfig.luban_conf_path = "Temp/Luban/luban.conf";
            exportConfig.config = new global::Luban.Editor.LubanConfig
            {
                data_dir = "Assets/Game/Luban",
                groups = new()
                {
                    new global::Luban.Editor.RawGroup { names = "c", is_default = true }
                },
                schema_files = new()
                {
                    new global::Luban.Editor.SchemaFileInfo { file_name = "Datas/Tables.xlsx", type = "table" }
                },
                targets = new()
                {
                    new global::Luban.Editor.RawTarget { name = "client", manager = "Tables", groups = "c" }
                }
            };
            exportConfig.additional_configs = new()
            {
                new global::Luban.Editor.LubanConfig
                {
                    data_dir = "Packages/com.lframework.core/Luban",
                    groups = new()
                    {
                        new global::Luban.Editor.RawGroup { names = "framework", is_default = true }
                    },
                    schema_files = new()
                    {
                        new global::Luban.Editor.SchemaFileInfo { file_name = "Datas/FrameworkTables.xlsx", type = "table" },
                        new global::Luban.Editor.SchemaFileInfo { file_name = "Datas/FrameworkBeans.xlsx", type = "bean" }
                    },
                    targets = new()
                    {
                        new global::Luban.Editor.RawTarget { name = "client", manager = "Tables", groups = "framework" }
                    }
                }
            };

            global::Luban.Editor.LubanConfig mergedConfig = exportConfig.BuildExportConfig();

            Assert.That(mergedConfig.data_dir, Is.EqualTo("."));
            Assert.That(mergedConfig.schema_files, Has.Count.EqualTo(3));
            Assert.That(mergedConfig.schema_files[0].file_name.Replace("\\", "/"), Does.EndWith("Assets/Game/Luban/Datas/Tables.xlsx"));
            Assert.That(mergedConfig.schema_files[1].file_name.Replace("\\", "/"), Does.EndWith("Packages/com.lframework.core/Luban/Datas/FrameworkTables.xlsx"));
            Assert.That(mergedConfig.schema_files[2].file_name.Replace("\\", "/"), Does.EndWith("Packages/com.lframework.core/Luban/Datas/FrameworkBeans.xlsx"));
            Assert.That(mergedConfig.targets, Has.Count.EqualTo(1));
            Assert.That(mergedConfig.targets[0].groups.Replace(" ", ""), Is.EqualTo("c,framework"));
        }

        [Test]
        public void ResolveDataRoots_ShouldIncludePrimaryAndAdditionalConfigRoots()
        {
            var exportConfig = UnityEngine.ScriptableObject.CreateInstance<global::Luban.Editor.LubanExportConfig>();
            exportConfig.luban_conf_path = Path.Combine("Temp", "Luban", "luban.conf");
            exportConfig.config = new global::Luban.Editor.LubanConfig { data_dir = "Assets/Game/Luban" };
            exportConfig.additional_configs = new()
            {
                new global::Luban.Editor.LubanConfig { data_dir = "Packages/com.lframework.core/Luban" }
            };

            var roots = global::Luban.Editor.PrimaryKey.LubanPrimaryKeyWorkbookReader.ResolveDataRoots(exportConfig);

            Assert.That(roots, Has.Count.EqualTo(2));
            Assert.That(roots[0].Replace("\\", "/"), Does.EndWith("Temp/Luban/Assets/Game/Luban"));
            Assert.That(roots[1].Replace("\\", "/"), Does.EndWith("Temp/Luban/Packages/com.lframework.core/Luban"));
        }

        [Test]
        public void ResolveWorkbookPath_ShouldSearchPrimaryAndAdditionalConfigRoots()
        {
            string tempRoot = Path.Combine(Path.GetTempPath(), "LFrameworkLubanMultiConfigTests", System.Guid.NewGuid().ToString("N"));
            string confPath = Path.Combine(tempRoot, "Config", "luban.conf");
            string projectTable = Path.Combine(tempRoot, "Config", "ProjectData", "Datas", "ProjectTable.xlsx");
            string frameworkTable = Path.Combine(tempRoot, "Config", "FrameworkData", "Datas", "FrameworkTable.xlsx");

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(projectTable)!);
                Directory.CreateDirectory(Path.GetDirectoryName(frameworkTable)!);
                File.WriteAllText(projectTable, string.Empty);
                File.WriteAllText(frameworkTable, string.Empty);

                var exportConfig = UnityEngine.ScriptableObject.CreateInstance<global::Luban.Editor.LubanExportConfig>();
                exportConfig.luban_conf_path = confPath;
                exportConfig.config = new global::Luban.Editor.LubanConfig { data_dir = "ProjectData" };
                exportConfig.additional_configs = new()
                {
                    new global::Luban.Editor.LubanConfig { data_dir = "FrameworkData" }
                };

                string resolvedProjectTable = global::Luban.Editor.PrimaryKey.LubanPrimaryKeyWorkbookReader.ResolveWorkbookPath(exportConfig, "ProjectTable");
                string resolvedFrameworkTable = global::Luban.Editor.PrimaryKey.LubanPrimaryKeyWorkbookReader.ResolveWorkbookPath(exportConfig, "FrameworkTable");

                Assert.That(resolvedProjectTable, Is.EqualTo(projectTable));
                Assert.That(resolvedFrameworkTable, Is.EqualTo(frameworkTable));
            }
            finally
            {
                if (Directory.Exists(tempRoot))
                {
                    Directory.Delete(tempRoot, true);
                }
            }
        }

        [Test]
        public void AdditionalConfigsField_ShouldUseExpandedListDrawer()
        {
            FieldInfo field = typeof(global::Luban.Editor.LubanExportConfig).GetField(nameof(global::Luban.Editor.LubanExportConfig.additional_configs));

            object listDrawerSettings = field!.GetCustomAttributes(false)
                .FirstOrDefault(static attribute => attribute.GetType().Name == "ListDrawerSettingsAttribute");

            Assert.That(listDrawerSettings, Is.Not.Null);
            Assert.That(ReadBooleanProperty(listDrawerSettings, "Expanded"), Is.True);
            Assert.That(ReadBooleanProperty(listDrawerSettings, "ShowIndexLabels"), Is.True);
        }

        private static bool ReadBooleanProperty(object instance, string propertyName)
        {
            PropertyInfo property = instance.GetType().GetProperty(propertyName);
            if (property != null)
            {
                return (bool)property.GetValue(instance);
            }

            FieldInfo field = instance.GetType().GetField(propertyName);
            Assert.That(field, Is.Not.Null);
            return (bool)field!.GetValue(instance);
        }
    }
}
