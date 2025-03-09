/// 「FxSort」のSortOrder文字列に合わせてFxLayerを並び替える。AddBlankをtrueにすると空のFxLayerを追加する

using com.github.pandrabox.pandravase.runtime;
using nadena.dev.ndmf;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using VRC.SDK3.Avatars.Components;
using static com.github.pandrabox.pandravase.editor.Util;

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
        PandraProject _prj;
        public FxSortMain(VRCAvatarDescriptor desc)
        {
            _prj = VaseProject(desc);
            PVFxSort fxSort = desc.transform.GetComponentInChildren<PVFxSort>();
            if (fxSort == null) return;
            PandraProject prj = VaseProject(desc);
            string[] sortOrder = fxSort.SortOrder;

            int fxn = _prj.PlayableIndex(VRCAvatarDescriptor.AnimLayerType.FX);

            if (fxSort.AddBlank)
            {
                var fx = prj?.BaseAnimationLayers[fxn].animatorController;
                if (fx == null)
                {
                    LowLevelExeption("FxSortでFxが見つかりませんでした");
                    return;
                }
                var fxc = (AnimatorController)fx;
                fxc.AddLayer("blank");
                prj.BaseAnimationLayers[fxn].animatorController = fxc;
            }
            var Layers = ((AnimatorController)prj.BaseAnimationLayers[fxn].animatorController).layers;
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
            ((AnimatorController)prj.BaseAnimationLayers[fxn].animatorController).layers = LayersCopy;

            // 最終的なsortOrderとレイヤ順番を表示
            LowLevelDebugPrint("Final Sort Order: " + string.Join(", ", sortOrder));
            LowLevelDebugPrint("Final Layer Order: " + string.Join(", ", LayersCopy.Select(layer => layer.name)));
        }
    }
}
