using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LFramework.Editor.Tests.Window
{
    public class GameWindowHomeIconTests
    {
        private static readonly Regex IconNamePattern = new("\"(?<name>d_[^\"]+)\"", RegexOptions.Compiled);

        [Test]
        public void ConfiguredDarkThemeIconNamesInGameWindowHomeResolveToBuiltinIcons()
        {
            string sourcePath = Path.Combine(
                Application.dataPath,
                "Framework",
                "Framework",
                "LFramework",
                "Scripts",
                "Editor",
                "Window",
                "GameWindowHome.cs");

            Assert.That(File.Exists(sourcePath), Is.True, $"Expected GameWindowHome source file at '{sourcePath}'.");

            string source = File.ReadAllText(sourcePath);
            string[] iconNames = IconNamePattern.Matches(source)
                .Select(match => match.Groups["name"].Value)
                .Distinct()
                .OrderBy(name => name)
                .ToArray();

            Assert.That(iconNames, Is.Not.Empty, "Expected GameWindowHome to declare built-in dark theme icon names.");

            var missingIconNames = new List<string>();
            foreach (string iconName in iconNames)
            {
                Texture icon = EditorGUIUtility.IconContent(iconName)?.image;
                if (icon == null)
                {
                    missingIconNames.Add(iconName);
                }
            }

            if (missingIconNames.Count > 0)
            {
                TestContext.WriteLine($"Missing GameWindowHome icons: {string.Join(", ", missingIconNames)}");
                WriteReplacementProbeResults();
            }

            Assert.That(missingIconNames, Is.Empty);
        }

        private static void WriteReplacementProbeResults()
        {
            string[] candidates =
            {
                "d_SettingsIcon",
                "SettingsIcon",
                "d_Preset.Context",
                "Preset.Context",
                "d_FilterByType",
                "FilterByType"
            };

            foreach (string candidate in candidates)
            {
                bool exists = EditorGUIUtility.IconContent(candidate)?.image != null;
                TestContext.WriteLine($"{candidate}: {(exists ? "FOUND" : "MISSING")}");
            }
        }
    }
}
