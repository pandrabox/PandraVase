/// プレイアブルレイヤから指定の名称のレイヤを削除する

using UnityEditor;
using nadena.dev.modular_avatar.core;
using UnityEngine;
using UnityEditor.Animations;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using nadena.dev.ndmf.util;
using nadena.dev.ndmf;
using com.github.pandrabox.pandravase.runtime;
using static com.github.pandrabox.pandravase.runtime.Util;
using System.Linq;
using VRC.SDK3.Avatars.Components;

namespace com.github.pandrabox.pandravase.editor
{
#if PANDRADBG
    public class PlayableRemoverDebug
    {
        [MenuItem("PanDbg/PVPlayableRemover")]
        public static void PVPlayableRemover_Debug() {
            SetDebugMode(true);
            new PVPlayableRemoverMain(TopAvatar);
        }
    }
#endif

    internal class PVPlayableRemoverPass : Pass<PVPlayableRemoverPass>
    {
        protected override void Execute(BuildContext ctx)
        {
            new PVPlayableRemoverMain(ctx.AvatarDescriptor);
        }
    }

    public class PVPlayableRemoverMain
    {
        public PVPlayableRemoverMain(VRCAvatarDescriptor desc)
        {
            PVPlayableRemover[] removers = desc.transform.GetComponentsInChildren<PVPlayableRemover>();
            for (int j = 0; j <= 4; j++)
            {
                AnimatorController animatorController = desc.baseAnimationLayers[j].animatorController as AnimatorController;
                if (removers == null || animatorController == null) continue;
                var layers = animatorController.layers;
                if (layers == null) continue;
                for (int i = layers.Length - 1; i >= 0; i--)
                {
                    foreach (PVPlayableRemover remover in removers)
                    {
                        if (remover != null || layers[i] != null)
                        {
                            string[] targetLayerNames;
                            if (j == 0) { targetLayerNames = remover.BaseLayer; }
                            else if (j == 1) { targetLayerNames = remover.AdditiveLayer; }
                            else if (j == 2) { targetLayerNames = remover.GestureLayer; }
                            else if (j == 3) { targetLayerNames = remover.ActionLayer; }
                            else { targetLayerNames = remover.FXLayer; }
                            foreach (var layerName in targetLayerNames)
                            {
                                if (layers[i].name == layerName)
                                {
                                    animatorController.RemoveLayer(i);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
