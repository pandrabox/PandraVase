using com.github.pandrabox.pandravase.editor;
using nadena.dev.ndmf;
using nadena.dev.ndmf.fluent;

[assembly: ExportsPlugin(typeof(PanPluginDefinition))]

namespace com.github.pandrabox.pandravase.editor
{
    internal class PanPluginDefinition : Plugin<PanPluginDefinition>
    {
        private Sequence seq;

        public override string DisplayName => "PandraVase";
        public override string QualifiedName => "com.github.pandrabox.pandravase";

        protected override void Configure()
        {
            seq = InPhase(BuildPhase.Resolving).BeforePlugin("nadena.dev.modular-avatar");
            seq = InPhase(BuildPhase.Generating).BeforePlugin("nadena.dev.modular-avatar");
            seq = InPhase(BuildPhase.Transforming).BeforePlugin("nadena.dev.modular-avatar");
            // ここからプログレスバーで管理
            seq.Run(PVInstantiatePass.Instance); //これは必ず最初にしてください（プログレスバーをここで開始しているので）

            seq.Run(PVWriteDefaultOnPass.Instance);
            seq.Run(PVPlayableRemoverPass.Instance);
            seq.Run(PVUniquefyObjectPass.Instance);
            seq.Run(PVReplacePlayablePass.Instance);
            seq.Run(PVActiveOverridePass.Instance);
            seq.Run(PVParamView2Pass.Instance);
            seq.Run(PVMoveToRootPass.Instance);
            seq.Run(PVDanceControllerPass.Instance);


            seq.Run(PVGridUIPass.Instance);
            seq.Run(PVnBitSyncPass.Instance);
            seq.Run(PVMessageUIPass.Instance); // MenuBuilderで作成されるComponentの解決
            seq.Run(PVMergeMASubMenuPass.Instance); //PVDanceControllerPass, PVMessageUIPassの解決
            seq.Run(PVFrameCounterPass.Instance); // FrameCounterの解決
            seq.Run(PVParameterPass.Instance); // 追々これを最後にすべき気がする
            seq.Run(PVPanMergeBlendTreePass.Instance); // PanMergeBlendTreeの解決　これは必ず最後にして下さい（プログレスバーをここで終了しているので）
            PanProgressBar.Hide();//あんまり意味はないのだがおなじない
            // ここまでプログレスバーで管理
            seq = InPhase(BuildPhase.Optimizing).BeforePlugin("nadena.dev.modular-avatar");
            seq.Run(PVFxSortPass.Instance);
        }
    }
}
