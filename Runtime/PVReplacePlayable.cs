using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace com.github.pandrabox.pandravase.runtime
{
    public class PVReplacePlayable : PandraComponent
    {
        public VRCAvatarDescriptor.AnimLayerType LayerType;
        public RuntimeAnimatorController controller;
    }
}