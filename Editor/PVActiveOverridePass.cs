/// 「ActiveOverride」がアタッチされているGameObjectをビルド時設定したActive状態にする

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
    public class PVActiveOverrideDebug
    {
        [MenuItem("PanDbg/PVActiveOverride")]
        public static void PVActiveOverride_Debug() {
            SetDebugMode(true);
            new PVActiveOverrideMain(TopAvatar);
        }
    }
#endif

    internal class PVActiveOverridePass : Pass<PVActiveOverridePass>
    {
        protected override void Execute(BuildContext ctx)
        {
            PanProgressBar.Show();
            new PVActiveOverrideMain(ctx.AvatarDescriptor);
        }
    }

    public class PVActiveOverrideMain
    {
        public PVActiveOverrideMain(VRCAvatarDescriptor desc)
        {
            PVActiveOverride[] targets = desc.transform.GetComponentsInChildren<PVActiveOverride>();

            foreach (PVActiveOverride target in targets)
            {
                target.gameObject.SetActive(target.active);
            }
        }
    }
}
