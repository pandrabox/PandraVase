using com.github.pandrabox.pandravase.editor;
using com.github.pandrabox.pandravase.runtime;
using nadena.dev.ndmf;
using nadena.dev.ndmf.fluent;

[assembly: ExportsPlugin(typeof(PanPluginDefinition))]

namespace com.github.pandrabox.pandravase.editor
{
    internal class PanPluginDefinition : Plugin<PanPluginDefinition>
    {
        public override string DisplayName => "PandraVase";
        public override string QualifiedName => "com.github.pandrabox.pandravase";

        protected override void Configure()
        {
            Sequence seq;
            seq = InPhase(BuildPhase.Transforming).BeforePlugin("nadena.dev.modular-avatar");
            seq.Run(PVPlayableRemoverPass.Instance);
            seq.Run(PVUniquefyObjectPass.Instance);
            seq.Run(PVPanMergeBlendTreePass.Instance);
            seq.Run(PVReplacePlayablePass.Instance);
            seq.Run(PVActiveOverridePass.Instance);
        }
    }
}
