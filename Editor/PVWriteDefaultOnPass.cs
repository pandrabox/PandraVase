using com.github.pandrabox.pandravase.runtime;
using nadena.dev.modular_avatar.core;
using nadena.dev.ndmf;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using static com.github.pandrabox.pandravase.editor.Util;

namespace com.github.pandrabox.pandravase.editor
{
#if PANDRADBG
    public class WriteDefaultOnDebug
    {
        [MenuItem("PanDbg/WriteDefaultOn")]
        public static void WriteDefaultOnt_Debug()
        {
            SetDebugMode(true);
            new WriteDefaultOnMain(TopAvatar);
        }
    }
#endif
    internal class PVWriteDefaultOnPass : Pass<PVWriteDefaultOnPass>
    {
        protected override void Execute(BuildContext ctx)
        {
            PanProgressBar.Show();
            new WriteDefaultOnMain(ctx.AvatarDescriptor);
        }
    }
    public class WriteDefaultOnMain
    {
        PandraProject _prj;
        public WriteDefaultOnMain(VRCAvatarDescriptor desc)
        {
            var _tgt = desc.GetComponentInChildren<PVWriteDefaultOn>();
            if (_tgt == null) return;
            Log.I.StartMethod();

            //FXのWDをON
            _prj = VaseProject(desc);
            int fxIndex = _prj.PlayableIndex(VRCAvatarDescriptor.AnimLayerType.FX);
            RuntimeAnimatorController runtimeFxController = desc?.baseAnimationLayers[fxIndex].animatorController;
            if (runtimeFxController == null) return;
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
            foreach (var m in mama)
            {
                m.matchAvatarWriteDefaults = true;
            }

            //FX以外のPlayable,Animatorはとりあえず放置
            Log.I.EndMethod();
        }
    }
}