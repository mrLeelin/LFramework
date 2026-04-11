using System.IO;
using System.Reflection;
using NUnit.Framework;

namespace LFramework.Editor.Tests.Luban
{
    public class LubanPrimaryKeyWorkbookReaderTests
    {
        [Test]
        public void ReadRows_ShouldReadUiFormConfigRows()
        {
            string workbookPath = Path.GetFullPath(
                Path.Combine(Directory.GetCurrentDirectory(), "Tools", "Datas", "Assets", "UiFormConfig.xlsx"));

            Assert.That(File.Exists(workbookPath), Is.True, "Workbook path should exist for the smoke test.");

            var rows = global::Luban.Editor.PrimaryKey.LubanPrimaryKeyWorkbookReader.ReadRows(workbookPath);

            Assert.That(rows.Count, Is.GreaterThan(0));
            Assert.That(rows[0].ContainsKey("Id"), Is.True);
            Assert.That(rows[0].ContainsKey("AssetsName"), Is.True);
        }

        [TestCase("##var", true)]
        [TestCase(" ##var ", true)]
        [TestCase("##type", false)]
        [TestCase("", false)]
        public void IsHeaderRow_ShouldOnlyMatchVarMarker(string firstCellValue, bool expected)
        {
            bool actual = InvokePrivateBoolean("IsHeaderRow", firstCellValue);

            Assert.That(actual, Is.EqualTo(expected));
        }

        [TestCase("##type", true)]
        [TestCase("##", true)]
        [TestCase("anything", true)]
        [TestCase("", false)]
        [TestCase("   ", false)]
        [TestCase(null, false)]
        public void ShouldSkipRow_ShouldSkipOnlyWhenFirstCellHasContent(string firstCellValue, bool expected)
        {
            bool actual = InvokePrivateBoolean("ShouldSkipRow", firstCellValue);

            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void GetTableNameFromWorkbookPath_ShouldReturnFileNameWithoutExtension()
        {
            string workbookPath = Path.Combine("Tools", "Datas", "Assets", "UiFormConfig.xlsx");

            string tableName = global::Luban.Editor.PrimaryKey.LubanPrimaryKeyWorkbookReader.GetTableNameFromWorkbookPath(workbookPath);

            Assert.That(tableName, Is.EqualTo("UiFormConfig"));
        }

        [Test]
        public void ReadHeaderNames_ShouldReturnWorkbookHeaders()
        {
            string workbookPath = Path.GetFullPath(
                Path.Combine(Directory.GetCurrentDirectory(), "Tools", "Datas", "Assets", "UiFormConfig.xlsx"));

            var headers = global::Luban.Editor.PrimaryKey.LubanPrimaryKeyWorkbookReader.ReadHeaderNames(workbookPath);

            Assert.That(headers, Does.Contain("Id"));
            Assert.That(headers, Does.Contain("AssetsName"));
            Assert.That(headers, Does.Contain("UiGroupName"));
        }

        private static bool InvokePrivateBoolean(string methodName, string argument)
        {
            MethodInfo method = typeof(global::Luban.Editor.PrimaryKey.LubanPrimaryKeyWorkbookReader)
                .GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static);

            Assert.That(method, Is.Not.Null, $"Expected private static method '{methodName}' to exist.");

            return (bool)method.Invoke(null, new object[] { argument });
        }
    }
}
