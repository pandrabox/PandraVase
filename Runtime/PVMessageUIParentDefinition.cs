#if UNITY_EDITOR
using System;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using nadena.dev.modular_avatar.core;
using VRC.SDKBase;
using UnityEditor.Animations;

namespace com.github.pandrabox.pandravase.runtime
{
    public class PVMessageUIParentDefinition : PandraComponent
    {
        public string ParentFolder;
    }
}
#endif