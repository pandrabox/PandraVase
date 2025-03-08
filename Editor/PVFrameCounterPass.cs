
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
using static com.github.pandrabox.pandravase.editor.Util;
using System.Linq;
using VRC.SDK3.Avatars.Components;
using VRC.SDKBase;
using System.Text.RegularExpressions;
using static com.github.pandrabox.pandravase.editor.TextureUtil;
using System.Security.Cryptography;


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
            bb.RootDBT(()=> {
                bb.Param("1").AddAAP(_prj.FrameCount, 1);
                bb.Param(_prj.FrameCount).AddAAP(_prj.FrameCount, 1);
            });
            bb.Attach(_prj);
        }
    }
}