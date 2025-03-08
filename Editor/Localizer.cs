using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using static com.github.pandrabox.pandravase.editor.Util;


namespace com.github.pandrabox.pandravase.editor
{
    public static class Localizer
    {
        public static string LocalizerLanguage => language;
        private static string language;
        private static Dictionary<string, string> localizationDictionary = new Dictionary<string, string>();

        private static Dictionary<SystemLanguage, string> languageMap = new Dictionary<SystemLanguage, string>()
                                    {
                                        { SystemLanguage.English, "en" },
                                        { SystemLanguage.Japanese, "ja" },
                                        { SystemLanguage.Korean, "ko" },
                                        { SystemLanguage.ChineseSimplified, "zh-CN" },
                                        { SystemLanguage.ChineseTraditional, "zh-TW" }
                                    };

        public static void SetLanguage()
        {
            SystemLanguage systemLanguage = Application.systemLanguage;
            string l = null;
            if (languageMap.ContainsKey(systemLanguage))
            {
                l = languageMap[systemLanguage];
            }
            else
            {
                l = "en";
            }
            ReLoadText(l);
        }

        public static string GetDefaultLanguage()
        {
            SystemLanguage systemLanguage = Application.systemLanguage;
            if (languageMap.ContainsKey(systemLanguage))
            {
                return languageMap[systemLanguage];
            }
            else
            {
                return "en";
            }
        }

        public static void SetLanguage(string l)
        {
            ReLoadText(l);
        }

        private static void ReLoadText(string l)
        {
            if (language == l) return;
            language = l;
            ClearCache();
            LoadText();
        }

        private static void LoadText()
        {
            if (localizationDictionary.Count > 0) return;
            if (LocalizerLanguage == null) SetLanguage();
            localizationDictionary = new Dictionary<string, string>();
            string projectPath = Directory.GetParent(Application.dataPath).FullName;
            //LowLevelDebugPrint("Project path: " + projectPath);
            string[] files = Directory.GetFiles(projectPath, "PanLocalize.txt", SearchOption.AllDirectories);
            if (files.Length == 0)
            {
                LowLevelExeption("Localization files not found.");
                return;
            }

            //LowLevelDebugPrint("Found localization files:");
            foreach (var file in files)
            {
                //LowLevelDebugPrint(file);
                string[] lines = File.ReadAllLines(file);
                string[] headers = lines[0].Split(',');
                int langIndex = Array.IndexOf(headers, LocalizerLanguage);
                if (langIndex == -1) langIndex = Array.IndexOf(headers, "en");
                for (int i = 1; i < lines.Length; i++)
                {
                    string[] columns = lines[i].Split(',');
                    if (columns.Length > langIndex)
                    {
                        string key = columns[0];
                        string value = columns[langIndex];
                        if (localizationDictionary.ContainsKey(key))
                        {
                            LowLevelDebugPrint("Duplicate key found in localization file: " + key);
                        }
                        localizationDictionary[key] = value;
                    }
                }
            }
        }

        public static string L(string name)
        {
            LoadText();
            if (localizationDictionary.TryGetValue(name, out string res))
            {
                res = res.Replace(@"\n", "\n");
                res = res.Replace(@"\r", "\r");
                return res;
            }
            else
            {
                LowLevelDebugPrint("Localization key not found: " + name);
                return name;
            }
        }
        public static string LL(this string name)
        {
            return L(name);
        }

        private static void ClearCache()
        {
            localizationDictionary.Clear();
        }
    }
}