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
}
