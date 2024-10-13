#if PANDRADBG
using com.github.pandrabox.pandravase.runtime;
using nadena.dev.modular_avatar.core;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using static com.github.pandrabox.pandravase.runtime.Util;

namespace com.github.pandrabox.pandravase.editor
{
    public class PanDebug
    {
        [MenuItem("PanDbg/ReplacePlayableMain")]
        public static void Dbg1() {
            SetDebugMode(true);
            new ReplacePlayableMain(TopAvatar);
        }
    }
}
#endif