#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using static com.github.pandrabox.pandravase.runtime.Util;
using System.Runtime.CompilerServices;
using VRC.SDK3.Avatars.Components;
using System.Linq;

namespace com.github.pandrabox.pandravase.runtime
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