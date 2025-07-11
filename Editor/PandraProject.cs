﻿#if UNITY_EDITOR
using com.github.pandrabox.pandravase.runtime;
using nadena.dev.modular_avatar.core;
using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using static com.github.pandrabox.pandravase.editor.Util;

namespace com.github.pandrabox.pandravase.editor
{
    public enum ProjectTypes { VPM, Asset };
    public class PandraProject
    {
        private VRCAvatarDescriptor _descriptor;
        public string ProjectName;
        public ProjectTypes ProjectType;
        public string ProjectFolder;
        public VRCAvatarDescriptor Descriptor { get { if (_descriptor == null) Log.I.Error("未定義のDescriptorが呼び出されました"); return _descriptor; } }
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
        public VRCAvatarDescriptor.CustomAnimLayer[] PlayableLayers => BaseAnimationLayers;
        //public VRCAvatarDescriptor.CustomAnimLayer FXLayer => BaseAnimationLayers[PlayableIndex(VRCAvatarDescriptor.AnimLayerType.FX)];
        public int PlayableIndex(VRCAvatarDescriptor.AnimLayerType type) => Array.IndexOf(BaseAnimationLayers, BaseAnimationLayers.FirstOrDefault(l => l.type == type));
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
        public string FrameCount => "Vase/FrameCount";
        public string IsFxLayer1Off => "Vase/IsFxLayer1Off";
        public string IsDanceHost => "Vase/IsDance/Host";
        public string DanceDetectMode => "Vase/DanceDetectMode";
        public string IsDance => "Vase/IsDance";
        public string IsNotDance => "Vase/IsNotDance";
        public string OnDanceFxEnable => "Vase/OnDanceFxEnable";

        public AnimatorController FXAnimatorController
        {
            get => GetPlayableAnimatorController(VRCAvatarDescriptor.AnimLayerType.FX);
            set => SetPlayableAnimatorController(VRCAvatarDescriptor.AnimLayerType.FX, value);
        }
        public AnimatorController GestureAnimatorController
        {
            get => GetPlayableAnimatorController(VRCAvatarDescriptor.AnimLayerType.Gesture);
            set => SetPlayableAnimatorController(VRCAvatarDescriptor.AnimLayerType.Gesture, value);
        }
        public AnimatorController GetPlayableAnimatorController(VRCAvatarDescriptor.AnimLayerType type)
        {
            var c = BaseAnimationLayers[PlayableIndex(type)].animatorController;
            if (c == null)
            {
                return null;
            }
            return (AnimatorController)c;
        }
        public void SetPlayableAnimatorController(VRCAvatarDescriptor.AnimLayerType type, AnimatorController c)
        {
            BaseAnimationLayers[PlayableIndex(type)].isDefault = false;
            BaseAnimationLayers[PlayableIndex(type)].animatorController = c;
        }



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
            Log.I.Info("DebugModeが開始されました。PrjRootObjの削除・Debugフォルダ・Tmpフォルダの削除を行います。");
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
            else if (ContainsFirstString(parameterName, new string[] { "Time", "ExLoaded", "IsDance", "IsNotDance", "IsLocal", "FrameTime" })) res = $@"Env/{parameterName}";
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
            Log.I.Error($@"Motion「{motionPath}」が見つかりませんでした");
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
                Log.I.Error($@"{packageJsonPath}が見つかりませんでした");
                return null;
            }
            string jsonContent = File.ReadAllText(packageJsonPath);
            if (jsonContent == null)
            {
                Log.I.Error("package.jsonの読み込みに失敗しました");
                return null;
            }
            string versionPattern = @"""version"":\s*""([0-9]+\.[0-9]+\.[0-9]+)""";
            Match match = Regex.Match(jsonContent, versionPattern);
            if (match.Success)
            {
                string version = match.Groups[1].Value;
                return version;
            }
            Log.I.Error("バージョンの正規表現がマッチしませんでした");
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
        /// <param name="toggleSync">指定してあるならそのパラメータがONの時だけ同期する</param>
        public PVnBitSync.PVnBitSyncData VirtualSync(string txName, int Bit, PVnBitSync.nBitSyncMode syncMode, bool hostDecode = false, float min = 0.0f, float max = 1.0f, bool toggleSync = false)
        {
            var s = CreateComponentObject<PVnBitSync>($@"VirtualSync{txName}");
            return s.Set(txName, Bit, syncMode, hostDecode, min, max, toggleSync);
        }


        /// <summary>
        /// MessageUIを作成する
        /// </summary>
        /// <param name="message">表示する文字列</param>
        /// <param name="parameterName">条件パラメータ</param>
        /// <param name="conditionMode">判定方法</param>
        /// <param name="parameterValue">判定値</param>
        /// <param name="duration">表示時間</param>
        /// <param name="inactiveByParameter">条件外ですぐにOFF</param>
        /// <param name="isRemote">Remoteのみ実行</param>
        /// <param name="textColor">文字色（白）</param>
        /// <param name="outlineColor">アウトライン色（黒）</param>
        public PVMessageUI SetMessage(
            string message
            , string parameterName = ""
            , AnimatorConditionMode conditionMode = AnimatorConditionMode.Equals
            , float parameterValue = 1
            , float duration = 5
            , bool inactiveByParameter = true
            , bool isRemote = false
            , Color? textColor = null
            , Color? outlineColor = null)
        {
            if (message == null) return null;
            var p = GetOrCreateObject("MessageUI");
            var pvMessageUI = p.AddComponent<PVMessageUI>();
            pvMessageUI.Message = message;
            pvMessageUI.DisplayDuration = duration;
            pvMessageUI.InactiveByParameter = inactiveByParameter;
            pvMessageUI.IsRemote = isRemote;
            pvMessageUI.ParameterName = parameterName;
            pvMessageUI.ConditionMode = conditionMode;
            pvMessageUI.ParameterValue = parameterValue;
            pvMessageUI.TextColor = textColor ?? Color.white;
            pvMessageUI.OutlineColor = outlineColor ?? Color.black;
            return pvMessageUI;
        }

        /// <summary>
        /// GridUIを作成する
        /// </summary>
        /// <param name ="ParameterName">GridUIで管掌するパラメータ名</param>
        /// <param name ="x">Gridの横の個数</param>
        /// <param name ="y">Gridの縦の個数</param>
        /// <param name = "MainTex">メインテクスチャ</param>
        /// <param name = "LockTex">ロックテクスチャ</param>
        /// <param name="nVirtualSync">選択対象のVirtualSync有無</param>
        /// <param name="speed">選択移動速度</param>
        /// <param name="createSampleMenu">サンプルメニューを作成するか(For Debug)</param>
        public PVGridUI SetGridUI(string ParameterName, int x, int y, Texture2D MainTex = null, Texture2D LockTex = null, bool nVirtualSync = true, float speed = 0.3f, bool createSampleMenu = false)
        {
            var p = GetOrCreateObject("GridUI");
            var pvGridUI = p.AddComponent<PVGridUI>();
            pvGridUI.ParameterName = ParameterName;
            pvGridUI.xMax = x;
            pvGridUI.yMax = y;
            pvGridUI.MainTex = MainTex;
            pvGridUI.LockTex = LockTex;
            pvGridUI.nVirtualSync = nVirtualSync;
            pvGridUI.Speed = speed;
            pvGridUI.CreateSampleMenu = createSampleMenu;
            return pvGridUI;
        }

        public PVFrameCounter SetFrameCounter()
        {
            return GetOrCreateComponentObject<PVFrameCounter>("FrameCounter");
        }

        /////////////////////////DEBUG/////////////////////////
        /// <summary>
        /// DebugMessageを表示する
        /// </summary>
        /// <param name="message">表示するMessage</param>
        /// <param name="debugOnly">DebugModeでのみ表示</param>
        /// <param name="level">ログレベル</param>
        /// <param name="callerMethodName">システムが使用</param>
        [Obsolete]
        public void DebugPrint(string message, bool debugOnly = true, LogType level = LogType.Warning, [CallerMemberName] string callerMethodName = "", [CallerLineNumber] int callerLineNumber = 0)
        {
            //LowLevelDebugPrint(message, debugOnly, level, ProjectName, callerMethodName);
            Log.I.Info(message);
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


        public PVParameter AddParameter(string ParameterName, ParameterSyncType? syncType = null, bool? localOnly = null, float? defaultValue = null, bool? saved = null)
        {
            var parent = GetOrCreateObject("PVParameter");
            var p = CreateComponentObject<PVParameter>(ParameterName);
            p.transform.SetParent(parent.transform);
            p.ParameterName = ParameterName;
            p.syncType = syncType;
            p.localOnly = localOnly;
            p.defaultValue = defaultValue;
            p.saved = saved;
            return p;
        }

        public PVMenuIcoOverride OverrideMenuIco(string paramName1, string paramName2, float? paramVal1, string icoPath)
        {
            var parent = GetOrCreateObject("PVMenuIcoOverride");
            var p = CreateComponentObject<PVMenuIcoOverride>($@"{paramName1}_{paramName2}");
            p.transform.SetParent(parent.transform);
            p.ParameterName1 = paramName1;
            p.ParameterName2 = paramName2;
            p.ParamValue1 = paramVal1;
            p.Ico = AssetDatabase.LoadAssetAtPath<Texture2D>(icoPath);
            return p;
        }
        public PVMenuIcoOverride OverrideFolderIco(string folderName, string icoPath)
        {
            var parent = GetOrCreateObject("PVMenuIcoOverride");
            var p = CreateComponentObject<PVMenuIcoOverride>($@"{folderName}");
            p.transform.SetParent(parent.transform);
            p.FolderName = folderName;
            p.Ico = AssetDatabase.LoadAssetAtPath<Texture2D>(icoPath);
            return p;
        }
        public PVMenuIcoOverride OverrideRadialIco(string radialParamName, string icoPath)
        {
            var parent = GetOrCreateObject("PVMenuIcoOverride");
            var p = CreateComponentObject<PVMenuIcoOverride>($@"{radialParamName}");
            p.transform.SetParent(parent.transform);
            p.RadialParameterName = radialParamName;
            p.Ico = AssetDatabase.LoadAssetAtPath<Texture2D>(icoPath);
            if (p.Ico == null)
            {
                Log.I.Error($@"{icoPath}が見つかりませんでした");
            }
            return p;
        }
    }
}
#endif