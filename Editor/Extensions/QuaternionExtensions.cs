using System;
using UnityEngine;


namespace com.github.pandrabox.pandravase.editor
{
    public static class QuaternionExtensions
    {
        public static float GetAxis(this Quaternion quaternion, Axis axis)
        {
            switch (axis)
            {
                case Axis.X: return quaternion.x;
                case Axis.Y: return quaternion.y;
                case Axis.Z: return quaternion.z;
                case Axis.W: return quaternion.w;
                default: throw new ArgumentOutOfRangeException(nameof(axis), "Invalid axis");
            }
        }
    }
}