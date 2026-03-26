using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace LFramework.Editor.Tests.BuildPackage
{
    public class BuildArtifactPostprocessHelperTests
    {
        private string _rootPath;

        [SetUp]
        public void SetUp()
        {
            _rootPath = Path.Combine(Path.GetTempPath(), "LFrameworkBuildArtifactTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_rootPath);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_rootPath))
            {
                Directory.Delete(_rootPath, true);
            }
        }

        [Test]
        public void ProcessBuildArtifacts_CopiesChangedFilesAndDeletedManifest()
        {
            string currentBuildPath = CreateDirectory("Current");
            string lastBuildPath = CreateDirectory("LastBuild");
            string diffBuildPath = CreateDirectory("Diff");

            WriteFile(currentBuildPath, "same.txt", "same");
            WriteFile(currentBuildPath, "changed.txt", "new");
            WriteFile(currentBuildPath, "added/new.txt", "added");

            WriteFile(lastBuildPath, "same.txt", "same");
            WriteFile(lastBuildPath, "changed.txt", "old");
            WriteFile(lastBuildPath, "deleted/old.txt", "gone");

            InvokeProcessBuildArtifacts(currentBuildPath, lastBuildPath, diffBuildPath);

            Assert.That(File.Exists(Path.Combine(diffBuildPath, "same.txt")), Is.False);
            Assert.That(File.ReadAllText(Path.Combine(diffBuildPath, "changed.txt")), Is.EqualTo("new"));
            Assert.That(File.ReadAllText(Path.Combine(diffBuildPath, "added", "new.txt")), Is.EqualTo("added"));

            string deletedManifestPath = Path.Combine(diffBuildPath, "DeletedFiles.txt");
            Assert.That(File.Exists(deletedManifestPath), Is.True);

            string[] deletedFiles = File.ReadAllLines(deletedManifestPath);
            Assert.That(deletedFiles, Is.EquivalentTo(new[] { "deleted/old.txt" }));
        }

        [Test]
        public void ProcessBuildArtifacts_ReplacesLastBuildWithCurrentSnapshot()
        {
            string currentBuildPath = CreateDirectory("Current");
            string lastBuildPath = CreateDirectory("LastBuild");
            string diffBuildPath = CreateDirectory("Diff");

            WriteFile(currentBuildPath, "content/file.txt", "current");
            WriteFile(lastBuildPath, "content/file.txt", "old");
            WriteFile(diffBuildPath, "stale.txt", "stale");

            InvokeProcessBuildArtifacts(currentBuildPath, lastBuildPath, diffBuildPath);

            Assert.That(File.ReadAllText(Path.Combine(lastBuildPath, "content", "file.txt")), Is.EqualTo("current"));
            Assert.That(File.Exists(Path.Combine(diffBuildPath, "stale.txt")), Is.False);
        }

        private void InvokeProcessBuildArtifacts(string currentBuildPath, string lastBuildPath, string diffBuildPath)
        {
            var helperType = Type.GetType(
                "LFramework.Editor.Builder.BuildingResource.BuildArtifactPostprocessHelper, LFramework.Editor");
            Assert.That(helperType, Is.Not.Null, "BuildArtifactPostprocessHelper type is missing.");

            var method = helperType.GetMethod(
                "ProcessBuildArtifacts",
                BindingFlags.Static | BindingFlags.Public,
                null,
                new[] { typeof(string), typeof(string), typeof(string) },
                null);

            Assert.That(method, Is.Not.Null, "ProcessBuildArtifacts method is missing.");
            method.Invoke(null, new object[] { currentBuildPath, lastBuildPath, diffBuildPath });
        }

        private string CreateDirectory(string relativePath)
        {
            string path = Path.Combine(_rootPath, relativePath);
            Directory.CreateDirectory(path);
            return path;
        }

        private static void WriteFile(string rootPath, string relativePath, string content)
        {
            string fullPath = Path.Combine(rootPath, relativePath);
            string directoryPath = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            File.WriteAllText(fullPath, content);
        }
    }
}

namespace LFramework.Editor.Tests.Window
{
    public class GameWindowPageDescriptorTests
    {
        [Test]
        public void CreateForComponent_ReturnsNull_WhenTypeNameMissing()
        {
            object descriptor = InvokeFactory("CreateForComponent", null, null);

            Assert.That(descriptor, Is.Null);
        }

        [Test]
        public void CreateForComponent_BuildsFrameworkSettingDescriptor()
        {
            object descriptor = InvokeFactory(
                "CreateForComponent",
                "BaseComponentSetting",
                "LFramework.Runtime.BaseComponent");

            Assert.That(descriptor, Is.Not.Null);
            Assert.That(GetStringProperty(descriptor, "ContextLabel"), Is.EqualTo("Framework Setting"));
            Assert.That(GetStringProperty(descriptor, "Title"), Is.EqualTo("Base"));
            Assert.That(GetStringProperty(descriptor, "Subtitle"), Is.EqualTo("LFramework.Runtime.BaseComponent"));
        }

        [Test]
        public void CreateForProfiled_BuildsFrameworkProfiledDescriptor_FromExplicitMetadata()
        {
            object descriptor = InvokeFactory(
                "CreateForProfiled",
                "RuntimeCountersProfiled",
                "Runtime Counters",
                "Live component metrics");

            Assert.That(descriptor, Is.Not.Null);
            Assert.That(GetStringProperty(descriptor, "ContextLabel"), Is.EqualTo("Framework Profiled"));
            Assert.That(GetStringProperty(descriptor, "Title"), Is.EqualTo("Runtime Counters"));
            Assert.That(GetStringProperty(descriptor, "Subtitle"), Is.EqualTo("Live component metrics"));
        }

        [Test]
        public void CreateForProfiled_StripsProfiledSuffix_WhenExplicitTitleMissing()
        {
            object descriptor = InvokeFactory(
                "CreateForProfiled",
                "NetworkOverviewProfiled",
                null,
                null);

            Assert.That(descriptor, Is.Not.Null);
            Assert.That(GetStringProperty(descriptor, "ContextLabel"), Is.EqualTo("Framework Profiled"));
            Assert.That(GetStringProperty(descriptor, "Title"), Is.EqualTo("Network Overview"));
            Assert.That(GetStringProperty(descriptor, "Subtitle"), Is.EqualTo("NetworkOverviewProfiled"));
        }

        private static object InvokeFactory(string methodName, params object[] args)
        {
            Assembly editorAssembly = Assembly.Load("LFramework.Editor");
            System.Type factoryType = editorAssembly.GetType("LFramework.Editor.Window.GameWindowPageDescriptorFactory");

            Assert.That(factoryType, Is.Not.Null, "Expected GameWindowPageDescriptorFactory to exist in LFramework.Editor.");

            MethodInfo createMethod = factoryType.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(createMethod, Is.Not.Null, $"Expected GameWindowPageDescriptorFactory.{methodName} to exist.");

            return createMethod.Invoke(null, args);
        }

        private static string GetStringProperty(object instance, string propertyName)
        {
            PropertyInfo property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(property, Is.Not.Null, $"Expected property '{propertyName}' to exist.");
            return property.GetValue(instance) as string;
        }
    }
}
