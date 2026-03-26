using UnityEditor;

namespace LFramework.Editor
{
    internal sealed class EditorPrefsLocalResourceServerPortStorage : ILocalResourceServerPortStorage
    {
        private const string PortKey = "LFramework.LocalResourceServer.Port";

        public int LoadPort(int defaultPort)
        {
            return EditorPrefs.GetInt(PortKey, defaultPort);
        }

        public void SavePort(int port)
        {
            EditorPrefs.SetInt(PortKey, port);
        }
    }

    internal sealed class EditorSessionLocalResourceServerRunStateStorage : ILocalResourceServerRunStateStorage
    {
        private const string ShouldRestoreKey = "LFramework.LocalResourceServer.ShouldRestoreAfterReload";

        public bool LoadShouldRestoreAfterReload()
        {
            return SessionState.GetBool(ShouldRestoreKey, false);
        }

        public void SaveShouldRestoreAfterReload(bool shouldRestore)
        {
            SessionState.SetBool(ShouldRestoreKey, shouldRestore);
        }
    }

    internal sealed class VolatileLocalResourceServerRunStateStorage : ILocalResourceServerRunStateStorage
    {
        private bool _shouldRestore;

        public bool LoadShouldRestoreAfterReload()
        {
            return _shouldRestore;
        }

        public void SaveShouldRestoreAfterReload(bool shouldRestore)
        {
            _shouldRestore = shouldRestore;
        }
    }
}
