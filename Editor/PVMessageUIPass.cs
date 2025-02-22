
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
    public class PVMessageUIPassDebug
    {
        [MenuItem("PanDbg/PVMessageUIPass")]
        public static void PVMessageUIPass_Debug()
        {
            SetDebugMode(true);
            new PVMessageUIPassMain(TopAvatar);
        }
    }
#endif

    public class PVMessageUIPass : Pass<PVMessageUIPass>
    {
        protected override void Execute(BuildContext ctx)
        {
            new PVMessageUIPassMain(ctx.AvatarDescriptor);
        }
    }
    public class PVMessageUIPassMain
    {
        PandraProject _prj;
        PVMessageUI[] targets;
        GameObject MsgRoot;
        
        public PVMessageUIPassMain(VRCAvatarDescriptor desc)
        {
            targets = desc.transform.GetComponentsInChildren<PVMessageUI>();
            if (targets.Length == 0) return;
            _prj = VaseProject(desc);
            PrefabInstantiate();
            CreateImage();
            CreateAnimator();
        }

        private void CreateAnimator()
        {



            var ac = new AnimationClipsBuilder();
            ac.Clip("off").Bind("Display", typeof(GameObject), "m_IsActive").Const2F(0);
            var ab = new AnimatorBuilder("MessageUI").AddLayer();
            ab.AddSubStateMachine("Local");
            ab.AddState("Local").SetMotion(ac.Outp("off")).TransToCurrent(ab.InitialState).AddCondition(AnimatorConditionMode.Greater, .5f, "IsLocal");
            var localState = ab.CurrentState;
            ab.AddSubStateMachine("Remote");
            ab.AddState("Remote").SetMotion(ac.Outp("off")).TransToCurrent(ab.InitialState).AddCondition(AnimatorConditionMode.Less, .5f, "IsLocal");
            var remoteState = ab.CurrentState;


            var mb = new MenuBuilder(_prj).AddFolder("MessageUI");
            for (int i = 0; i < targets.Length; i++)
            {
                PVMessageUI tgt = targets[i];

                ac.Clip($"Appear{i}")
                    .Bind("Display", typeof(GameObject), "m_IsActive").Smooth(0, 1, tgt.DisplayDuration, 1)
                    .Bind("Display", typeof(MeshRenderer), "material._CurrentNo").Const2F(i)
                    .Color("Display", typeof(MeshRenderer), "material._TextColor", tgt.TextColor)
                    .Color("Display", typeof(MeshRenderer), "material._OutlineColor", tgt.OutlineColor);


                var rootState = tgt.IsRemote ? remoteState : localState;
                string usedParamName = $"{tgt.ParameterName}IsUsed";
                ab.SetCurrentStateMachine(tgt.IsRemote ? "Remote" : "Local");

                


                //-----------------Appear-----------------
                ab.AddState($"Appear{i}").SetMotion(ac.Outp($"Appear{i}"));
                ab.SetParameterDriver(usedParamName, 1);

                //if (tgt.ConditionMode == AnimatorConditionMode.Equals)
                //{
                //    ab.TransToCurrent(root)
                //        .AddCondition(AnimatorConditionMode.Greater, tgt.ParameterValue - 0.00001f, tgt.ParameterName).MoveInstant()
                //        .AddCondition(AnimatorConditionMode.Less, tgt.ParameterValue + 0.00001f, tgt.ParameterName).MoveInstant();
                //    if (tgt.InactiveByParameter)
                //    {
                //        ab.TransFromCurrent(root).AddCondition(AnimatorConditionMode.Less, tgt.ParameterValue - 0.00001f, tgt.ParameterName).MoveInstant();
                //        ab.TransFromCurrent(root).AddCondition(AnimatorConditionMode.Greater, tgt.ParameterValue + 0.00001f, tgt.ParameterName).MoveInstant();
                //    }
                //}

                if (tgt.ConditionMode == AnimatorConditionMode.If)
                {
                    ab.TransToCurrent(rootState)
                        .AddCondition(AnimatorConditionMode.Greater, 0.5f, tgt.ParameterName)
                        .AddCondition(AnimatorConditionMode.IfNot, 0, usedParamName);
                    if(tgt.InactiveByParameter)
                    {
                        ab.TransFromCurrent(rootState)
                            .AddCondition(AnimatorConditionMode.Less, 0.5f, tgt.ParameterName);
                    }
                }
                ab.TransFromCurrent(rootState, hasExitTime: true);




                //-----------------Reset-----------------

                ab.AddState($"Reset{i}").SetParameterDriver(usedParamName, 0);
                if (tgt.ConditionMode == AnimatorConditionMode.If)
                {
                    ab.TransToCurrent(rootState)
                        .AddCondition(AnimatorConditionMode.Less, 0.5f, tgt.ParameterName)
                        .AddCondition(AnimatorConditionMode.If, 0, usedParamName);
                }

                ab.TransFromCurrent(rootState).MoveInstant();






                mb.AddToggle(tgt.ParameterName, 1, ParameterSyncType.Bool);
            }


            ac.Clip("s0").Bind("Display", typeof(MeshRenderer), "material._Size").Const2F(0);
            ac.Clip("s1").Bind("Display", typeof(MeshRenderer), "material._Size").Const2F(1);
            var bb = new BlendTreeBuilder("size");
            bb.RootDBT(() => {
                bb.Param("1").Add1D("menusize", () =>
                {
                    bb.Param(0).AddMotion(ac.Outp("s0"));
                    bb.Param(1).AddMotion(ac.Outp("s1"));
                });
            });
            mb.AddRadial("menusize", defaultVal: 1);


            ab.Attach(MsgRoot);
            bb.Attach(MsgRoot);
        }

        private void CreateImage()
        {
            int padding = 3;
            int width = 2048;
            float heightRatio = 1 / 8f;
            int height = (int)(width * heightRatio);
            int y = (width - height) / 2;
            List<Texture2D> msgTexs = new List<Texture2D>();
            using (var c = new PanCapture(Color.black, padding: padding, width: width))
            {
                foreach (PVMessageUI tgt in targets)
                {
                    msgTexs.Add(c.TextToImage(tgt.Message.PadString(100), Color.white).Trim(0, y));
                }
            }
            Texture2D combinedTexture = CombineTexturesVertically(msgTexs);
            var path = OutpAsset(combinedTexture);
            SetTextureImportSettings(path);

            // Set combinedTexture to the MainTexture of the MeshRenderer's 0th material in "Display" object
            var displayObj = MsgRoot.transform.Find("Display");
            if (displayObj != null)
            {
                var meshRenderer = displayObj.GetComponent<MeshRenderer>();
                if (meshRenderer != null && meshRenderer.materials.Length > 0)
                {
                    meshRenderer.materials[0].mainTexture = combinedTexture;
                }
            }
        }


        private Texture2D CombineTexturesVertically(List<Texture2D> textures)
        {
            if (textures == null || textures.Count == 0)
            {
                Debug.LogError("No textures to combine.");
                return null;
            }

            int width = textures[0].width;
            int totalHeight = textures.Sum(tex => tex.height);

            Texture2D combinedTexture = new Texture2D(width, totalHeight, TextureFormat.RGBA32, false);
            int offsetY = 0;

            // テクスチャを逆順に結合
            for (int i = textures.Count - 1; i >= 0; i--)
            {
                Texture2D tex = textures[i];
                Color[] pixels = tex.GetPixels();
                combinedTexture.SetPixels(0, offsetY, tex.width, tex.height, pixels);
                offsetY += tex.height;
            }

            combinedTexture.Apply();
            return combinedTexture;
        }

        private void SetTextureImportSettings(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {

                importer.textureType = TextureImporterType.Default;
                importer.textureShape = TextureImporterShape.Texture2D;
                importer.sRGBTexture = false;
                importer.alphaSource = TextureImporterAlphaSource.None;
                importer.alphaIsTransparency = false;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.isReadable = true;
                importer.streamingMipmaps = true;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.filterMode = FilterMode.Bilinear;
                importer.maxTextureSize = 8192; // Set to the maximum size supported by Unity
                importer.textureCompression = TextureImporterCompression.CompressedLQ;
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
        }



        private void PrefabInstantiate()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Packages/com.github.pandrabox.pandravase/Assets/MessageUI/MessageUI.prefab");
            MsgRoot = GameObject.Instantiate(prefab);
            MsgRoot.transform.SetParent(_prj.PrjRootObj.transform);
        }


    }

}