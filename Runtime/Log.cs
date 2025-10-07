#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace com.github.pandrabox.pandravase.runtime
{
    public class Log
    {
        public static Log I => instance;
        private static Log instance = new Log();
        private Log() { }
        private List<string> _keywords = new List<string>();

        enum LogType
        {
            Info,
            Warning,
            Error,
            Exception,
            StartMethod,
            EndMethod,
            Old
        }

        public void SetKeyWord(string keyWord)
        {
            _keywords.Add(keyWord);
        }
        public void ReleaseKeyWord()
        {
            if (_keywords.Count > 0)
            {
                _keywords.RemoveAt(_keywords.Count - 1);
            }
        }
        public string CurrentKeyWord => _keywords.Count > 0 ? _keywords[_keywords.Count - 1] : "";

        /// <summary>
        /// ログの初期化
        /// </summary>
        public void Initialize()
        {
            SetKeyWord("Root");
            Write(LogType.Info, "ログを初期化しました");
        }

        public void Initialize(string a, bool b = true, bool c = true)
        {
            Initialize(); //互換性維持
        }

        public void Info(string message) => Write(LogType.Info, message);
        public void Warning(string message) => Write(LogType.Warning, message);
        public void Error(string message) => Write(LogType.Error, message);
        public void Exception(Exception ex, string message = null) => Write(LogType.Exception, message, ex);
        public void StartMethod(string message = null) => Write(LogType.StartMethod, message);
        public void EndMethod(string message = null) => Write(LogType.EndMethod, message);
        public void Old(string message = null) => Write(LogType.Old, message);

        private void Write(LogType lType, string message, Exception ex = null)
        {
            // 呼び出し元の情報を取得
            StackFrame frame = new StackFrame(2, true);
            MethodBase method = frame.GetMethod();
            Type declaringType = method?.DeclaringType;
            string className = declaringType?.Name ?? "Unknown";
            string methodName = method?.Name ?? "Unknown";
            string projectName = declaringType?.Assembly?.GetName()?.Name ?? "Unknown";
            string fileName = frame.GetFileName() ?? "Unknown";
            int lineNumber = frame.GetFileLineNumber();
            string fileInfo = $"{fileName}({lineNumber})";

            // ログメッセージを構築
            StringBuilder sb = new StringBuilder();

            // 日時
            sb.Append($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff},");

            // LogType
            sb.Append($"{lType.ToString().Substring(0, 4)},");

            sb.Append($@"{CurrentKeyWord},");

            // プロジェクト名
            sb.Append($"{projectName},");

            // クラス名
            sb.Append($"{className},");

            // メソッド名
            sb.Append($"{methodName},");

            // メッセージ
            sb.Append(message);

            // 呼び出し元のファイル情報を追加（改行なし）
            sb.AppendLine();
            sb.Append($"Source Location: {fileInfo}");

            if (lType == LogType.Old || lType == LogType.Warning || lType == LogType.Error || lType == LogType.Exception)
            {
                // すべてのWarning, Error, Exception, Old タイプのログにスタックトレースを追加（空行なし）
                sb.AppendLine();
                sb.Append("StackTrace:");

                // 現在のスタックトレースを取得（詳細情報も含む）
                StackTrace stackTrace = new StackTrace(true);

                // スタックフレームを1つずつ処理して詳細情報を追加
                for (int i = 0; i < stackTrace.FrameCount; i++)
                {
                    StackFrame sf = stackTrace.GetFrame(i);
                    MethodBase mb = sf.GetMethod();

                    if (mb == null) continue;

                    Type declType = mb.DeclaringType;
                    string declTypeName = declType != null ? declType.FullName : "Unknown";
                    string methodSig = mb.ToString();
                    string srcFile = sf.GetFileName();
                    int srcLine = sf.GetFileLineNumber();
                    int srcColumn = sf.GetFileColumnNumber();

                    // 改行してからフレーム情報を追加
                    sb.AppendLine();
                    string frameDetail = $"   at {declTypeName}.{mb.Name}";

                    // パラメータ情報を追加
                    ParameterInfo[] parameters = mb.GetParameters();
                    if (parameters.Length > 0)
                    {
                        frameDetail += "(";
                        for (int p = 0; p < parameters.Length; p++)
                        {
                            if (p > 0) frameDetail += ", ";
                            frameDetail += $"{parameters[p].ParameterType.Name} {parameters[p].Name}";
                        }
                        frameDetail += ")";
                    }

                    // ソースファイル情報があれば追加
                    if (!string.IsNullOrEmpty(srcFile))
                    {
                        frameDetail += $" in {srcFile}:line {srcLine}";
                        if (srcColumn > 0)
                        {
                            frameDetail += $", column {srcColumn}";
                        }
                    }

                    sb.Append(frameDetail);
                }

                // 例外情報があれば追加（既存のコードを維持、余分な改行削除）
                if (ex != null)
                {
                    sb.AppendLine();
                    sb.Append("Exception Details:");
                    sb.AppendLine();
                    sb.Append($"Type: {ex.GetType().FullName}");
                    sb.AppendLine();
                    sb.Append($"Message: {ex.Message}");
                    sb.AppendLine();
                    sb.Append($"Source: {ex.Source}");
                    sb.AppendLine();
                    sb.Append($"StackTrace: {ex.StackTrace}");

                    // InnerExceptionがあれば追加
                    Exception innerEx = ex.InnerException;
                    while (innerEx != null)
                    {
                        sb.AppendLine();
                        sb.Append($"Inner Exception Type: {innerEx.GetType().FullName}");
                        sb.AppendLine();
                        sb.Append($"Inner Exception Message: {innerEx.Message}");
                        sb.AppendLine();
                        sb.Append($"Inner Exception StackTrace: {innerEx.StackTrace}");
                        innerEx = innerEx.InnerException;
                    }

                    // 追加のデータがあれば表示
                    if (ex.Data.Count > 0)
                    {
                        sb.AppendLine();
                        sb.Append("Additional Data:");
                        foreach (var key in ex.Data.Keys)
                        {
                            sb.AppendLine();
                            sb.Append($"{key}: {ex.Data[key]}");
                        }
                    }
                }
            }

            string logMessage = ConvertToUnityPath(sb.ToString());

            // LogTypeに応じてUnityのログ機能を使い分ける
            switch (lType)
            {
                case LogType.Info:
                case LogType.StartMethod:
                case LogType.EndMethod:
                    UnityEngine.Debug.Log(logMessage);
                    break;
                case LogType.Warning:
                case LogType.Old:
                    UnityEngine.Debug.LogWarning(logMessage);
                    break;
                case LogType.Error:
                case LogType.Exception:
                    UnityEngine.Debug.LogError(logMessage);
                    ShowErrorPopup(lType.ToString(), logMessage);
                    break;
            }
        }

        // エラーポップアップを表示する
        private void ShowErrorPopup(string title, string message)
        {
#if PANDRADBG
            // ポップアップ用のメッセージを作成
            string popupMessage = message;

            // メッセージが長すぎる場合は短縮する
            const int maxLength = 1000;
            if (popupMessage.Length > maxLength)
            {
                popupMessage = popupMessage.Substring(0, maxLength) + "...(続きはログファイルを確認してください)";
            }

            EditorUtility.DisplayDialog($"エラーが発生しました: {title}", popupMessage, "OK");
#endif
        }

        private static string ConvertToUnityPath(string s)
        {
            s = s.Replace(Application.dataPath, "Assets");
            s = s.Replace(Path.Combine(new DirectoryInfo(Application.dataPath).Parent.FullName, "Packages"), "Packages");
            return s;
        }

    }
}
#endif
