using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LFramework.Editor.Tests.Window
{
    public class GameWindowLocalResourceServerIconTests
    {
        private static readonly Regex IconNamePattern = new("\"(?<name>d_[^\"]+)\"", RegexOptions.Compiled);

        [Test]
        public void ConfiguredDarkThemeIconNamesInGameWindowLocalResourceServerResolveToBuiltinIcons()
        {
            List<string> missingIconNames = FindMissingIconNames();
            if (missingIconNames.Count > 0)
            {
                TestContext.WriteLine($"Missing GameWindowLocalResourceServer icons: {string.Join(", ", missingIconNames)}");
                WriteReplacementProbeResults(TestContext.WriteLine);
            }

            Assert.That(missingIconNames, Is.Empty);
        }

        public static void RunIconValidation()
        {
            List<string> missingIconNames = FindMissingIconNames();
            if (missingIconNames.Count > 0)
            {
                Debug.LogError($"Missing GameWindowLocalResourceServer icons: {string.Join(", ", missingIconNames)}");
                WriteReplacementProbeResults(Debug.Log);
                EditorApplication.Exit(1);
                return;
            }

            Debug.Log("GameWindowLocalResourceServer icon validation passed.");
            EditorApplication.Exit(0);
        }

        private static List<string> FindMissingIconNames()
        {
            string sourcePath = Path.Combine(
                Application.dataPath,
                "Framework",
                "Framework",
                "LFramework",
                "Scripts",
                "Editor",
                "Window",
                "GameWindowLocalResourceServer.cs");

            Assert.That(File.Exists(sourcePath), Is.True, $"Expected GameWindowLocalResourceServer source file at '{sourcePath}'.");

            string source = File.ReadAllText(sourcePath);
            string[] iconNames = IconNamePattern.Matches(source)
                .Select(match => match.Groups["name"].Value)
                .Distinct()
                .OrderBy(name => name)
                .ToArray();

            Assert.That(iconNames, Is.Not.Empty, "Expected GameWindowLocalResourceServer to declare built-in dark theme icon names.");

            var missingIconNames = new List<string>();
            foreach (string iconName in iconNames)
            {
                Texture icon = EditorGUIUtility.IconContent(iconName)?.image;
                if (icon == null)
                {
                    missingIconNames.Add(iconName);
                }
            }

            return missingIconNames;
        }

        private static void WriteReplacementProbeResults(System.Action<string> writeLine)
        {
            string[] candidates =
            {
                "d_Clipboard",
                "Clipboard",
                "d_TreeEditor.Duplicate",
                "TreeEditor.Duplicate",
                "d_Linked",
                "Linked"
            };

            foreach (string candidate in candidates)
            {
                bool exists = EditorGUIUtility.IconContent(candidate)?.image != null;
                writeLine($"{candidate}: {(exists ? "FOUND" : "MISSING")}");
            }
        }
    }
}
