using System;
using System.IO;
using NUnit.Framework;

namespace LFramework.Editor.Tests.LocalResourceServer
{
    public class LocalResourceServerControllerTests
    {
        private string _tempProjectRoot;

        [SetUp]
        public void SetUp()
        {
            _tempProjectRoot = Path.Combine(Path.GetTempPath(), "LFrameworkLocalResourceServerTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempProjectRoot);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tempProjectRoot))
            {
                Directory.Delete(_tempProjectRoot, true);
            }
        }

        [Test]
        public void Constructor_ResolvesServerDataUnderProjectRoot()
        {
            var host = new FakeLocalResourceServerHost();
            var controller = new LocalResourceServerController(host, _tempProjectRoot, new FakePortStorage());

            Assert.That(controller.RootDirectory, Is.EqualTo(Path.Combine(_tempProjectRoot, "ServerData")));
        }

        [Test]
        public void EnsureServerDataDirectory_CreatesMissingDirectory()
        {
            var host = new FakeLocalResourceServerHost();
            var controller = new LocalResourceServerController(host, _tempProjectRoot, new FakePortStorage());

            Assert.That(Directory.Exists(controller.RootDirectory), Is.False);
            controller.EnsureServerDataDirectory();
            Assert.That(Directory.Exists(controller.RootDirectory), Is.True);
        }

        [Test]
        public void TryStart_ReturnsFalse_WhenPortOutOfRange()
        {
            var host = new FakeLocalResourceServerHost();
            var controller = new LocalResourceServerController(host, _tempProjectRoot, new FakePortStorage());

            controller.Port = 1000;
            bool startedLow = controller.TryStart(out string lowError);

            controller.Port = 70000;
            bool startedHigh = controller.TryStart(out string highError);

            Assert.That(startedLow, Is.False);
            Assert.That(startedHigh, Is.False);
            Assert.That(lowError, Does.Contain("1024"));
            Assert.That(highError, Does.Contain("65535"));
        }

        [Test]
        public void TryStart_UsesHostAndExposesBaseUrlAndRunningState()
        {
            var host = new FakeLocalResourceServerHost();
            var controller = new LocalResourceServerController(host, _tempProjectRoot, new FakePortStorage());
            controller.Port = 18080;

            bool started = controller.TryStart(out string error);

            Assert.That(started, Is.True);
            Assert.That(error, Is.Empty);
            Assert.That(host.StartCalled, Is.True);
            Assert.That(host.LastPort, Is.EqualTo(18080));
            Assert.That(host.LastRootDirectory, Is.EqualTo(controller.RootDirectory));
            Assert.That(controller.IsRunning, Is.True);
            Assert.That(controller.BaseUrl, Is.EqualTo("http://127.0.0.1:18080/"));
        }

        [Test]
        public void Stop_StopsHost()
        {
            var host = new FakeLocalResourceServerHost();
            var controller = new LocalResourceServerController(host, _tempProjectRoot, new FakePortStorage());

            controller.TryStart(out _);
            controller.Stop();

            Assert.That(host.StopCalled, Is.True);
            Assert.That(controller.IsRunning, Is.False);
        }

        [Test]
        public void TryStart_PersistsRestoreFlag_WhenStartSucceeds()
        {
            var host = new FakeLocalResourceServerHost();
            var runStateStorage = new FakeRunStateStorage();
            var controller = new LocalResourceServerController(host, _tempProjectRoot, new FakePortStorage(), runStateStorage);

            bool started = controller.TryStart(out string error);

            Assert.That(started, Is.True);
            Assert.That(error, Is.Empty);
            Assert.That(controller.ShouldRestoreAfterReload, Is.True);
            Assert.That(runStateStorage.StoredValue, Is.True);
        }

        [Test]
        public void TryStart_ClearsRestoreFlag_WhenStartFails()
        {
            var host = new FakeLocalResourceServerHost { StartResult = false, StartErrorMessage = "Port already in use." };
            var runStateStorage = new FakeRunStateStorage { StoredValue = true };
            var controller = new LocalResourceServerController(host, _tempProjectRoot, new FakePortStorage(), runStateStorage);

            bool started = controller.TryStart(out string error);

            Assert.That(started, Is.False);
            Assert.That(error, Is.EqualTo("Port already in use."));
            Assert.That(controller.ShouldRestoreAfterReload, Is.False);
            Assert.That(runStateStorage.StoredValue, Is.False);
        }

        [Test]
        public void Stop_ClearsRestoreFlag()
        {
            var host = new FakeLocalResourceServerHost();
            var runStateStorage = new FakeRunStateStorage { StoredValue = true };
            var controller = new LocalResourceServerController(host, _tempProjectRoot, new FakePortStorage(), runStateStorage);

            controller.TryStart(out _);
            controller.Stop();

            Assert.That(runStateStorage.StoredValue, Is.False);
            Assert.That(controller.ShouldRestoreAfterReload, Is.False);
        }

        private sealed class FakeLocalResourceServerHost : ILocalResourceServerHost
        {
            public bool StartCalled { get; private set; }
            public bool StopCalled { get; private set; }
            public int LastPort { get; private set; }
            public string LastRootDirectory { get; private set; }
            public bool IsRunning { get; private set; }
            public bool StartResult { get; set; } = true;
            public string StartErrorMessage { get; set; } = string.Empty;

            public bool TryStart(int port, string rootDirectory, out string errorMessage)
            {
                StartCalled = true;
                LastPort = port;
                LastRootDirectory = rootDirectory;
                IsRunning = StartResult;
                errorMessage = StartErrorMessage;
                return StartResult;
            }

            public void Stop()
            {
                StopCalled = true;
                IsRunning = false;
            }
        }

        private sealed class FakePortStorage : ILocalResourceServerPortStorage
        {
            public int? StoredPort { get; private set; }

            public int LoadPort(int defaultPort)
            {
                return StoredPort ?? defaultPort;
            }

            public void SavePort(int port)
            {
                StoredPort = port;
            }
        }

        private sealed class FakeRunStateStorage : ILocalResourceServerRunStateStorage
        {
            public bool StoredValue { get; set; }

            public bool LoadShouldRestoreAfterReload()
            {
                return StoredValue;
            }

            public void SaveShouldRestoreAfterReload(bool shouldRestore)
            {
                StoredValue = shouldRestore;
            }
        }
    }
}
