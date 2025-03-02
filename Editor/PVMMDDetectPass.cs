using UnityEditor;
using UnityEngine;
using System;
using VRC.SDK3A.Editor;
using System.Threading.Tasks;
using VRC.SDKBase.Editor.Api;
using VRC.Core;
using System.Reflection;
using VRC.SDKBase.Editor;
using static com.github.pandrabox.pandravase.editor.Util;
using nadena.dev.ndmf;
using VRC.SDK3.Avatars.Components;
using com.github.pandrabox.pandravase.runtime;
using System.Linq;

namespace com.github.pandrabox.pandravase.editor
{
#if PANDRADBG
    public class PVMMDDetectDebug
    {
        [MenuItem("PanDbg/PVMMDDetect")]
        public static void PVMMDDetect_Attach()
        {
            SetDebugMode(true);
            foreach (var a in AllAvatar)
            {
                new PVMMDDetectMain(a);
            }
        }
    }
#endif

    internal class PVMMDDetectPass : Pass<PVMMDDetectPass>
    {
        protected override void Execute(BuildContext ctx)
        {
            new PVMMDDetectMain(ctx.AvatarDescriptor);
        }
    }

    public class PVMMDDetectMain
    {
        private PVMMDDetect[] _tgts;
        private PandraProject _prj;
        public PVMMDDetectMain(VRCAvatarDescriptor desc)
        {
            _tgts = desc.GetComponentsInChildren<PVMMDDetect>();
            if(_tgts.Length == 0) return;
            PandraProject _prj = VaseProject(desc);
            var ac = new AnimationClipsBuilder();
            var ab = new AnimatorBuilder("MMDDetector");
            ab.AddLayer();
            ab.AddState("Normal", ac.AAP(_prj.IsNotMMD, 1));
            ab.TransToCurrent(ab.InitialState).AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, .5f, _prj.MMDForce, true);

            ab.AddState("ForceIsNotMMD", ac.AAP(_prj.IsNotMMD, 1));
            var forceIsNotMMDState = ab.CurrentState;
            ab.TransToCurrent(ab.InitialState).AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, .5f, _prj.MMDForce, true);

            ab.AddState("ForceIsMMD", ac.AAP(_prj.IsNotMMD, 0));
            var forceIsMMDState = ab.CurrentState;
            ab.TransToCurrent(forceIsNotMMDState)
                .AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, .5f, "InStation")
                .AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, .5f, "Seated");
            ab.TransFromCurrent(forceIsNotMMDState)
                .AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, .5f, "InStation");
            ab.TransFromCurrent(forceIsNotMMDState)
                .AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, .5f, "Seated");
            ab.TransFromCurrent(ab.InitialState)
                .AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, .5f, _prj.MMDForce);

            ab.AddState("ManualIsMMD", ac.AAP(_prj.IsNotMMD, 0));
            ab.TransFromAny().AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, .5f, _prj.MMDManual);
            ab.TransFromCurrent(ab.InitialState).AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, .5f, _prj.MMDManual);
            ab.Attach(_tgts[0].gameObject);
            var s = _tgts[0].gameObject.AddComponent<PVFxSort>();
            s.SortOrder = new[] { "blank", "MMDDetector" };
            s.AddBlank = true;
        }
    }
}
