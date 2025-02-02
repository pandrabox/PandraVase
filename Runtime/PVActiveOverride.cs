﻿#if UNITY_EDITOR
using System;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using nadena.dev.modular_avatar.core;
using VRC.SDKBase;
using UnityEditor;

namespace com.github.pandrabox.pandravase.runtime
{
    [DisallowMultipleComponent]
    public class PVActiveOverride : PandraComponent
    {
        public bool active = true;
    }
}
#endif