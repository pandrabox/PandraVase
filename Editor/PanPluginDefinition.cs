
using System;
using com.github.pandrabox.pandravase.editor;
using nadena.dev.modular_avatar.animation;
using nadena.dev.modular_avatar.core.ArmatureAwase;
using nadena.dev.modular_avatar.core.editor.plugin;
using nadena.dev.modular_avatar.editor.ErrorReporting;
using nadena.dev.ndmf;
using nadena.dev.ndmf.fluent;
using Packages.com.github.pandrabox.pandravase.Editor;
using UnityEngine;

[assembly: ExportsPlugin(typeof(PanPluginDefinition))]

namespace Packages.com.github.pandrabox.pandravase.Editor
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
        }
    }
}
