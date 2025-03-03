
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
using System.Collections.Immutable;
using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities.Collections;


namespace com.github.pandrabox.pandravase.editor
{
#if PANDRADBG
    public class PVInstantiatePassDebug
    {
        [MenuItem("PanDbg/PVInstantiatePass")]
        public static void PVInstantiatePass_Debug()
        {
            SetDebugMode(true);
            new PVInstantiatePassMain(TopAvatar);
        }
    }
#endif

    public class PVInstantiatePass : Pass<PVInstantiatePass>
    {
        protected override void Execute(BuildContext ctx)
        {
            new PVInstantiatePassMain(ctx.AvatarDescriptor);
        }
    }
    public class PVInstantiatePassMain
    {
        PVInstantiate[] _tgts;
        public PVInstantiatePassMain(VRCAvatarDescriptor desc)
        {
            _tgts = desc.GetComponentsInChildren<PVInstantiate>();
            if (_tgts.Length == 0) return;
            List<GameObject> prefabs = _tgts.SelectMany(t => t.prefabs).Distinct().ToList();
            foreach (GameObject prefab in prefabs)
            {
                GameObject go = GameObject.Instantiate(prefab);
                go.transform.SetParent(desc.transform);
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale = Vector3.one;

            }
        }
    }
}