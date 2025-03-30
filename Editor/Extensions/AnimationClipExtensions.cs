using UnityEditor;
using UnityEngine;
using static com.github.pandrabox.pandravase.editor.Util;
using com.github.pandrabox.pandravase.runtime;


namespace com.github.pandrabox.pandravase.editor
{
    public static class AnimationClipExtensions
    {
        public static AnimationClip Multiplication(this AnimationClip clip, float weight)
        {
            if (clip == null)
            {
                Log.I.Error("clip is null");
            }
            AnimationClip newClip = new AnimationClip();
            EditorCurveBinding[] curveBindings = AnimationUtility.GetCurveBindings(clip);

            foreach (var binding in curveBindings)
            {
                AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);
                for (int i = 0; i < curve.keys.Length; i++)
                {
                    Keyframe key = curve.keys[i];
                    key.value *= weight;
                    curve.MoveKey(i, key);
                }
                newClip.SetCurve(binding.path, binding.type, binding.propertyName, curve);
            }

            return newClip;
        }
        public static AnimationClip Zero(this AnimationClip clip)=> Multiplication(clip, 0);
    }
}