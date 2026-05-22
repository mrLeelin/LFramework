using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace LFramework.Editor.Builder.iOS.Configurators
{
    /// <summary>
    /// Configures the generated iOS Info.plist.
    /// </summary>
    public class iOSPlistConfigurator
    {
        private readonly iOSBuildConfig _config;

        public iOSPlistConfigurator(iOSBuildConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public void Configure()
        {
            iOSBuildLogger.LogStep("Info.plist configuration");

            string plistPath = Path.Combine(_config.OutputPath, iOSBuildConstants.INFO_PLIST_PATH);
            if (!File.Exists(plistPath))
            {
                throw new FileNotFoundException("Info.plist was not found.", plistPath);
            }

            XDocument plist = XDocument.Load(plistPath, LoadOptions.PreserveWhitespace);
            XElement rootDict = plist.Root?.Element("dict");
            if (rootDict == null)
            {
                throw new InvalidOperationException($"Info.plist root dict was not found: {plistPath}");
            }

            SetBoolean(rootDict, iOSBuildConstants.PLIST_KEY_ENCRYPTION, false);
            SetString(rootDict, iOSBuildConstants.PLIST_KEY_ATT, iOSBuildConstants.ATT_USAGE_DESCRIPTION);
            SetOptionalString(rootDict, iOSBuildConstants.PLIST_KEY_CAMERA, _config.CameraUsageDescription);
            SetOptionalString(rootDict, iOSBuildConstants.PLIST_KEY_LOCATION_WHEN_IN_USE, _config.LocationUsageDescription);
            ConfigureURLSchemes(rootDict);
            ConfigureFacebookSupport(rootDict);

            Save(plist, plistPath);
            iOSBuildLogger.LogSuccess("Info.plist configuration");
        }

        private void ConfigureURLSchemes(XElement rootDict)
        {
            if (string.IsNullOrWhiteSpace(_config.URLScheme) ||
                string.IsNullOrWhiteSpace(_config.BundleURLName))
            {
                iOSBuildLogger.LogInfo("URL Scheme configuration skipped because URLScheme or BundleURLName is empty");
                return;
            }

            XElement urlTypesArray = GetOrCreateArray(rootDict, iOSBuildConstants.PLIST_KEY_URL_TYPES);
            RemoveUrlType(urlTypesArray, _config.BundleURLName);

            urlTypesArray.Add(
                new XElement(
                    "dict",
                    new XElement("key", iOSBuildConstants.PLIST_KEY_URL_NAME),
                    new XElement("string", _config.BundleURLName),
                    new XElement("key", iOSBuildConstants.PLIST_KEY_URL_SCHEMES),
                    new XElement(
                        "array",
                        new XElement("string", "https"),
                        new XElement("string", "http"),
                        new XElement("string", _config.URLScheme))));

            iOSBuildLogger.LogInfo($"Configured URL Schemes: https, http, {_config.URLScheme}");
        }

        private void ConfigureFacebookSupport(XElement rootDict)
        {
            XElement querySchemesArray = GetOrCreateArray(rootDict, iOSBuildConstants.PLIST_KEY_QUERIES_SCHEMES);
            foreach (string scheme in iOSBuildConstants.FACEBOOK_QUERY_SCHEMES)
            {
                if (!querySchemesArray.Elements("string").Any(element => element.Value == scheme))
                {
                    querySchemesArray.Add(new XElement("string", scheme));
                }
            }

            iOSBuildLogger.LogInfo($"Configured Facebook support with {iOSBuildConstants.FACEBOOK_QUERY_SCHEMES.Length} query schemes");
        }

        private static void SetOptionalString(XElement dict, string key, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                RemoveKey(dict, key);
                return;
            }

            SetString(dict, key, value);
        }

        private static void SetString(XElement dict, string key, string value)
        {
            SetValue(dict, key, new XElement("string", value ?? string.Empty));
        }

        private static void SetBoolean(XElement dict, string key, bool value)
        {
            SetValue(dict, key, new XElement(value ? "true" : "false"));
        }

        private static XElement GetOrCreateArray(XElement dict, string key)
        {
            XElement value = FindValue(dict, key);
            if (value == null)
            {
                var array = new XElement("array");
                dict.Add(new XElement("key", key));
                dict.Add(array);
                return array;
            }

            if (value.Name.LocalName != "array")
            {
                throw new InvalidOperationException($"Info.plist key {key} exists but is not an array.");
            }

            return value;
        }

        private static void SetValue(XElement dict, string key, XElement newValue)
        {
            XElement keyElement = FindKey(dict, key);
            if (keyElement == null)
            {
                dict.Add(new XElement("key", key));
                dict.Add(newValue);
                return;
            }

            XElement oldValue = keyElement.ElementsAfterSelf().FirstOrDefault();
            if (oldValue == null)
            {
                keyElement.AddAfterSelf(newValue);
                return;
            }

            oldValue.ReplaceWith(newValue);
        }

        private static void RemoveKey(XElement dict, string key)
        {
            XElement keyElement = FindKey(dict, key);
            if (keyElement == null)
            {
                return;
            }

            XElement valueElement = keyElement.ElementsAfterSelf().FirstOrDefault();
            valueElement?.Remove();
            keyElement.Remove();
        }

        private static XElement FindKey(XElement dict, string key)
        {
            return dict.Elements("key").FirstOrDefault(element => element.Value == key);
        }

        private static XElement FindValue(XElement dict, string key)
        {
            return FindKey(dict, key)?.ElementsAfterSelf().FirstOrDefault();
        }

        private static void RemoveUrlType(XElement urlTypesArray, string bundleURLName)
        {
            foreach (XElement dict in urlTypesArray.Elements("dict").ToList())
            {
                XElement nameValue = FindValue(dict, iOSBuildConstants.PLIST_KEY_URL_NAME);
                if (nameValue != null && nameValue.Value == bundleURLName)
                {
                    dict.Remove();
                }
            }
        }

        private static void Save(XDocument plist, string path)
        {
            var settings = new XmlWriterSettings
            {
                Encoding = new UTF8Encoding(false),
                Indent = true,
                OmitXmlDeclaration = false
            };

            using (XmlWriter writer = XmlWriter.Create(path, settings))
            {
                plist.Save(writer);
            }
        }
    }
}
