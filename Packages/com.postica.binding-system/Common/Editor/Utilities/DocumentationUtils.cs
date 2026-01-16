using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using UnityEditor;
using UnityEngine;

namespace Postica.Common.Utilities
{
    internal static class DocumentationUtils
    {
        private static readonly Dictionary<Type, string> classSummaryCache = new();

        /// <summary>
        /// Gets the summary comment of a class.
        /// </summary>
        public static string GetClassSummary(Type type)
        {
            if (classSummaryCache.TryGetValue(type, out string cachedSummary))
            {
                return cachedSummary;
            }

            string xmlPath = GetXmlPath(type.Assembly);

            if (!File.Exists(xmlPath))
            {
                // Debug.LogWarning($"XML Documentation not found at: {xmlPath}. Did you enable it in Player Settings?");
                return GetCodeClassSummary(type);
            }

            try
            {
                XDocument xml = XDocument.Load(xmlPath);
                // The compiler prefixes classes with "T:"
                string memberName = "T:" + type.FullName;

                // Find the <member> tag
                XElement member = null;
                foreach (var element in xml.Descendants("member"))
                {
                    if (element.Attribute("name")?.Value == memberName)
                    {
                        member = element;
                        break;
                    }
                }

                if (member != null)
                {
                    // Return the text inside <summary>, trimmed to remove extra whitespace/newlines
                    var summary = member.Element("summary")?.Value;
                    summary = summary?.Trim();
                    classSummaryCache[type] = summary;
                    return summary;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error reading XML documentation: {e.Message}");
            }

            classSummaryCache[type] = null;
            return null;
        }

        private static string GetXmlPath(Assembly assembly)
        {
            // Get the location of the compiled assembly (.dll)
            string assemblyPath = assembly.Location;

            // The XML file is usually right next to the DLL with the same name
            return Path.ChangeExtension(assemblyPath, ".xml");
        }

        /// <summary>
        /// Extracts the summary tooltip from a class type in the Editor.
        /// Works without enabling "XML Documentation" in Player Settings.
        /// </summary>
        public static string GetCodeClassSummary(Type type)
        {
            if (classSummaryCache.TryGetValue(type, out string cachedSummary))
            {
                return cachedSummary;
            }
            
            // 1. Find the MonoScript asset for this type
            // This relies on Unity's AssetDatabase finding the file by type name
            string[] guids = AssetDatabase.FindAssets($"{type.Name} t:MonoScript");

            if (guids.Length == 0)
            {
                classSummaryCache[type] = null;
                return null;
            }

            string path = AssetDatabase.GUIDToAssetPath(guids[0]);

            // Double check: If multiple scripts have the same name, we might grab the wrong one.
            // Usually safe for unique class names.

            if (string.IsNullOrEmpty(path))
            {
                classSummaryCache[type] = null;
                return null;
            }

            // 2. Read the file content
            string fileContent = File.ReadAllText(path);

            // 3. Parse the comments
            var summary = ExtractSummary(fileContent, type.Name);
            classSummaryCache[type] = summary;
            return summary;
        }

        private static string ExtractSummary(string fileContent, string className)
        {
            // Regex explanation:
            // We look for "class [className]" but we allow "public", "partial", etc. before it.
            // We capture the declaration line index.
            string classPattern = $@"class\s+{className}\b";
            var match = Regex.Match(fileContent, classPattern);

            if (!match.Success) return null;

            // Split file into lines to walk backwards from the class definition
            string[] lines = fileContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            // Find line number of the match
            int classLineIndex = 0;
            int charCount = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                charCount += lines[i].Length + 1; // +1 for newline
                if (charCount > match.Index)
                {
                    classLineIndex = i;
                    break;
                }
            }

            // Walk backwards to find /// comments
            StringBuilder sb = new();
            bool insideSummary = false;

            // We search backwards from the line before the class
            for (int i = classLineIndex - 1; i >= 0; i--)
            {
                string line = lines[i].Trim();

                // Skip Attributes (e.g., [Serializable], [CreateAssetMenu])
                if (line.StartsWith("[") && line.EndsWith("]")) continue;

                // Skip empty lines
                if (string.IsNullOrEmpty(line)) continue;

                // If we hit a line that is NOT a comment, we stop.
                if (!line.StartsWith("///")) break;

                // We are in the comment block. 
                // We only want the text inside <summary> tags.
                if (line.Contains("</summary>"))
                {
                    insideSummary = true;
                    continue; // Move to next line (upwards)
                }

                if (line.Contains("<summary>"))
                {
                    insideSummary = false;
                    break; // We are done
                }

                if (insideSummary)
                {
                    // Remove the "///" and trim
                    string cleanLine = line.Replace("///", "").Trim();

                    // Because we are reading backwards, prepend the line
                    sb.Insert(0, cleanLine);
                }
            }
            
            // Final trim to clean up whitespace/newlines
            int start = 0;
            while (start < sb.Length && char.IsWhiteSpace(sb[start])) start++;
            int end = sb.Length - 1;
            while (end >= start && char.IsWhiteSpace(sb[end])) end--;
            sb.Length = end + 1;
            sb.Remove(0, start);
            
            // Replace </br> with newlines if any
            sb = sb.Replace("<br/>", "\n").Replace("<br />", "\n");
            
            // Replace <see cref="..."/> with just the referenced type name
            var summary = Regex.Replace(sb.ToString(), @"<see cref=""([^""]+)""\s*/>", m => "<b>" + m.Groups[1].Value + "</b>");

            return summary;
        }
    }
}