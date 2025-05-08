
using com.github.pandrabox.pandravase.runtime;
using nadena.dev.ndmf;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using static com.github.pandrabox.pandravase.editor.Util;


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
            PanProgressBar.Show();
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