using System.Reflection;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using static com.github.pandrabox.pandravase.editor.Util;

namespace com.github.pandrabox.pandravase.editor
{
#if PANDRADBG
    public class SetClippingCancelerDebug
    {
        [MenuItem("PanDbg/ClippingCancelerOn")]
        public static void ClippingCancelerOnt_Debug()
        {
            SetDebugMode(true);
            new SetClippingCanceler(true);
        }
        [MenuItem("PanDbg/ClippingCancelerOff")]
        public static void ClippingCancelerOfft_Debug()
        {
            SetDebugMode(true);
            new SetClippingCanceler(false);
        }
    }
#endif
    public class SetClippingCanceler
    {
        public SetClippingCanceler(bool val)
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
#endif
        }

#if PANDRAVASE_LILTOON
        private static void InitializeShaderSetting(ref lilToonSetting shaderSetting)
        {
            MethodInfo method = typeof(lilToonSetting).GetMethod("InitializeShaderSetting", BindingFlags.NonPublic | BindingFlags.Static);
            if (method == null)
            {
                Debug.Log("method is null");
                return;
            }
            object[] parameters = new object[] { shaderSetting };
            method.Invoke(null, parameters);
            shaderSetting = (lilToonSetting)parameters[0];
        }

        private static void SaveShaderSetting(lilToonSetting shaderSetting)
        {
            MethodInfo method = typeof(lilToonSetting).GetMethod("SaveShaderSetting", BindingFlags.Public | BindingFlags.Static);
            if (method == null)
            {
                Debug.Log("method is null");
                return;
            }
            method.Invoke(null, new object[] { shaderSetting });
        }
#endif
    }
}