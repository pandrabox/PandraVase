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
    public class PVMoveToRootDebug
    {
        [MenuItem("PanDbg/PVMoveToRoot")]
        public static void PVMoveToRoot_Attach()
        {
            SetDebugMode(true);
            foreach (var a in AllAvatar)
            {
                new PVMoveToRootMain(a);
            }
        }
    }
#endif

    internal class PVMoveToRootPass : Pass<PVMoveToRootPass>
    {
        protected override void Execute(BuildContext ctx)
        {
            PanProgressBar.Show();
            new PVMoveToRootMain(ctx.AvatarDescriptor);
        }
    }

    public class PVMoveToRootMain
    {
        private PVMoveToRoot[] _tgts;
        public PVMoveToRootMain(VRCAvatarDescriptor desc)
        {
            _tgts = desc.GetComponentsInChildren<PVMoveToRoot>();
            foreach (var tgt in _tgts)
            {
                tgt.transform.SetParent(desc.transform);
            }
        }
    }
}
