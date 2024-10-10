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

namespace com.github.pandrabox.pandravase.editor
{
    public class PandraProject
    {
        const string ONEPARAM = "__ModularAvatarInternal/One";
        public VRCAvatarDescriptor Descriptor;
        public string Suffix;
        private string _workFolder;

        public GameObject RootObject => Descriptor.gameObject;
        public Transform RootTransform => Descriptor.transform;
        public string WorkFolder
        {
            get 
            {
                if (string.IsNullOrEmpty(_workFolder))
                {
                    DebugPrint("初期化されていないWorkFolderを呼び出しました", true, LogType.Error);
                    return null;
                }
                return _workFolder;
            }
        }
        public string ResFolder => $@"{WorkFolder}Res/";
        public string ImgFolder => $@"{ResFolder}Img/";
        public string AssetsFolder => $@"{WorkFolder}Assets/";
        public string AnimFolder => $@"{AssetsFolder}Anim/";
        public string EditorFolder => $@"{WorkFolder}Editor/";
        public string RuntimeFolder => $@"{WorkFolder}Runtime/";

        /// <summary>
        /// 1つのAvatarを編集するためのProjectを統括する基底クラス
        /// </summary>
        /// <param name="descriptor">Descriptor</param>
        /// <param name="suffix">変数名・レイヤ名等の前置詞</param>
        /// <param name="workFolder">Anim等を読み込む際使用する基本フォルダ</param>
        public PandraProject(VRCAvatarDescriptor descriptor, string suffix = "", string workFolder="")
        {
            Descriptor = descriptor;
            Suffix = SanitizeStr(suffix);
            _workFolder = string.IsNullOrEmpty(workFolder) ? "" : $@"{workFolder.TrimEnd('/')}/";
        }

        public string GetParameterName(string parameterName)
        {
            if (string.IsNullOrEmpty(parameterName)) throw new NotImplementedException("ParameterName is Null");
            string res;
            if (ContainsFirstString(parameterName, new string[] { "ONEf", "PBTB_CONST_1", ONEPARAM })) res = ONEPARAM;
            else if (ContainsFirstString(parameterName, new string[] { "GestureRight", "GestureLeft", "GestureRightWeight", "GestureLeftWeight", "IsLocal", "InStation", "Seated", "VRMode" })) res = parameterName;
            else if (parameterName.StartsWith("Env/")) res = parameterName;
            else if (ContainsFirstString(parameterName, new string[] { "Time", "ExLoaded", "IsMMD", "IsNotMMD", "IsLocal", "FrameTime" })) res = $@"Env/{parameterName}";
            else if (parameterName.Length > 0 && parameterName[0] == '$') res = parameterName.Substring(1);
            else res = $@"{Suffix}/{parameterName}";
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

        protected Motion LoadMotion(string motionPath)
        {
            return AssetDatabase.LoadAssetAtPath<Motion>(NormalizedMotionPath(motionPath));
        }
    }
}
