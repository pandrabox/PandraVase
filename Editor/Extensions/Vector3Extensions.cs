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

    public static class Vector3Extensions
    {
        public static float GetAxis(this Vector3 vector, Axis axis)
        {
            switch (axis)
            {
                case Axis.X: return vector.x;
                case Axis.Y: return vector.y;
                case Axis.Z: return vector.z;
                default: throw new ArgumentOutOfRangeException(nameof(axis), "Invalid axis");
            }
        }

        public static Vector3 SetAxis(this Vector3 vector, Axis axis, float value)
        {
            switch (axis)
            {
                case Axis.X:
                    vector.x = value;
                    break;
                case Axis.Y:
                    vector.y = value;
                    break;
                case Axis.Z:
                    vector.z = value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(axis), "Invalid axis");
            }
            return vector;
        }
    }
}