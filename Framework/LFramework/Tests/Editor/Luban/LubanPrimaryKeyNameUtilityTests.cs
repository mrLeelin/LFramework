using NUnit.Framework;

namespace LFramework.Editor.Tests.Luban
{
    public class LubanPrimaryKeyNameUtilityTests
    {
        [TestCase("Login", "Login")]
        [TestCase("main-panel", "main_panel")]
        [TestCase("1001", "_1001")]
        [TestCase("class", "_class")]
        [TestCase("  Ui/Main  ", "Ui_Main")]
        public void SanitizeIdentifier_ShouldReturnExpectedName(string raw, string expected)
        {
            Assert.That(global::Luban.Editor.PrimaryKey.LubanPrimaryKeyNameUtility.SanitizeIdentifier(raw), Is.EqualTo(expected));
        }
    }
}
