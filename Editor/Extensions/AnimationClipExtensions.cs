using com.github.pandrabox.pandravase.runtime;
using nadena.dev.modular_avatar.core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDKBase;
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