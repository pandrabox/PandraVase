using System.Reflection;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using static com.github.pandrabox.pandravase.editor.Util;

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