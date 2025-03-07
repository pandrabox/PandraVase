﻿using UnityEditor;
using UnityEngine;
using System;
using VRC.SDK3A.Editor;
using System.Threading.Tasks;
using VRC.SDKBase.Editor.Api;
using VRC.Core;
using System.Reflection;
using VRC.SDKBase.Editor;
using static com.github.pandrabox.pandravase.editor.Util;
using VRC.SDK3.Avatars.Components;
using UnityEditor.Animations;
using nadena.dev.modular_avatar.core;

namespace com.github.pandrabox.pandravase.editor
{
#if PANDRADBG
    public class WriteDefaultOnDebug
    {
        [MenuItem("PanDbg/WriteDefaultOn")]
        public static void WriteDefaultOnt_Debug()
        {
            SetDebugMode(true);
            new WriteDefaultOn(TopAvatar);
        }
    }
#endif
    public class WriteDefaultOn
    {
        PandraProject _prj;
        public WriteDefaultOn(VRCAvatarDescriptor desc)
        {
            //FXのWDをON
            _prj = VaseProject(desc);
            int fxIndex = _prj.PlayableIndex(VRCAvatarDescriptor.AnimLayerType.FX);
            RuntimeAnimatorController runtimeFxController = desc?.baseAnimationLayers[fxIndex].animatorController;
            if(runtimeFxController == null) return;
            AnimatorController fxController = (AnimatorController)runtimeFxController;
            foreach (var layer in fxController.layers)
            {
                foreach (var state in layer.stateMachine.states)
                {
                    if (state.state == null) continue;
                    state.state.writeDefaultValues = true;
                }
            }
            desc.baseAnimationLayers[fxIndex].animatorController = fxController;

            //MAのWDをマッチに設定
            ModularAvatarMergeAnimator[] mama = desc.GetComponentsInChildren<ModularAvatarMergeAnimator>();
            foreach(var m in mama)
            {
                m.matchAvatarWriteDefaults = true;
            }

            LowLevelDebugPrint("WD On");
            //FX以外のPlayable,Animatorはとりあえず放置
        }
    }
}