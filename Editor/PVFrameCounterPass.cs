
using com.github.pandrabox.pandravase.runtime;
using nadena.dev.ndmf;
using UnityEditor;
using VRC.SDK3.Avatars.Components;
using static com.github.pandrabox.pandravase.editor.Util;


namespace com.github.pandrabox.pandravase.editor
{
#if PANDRADBG
    public class PVFrameCounterPassDebug
    {
        [MenuItem("PanDbg/PVFrameCounterPass")]
        public static void PVFrameCounterPass_Debug()
        {
            SetDebugMode(true);
            new PVFrameCounterPassMain(TopAvatar);
        }
    }
#endif

    public class PVFrameCounterPass : Pass<PVFrameCounterPass>
    {
        protected override void Execute(BuildContext ctx)
        {
            PanProgressBar.Show();
            new PVFrameCounterPassMain(ctx.AvatarDescriptor);
        }
    }
    public class PVFrameCounterPassMain
    {
        PandraProject _prj;
        public PVFrameCounterPassMain(VRCAvatarDescriptor desc)
        {
            var t = desc.GetComponentInChildren<PVFrameCounter>();
            if (t == null) return;
            _prj = VaseProject(desc);
            var ac = new AnimationClipsBuilder();

            var bb = new BlendTreeBuilder("FlameCounter");
            bb.RootDBT(() =>
            {
                bb.Param("1").AddAAP(_prj.FrameCount, 1);
                bb.Param(_prj.FrameCount).AddAAP(_prj.FrameCount, 1);
            });
            bb.Attach(_prj);
        }
    }
}