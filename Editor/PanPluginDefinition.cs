using com.github.pandrabox.pandravase.editor;
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
            seq.Run(PanMergeBlendTreePass.Instance);
            seq.Run(ReplacePlayablePass.Instance);
        }
    }
}
