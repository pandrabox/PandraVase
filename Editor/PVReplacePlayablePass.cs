using com.github.pandrabox.pandravase.runtime;
using nadena.dev.ndmf;
using System.Linq;
using VRC.SDK3.Avatars.Components;
using static com.github.pandrabox.pandravase.editor.Util;

namespace com.github.pandrabox.pandravase.editor
{
    /// <summary>
    /// 任意のPlayableLayerを指定したControllerで置換する
    /// </summary>
    internal class PVReplacePlayablePass : Pass<PVReplacePlayablePass>
    {
        protected override void Execute(BuildContext ctx)
        {
            PanProgressBar.Show();
            new PVReplacePlayableMain(ctx.AvatarDescriptor);
        }
    }

    public class PVReplacePlayableMain
    {
        private PandraProject _prj;
        public PVReplacePlayableMain(VRCAvatarDescriptor desc)
        {
            _prj = VaseProject(desc);

            // ターゲットの取得
            PVReplacePlayable[] components = _prj.RootObject.GetComponentsInChildren<PVReplacePlayable>(true);

            if (components.Length == 0) return;

            //重複してたら警告
            var duplicateLayerTypes = components.GroupBy(c => c.LayerType).Where(g => g.Count() > 1).Select(g => g.Key);
            foreach (var layerType in duplicateLayerTypes)
            {
                _prj.DebugPrint($@"{layerType}において複数回ReplacePlayableしようとしています。これは、どれか1つしか成功しません。", false);
            }

            //実処理 (指定のとおりに設定、DefaultフラグをOFF)
            foreach (var component in components)
            {
                var index = _prj.PlayableIndex(component.LayerType);
                _prj.BaseAnimationLayers[index].animatorController = component.controller;
                _prj.BaseAnimationLayers[index].isDefault = false;
            }
        }
    }
}
