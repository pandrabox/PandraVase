
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

namespace com.github.pandrabox.pandravase.editor
{
#if PANDRADBG
    public class PVMessageUIPassDebug
    {
        [MenuItem("PanDbg/PVMessageUIPass")]
        public static void PVMessageUIPass_Debug()
        {
            SetDebugMode(true);
            new MsgTexture();
        }
    }
#endif

    public class MsgTexture
    {
        public MsgTexture()
        {
            string[] msgs = new string[] { "Hello", "World", "!" };
            int width = 512;
            float heightRatio = .1f;
            int height = (int)(width * heightRatio);
            using (var capture = new PanCapture(Color.black))
            {
                var p = capture.TextToImage("test", Color.white);
                var p2 = Util.OutpAsset(p);
                LowLevelDebugPrint(p2);
            }

            //foreach (var msg in msgs)
            //{
            //    var p = capture.TextToImage(msg, Color.white);
            //    var p2 = Util.OutpAsset(p);
            //    LowLevelDebugPrint(p2);
            //}
        }
    }
}