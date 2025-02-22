#if UNITY_EDITOR
using System;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using nadena.dev.modular_avatar.core;
using VRC.SDKBase;
using UnityEditor.Animations;

namespace com.github.pandrabox.pandravase.runtime
{
    public class PVMessageUI : PandraComponent
    {
        public string Message;
        public float DisplayDuration=5;
        public bool InactiveByParameter=true;
        public bool IsRemote=false; //基本的には触れない
        public string ParameterName;
        public AnimatorConditionMode ConditionMode;
        public float ParameterValue;
        public Color TextColor;
        public Color OutlineColor;
    }
}
#endif