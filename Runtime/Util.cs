#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text;
using static com.github.pandrabox.pandravase.runtime.Global;
using UnityEngine;
using System.Runtime.CompilerServices;
using System.IO;
using VRC.SDK3.Avatars.Components;
using System.Linq;
using UnityEditor;

namespace com.github.pandrabox.pandravase.runtime
{
    public static class Util
    {
        /////////////////////////DEBUG/////////////////////////
        /// <summary>
        /// DebugModeを設定する
        /// ※ using static com.github.pandrabox.pandravase.runtime.Global が必要です
        /// </summary>
        /// <param name="mode">設定するMode</param>
        public static void SetDebugMode(bool mode)
        {
            PDEBUGMODE = mode;
        }

        /// <summary>
        /// DebugMessageを表示する
        /// </summary>
        /// <param name="message">表示するMessage</param>
        /// <param name="debugOnly">DebugModeでのみ表示</param>
        /// <param name="level">ログレベル</param>
        /// <param name="callerMethodName">システムが使用</param>
        public static void DebugPrint(string message, bool debugOnly = true, LogType level = LogType.Warning, [CallerMemberName] string callerMethodName = "")
        {
            if (debugOnly && !PDEBUGMODE) return;

            var msg = $@"[PandraBox.{callerMethodName}]:{message}";

            if (level == LogType.Log)
            {
                Debug.Log(msg);
            }
            else if(level == LogType.Error)
            {
                Debug.LogError(msg);
            }
            else
            {
                Debug.LogWarning(msg);
            }
        }

        /// <summary>
        /// Debug用アセットの出力フォルダ
        /// </summary>
        private const string DEBUGOUTPFOLDER = "Assets/Pan/Debug/";
        public static string DebugOutpFolder
        {
            get
            {
                if (PDEBUGMODE)
                {
                    if (!Directory.Exists(DEBUGOUTPFOLDER)) Directory.CreateDirectory(DEBUGOUTPFOLDER);
                    return DEBUGOUTPFOLDER;
                }
                else
                {
                    DebugPrint("この機能はDEBUG専用ですが、非DEBUGMODEで実行されました。開発者に連絡して下さい。", false, LogType.Error);
                    return null;
                }
            }
        }

        public static string DebugOutp(UnityEngine.Object asset, string path="")
        {
            if (!PDEBUGMODE) return null;
            if (path == "") path = DebugOutpFolder;
            AssetDatabase.CreateAsset(asset, path);
            string assetPath = AssetDatabase.GetAssetPath(asset);
            // ファイルが存在するか確認
            if (File.Exists(assetPath))
            {
                DebugPrint($@"成功：ファイルを生成しました：{assetPath}");
                return assetPath;
            }
            else
            {
                DebugPrint($@"失敗：ファイルは生成できませんでした：{assetPath}");
                return null;
            }
        }

        /////////////////////////上方向 Component探索/////////////////////////
        /// <summary>
        /// 上方向にComponentを探索し、最初に見つかったものを返します。
        /// 下方向はUnity標準機能を使ってください　例：
        ///     currentComponent.GetComponentsInChildren<T>(true);
        ///     ParentTransform.GetComponentsInChildren<Transform>(true)?.Where(t => t.name == TargetName)?.ToArray();
        ///     GameObject.FindObjectsOfType<Transform>()?.Where(t => t.name == TargetName)?.ToArray();
        /// </summary>
        /// <typeparam name="T">探すComponent</typeparam>
        /// <param name="current">探索基準コンポーネント</param>
        /// <returns>見つかったComponentないしnull</returns>
        public static T FindComponentInParent<T>(GameObject current) where T : Component => FindComponentInParent<T>(current?.transform);
        public static T FindComponentInParent<T>(Transform current) where T : Component
        {
            Transform parent = current?.transform?.parent;
            while (parent != null)
            {
                T component = parent.GetComponent<T>();
                if (component != null) return component;
                parent = parent.parent;
            }
            return null;
        }
        public static VRCAvatarDescriptor GetAvatarDescriptor(GameObject current) => GetAvatarDescriptor(current?.transform);
        public static VRCAvatarDescriptor GetAvatarDescriptor(Transform current)
        {
            return FindComponentInParent<VRCAvatarDescriptor>(current);
        }
        public static bool IsInAvatar(GameObject current) => IsInAvatar(current?.transform);
        public static bool IsInAvatar(Transform current)
        {
            return GetAvatarDescriptor(current) != null;
        }

        /////////////////////////CreateObject/////////////////////////
        /// <summary>
        /// オブジェクトの生成
        /// </summary>
        /// <param name="parent">親</param>
        /// <param name="name">生成オブジェクト名</param>
        /// <param name="initialAction">生成時処理</param>
        /// <returns>生成したオブジェクト</returns>
        private enum CreateType
        {
            Normal,
            ReCreate,
            GetOrCreate
        }
        public static GameObject GetOrCGetOrateObject(GameObject paGetOrnt, string name, Action<GameObject> initialAction = null) => CreateObjectBase(paGetOrnt, name, CreateType.GetOrCreate, initialAction);
        public static GameObject GetOrCGetOrateObject(Transform paGetOrnt, string name, Action<GameObject> initialAction = null) => CreateObjectBase(paGetOrnt, name, CreateType.GetOrCreate, initialAction);
        public static GameObject ReCreateObject(GameObject parent, string name, Action<GameObject> initialAction = null) => CreateObjectBase(parent, name, CreateType.ReCreate, initialAction);
        public static GameObject ReCreateObject(Transform parent, string name, Action<GameObject> initialAction = null) => CreateObjectBase(parent, name, CreateType.ReCreate, initialAction);
        public static GameObject CreateObject(GameObject parent, string name, Action<GameObject> initialAction = null) => CreateObjectBase(parent, name, CreateType.Normal, initialAction);
        public static GameObject CreateObject(Transform parent, string name, Action<GameObject> initialAction = null) => CreateObjectBase(parent, name, CreateType.Normal, initialAction);
        private static GameObject CreateObjectBase(GameObject parent, string name, CreateType createType, Action<GameObject> initialAction = null) => CreateObjectBase(parent?.transform, name, createType, initialAction);
        private static GameObject CreateObjectBase(Transform parent, string name, CreateType createType, Action<GameObject> initialAction = null)
        {
            if (createType == CreateType.ReCreate) RemoveChildObject(parent.transform, name);
            if (createType == CreateType.GetOrCreate)
            {
                GameObject tmp = parent.transform?.Find(name)?.gameObject;
                if (tmp != null) return tmp;
            }

            GameObject res = new GameObject(name);
            res.transform.SetParent(parent.transform);
            initialAction?.Invoke(res);
            return res;
        }        
        /// <summary>
        /// オブジェクトの削除
        /// </summary>
        /// <param name="target"></param>
        static public void RemoveObject(Transform target) => RemoveObject(target?.gameObject);
        static public void RemoveObject(GameObject target) 
        {
            if (target != null)
            {
                GameObject.DestroyImmediate(target);
            }
        }
        static public void RemoveChildObject(Transform parent, string name) => RemoveObject(GetChildObject(parent, name));
        static public void RemoveChildObject(GameObject parent, string name) => RemoveChildObject(parent?.transform, name);
        static public GameObject GetChildObject(Transform parent, string name) => parent?.Find(name)?.gameObject;
        static public GameObject GetChildObject(GameObject parent, string name) => GetChildObject(parent?.transform, name);

        /// <summary>
        /// 文字列を無害化
        /// </summary>
        /// <param name="original">元の文字列</param>
        /// <returns>無害化された文字列</returns>
        public static string SanitizeStr(string original)
        {
            if (string.IsNullOrEmpty(original)) return "Untitled";
            char[] invalidChars = Path.GetInvalidFileNameChars();
            foreach (char c in invalidChars)
            {
                original = original.Replace(c.ToString(), string.Empty);
            }
            original = original.Trim();
            return string.IsNullOrEmpty(original) ? "Untitled" : original;
        }

        /// <summary>
        /// 対象をエディタオンリー・非表示に設定
        /// </summary>
        /// <param name="Target">対象</param>
        /// <param name="SW">設定値</param>
        public static void SetEditorOnly(Transform Target, bool SW) => SetEditorOnly(Target?.gameObject, SW);
        public static void SetEditorOnly(GameObject Target, bool SW)
        {
            if (SW)
            {
                Target.tag = "EditorOnly";
                Target.SetActive(false);
            }
            else
            {
                Target.tag = "Untagged";
                Target.SetActive(true);
            }
        }

        /// <summary>
        /// 対象のエディタオンリー状態をチェック
        /// </summary>
        /// <param name="target">対象</param>
        /// <returns>状態(trueならばエディタオンリー)</returns>
        public static bool IsEditorOnly(Transform target) => IsEditorOnly(target?.gameObject);
        public static bool IsEditorOnly(GameObject target)
        {
            return target.tag == "EditorOnly" && target.activeSelf == false;
        }

        /// <summary>
        /// 相対パスの取得
        /// </summary>
        /// <param name="parent">親</param>
        /// <param name="child">子</param>
        /// <returns>相対パス</returns>
        public static string GetRelativePath(Transform parent, GameObject child) => GetRelativePath(parent, child?.transform);
        public static string GetRelativePath(GameObject parent, Transform child) => GetRelativePath(parent?.transform, child);
        public static string GetRelativePath(GameObject parent, GameObject child) => GetRelativePath(parent?.transform, child?.transform);
        public static string GetRelativePath(Transform parent, Transform child)
        {
            if (parent == null || child == null) return null;
            if (!child.IsChildOf(parent)) return null;
            string path = "";
            Transform current = child;
            while (current != parent)
            {
                path = current.name + (path == "" ? "" : "/" ) + path;
                current = current.parent;
            }
            return path;
        }

        /// <summary>
        /// RendererにlilToonが使われているかどうか判定
        /// </summary>
        /// <param name="renderer"></param>
        /// <returns>使われていればtrue</returns>
        public static bool IsLil(Renderer renderer)
        {
            if (renderer == null || renderer.sharedMaterials == null) return false;
            foreach (var material in renderer.sharedMaterials)
            {
                if (material != null && material.shader != null && material.shader.name.Contains("lilToon")) return true;
            }
            return false;
        }

        /// <summary>
        /// 1D BlendTreeなどでわずかに違う値を使うときの値
        /// </summary>
        public static float DELTA = 0.00001f;

        /// <summary>
        /// ジェスチャ名
        /// </summary>
        public static string[] GestureNames = new string[] { "Neutral", "Fist", "HandOpen", "FingerPoint", "Victory", "RocknRoll", "HandGun", "Thumbsup" };

        /// <summary>
        /// ジェスチャ番号
        /// </summary>
        public enum Gesture
        {
            Neutral,
            Fist,
            HandOpen,
            FingerPoint,
            Victory,
            RocknRoll,
            HandGun,
            Thumbsup
        }
        public const int GESTURENUM = 8;

        /// <summary>
        /// 引数より大きい最小の2の累乗を求める
        /// </summary>
        /// <param name="paramNum">元</param>
        /// <returns>最小の2の累乗</returns>
        public static int NextPowerOfTwoExponent(int paramNum)
        {
            if (paramNum <= 0) return 0;
            paramNum--;
            int exponent = 0;
            while (paramNum > 0)
            {
                paramNum >>= 1;
                exponent++;
            }
            return exponent;
        }

        /// <summary>
        /// 1つ目の文字列が配列の中にあるものかどうか調べる
        /// </summary>
        /// <param name="firstString">1つ目</param>
        /// <param name="otherStrings">配列</param>
        /// <returns>あればtrue</returns>
        public static bool ContainsFirstString(string firstString, params string[] otherStrings)
        {
            return otherStrings.Any(s => s.Contains(firstString));
        }

        /// <summary>
        /// フォルダを作成する
        /// </summary>
        /// <param name="path">作成するパス</param>
        public static void CreateDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                AssetDatabase.Refresh();
            }
        }


    }
}
#endif