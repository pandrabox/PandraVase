/// 「UniquefyObject」がアタッチされているGameObjectにおいて同じnameのものが複数ある場合、1つにする

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

namespace com.github.pandrabox.pandravase.editor
{
#if PANDRADBG
    public class UniquefyObjectDebug
    {
        [MenuItem("PanDbg/UniquefyObject")]
        public static void UniquefyObject_Debug() {
            SetDebugMode(true);
            new UniquefyObjectMain(TopAvatar);
        }
    }
#endif

    internal class PVUniquefyObjectPass : Pass<PVUniquefyObjectPass>
    {
        protected override void Execute(BuildContext ctx)
        {
            PanProgressBar.Show();
            new UniquefyObjectMain(ctx.AvatarDescriptor);
        }
    }

    public class UniquefyObjectMain
    {
        public UniquefyObjectMain(VRCAvatarDescriptor desc)
        {
            List<Component> targets = desc.transform.GetComponentsInChildren(typeof(PVUniquefyObject)).ToList();
            for (int i = targets.Count - 1; i >= 0; i--)
            {
                Component target = targets[i];
                if(targets.Count(x => x.name == target.name)>1)
                {
                    GameObject.DestroyImmediate(target.gameObject);
                    targets.RemoveAt(i);
                    LowLevelDebugPrint($@"Remove {target.name} ({i})");
                }
            }
        }
    }
}
