/// 「FxSort」のSortOrder文字列に合わせてFxLayerを並び替える。AddBlankをtrueにすると空のFxLayerを追加する

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
using static UnityEngine.Tilemaps.TilemapRenderer;

namespace com.github.pandrabox.pandravase.editor
{
#if PANDRADBG
    public class FxSortDebug
    {
        [MenuItem("PanDbg/FxSort")]
        public static void FxSort_Debug()
        {
            SetDebugMode(true);
            new FxSortMain(TopAvatar);
        }
    }
#endif

    internal class PVFxSortPass : Pass<PVFxSortPass>
    {
        protected override void Execute(BuildContext ctx)
        {
            new FxSortMain(ctx.AvatarDescriptor);
        }
    }

    public class FxSortMain
    {
        public FxSortMain(VRCAvatarDescriptor desc)
        {
            PVFxSort fxSort = (PVFxSort)desc.transform.GetComponentsInChildren(typeof(PVFxSort)).FirstOrDefault();
            if(fxSort == null) return;
            PandraProject prj = VaseProject(desc);
            string[] sortOrder = fxSort.SortOrder;

            if (fxSort.AddBlank)
            {
                ((AnimatorController)prj.BaseAnimationLayers[3].animatorController).AddLayer("blank");
            }
            var Layers = ((AnimatorController)prj.BaseAnimationLayers[3].animatorController).layers;
            var LayersCopy = new AnimatorControllerLayer[Layers.Length];
            for (int n = 0; n < sortOrder.Length; n++)
            {
                for (int m = 0; m < Layers.Length; m++)
                {
                    if (Layers[m] != null)
                    {
                        if (sortOrder[n] == Layers[m].name)
                        {
                            if (n < Layers.Length)
                            {
                                LayersCopy[n] = Layers[m];
                                Layers[m] = null;
                                break;
                            }
                        }
                    }
                }
            }
            for (int n = 0; n < Layers.Length; n++)
            {
                if (LayersCopy[n] == null)
                {
                    for (int m = 0; m < Layers.Length; m++)
                    {
                        if (Layers[m] != null)
                        {
                            LayersCopy[n] = Layers[m];
                            Layers[m] = null;
                            break;
                        }
                    }
                }
            }
            ((AnimatorController)prj.BaseAnimationLayers[3].animatorController).layers = LayersCopy;
        }
    }
}
