using com.github.pandrabox.pandravase.runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using static com.github.pandrabox.pandravase.editor.Util;
using Debug = UnityEngine.Debug;


namespace com.github.pandrabox.pandravase.editor
{
    public static class PanLog
    {
        private static string _logTextPath = "Packages/com.github.pandrabox.pandravase/Log/Log.txt";
        public static bool UnitTestMode = false;
        public static string CurrentClassName = "";

        public static void SetLogPath(string path)
        {
            _logTextPath = path;
        }

        private static void DirectoryCheck()
        {
            string directoryPath = Path.GetDirectoryName(_logTextPath);
            if (!Directory.Exists(directoryPath)) Directory.CreateDirectory(directoryPath);
        }

        public static void Write(string msg, string logPath = null, bool detail = false)
        {
            string path = logPath ?? _logTextPath;
            try
            {
                DirectoryCheck();
                using (var writer = new StreamWriter(path, true))
                {
                    if(detail)
                    {
                        var stackTrace = new StackTrace(true);
                        var stackFrames = stackTrace.GetFrames();
                        var stackInfo = string.Join(" -> ", stackFrames.Select(frame => $"{frame.GetMethod().DeclaringType.FullName}.{frame.GetMethod().Name} (at {frame.GetFileName()}:{frame.GetFileLineNumber()})"));
                        var message = $"{DateTime.Now.ToString("HH:mm:ss")}, {CurrentClassName}, {msg}, StackTrace: {stackInfo}";
                    }
                    writer.WriteLine(msg);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to write log file: {ex.Message}");
            }
        }

        public static void Clear(string logPath = null)
        {
            string path = logPath ?? _logTextPath;
            try
            {
                if (File.Exists(path)) File.Delete(path);
                ClearConsole();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to clear log file: {ex.Message}");
            }
        }
    }
}