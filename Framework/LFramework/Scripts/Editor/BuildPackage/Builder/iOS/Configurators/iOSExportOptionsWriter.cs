using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace LFramework.Editor.Builder.iOS.Configurators
{
    /// <summary>
    /// Writes ExportOptions.plist files used by xcodebuild -exportArchive.
    /// </summary>
    public static class iOSExportOptionsWriter
    {
        public static void Write(iOSBuildConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            string directory = Path.GetDirectoryName(config.ExportOptionsPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var document = new XDocument(
                new XDeclaration("1.0", "UTF-8", null),
                new XDocumentType(
                    "plist",
                    "-//Apple//DTD PLIST 1.0//EN",
                    "http://www.apple.com/DTDs/PropertyList-1.0.dtd",
                    null),
                new XElement(
                    "plist",
                    new XAttribute("version", "1.0"),
                    new XElement(
                        "dict",
                        KeyString("method", config.ExportMethod),
                        KeyDict(
                            "provisioningProfiles",
                            KeyString(config.BundleIdentifier, config.MobileProvisionUuid)),
                        KeyString("signingStyle", "manual"),
                        KeyString("signingCertificate", config.CodeSignIdentity),
                        KeyElement("stripSwiftSymbols", new XElement("true")),
                        KeyString("teamID", config.AppleDevelopTeamId),
                        KeyElement("uploadSymbols", new XElement("true")))));

            var settings = new XmlWriterSettings
            {
                Encoding = new UTF8Encoding(false),
                Indent = true,
                OmitXmlDeclaration = false
            };

            using (XmlWriter writer = XmlWriter.Create(config.ExportOptionsPath, settings))
            {
                document.Save(writer);
            }

            iOSBuildLogger.LogInfo($"ExportOptions.plist written: {config.ExportOptionsPath}");
        }

        private static object[] KeyString(string key, string value)
        {
            return KeyElement(key, new XElement("string", value ?? string.Empty));
        }

        private static object[] KeyDict(string key, params object[] content)
        {
            return KeyElement(key, new XElement("dict", content));
        }

        private static object[] KeyElement(string key, XElement value)
        {
            return new object[]
            {
                new XElement("key", key),
                value
            };
        }
    }
}
