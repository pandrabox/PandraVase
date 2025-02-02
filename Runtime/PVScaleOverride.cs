#if UNITY_EDITOR
using System;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using nadena.dev.modular_avatar.core;
using VRC.SDKBase;
using UnityEditor;

namespace com.github.pandrabox.pandravase.runtime
{
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    public class PVScaleOverride : PandraComponent
    {
        [SerializeField]
        public Vector3 OverrideScale;

        internal void Update()
        {
            if (transform.localScale != OverrideScale)
            {
                transform.localScale = OverrideScale;
                //boneProxyがあるときアクティブにすることでスケールが反映される
                ModularAvatarBoneProxy[] boneProxies = FindObjectsOfType<ModularAvatarBoneProxy>();
                foreach (ModularAvatarBoneProxy boneProxy in boneProxies)
                {
                    Selection.activeGameObject = boneProxy.gameObject;
                    break;
                }
            }
        }
    }
}
#endif