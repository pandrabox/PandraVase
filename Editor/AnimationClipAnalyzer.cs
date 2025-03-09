using System.Text;
using UnityEditor;
using UnityEngine;
using static com.github.pandrabox.pandravase.editor.Util;


namespace com.github.pandrabox.pandravase.editor
{
    /// <summary>
    /// ProjectにおけるAnimationClipで実行するとAnimationClipsBuilder相当のコードを生成してコンソール・クリップボードに返す開発ツール
    /// 現状(2025/02/11)において取得するのは名称とBindのみです。つまり、キーは含みません（ループ順序に悩んだため）
    /// </summary>

#if PANDRADBG
    public class AnimationClipAnalyzer
    {
        [MenuItem("Assets/PanDev/AnimationClipAnalyzer")]
        public static void AnimationClipAnalyzerMain()
        {
            var sb = new StringBuilder();
            var selectedObject = Selection.activeObject;
            if (selectedObject == null || (!(selectedObject is AnimationClip))) return;
            AnimationClip tgt = selectedObject as AnimationClip;
            //sb.AppendLine($@"ab.Clip(""{tgt.name}"")");
            EditorCurveBinding[] curveBindings = AnimationUtility.GetCurveBindings(tgt);

            foreach (EditorCurveBinding binding in curveBindings)
            {
                var type = binding.type.ToString().Replace("UnityEngine.", "").Replace("Animations.", "");
                sb.AppendLine($@".Bind(""{binding.path}"", typeof({type}), ""{binding.propertyName}"")");
                //AnimationCurve curve = AnimationUtility.GetEditorCurve(tgt, binding);
                //for (var i = 0; i < curve.keys.Length; i++)
                //{
                //    Keyframe key = curve.keys[i];
                //    Debug.LogWarning(key.time + ", " + key.value);
                //}
            }
            string result = sb.ToString();
            LowLevelDebugPrint(result);
            EditorGUIUtility.systemCopyBuffer = result;
        }
    }
#endif
}