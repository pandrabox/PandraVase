using System;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using nadena.dev.modular_avatar.core;
using VRC.SDKBase;

namespace com.github.pandrabox.pandravase.runtime
{
    public class PVReplacePlayable : PandraComponent
    {
        public VRCAvatarDescriptor.AnimLayerType LayerType;
        public RuntimeAnimatorController controller;
    }
}