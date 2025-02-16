#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using static com.github.pandrabox.pandravase.editor.Util;
using System.Runtime.CompilerServices;
using VRC.SDK3.Avatars.Components;
using System.Linq;
using System.Text.RegularExpressions;
using nadena.dev.modular_avatar.core;
using com.github.pandrabox.pandravase.runtime;

namespace com.github.pandrabox.pandravase.editor
{
    public enum ProjectTypes { VPM, Asset };
    public class PandraProject
    {
        private VRCAvatarDescriptor _descriptor;
        public string ProjectName;
        public ProjectTypes ProjectType;
        public string ProjectFolder;
        public VRCAvatarDescriptor Descriptor { get { if (_descriptor == null) DebugPrint("未定義のDescriptorが呼び出されました", false, LogType.Error); return _descriptor; } }
        public GameObject RootObject => Descriptor.gameObject;
        public Transform RootTransform => Descriptor.transform;
        public string ResFolder => $@"{ProjectFolder}Res/";
        public string ImgFolder => $@"{DataFolder}Img/";
        public string AssetsFolder => $@"{ProjectFolder}Assets/";
        public string DataFolder => ProjectType == ProjectTypes.VPM ? AssetsFolder : ResFolder;
        public string AnimFolder => $@"{AssetsFolder}Anim/";
        public string EditorFolder => $@"{ProjectFolder}Editor/";
        public string RuntimeFolder => $@"{ProjectFolder}Runtime/";
        public string DebugFolder => $@"{ProjectFolder}Debug/";
        public string WorkFolder => $@"{ProjectFolder}Work/";
        public string VaseFolder => "Packages/com.github.pandrabox.pandravase/";
        public string VaseDebugFolder => $@"{VaseFolder}Debug/";
        public string Suffix => EnableSuffix ? $@"Pan/{ProjectName}/" : "";
        public string TmpFolder => Util.TmpFolder;
        public string PrjRootObjName => $@"{ProjectName}_PrjRootObj";
        public VRCAvatarDescriptor.CustomAnimLayer[] BaseAnimationLayers => Descriptor.baseAnimationLayers;
        public int PlayableIndex (VRCAvatarDescriptor.AnimLayerType type) => Array.IndexOf(BaseAnimationLayers, BaseAnimationLayers.FirstOrDefault(l => l.type == type));
        public GameObject PrjRootObj => Util.GetOrCreateObject(RootTransform, PrjRootObjName);
        public bool IsVPM => ProjectType == ProjectTypes.VPM;
        public string PackageJsonPath => IsVPM ? $@"{ProjectFolder}package.json" : null;
        public string VPMVersion => IsVPM ? GetVPMVer() : null;
        public bool EnableSuffix = false;
        public Animator Animator => RootObject?.GetComponent<Animator>();
        public Transform HumanoidTransform(HumanBodyBones b) => Animator?.GetBoneTransform(b);
        public GameObject HumanoidGameObject(HumanBodyBones b) => HumanoidTransform(b)?.gameObject;
        public AvatarObjectReference HumanoidObjectReference(HumanBodyBones b) => GetObjectReference(HumanoidGameObject(b));
        public Transform ArmatureTransform => HumanoidTransform(HumanBodyBones.Hips).parent;
        public GameObject ArmatureGameObject => ArmatureTransform.gameObject;
        public AvatarObjectReference ArmatureObjectReference => GetObjectReference(ArmatureGameObject);




        /// <summary>
        /// 1つのAvatarを編集するためのProjectを統括するクラス。Project共通で使うsuffix,ProjectNameなどの管理を提供する
        /// </summary>
        /// <param name="descriptor">Descriptor</param>
        /// <param name="suffix">変数名・レイヤ名等の前置詞</param>
        /// <param name="workFolder">Anim等を読み込む際使用する基本フォルダ</param>
        public PandraProject() => Init(null, "Generic", ProjectTypes.Asset);
        public PandraProject(string projectName, ProjectTypes projectTypes = ProjectTypes.Asset) => Init(null, projectName, projectTypes);
        public PandraProject(Transform somethingAvatarParts, string projectName, ProjectTypes projectType) => Init(GetAvatarDescriptor(somethingAvatarParts), projectName, projectType);
        public PandraProject(GameObject somethingAvatarParts, string projectName, ProjectTypes projectType) => Init(GetAvatarDescriptor(somethingAvatarParts), projectName, projectType);
        public PandraProject(VRCAvatarDescriptor descriptor, string projectName, ProjectTypes projectType) => Init(descriptor, projectName, projectType);
        protected void Init(VRCAvatarDescriptor descriptor, string projectName, ProjectTypes projectType)
        {
            _descriptor = descriptor;
            ProjectName = projectName;
            ProjectType = projectType;
            if (ProjectType == ProjectTypes.Asset)
            {
                ProjectFolder = $@"{RootDir_Asset}{projectName}/"; //memo:PanはRootDirに含まれています
            }
            else
            {
                ProjectFolder = $@"{RootDir_VPM}{VPMDomainNameSuffix}{ProjectName.ToLower()}/";
            }
        }

        /// <summary>
        /// EnableSuffixを切り替える
        /// </summary>
        public PandraProject SetSuffixMode(bool mode)
        {
            EnableSuffix = mode;
            return this;
        }

        /// <summary>
        /// デバッグを開始し、PrjRootObjを削除する
        /// </summary>
        /// <param name="mode"></param>
        public void SetDebugMode(bool mode)
        {
            Util.SetDebugMode(mode);
            DebugPrint("DebugModeが開始されました。PrjRootObjの削除・Debugフォルダ・Tmpフォルダの削除を行います。");
            GameObject.DestroyImmediate(PrjRootObj);
            DeleteFolder(DebugFolder);
            DeleteFolder(TmpFolder);
            DeleteFolder(VaseDebugFolder);
        }

        public string GetParameterName(string parameterName)
        {
            if (string.IsNullOrEmpty(parameterName)) return null;//            { DebugPrint("nullが入力されました"); return null; };
            string res;
            if (ContainsFirstString(parameterName, new string[] { "ONEf", "PBTB_CONST_1", ONEPARAM })) res = ONEPARAM;
            else if (ContainsFirstString(parameterName, new string[] { "GestureRight", "GestureLeft", "GestureRightWeight", "GestureLeftWeight", "IsLocal", "InStation", "Seated", "VRMode" })) res = parameterName;
            else if (parameterName.StartsWith("Env/")) res = parameterName;
            else if (parameterName.StartsWith("Pan/")) res = parameterName;
            else if (ContainsFirstString(parameterName, new string[] { "Time", "ExLoaded", "IsMMD", "IsNotMMD", "IsLocal", "FrameTime" })) res = $@"Env/{parameterName}";
            else if (parameterName.Length > 0 && parameterName[0] == '$') res = parameterName.Substring(1);
            else res = $@"{Suffix}{parameterName}";
            return res;
        }

        private string NormalizedMotionPath(string motionPath)
        {
            if (File.Exists(motionPath)) return motionPath;
            motionPath = motionPath.Trim().Replace("\\", "/");
            if (File.Exists(motionPath)) return motionPath;
            if (!motionPath.Contains("/")) motionPath = $@"{AnimFolder}{motionPath}";
            if (File.Exists(motionPath)) return motionPath;
            if (!motionPath.Contains(".")) motionPath = $@"{motionPath}.anim";
            if (File.Exists(motionPath)) return motionPath;
            DebugPrint($@"Motion「{motionPath}」が見つかりませんでした");
            return null;
        }

        public Motion LoadMotion(string motionPath)
        {
            return AssetDatabase.LoadAssetAtPath<Motion>(NormalizedMotionPath(motionPath));
        }


        /// <summary>
        /// VPMのバージョンを取得する
        /// </summary>
        /// <returns></returns>
        public string GetVPMVer()
        {
            string packageJsonPath = Path.Combine(ProjectFolder, "package.json");
            if (!File.Exists(packageJsonPath))
            {
                DebugPrint($@"{packageJsonPath}が見つかりませんでした");
                return null;
            }
            string jsonContent = File.ReadAllText(packageJsonPath);
            if (jsonContent == null)
            {
                DebugPrint("package.jsonの読み込みに失敗しました");
                return null;
            }
            string versionPattern = @"""version"":\s*""([0-9]+\.[0-9]+\.[0-9]+)""";
            Match match = Regex.Match(jsonContent, versionPattern);
            if (match.Success)
            {
                string version = match.Groups[1].Value;
                return version;
            }
            DebugPrint("バージョンの正規表現がマッチしませんでした");
            return null;
        }


        /// <summary>
        /// nBitSyncを作成する
        /// </summary>
        /// <param name="txName">Sync Parameter Name</param>
        /// <param name="Bit">Sync Bit</param>
        /// <param name="syncMode">Mode</param>
        /// <param name="hostDecode">Hostででコードするかどうか</param>
        /// <param name="min">Mode=Custom時の最小</param>
        /// <param name="max">Mode=Custom時の最大</param>
        /// <param name="SyncSwitch">指定してあるならそのパラメータがONの時だけ同期する</param>
        public PVnBitSync.PVnBitSyncData VirtualSync(string txName, int Bit, PVnBitSync.nBitSyncMode syncMode, bool hostDecode = false, float min = 0.0f, float max = 1.0f, bool SyncSwitch = false)
        {
            var s = CreateComponentObject<PVnBitSync>($@"VirtualSync{txName}");
            return s.Set(txName, Bit, syncMode, hostDecode, min, max, SyncSwitch);
        }

        /////////////////////////DEBUG/////////////////////////
        /// <summary>
        /// DebugMessageを表示する
        /// </summary>
        /// <param name="message">表示するMessage</param>
        /// <param name="debugOnly">DebugModeでのみ表示</param>
        /// <param name="level">ログレベル</param>
        /// <param name="callerMethodName">システムが使用</param>
        public void DebugPrint(string message, bool debugOnly = true, LogType level = LogType.Warning, [CallerMemberName] string callerMethodName = "", [CallerLineNumber] int callerLineNumber = 0)
        {
            LowLevelDebugPrint(message, debugOnly, level, ProjectName, callerMethodName);
        }

        /// <summary>
        /// アセットをデバッグ出力
        /// </summary>
        /// <param name="asset">アセット</param>
        /// <param name="path">パス</param>
        /// <returns>成功したらそのパス、失敗したらnull</returns>
        public string DebugOutp(UnityEngine.Object asset, string path = "")
        {
            if (!PDEBUGMODE) return null;
            return OutpAsset(asset, path, true);
        }

        /////////////////////////CreateObject/////////////////////////
        /// <summary>
        /// PrjRootObjの直下にオブジェクトを生成
        /// </summary>
        /// <param name="name">生成オブジェクト名</param>
        /// <param name="initialAction">生成時処理</param>
        /// <returns>生成したオブジェクト</returns>
        public GameObject GetOrCreateObject(string name, Action<GameObject> initialAction = null) => Util.GetOrCreateObject(PrjRootObj, name, initialAction);
        public GameObject ReCreateObject(string name, Action<GameObject> initialAction = null) => Util.ReCreateObject(PrjRootObj, name, initialAction);
        public GameObject CreateObject(string name, Action<GameObject> initialAction = null) => Util.CreateObject(PrjRootObj, name, initialAction);

        /////////////////////////CreateComponentObject/////////////////////////
        /// <summary>
        /// PrjRootObjの直下にComponentオブジェクトを生成
        /// </summary>
        /// <param name="name">生成オブジェクト名</param>
        /// <param name="initialAction">生成時処理</param>
        /// <returns>生成したオブジェクト</returns>
        public T AddOrCreateComponentObject<T>(string name, Action<T> initialAction = null) where T : Component => Util.AddOrCreateComponentObject<T>(PrjRootObj, name, initialAction);
        public T GetOrCreateComponentObject<T>(string name, Action<T> initialAction = null) where T : Component => Util.GetOrCreateComponentObject<T>(PrjRootObj, name, initialAction);
        public T ReCreateComponentObject<T>(string name, Action<T> initialAction = null) where T : Component => Util.ReCreateComponentObject<T>(PrjRootObj, name, initialAction);
        public T CreateComponentObject<T>(string name, Action<T> initialAction = null) where T : Component => Util.CreateComponentObject<T>(PrjRootObj, name, initialAction);
    }
}
#endif