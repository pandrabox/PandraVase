/// 複数・同名のGameObjectに「ParamView2」「MASubMenu」がアタッチされているとき、それをまとめる

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
using static com.github.pandrabox.pandravase.editor.TextureUtil;
using System.Linq;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace com.github.pandrabox.pandravase.editor
{
#if PANDRADBG
    public class ParamView2Debug
    {
        [MenuItem("PanDbg/PVParamView2")]
        public static void ParamView2_Debug()
        {
            SetDebugMode(true);
            new ParamView2Main(TopAvatar);
        }
    }
#endif

    internal class PVParamView2Pass : Pass<PVParamView2Pass>
    {
        protected override void Execute(BuildContext ctx)
        {
            new ParamView2Main(ctx.AvatarDescriptor);
        }
    }

    public class ParamView2Main
    {
        private const float DisplayInterval= -0.06f;
        public ParamView2Main(VRCAvatarDescriptor desc)
        {
            using(PanCapture capture = new PanCapture(new Color(0, 0, 0, 0), width: 512))
            {
                PVParamView2[] targets = desc.transform.GetComponentsInChildren<PVParamView2>();
                AnimationClipsBuilder ab = new AnimationClipsBuilder();
                foreach (PVParamView2 target in targets)
                {
                    var maxDisits = Math.Max(Math.Abs(target.MinValue).ToString().Length, Math.Abs(target.MaxValue).ToString().Length);
                    GameObject obj = target.gameObject;
                    BlendTreeBuilder bb = new BlendTreeBuilder("ParamView2", "ParamView2");
                    bb.RootDBT(() =>
                    {
                        for (int i = 0; i < target.parameterName.Count; i++)
                        {
                            if (i == 0)
                            {

                            }
                            string paramName = target.parameterName[i];
                            ParamDisplay display = new ParamDisplay(target.gameObject, i, maxDisits);
                            Texture2D paramNameImg = capture.TextToImage(paramName.PadString(40), Color.white);
                            display.NameMaterial.mainTexture = paramNameImg;
                            ab.Clip($@"D{i}0").Bind($@"ParamDisplay{i}", typeof(MeshRenderer), "material._Test").Const2F(target.MinValue);
                            ab.Clip($@"D{i}1").Bind($@"ParamDisplay{i}", typeof(MeshRenderer), "material._Test").Const2F(target.MaxValue);
                            bb.Param("1").Add1D($@"${paramName}", () =>
                            {
                                bb.Param(target.MinValue).AddMotion(ab.Outp($@"D{i}0"));
                                bb.Param(target.MaxValue).AddMotion(ab.Outp($@"D{i}1"));
                            });
                        }
                    });
                    bb.Attach(obj);
                }
            }            
        }

        private class ParamDisplay
        {
            private GameObject OrgObj { get; }
            public GameObject Obj { get; }
            public int Count { get; }
            public Material NameMaterial { get; }
            public Material ValueMaterial { get; }
            public string Name => Obj.name;
            public ParamDisplay(GameObject parent, int c, int maxDigits)
            {
                Count = c;
                OrgObj = parent.transform.Find("ParamDisplay0").gameObject;
                if (c == 0)
                {
                    Obj = OrgObj;
                }
                else
                {
                    Obj = GameObject.Instantiate(OrgObj, parent.transform);
                    Obj.name = "ParamDisplay" + c;
                    Obj.transform.localPosition = new Vector3(0, OrgObj.transform.localPosition.y + DisplayInterval * c, 0);
                }
                var renderer = Obj.GetComponent<Renderer>();
                NameMaterial = renderer.materials[0];
                ValueMaterial = renderer.materials[1];
                if (c == 0)
                {
                    ValueMaterial.SetFloat("_IntDigits", maxDigits);
                }
            }
        }
    }
}
