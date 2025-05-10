using com.github.pandrabox.pandravase.runtime;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;


namespace com.github.pandrabox.pandravase.editor
{
#if PANDRADBG
    using System.Linq;
    using UnityEditor;
    public static class PanDbgMenu
    {
        private const int ExpectedCommaCount = 5; // カンマの期待される数

        [MenuItem("PanDbg/**LocalizerCheck")]
        public static void LocalizerCheck()
        {
            string projectPath = Directory.GetParent(Application.dataPath).FullName;
            string[] files = Directory.GetFiles(projectPath, "PanLocalize.txt", SearchOption.AllDirectories);
            bool hasError = false;

            foreach (var file in files)
            {
                string[] lines = File.ReadAllLines(file);

                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];
                    int actualCommaCount = line.Count(c => c == ','); // 実際のカンマ数をカウント

                    // 空行またはカンマが期待される数と一致するかをチェック
                    if (!string.IsNullOrWhiteSpace(line) && actualCommaCount != ExpectedCommaCount)
                    {
                        Debug.LogError($"エラー: ファイル '{file}'   {i + 1} 行目のカンマ数が不正です (期待値: {ExpectedCommaCount}, 実際: {actualCommaCount})。内容: {line}");
                        hasError = true;
                    }
                }
            }

            if (!hasError)
            {
                Debug.Log("全てのPanLocalize.txtは適切です!!");
            }
        }
    }
#endif

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
            Log.I.Info("Project path: " + projectPath);
            string[] files = Directory.GetFiles(projectPath, "PanLocalize.txt", SearchOption.AllDirectories);
            if (files.Length == 0)
            {
                Log.I.Warning("Localization files not found.");
                return;
            }

            foreach (var file in files)
            {
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
                            Log.I.Warning("Duplicate key found in localization file: " + key);
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
                Log.I.Warning("Localization key not found: " + name);
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