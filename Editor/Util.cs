#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using System.Runtime.CompilerServices;
using System.IO;
using VRC.SDK3.Avatars.Components;
using System.Linq;
using UnityEditor;
using nadena.dev.ndmf;
using nadena.dev.modular_avatar.core;

namespace com.github.pandrabox.pandravase.editor
{
    public static class Util
    {
        /////////////////////////Global/////////////////////////
        public static bool PDEBUGMODE = false;
        public const float FPS = 60;
        public const string ONEPARAM = "__ModularAvatarInternal/One";
        public static string RootDir_VPM = "Packages/";
        public static string RootDir_Asset = "Assets/Pan/";
        public static string VPMDomainNameSuffix = "com.github.pandrabox.";
        public static string TmpFolder = $@"{RootDir_Asset}Temp/";
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        public const string DIRSEPARATOR = "\\";
#else
        public const string DIRSEPARATOR = "/";
#endif

        /////////////////////////DEBUG/////////////////////////
        /// <summary>
        /// DebugModeを設定する
        /// ※ using static com.github.pandrabox.pandravase.runtime.Global が必要です
        /// </summary>
        /// <param name="mode">設定するMode</param>
        public static void SetDebugMode(bool mode)
        {
            PDEBUGMODE = mode;
            DeleteFolder(TmpFolder);
            AssetDatabase.Refresh();
        }

        public static void LowLevelDebugPrint(string message, bool debugOnly = true, LogType level = LogType.Warning, string projectName = "Vase", [CallerMemberName] string callerMethodName = "", [CallerLineNumber] int callerLineNumber = 0)
        {
            if (debugOnly && !PDEBUGMODE) return;
            var msg = $@"[PandraBox.{projectName}.{callerMethodName}:{callerLineNumber}]:{message}";

            if (level == LogType.Log) Debug.Log(msg);
            else if (level == LogType.Error) Debug.LogError(msg);
            else Debug.LogWarning(msg);
        }

        /// <summary>
        /// シーンの最初に存在するアバターのDescriptor
        /// </summary>
        public static VRCAvatarDescriptor TopAvatar => GameObject.FindObjectOfType<VRCAvatarDescriptor>();

        /// <summary>
        /// 全アバターのDescriptor
        /// </summary>
        public static VRCAvatarDescriptor[] AllAvatar => GameObject.FindObjectsOfType<VRCAvatarDescriptor>();

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
            LowLevelDebugPrint($@"Componentの探索に失敗しました");
            return null;
        }
        public static GameObject GetAvatarRootGameObject(GameObject tgt) => GetAvatarDescriptor(tgt).gameObject;
        public static GameObject GetAvatarRootGameObject(Transform tgt) => GetAvatarDescriptor(tgt).gameObject;
        public static Transform GetAvatarRootTransform(GameObject tgt) => GetAvatarDescriptor(tgt).transform;
        public static Transform GetAvatarRootTransform(Transform tgt) => GetAvatarDescriptor(tgt).transform;
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


        /////////////////////////Pathの解決/////////////////////////
        /// <summary>
        /// アセットを作成する
        /// </summary>
        /// <param name="asset">作成するアセット</param>
        /// <param name="path">パス</param>
        /// <returns></returns>
        public static string OutpAsset(UnityEngine.Object asset, string path = "", bool debugOnly = false)
        {
            if (debugOnly && !PDEBUGMODE) return null;
            if (path == "" || path==null) path = TmpFolder;
            var UnityDirPath = CreateDir(path);
            if (UnityDirPath == null)
            {
                LowLevelDebugPrint("ディレクトリ[{path}]の生成に失敗したためアセットの生成に失敗しました。");
                return null;
            }

            var assetPath = AssetSavePath(asset, path);
            AssetDatabase.CreateAsset(asset, assetPath);

            return assetPath;
        }

        /// <summary>
        /// アセットを保存する適切なパスを返す（パスタイプは保証しない）
        /// </summary>
        /// <param name="asset"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string AssetSavePath(UnityEngine.Object asset, string path)
        {
            if (path.HasExtension()) return path;
            string fileName = SanitizeStr(asset.name);
            var extensionMap = new Dictionary<Type, string>() {
                { typeof(AnimationClip), ".anim" },
                { typeof(Texture2D), ".png" },
                { typeof(Material), ".mat" },
            };
            string extension = extensionMap.TryGetValue(asset.GetType(), out string e) ? e : ".asset";
            string guid = path==TmpFolder ? "_" + Guid.NewGuid().ToString() : "";
            return Path.Combine(path, fileName + guid + extension);
        }

        /// <summary>
        /// ディレクトリを作成する
        /// </summary>
        /// <param name="path">ディレクトリパス(絶対またはAssets/,Packages/から始まる相対)</param>
        /// <returns>成功すればUnityディレクトリパス、失敗したらnull</returns>
        public static string CreateDir(string path)
        {
            var absPath = Path.GetDirectoryName(GetAbsolutePath(path));
            Directory.CreateDirectory(absPath);
            if (Directory.Exists(absPath)) return GetUnityPath(absPath);
            LowLevelDebugPrint($@"ディレクトリ[{absPath}]の作成に失敗しました。");
            return null;
        }

        /// <summary>
        /// PathTypesの判定
        /// </summary>
        public enum PathTypes { Error, UnityAsset, AbsoluteAsset, UnityDir, AbsoluteDir };
        public static PathTypes PathType(this string path)
        {
            if (path.IsUnityPath()) return path.HasExtension() ? PathTypes.UnityAsset : PathTypes.UnityDir;
            if (path.IsAbsolutePath()) return path.HasExtension() ? PathTypes.AbsoluteAsset : PathTypes.AbsoluteDir;
            LowLevelDebugPrint($@"無効なパス[{path}]を判定しました。");
            return PathTypes.Error;
        }

        /// <summary>
        /// UnityPathかどうか判定する
        /// </summary>
        public static bool IsUnityPath(this string path)
        {
            var tmp = DirSeparatorNormalize(path);
            return tmp.StartsWith("Assets/") || tmp.StartsWith("Packages/") || tmp == "Assets" || tmp == "Packages";
        }

        /// <summary>
        /// AbsolutePathかどうか判定する
        /// </summary>
        public static bool IsAbsolutePath(this string path)
        {
            var tmp = DirSeparatorLocalize(path);
            return tmp.StartsWith(AbsoluteAssetsPath) || tmp.StartsWith(AbsolutePackagesPath);
        }

        /// <summary>
        /// pathが拡張子を持つかどうかの判定
        /// </summary>
        /// <param name="path"></param>
        /// <returns>持つならtrue</returns>
        public static bool HasExtension(this string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            var tmp = DirSeparatorNormalize(path);
            int lastSeparatorIndex = tmp.LastIndexOf('/');
            if (lastSeparatorIndex == -1 || lastSeparatorIndex == tmp.Length - 1) return false;
            return tmp.IndexOf('.', lastSeparatorIndex + 1) >= 0;
        }

        /// <summary>
        /// DirSeparatorを"/"にする
        /// </summary>
        public static string DirSeparatorNormalize(string path) => (path ?? "").Replace(DIRSEPARATOR, "/");

        /// <summary>
        /// "/"をDirSeparatorにする
        /// </summary>
        public static string DirSeparatorLocalize(string path) => (path ?? "").Replace("/", DIRSEPARATOR);

        /// <summary>
        /// パスのAbsolute(例：c:/test/Assets/aaa)とUnity(例：Assets/aaa)の変換
        /// </summary>
        public static string AbsoluteAssetsPath => DirSeparatorLocalize(Application.dataPath);
        public static string AbsolutePackagesPath => DirSeparatorLocalize(Path.Combine(new DirectoryInfo(AbsoluteAssetsPath).Parent.FullName, "Packages"));
        public static string GetAbsolutePath(string path)
        {
            var tmp = DirSeparatorLocalize(path);
            if (IsAbsolutePath(tmp)) return tmp;
            if (ReplaceSubstring(ref tmp, "Assets", AbsoluteAssetsPath)) return tmp;
            if (ReplaceSubstring(ref tmp, "Packages", AbsolutePackagesPath)) return tmp;
            LowLevelDebugPrint($@"無効なパス[{path}]の変換を試みました");
            return null;
        }
        public static string GetUnityPath(string path)
        {
            var tmp = DirSeparatorLocalize(path);
            if (IsUnityPath(tmp)) return DirSeparatorNormalize(tmp);
            if (ReplaceSubstring(ref tmp, AbsoluteAssetsPath, "Assets")) return DirSeparatorNormalize(tmp);
            if (ReplaceSubstring(ref tmp, AbsolutePackagesPath, "Packages")) return DirSeparatorNormalize(tmp);
            LowLevelDebugPrint($@"無効なパス[{path}]の変換を試みました");
            return null;
        }

        /// <summary>
        /// A(参照渡し)がBから始まっていればCに書き換える
        /// </summary>
        /// <param name="strA">文字列</param>
        /// <param name="strB">文字列</param>
        /// <param name="strC">文字列</param>
        /// <returns>書き換えが実行されたかどうか</returns>
        public static bool ReplaceSubstring(ref string strA, string strB, string strC)
        {
            if (strA.StartsWith(strB))
            {
                strA = strC + strA.Substring(strB.Length);
                return true;
            }
            return false;
        }

        /// <summary>
        /// フォルダを削除
        /// </summary>
        /// <param name="path"></param>
        public static void DeleteFolder(string path)
        {
            if (!path.Contains("Packages") && path.Contains("Assets"))
            {
                var tgt = GetUnityPath(path);
                FileUtil.DeleteFileOrDirectory(tgt);
            }
            else
            {
                var tgt = GetAbsolutePath(path);
                if (Directory.Exists(tgt)) Directory.Delete(tgt, true);
            }
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
            GetOrCreate, //あれば取得なければ作成。Componentについても同様。
            AddOrCreate //あれば取得なければ作成。Componentは既存があっても追加する
        }
        public static GameObject GetOrCreateObject(GameObject parent, string name, Action<GameObject> initialAction = null) => CreateObjectBase(parent, name, CreateType.GetOrCreate, initialAction);
        public static GameObject GetOrCreateObject(Transform parent, string name, Action<GameObject> initialAction = null) => CreateObjectBase(parent, name, CreateType.GetOrCreate, initialAction);
        public static GameObject ReCreateObject(GameObject parent, string name, Action<GameObject> initialAction = null) => CreateObjectBase(parent, name, CreateType.ReCreate, initialAction);
        public static GameObject ReCreateObject(Transform parent, string name, Action<GameObject> initialAction = null) => CreateObjectBase(parent, name, CreateType.ReCreate, initialAction);
        public static GameObject CreateObject(GameObject parent, string name, Action<GameObject> initialAction = null) => CreateObjectBase(parent, name, CreateType.Normal, initialAction);
        public static GameObject CreateObject(Transform parent, string name, Action<GameObject> initialAction = null) => CreateObjectBase(parent, name, CreateType.Normal, initialAction);
        private static GameObject CreateObjectBase(GameObject parent, string name, CreateType createType, Action<GameObject> initialAction = null) => CreateObjectBase(parent?.transform, name, createType, initialAction);
        private static GameObject CreateObjectBase(Transform parent, string name, CreateType createType, Action<GameObject> initialAction = null)
        {
            if (createType == CreateType.ReCreate) RemoveChildObject(parent.transform, name);
            if (createType == CreateType.GetOrCreate || createType == CreateType.AddOrCreate)
            {
                GameObject tmp = parent.transform?.Find(name)?.gameObject;
                if (tmp != null) return tmp;
            }

            GameObject res = new GameObject(name);
            if(parent != null) res.transform.SetParent(parent.transform);
            initialAction?.Invoke(res);
            return res;
        }

        /////////////////////////CreateComponentObject/////////////////////////
        /// <summary>
        /// コンポーネント付きのオブジェクトの生成
        /// </summary>
        /// <typeparam name="T">アタッチするコンポーネント</typeparam>
        /// <param name="parent">親</param>
        /// <param name="initialAction">生成時処理</param>
        /// <returns>アタッチしたコンポーネント</returns>
        public static T AddOrCreateComponentObject<T>(GameObject parent, string name, Action<T> initialAction = null) where T : Component => CreateComponentObjectBase<T>(parent, name, CreateType.AddOrCreate, initialAction);
        public static T AddOrCreateComponentObject<T>(Transform parent, string name, Action<T> initialAction = null) where T : Component => CreateComponentObjectBase<T>(parent, name, CreateType.AddOrCreate, initialAction);
        public static T GetOrCreateComponentObject<T>(GameObject parent, string name, Action<T> initialAction = null) where T : Component => CreateComponentObjectBase<T>(parent, name, CreateType.GetOrCreate, initialAction);
        public static T GetOrCreateComponentObject<T>(Transform parent, string name, Action<T> initialAction = null) where T : Component => CreateComponentObjectBase<T>(parent, name, CreateType.GetOrCreate, initialAction);
        public static T ReCreateComponentObject<T>(GameObject parent, string name, Action<T> initialAction = null) where T : Component => CreateComponentObjectBase<T>(parent, name, CreateType.ReCreate, initialAction);
        public static T ReCreateComponentObject<T>(Transform parent, string name, Action<T> initialAction = null) where T : Component => CreateComponentObjectBase<T>(parent, name, CreateType.ReCreate, initialAction);
        public static T CreateComponentObject<T>(GameObject parent, string name, Action<T> initialAction = null) where T : Component => CreateComponentObjectBase<T>(parent, name, CreateType.Normal, initialAction);
        public static T CreateComponentObject<T>(Transform parent, string name, Action<T> initialAction = null) where T : Component => CreateComponentObjectBase<T>(parent, name, CreateType.Normal, initialAction);
        private static T CreateComponentObjectBase<T>(GameObject parent, string name, CreateType createType, Action<T> initialAction = null) where T : Component => CreateComponentObjectBase<T>(parent?.transform, name, createType, initialAction);
        private static T CreateComponentObjectBase<T>(Transform parent, string name, CreateType createType, Action<T> initialAction = null) where T : Component
        {
            GameObject obj = CreateObjectBase(parent, name, createType);
            if (createType == CreateType.GetOrCreate)
            {
                T cmp = obj.GetComponent<T>();
                if (cmp != null) return cmp;
            }
            T component = obj.AddComponent<T>();
            initialAction?.Invoke(component);
            return component;
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
                path = current.name + (path == "" ? "" : "/") + path;
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

        /// <summary>
        /// 安全なAddObjectToAsset
        /// </summary>
        /// <param name="objToAdd"></param>
        /// <param name="targetAsset"></param>
        public static void AddObjectToAssetSafe(UnityEngine.Object objToAdd, UnityEngine.Object targetAsset)
        {
            // ターゲットアセットがnullの場合、エラー
            if (targetAsset == null)
            {
                Debug.LogError("Target asset is null.");
                return;
            }

            // 追加するオブジェクトがnullの場合、エラー
            if (objToAdd == null)
            {
                Debug.LogError("Object to add is null.");
                return;
            }

            // すでにアセットの一部である場合、処理を中断
            if (AssetDatabase.Contains(objToAdd))
            {
                Debug.Log("Object is already part of an asset.");
                return;
            }

            // ターゲットアセットの子オブジェクトをすべて取得
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            var path = AssetDatabase.GetAssetPath(targetAsset);
            UnityEngine.Object[] existingObjects = AssetDatabase.LoadAllAssetRepresentationsAtPath(path);

            // 既存のオブジェクトの中に同じものが存在するか確認
            bool alreadyExists = false;
            foreach (UnityEngine.Object existingObj in existingObjects)
            {
                if (existingObj == objToAdd) // オブジェクトの参照が一致するかどうかを比較
                {
                    alreadyExists = true;
                    break;
                }
            }

            // 同じオブジェクトが存在しない場合のみ追加
            if (!alreadyExists)
            {
                AssetDatabase.AddObjectToAsset(objToAdd, targetAsset);
                AssetDatabase.SaveAssets(); // 変更を保存
                AssetDatabase.Refresh();    // アセットデータベースを更新
                Debug.Log("Object added to asset.");
            }
            else
            {
                Debug.Log("Object already exists in asset.");
            }
        }


        /// <summary>
        /// GameObjectのBoundsを取得
        /// </summary>
        /// <param name="target">対象</param>
        /// <returns></returns>
        public static Bounds GetObjectBounds(GameObject target)
        {
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return new Bounds(target.transform.position, Vector3.zero);
            }
            Bounds bounds = new Bounds(renderers[0].bounds.center, renderers[0].bounds.size);
            foreach (Renderer renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
            }
            return bounds;
        }

        /// <summary>
        /// TransformをAvatarObjectReferenceに変換
        /// </summary>
        public static AvatarObjectReference GetObjectReference(Transform t) => GetObjectReference(t.gameObject);
        public static AvatarObjectReference GetObjectReference(GameObject go)
        {
            var r = new AvatarObjectReference(); 
            r.Set(go); 
            return r;
        }

        public static PandraProject VaseProject(BuildContext ctx) => VaseProject(ctx.AvatarDescriptor);
        public static PandraProject VaseProject(GameObject child) => VaseProject(GetAvatarDescriptor(child));
        public static PandraProject VaseProject(Transform child) => VaseProject(GetAvatarDescriptor(child));
        public static PandraProject VaseProject(VRCAvatarDescriptor desc)
        {
            return new PandraProject(desc, "PandraVase", ProjectTypes.VPM);
        }
    }
}
#endif