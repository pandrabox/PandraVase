#if UNITY_EDITOR
using System;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using nadena.dev.modular_avatar.core;
using VRC.SDKBase;
using UnityEditor;

namespace com.github.pandrabox.pandravase.runtime
{
    [DisallowMultipleComponent]
    public class PVUniquefyObject : PandraComponent
    {
        //フラグ用コンポーネントであり、実装はない
    }
}
#endif