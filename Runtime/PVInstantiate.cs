#if UNITY_EDITOR
using System;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using nadena.dev.modular_avatar.core;
using VRC.SDKBase;
using UnityEditor;
using System.Collections.Generic;

namespace com.github.pandrabox.pandravase.runtime
{
    public class PVInstantiate : PandraComponent
    {
        public List<GameObject> prefabs;
    }
}
#endif