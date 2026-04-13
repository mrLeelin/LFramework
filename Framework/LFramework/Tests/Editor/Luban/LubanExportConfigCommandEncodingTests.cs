using System.Text;
using NUnit.Framework;

namespace LFramework.Editor.Tests.Luban
{
    public class LubanExportConfigCommandEncodingTests
    {
        [Test]
        public void BuildWindowsBatchFileBytes_ShouldEmitUtf8Bom_AndEnableUtf8CodePage()
        {
            byte[] bytes = global::Luban.Editor.LubanExportConfig.BuildWindowsBatchFileBytes(
                "dotnet C:/工具/Luban.dll ^\n--conf C:/配置/导表.conf");

            Assert.That(bytes.Length, Is.GreaterThan(3));
            Assert.That(bytes[0], Is.EqualTo(0xEF));
            Assert.That(bytes[1], Is.EqualTo(0xBB));
            Assert.That(bytes[2], Is.EqualTo(0xBF));

            string text = Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);

            Assert.That(text, Does.StartWith("@echo off"));
            Assert.That(text, Does.Contain("chcp 65001 >nul"));
            Assert.That(text, Does.Contain("dotnet C:/工具/Luban.dll ^\n--conf C:/配置/导表.conf"));
        }

        [Test]
        public void ShouldLogCommandOutputAsError_ShouldReturnTrue_WhenMessageContainsError()
        {
            bool shouldLogAsError = global::Luban.Editor.LubanExportConfig.ShouldLogCommandOutputAsError(
                "Error: 导表失败");

            Assert.That(shouldLogAsError, Is.True);
        }

        [Test]
        public void ShouldLogCommandOutputAsError_ShouldReturnFalse_WhenMessageDoesNotContainError()
        {
            bool shouldLogAsError = global::Luban.Editor.LubanExportConfig.ShouldLogCommandOutputAsError(
                "Generate 12 tables completed");

            Assert.That(shouldLogAsError, Is.False);
        }
    }
}
