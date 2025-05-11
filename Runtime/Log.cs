#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
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
        private string logFile;
        private StreamWriter _sw;
        private float _logTimeoutMS = 500;
        private bool _isConnected = false;
        private object _lockObject = new object();
        private Timer _disconnectTimer;
        private bool _appearPopupOnError = false;
        private int _yetSelectLogFile = 5;
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
        /// <param name="logfile">ログパス</param>
        /// <param name="appearPopupOnError">エラーのときポップアップ表示するかどうか</param>
        /// <param name="withClear">初期化時に既存のログを削除するかどうか</param>
        public void Initialize(string logfile, bool appearPopupOnError = false, bool withClear = false)
        {
            SetKeyWord("Root");
            _yetSelectLogFile = 5;
            _appearPopupOnError = appearPopupOnError;
            string directory = Path.GetDirectoryName(logfile);
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
            if (!File.Exists(logfile)) File.Create(logfile).Dispose();
            logFile = logfile;

            // タイマーの初期化
            _disconnectTimer = new Timer(DisconnectTimerCallback, null, Timeout.Infinite, Timeout.Infinite);
            if (withClear) Clear();
            Write(LogType.Info, $@"ログを初期化しました{logFile}");
        }

        /// <summary>
        /// ログファイルの内容をクリアする
        /// </summary>
        private void Clear()
        {
            lock (_lockObject)
            {
                // 既存の接続を閉じる
                Disconnect();

                try
                {
                    // ファイルを空にする（上書きモードでStreamWriterを作成して即座に閉じる）
                    using (var sw = new StreamWriter(logFile, false))
                    {
                        sw.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff},Info,Log,Log,Clear,ログファイルをクリアしました");
                    }
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"ログファイルのクリアに失敗しました: {ex.Message}");
                }
            }
        }

        // タイマーコールバック
        private void DisconnectTimerCallback(object state)
        {
            Disconnect();
        }

        private void Connect()
        {
            logFile = logFile == null ? "" : logFile.Trim(trimChars: new char[] { ' ', '\r', '\n' });
            if (logFile == null || logFile == "")
            {
                if (_yetSelectLogFile-- > 0)
                {
#if PANDRADBG
                    Debug.Log("ログファイルが設定されていません。");
#endif
                }
                return;
            }
            Debug.Log($@"ログファイルを設定します: {logFile}");
            lock (_lockObject)
            {
                // すでに接続済みで書き込み可能な状態であれば何もしない
                if (_isConnected && _sw != null && _sw.BaseStream.CanWrite)
                    return;

                try
                {
                    // 前回のStreamWriterが残っていればクローズ
                    if (_sw != null)
                    {
                        try
                        {
                            _sw.Close();
                            _sw.Dispose();
                        }
                        catch (Exception)
                        {
                            // クローズ時のエラーは無視
                        }
                        _sw = null;
                    }

                    // 新規にStreamWriterを作成（追記モード）
                    _sw = new StreamWriter(logFile, true);
                    _sw.AutoFlush = true;
                    _isConnected = true;

                    // 既存のタイマーをキャンセルして新たにタイマーをセット
                    ResetDisconnectTimer();
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"ログファイル接続エラー: {ex.Message}");
                    _isConnected = false;
                    _sw = null;
                }
            }
        }

        private void Disconnect()
        {
            lock (_lockObject)
            {
                if (_sw == null)
                    return;
                try
                {
                    _sw.Flush();
                    _sw.Close();
                    _sw.Dispose();
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"ログファイル切断エラー: {ex.Message}");
                }
                finally
                {
                    _sw = null;
                    _isConnected = false;
                }
            }
        }

        // タイマーをリセットする
        private void ResetDisconnectTimer()
        {
            // 既存のタイマーをキャンセル
            _disconnectTimer.Change(Timeout.Infinite, Timeout.Infinite);

            // 新たにタイマーをセット
            _disconnectTimer.Change((int)_logTimeoutMS, Timeout.Infinite);
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
            Connect();

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

            // ファイルに書き込み
            try
            {
                if (_sw != null)
                {
                    _sw.WriteLine(logMessage);

                    // 書き込みがあるたびにタイマーをリセット
                    ResetDisconnectTimer();
                }
            }
            catch (Exception fileEx)
            {
                UnityEngine.Debug.LogError($"ログファイル書き込みエラー: {fileEx.Message}");
                _isConnected = false;
                _sw = null;
            }

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
                    if (_appearPopupOnError) ShowErrorPopup(lType.ToString(), logMessage);
                    break;
            }
        }

        // エラーポップアップを表示する
        private void ShowErrorPopup(string title, string message)
        {
            // ポップアップ用のメッセージを作成
            string popupMessage = message;

            // メッセージが長すぎる場合は短縮する
            const int maxLength = 1000;
            if (popupMessage.Length > maxLength)
            {
                popupMessage = popupMessage.Substring(0, maxLength) + "...(続きはログファイルを確認してください)";
            }

            EditorUtility.DisplayDialog($"エラーが発生しました: {title}", popupMessage, "OK");
        }

        private static string ConvertToUnityPath(string s)
        {
            s = s.Replace(Application.dataPath, "Assets");
            s = s.Replace(Path.Combine(new DirectoryInfo(Application.dataPath).Parent.FullName, "Packages"), "Packages");
            return s;
        }

        // デストラクタ - タイマーを確実に解放
        ~Log()
        {
            _disconnectTimer?.Dispose();
        }

        /// <summary>
        /// ログファイルを解析し、キーワードごとのWarning、Error、Exceptionの数を集計します
        /// </summary>
        /// <param name="logFilePath">解析するログファイルのパス（指定しない場合は現在のログファイル）</param>
        /// <returns>キーワードごとのエラー集計情報を含む辞書</returns>
        public static Dictionary<string, (int Warnings, int Errors, int Exceptions)> AnalyzeLog(string logFilePath = null)
        {
            if (string.IsNullOrEmpty(logFilePath) || !File.Exists(logFilePath))
            {
                Debug.LogError("ログファイルが存在しないため、解析できません");
                return new Dictionary<string, (int, int, int)>();
            }

            var result = new Dictionary<string, (int Warnings, int Errors, int Exceptions)>();

            try
            {
                string[] lines = File.ReadAllLines(logFilePath);

                foreach (string line in lines)
                {
                    // カンマ区切りのログ形式を解析
                    string[] parts = line.Split(',');
                    if (parts.Length < 4)
                        continue;

                    // フォーマット: 日時,LogType(4文字),キーワード,プロジェクト名,クラス名,メソッド名,メッセージ
                    string logTypeStr = parts[1].Trim();
                    string keyword = parts[2].Trim();

                    // LogTypeを判断
                    bool isWarning = logTypeStr.StartsWith("Warn");
                    bool isError = logTypeStr.StartsWith("Erro");
                    bool isException = logTypeStr.StartsWith("Exce");

                    if (!isWarning && !isError && !isException)
                        continue;

                    if (!result.ContainsKey(keyword))
                    {
                        result[keyword] = (0, 0, 0);
                    }

                    var currentCounts = result[keyword];

                    if (isWarning)
                    {
                        result[keyword] = (currentCounts.Warnings + 1, currentCounts.Errors, currentCounts.Exceptions);
                    }
                    else if (isError)
                    {
                        result[keyword] = (currentCounts.Warnings, currentCounts.Errors + 1, currentCounts.Exceptions);
                    }
                    else if (isException)
                    {
                        result[keyword] = (currentCounts.Warnings, currentCounts.Errors, currentCounts.Exceptions + 1);
                    }
                }

                // エラーがないキーワードを除外
                result = result.Where(kv => kv.Value.Warnings > 0 || kv.Value.Errors > 0 || kv.Value.Exceptions > 0)
                               .ToDictionary(kv => kv.Key, kv => kv.Value);

                return result;
            }
            catch (Exception ex)
            {
                Debug.LogError($"ログの解析中にエラーが発生しました: {ex.Message}");
                return new Dictionary<string, (int, int, int)>();
            }
        }

    }
}
#endif
