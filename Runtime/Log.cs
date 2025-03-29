#if UNITY_EDITOR
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using UnityEditor;
using UnityEngine;

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

        enum LogType
        {
            Info,
            Warning,
            Error,
            Exception,
            StartMethod,
            EndMethod
        }

        /// <summary>
        /// ログの初期化
        /// </summary>
        /// <param name="logfile">ログパス</param>
        /// <param name="appearPopupOnError">エラーのときポップアップ表示するかどうか</param>
        /// <param name="withClear">初期化時に既存のログを削除するかどうか</param>
        public void Initialize(string logfile, bool appearPopupOnError = false, bool withClear = false)
        {
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
        public void Exception(Exception ex, string message=null) => Write(LogType.Exception, message, ex);
        public void StartMethod(string message = null) => Write(LogType.StartMethod, message);
        public void EndMethod(string message = null) => Write(LogType.EndMethod, message);


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

            // ログメッセージを構築
            StringBuilder sb = new StringBuilder();

            // 日時
            sb.Append($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff},");

            // LogType
            sb.Append($"{lType},");

            // プロジェクト名
            sb.Append($"{projectName},");

            // クラス名
            sb.Append($"{className},");

            // メソッド名
            sb.Append($"{methodName},");

            // メッセージ
            sb.Append(message);

            // 例外情報があれば追加
            if (ex != null)
            {
                sb.AppendLine();
                sb.AppendLine("Exception Details:");
                sb.AppendLine($"Type: {ex.GetType().FullName}");
                sb.AppendLine($"Message: {ex.Message}");
                sb.AppendLine($"Source: {ex.Source}");
                sb.AppendLine($"StackTrace: {ex.StackTrace}");

                // InnerExceptionがあれば追加
                Exception innerEx = ex.InnerException;
                while (innerEx != null)
                {
                    sb.AppendLine();
                    sb.AppendLine($"Inner Exception Type: {innerEx.GetType().FullName}");
                    sb.AppendLine($"Inner Exception Message: {innerEx.Message}");
                    sb.AppendLine($"Inner Exception StackTrace: {innerEx.StackTrace}");
                    innerEx = innerEx.InnerException;
                }

                // 追加のデータがあれば表示
                if (ex.Data.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("Additional Data:");
                    foreach (var key in ex.Data.Keys)
                    {
                        sb.AppendLine($"{key}: {ex.Data[key]}");
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
                    UnityEngine.Debug.LogWarning(logMessage);
                    break;
                case LogType.Error:
                case LogType.Exception:
                    UnityEngine.Debug.LogError(logMessage);

                    // エラーやException発生時にポップアップ表示
                    if (_appearPopupOnError)
                    {
                        ShowErrorPopup(lType.ToString(), logMessage, ex);
                    }
                    break;
            }
        }

        // エラーポップアップを表示する
        private void ShowErrorPopup(string title, string message, Exception ex)
        {
            // ポップアップ用のメッセージを作成
            string popupMessage = message;

            // メッセージが長すぎる場合は短縮する
            const int maxLength = 1000;
            if (popupMessage.Length > maxLength)
            {
                popupMessage = popupMessage.Substring(0, maxLength) + "...(続きはログファイルを確認してください)";
            }

            // 例外情報を追加
            if (ex != null)
            {
                popupMessage += $"\n\nException: {ex.GetType().Name}\nMessage: {ex.Message}";
            }

            // エディタ上では、EditorUtilityを使用してポップアップを表示
            EditorApplication.delayCall += () =>
            {
                EditorUtility.DisplayDialog($"エラーが発生しました: {title}", popupMessage, "OK");
            };
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
    }
}
#endif