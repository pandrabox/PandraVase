#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using static com.github.pandrabox.pandravase.editor.Util;

namespace com.github.pandrabox.pandravase.editor
{
    /// <summary>
    /// Asset直下に一時フォルダを作る。usingで呼び出し、終了時に自動で削除する。
    /// </summary>
    public class TemporaryAssetFolder : IDisposable
    {
        private string _folderPath;
        private string _tmpParent;
        public string FolderPath => _folderPath;

        public TemporaryAssetFolder()
        {
            string assetsPath = Application.dataPath;
            _tmpParent = Path.Combine(assetsPath, "Temp");
            _folderPath = Path.Combine(_tmpParent, Guid.NewGuid().ToString());
            Directory.CreateDirectory(_folderPath);
        }

        public void Dispose()
        {
            DeleteFolder(_folderPath);
            DeleteFolder(_tmpParent);
        }
    }
}
#endif