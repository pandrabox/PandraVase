﻿using UnityEditor;
using UnityEngine;
using static com.github.pandrabox.pandravase.editor.Util;


namespace com.github.pandrabox.pandravase.editor
{
    public static class AnimationClipExtensions
    {
        public static AnimationClip Multiplication(this AnimationClip clip, float weight)
        {
            if (clip == null)
            {
                LowLevelExeption("clip is null");
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
    }
}