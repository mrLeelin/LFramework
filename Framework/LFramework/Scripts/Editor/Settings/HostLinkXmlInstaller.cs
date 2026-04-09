using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using UnityEditor;
using UnityEngine;

namespace LFramework.Editor.Settings
{
    /// <summary>
    /// 在宿主项目的 Assets 目录下安装和维护 link.xml。
    /// Unity 不保证 Package 内的 link.xml 生效，因此需要同步到宿主项目。
    /// </summary>
    internal static class HostLinkXmlInstaller
    {
        private const string HostLinkXmlPath = "Assets/link.xml";

        private static readonly string[] RequiredAssemblies =
        {
            "GameFramework",
            "UnityGameFramework.Runtime",
            "LFramework.Runtime",
            "Zenject",
            "Zenject-usage"
        };

        [MenuItem("LFramework/Setup/Install Host Link.xml")]
        private static void InstallHostLinkXmlMenu()
        {
            EnsureHostLinkXml(logResult: true);
        }

        [InitializeOnLoadMethod]
        private static void EnsureHostLinkXmlOnLoad()
        {
            EnsureHostLinkXml(logResult: false);
        }

        private static void EnsureHostLinkXml(bool logResult)
        {
            LogExistingLinkXmlFiles(logResult);

            XDocument document = LoadOrCreateDocument();
            XElement linkerElement = document.Root;
            bool changed = false;

            foreach (string assemblyName in RequiredAssemblies)
            {
                XElement assemblyElement = linkerElement.Elements("assembly")
                    .FirstOrDefault(element => element.Attribute("fullname")?.Value == assemblyName);
                if (assemblyElement == null)
                {
                    linkerElement.Add(new XElement("assembly",
                        new XAttribute("fullname", assemblyName),
                        new XAttribute("preserve", "all")));
                    changed = true;
                    continue;
                }

                XAttribute preserveAttribute = assemblyElement.Attribute("preserve");
                if (preserveAttribute == null || preserveAttribute.Value != "all")
                {
                    assemblyElement.SetAttributeValue("preserve", "all");
                    changed = true;
                }
            }

            if (!changed)
            {
                if (logResult)
                {
                    Debug.Log("[HostLinkXmlInstaller] Host Assets/link.xml is already up to date.");
                }

                return;
            }

            string fullPath = Path.GetFullPath(HostLinkXmlPath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath) ?? "Assets");
            document.Save(fullPath);
            AssetDatabase.Refresh();

            if (logResult)
            {
                Debug.Log($"[HostLinkXmlInstaller] Host link.xml installed/updated at: {HostLinkXmlPath}");
            }
        }

        private static XDocument LoadOrCreateDocument()
        {
            if (File.Exists(HostLinkXmlPath))
            {
                return XDocument.Load(HostLinkXmlPath);
            }

            return new XDocument(
                new XDeclaration("1.0", "utf-8", null),
                new XElement("linker"));
        }

        private static void LogExistingLinkXmlFiles(bool logResult)
        {
            string assetsRoot = Path.GetFullPath("Assets");
            if (!Directory.Exists(assetsRoot))
            {
                return;
            }

            string[] linkXmlFiles = Directory.GetFiles(assetsRoot, "link.xml", SearchOption.AllDirectories)
                .Select(path => path.Replace('\\', '/'))
                .ToArray();
            if (linkXmlFiles.Length <= 1)
            {
                return;
            }

            StringBuilder builder = new StringBuilder();
            builder.AppendLine("[HostLinkXmlInstaller] Multiple link.xml files detected in host project:");
            foreach (string linkXmlFile in linkXmlFiles)
            {
                builder.AppendLine($" - {GetProjectRelativePath(linkXmlFile)}");
            }

            builder.AppendLine("Please ensure stripping rules are intentionally split across these files.");

            if (logResult)
            {
                Debug.LogWarning(builder.ToString());
            }
        }

        private static string GetProjectRelativePath(string fullPath)
        {
            string projectRoot = Path.GetFullPath(".");
            string normalizedProjectRoot = projectRoot.Replace('\\', '/').TrimEnd('/');
            string normalizedFullPath = fullPath.Replace('\\', '/');
            if (normalizedFullPath.StartsWith(normalizedProjectRoot))
            {
                return normalizedFullPath.Substring(normalizedProjectRoot.Length + 1);
            }

            return fullPath;
        }
    }
}
