using System;
using System.IO;
using UnityEngine;

namespace LFramework.Editor
{
    /// <summary>
    /// Coordinates local ServerData hosting for editor tooling.
    /// </summary>
    public sealed class LocalResourceServerController
    {
        private const int DefaultPort = 8080;
        private static readonly object SharedLock = new object();
        private static ILocalResourceServerHost s_SharedHost;
        private static ILocalResourceServerPortStorage s_SharedPortStorage;
        private static ILocalResourceServerRunStateStorage s_SharedRunStateStorage;

        private readonly ILocalResourceServerHost _host;
        private readonly ILocalResourceServerPortStorage _portStorage;
        private readonly ILocalResourceServerRunStateStorage _runStateStorage;
        private int _port;

        /// <summary>
        /// Creates a controller backed by shared editor host state.
        /// </summary>
        public LocalResourceServerController()
            : this(GetSharedHost(), GetProjectRoot(), GetSharedPortStorage(), GetSharedRunStateStorage())
        {
        }

        /// <summary>
        /// Creates a controller with injected collaborators for tests.
        /// </summary>
        public LocalResourceServerController(
            ILocalResourceServerHost host,
            string projectRoot,
            ILocalResourceServerPortStorage portStorage)
            : this(host, projectRoot, portStorage, new VolatileLocalResourceServerRunStateStorage())
        {
        }

        internal LocalResourceServerController(
            ILocalResourceServerHost host,
            string projectRoot,
            ILocalResourceServerPortStorage portStorage,
            ILocalResourceServerRunStateStorage runStateStorage)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
            _portStorage = portStorage ?? throw new ArgumentNullException(nameof(portStorage));
            _runStateStorage = runStateStorage ?? throw new ArgumentNullException(nameof(runStateStorage));

            if (string.IsNullOrWhiteSpace(projectRoot))
            {
                throw new ArgumentException("Project root is invalid.", nameof(projectRoot));
            }

            string fullProjectRoot = Path.GetFullPath(projectRoot);
            RootDirectory = Path.Combine(fullProjectRoot, "ServerData");
            _port = _portStorage.LoadPort(DefaultPort);
        }

        /// <summary>
        /// Gets or sets the listening port.
        /// </summary>
        public int Port
        {
            get => _port;
            set
            {
                _port = value;
                _portStorage.SavePort(value);
            }
        }

        /// <summary>
        /// Gets whether the underlying host is serving requests.
        /// </summary>
        public bool IsRunning => _host.IsRunning;

        /// <summary>
        /// Gets the absolute ServerData root path.
        /// </summary>
        public string RootDirectory { get; }

        /// <summary>
        /// Gets the base loopback URL for the current port.
        /// </summary>
        public string BaseUrl => $"http://127.0.0.1:{Port}/";

        /// <summary>
        /// Gets whether the server should be restored after an editor domain reload.
        /// </summary>
        internal bool ShouldRestoreAfterReload => _runStateStorage.LoadShouldRestoreAfterReload();

        /// <summary>
        /// Ensures the ServerData directory exists.
        /// </summary>
        public void EnsureServerDataDirectory()
        {
            Directory.CreateDirectory(RootDirectory);
        }

        /// <summary>
        /// Starts the local resource server if the current configuration is valid.
        /// </summary>
        public bool TryStart(out string errorMessage)
        {
            if (IsRunning)
            {
                _runStateStorage.SaveShouldRestoreAfterReload(true);
                errorMessage = string.Empty;
                return true;
            }

            if (Port < 1024 || Port > 65535)
            {
                _runStateStorage.SaveShouldRestoreAfterReload(false);
                errorMessage = "Port must be between 1024 and 65535.";
                return false;
            }

            EnsureServerDataDirectory();
            bool started = _host.TryStart(Port, RootDirectory, out errorMessage);
            _runStateStorage.SaveShouldRestoreAfterReload(started);
            return started;
        }

        /// <summary>
        /// Stops the local resource server.
        /// </summary>
        public void Stop()
        {
            _host.Stop();
            _runStateStorage.SaveShouldRestoreAfterReload(false);
        }

        private static ILocalResourceServerHost GetSharedHost()
        {
            lock (SharedLock)
            {
                s_SharedHost ??= new LocalResourceServerHost();
                return s_SharedHost;
            }
        }

        private static ILocalResourceServerPortStorage GetSharedPortStorage()
        {
            lock (SharedLock)
            {
                s_SharedPortStorage ??= new EditorPrefsLocalResourceServerPortStorage();
                return s_SharedPortStorage;
            }
        }

        private static ILocalResourceServerRunStateStorage GetSharedRunStateStorage()
        {
            lock (SharedLock)
            {
                s_SharedRunStateStorage ??= new EditorSessionLocalResourceServerRunStateStorage();
                return s_SharedRunStateStorage;
            }
        }

        private static string GetProjectRoot()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        }
    }

    /// <summary>
    /// Hosts static files for the local resource server.
    /// </summary>
    public interface ILocalResourceServerHost
    {
        /// <summary>
        /// Gets whether the host is running.
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// Starts serving from the given root.
        /// </summary>
        bool TryStart(int port, string rootDirectory, out string errorMessage);

        /// <summary>
        /// Stops serving requests.
        /// </summary>
        void Stop();
    }

    /// <summary>
    /// Persists the editor-selected port.
    /// </summary>
    public interface ILocalResourceServerPortStorage
    {
        /// <summary>
        /// Loads the last saved port or the provided fallback.
        /// </summary>
        int LoadPort(int defaultPort);

        /// <summary>
        /// Saves the current port.
        /// </summary>
        void SavePort(int port);
    }

    /// <summary>
    /// Persists whether the local server should be restored after an editor reload.
    /// </summary>
    public interface ILocalResourceServerRunStateStorage
    {
        /// <summary>
        /// Loads whether the server should be restored after an editor reload.
        /// </summary>
        bool LoadShouldRestoreAfterReload();

        /// <summary>
        /// Saves whether the server should be restored after an editor reload.
        /// </summary>
        void SaveShouldRestoreAfterReload(bool shouldRestore);
    }
}
