using UnityEditor;
using UnityEngine;
using System;
using VRC.SDK3A.Editor;
using System.Threading.Tasks;
using VRC.SDKBase.Editor.Api;
using VRC.Core;
using System.Reflection;
using VRC.SDKBase.Editor;
using static com.github.pandrabox.pandravase.editor.Util;
using VRC.SDK3.Avatars.Components;
using UnityEditor.Animations;
using nadena.dev.modular_avatar.core;
using lilToon;
using System.Collections.Generic;
using static lilToon.lilToonInspector;
using UnityEditor.Compilation;

namespace com.github.pandrabox.pandravase.editor
{
#if PANDRADBG
    public class LilClippingCancelerDebug
    {
        [MenuItem("PanDbg/LilClippingCancelerOn")]
        public static void LilClippingCanceler_ON_Debug()
        {
            SetDebugMode(true);
            new LilClippingCanceler(true);
        }
        [MenuItem("PanDbg/LilClippingCancelerOff")]
        public static void LilClippingCanceler_OFF_Debug()
        {
            SetDebugMode(true);
            new LilClippingCanceler(false);
        }
    }
#endif
    public class LilClippingCanceler
    {
        public LilClippingCanceler(bool val)
        {
#if PANDRAVASE_LILTOON
            lilToonSetting shaderSetting = null;
            InitializeShaderSetting(ref shaderSetting);
            if (shaderSetting == null)
            {
                Debug.Log("shaderSetting is null");
                return;
            }
            shaderSetting.LIL_FEATURE_CLIPPING_CANCELLER = val;
            SaveShaderSetting(shaderSetting);
            CompilationPipeline.RequestScriptCompilation();

            void InitializeShaderSetting(ref lilToonSetting _shaderSetting)
            {
                MethodInfo method = typeof(lilToonSetting).GetMethod("InitializeShaderSetting", BindingFlags.NonPublic | BindingFlags.Static);
                if (method == null)
                {
                    Debug.Log("method is null");
                    return;
                }
                object[] parameters = new object[] { _shaderSetting };
                method.Invoke(null, parameters);
                _shaderSetting = (lilToonSetting)parameters[0];
            }

            void SaveShaderSetting(lilToonSetting _shaderSetting)
            {
                MethodInfo method = typeof(lilToonSetting).GetMethod("SaveShaderSetting", BindingFlags.Public | BindingFlags.Static);
                if (method == null)
                {
                    Debug.Log("method is null");
                    return;
                }
                method.Invoke(null, new object[] { _shaderSetting });
            }
#endif
        }
    }
}