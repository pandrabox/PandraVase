using com.github.pandrabox.pandravase.runtime;
using nadena.dev.modular_avatar.core;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDKBase;
using static com.github.pandrabox.pandravase.editor.Util;


namespace com.github.pandrabox.pandravase.editor
{
    public static class QuaternionExtensions
    {
        public static float GetAxis(this Quaternion quaternion, Axis axis)
        {
            switch (axis)
            {
                case Axis.X:return quaternion.x;
                case Axis.Y:return quaternion.y; 
                case Axis.Z:return quaternion.z;
                case Axis.W:return quaternion.w;
                default: throw new ArgumentOutOfRangeException(nameof(axis), "Invalid axis");
            }
        }
    }
}