using NUnit.Framework;

namespace LFramework.Editor.Tests.Luban
{
    public class LubanQuickAccessTests
    {
        [Test]
        public void ExecuteExportMenuPath_ShouldExposeShortcut()
        {
            Assert.That(global::Luban.Editor.LubanQuickAccess.ExecuteExportMenuPath, Is.EqualTo("LFramework/Luban/执行导表 %#l"));
        }

        [Test]
        public void CanExecuteQuickExport_ShouldReturnFalse_WhenEditorIsCompiling()
        {
            bool canExecute = global::Luban.Editor.LubanQuickAccess.CanExecuteQuickExport(isCompiling: true, isUpdating: false);

            Assert.That(canExecute, Is.False);
        }

        [Test]
        public void CanExecuteQuickExport_ShouldReturnFalse_WhenEditorIsUpdating()
        {
            bool canExecute = global::Luban.Editor.LubanQuickAccess.CanExecuteQuickExport(isCompiling: false, isUpdating: true);

            Assert.That(canExecute, Is.False);
        }

        [Test]
        public void CanExecuteQuickExport_ShouldReturnTrue_WhenEditorIsIdle()
        {
            bool canExecute = global::Luban.Editor.LubanQuickAccess.CanExecuteQuickExport(isCompiling: false, isUpdating: false);

            Assert.That(canExecute, Is.True);
        }

        [Test]
        public void SceneToolbarElementId_ShouldStayStable()
        {
            Assert.That(global::Luban.Editor.LubanQuickAccess.SceneToolbarElementId, Is.EqualTo("LFramework/Luban/QuickExportButton"));
        }
    }
}
