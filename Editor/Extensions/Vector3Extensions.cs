using System;
using UnityEngine;


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

        public static Vector3 HadamardProduct(this Vector3 a, Vector3 b)
        {
            return new Vector3(a.x * b.x, a.y * b.y, a.z * b.z);
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