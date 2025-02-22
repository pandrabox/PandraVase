
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
        public PVMessageUIPassMain(VRCAvatarDescriptor desc)
        {
            var tgt = desc.transform.GetComponentInChildren<PVMessageUI>();
            if (tgt == null) return;
            _prj = VaseProject(desc);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Packages/com.github.pandrabox.pandravase/Assets/MessageUI/MessageUI.prefab");
            var go = GameObject.Instantiate(prefab);
            go.transform.SetParent(_prj.PrjRootObj.transform);

            var ac = new AnimationClipsBuilder();
            ac.Clip("off").Bind("Display", typeof(MeshRenderer), "material._CurrentNo").Const2F(0)
                        .Bind("Display", typeof(GameObject), "m_IsActive").Const2F(0);
            var ab = new AnimatorBuilder("MessageUI");
            ab.AddLayer().AddState("Local").SetMotion(ac.Outp("off")).TransToCurrent(ab.InitialState).AddCondition(AnimatorConditionMode.If, 0, "IsLocal");
            if (true)
            {
                LowLevelDebugPrint("注意：デバッグのために全体表示してます");
                ab.TransToCurrent(ab.InitialState).AddCondition(AnimatorConditionMode.IfNot, 0, "IsLocal");
            }
            var localState = ab.CurrentState;


            var mb = new MenuBuilder(_prj).AddFolder("MessageUI");
            for (int i = 0; i < 5; i++)
            {
                ac.Clip($"m{i}").Bind("Display", typeof(MeshRenderer), "material._CurrentNo").Smooth(0f, (float)i, 3f, (float)i)
                            .Bind("Display", typeof(GameObject), "m_IsActive").Smooth(0f, 1f, 3f, 1f);
                ab.AddState($"m{i}").SetMotion(ac.Outp($"m{i}")).TransToCurrent(localState).AddCondition(AnimatorConditionMode.If, 0, $"IsM{i}", true);
                mb.AddToggle($"IsM{i}", 1, ParameterSyncType.Bool, localOnly: false);
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
            mb.AddRadial("menusize",defaultVal:1,localOnly:false);


            ab.Attach(go);
            bb.Attach(go);


        }
    }
    public class MsgTexture
    {

        public MsgTexture()
        {
            int padding = 3;
            int width = 2048;
            float heightRatio = 1/8f;
            int height = (int)(width * heightRatio);
            int y = (width - height) / 2;
            List<Texture2D> msgTexs = new List<Texture2D>();
            using (var c = new PanCapture(Color.black, padding: padding, width: width))
            {
                msgTexs.Add(c.TextToImage("Hello, World!".PadString(100), Color.white).Trim(0, y));
                msgTexs.Add(c.TextToImage("こんにちは、世界!".PadString(100), Color.white).Trim(0, y));
                msgTexs.Add(c.TextToImage("안녕하세요, 세계!".PadString(100), Color.white).Trim(0, y));
                msgTexs.Add(c.TextToImage("你好，世界!".PadString(100), Color.white).Trim(0, y));
                msgTexs.Add(c.TextToImage("Hello, World!".PadString(100), Color.white).Trim(0, y));
                msgTexs.Add(c.TextToImage("こんにちは、世界!".PadString(100), Color.white).Trim(0, y));
                msgTexs.Add(c.TextToImage("안녕하세요, 세계!".PadString(100), Color.white).Trim(0, y));
                msgTexs.Add(c.TextToImage("你好，世界!".PadString(100), Color.white).Trim(0, y));
                msgTexs.Add(c.TextToImage("Hello, World!".PadString(100), Color.white).Trim(0, y));
                msgTexs.Add(c.TextToImage("こんにちは、世界!".PadString(100), Color.white).Trim(0, y));
                msgTexs.Add(c.TextToImage("안녕하세요, 세계!".PadString(100), Color.white).Trim(0, y));
                msgTexs.Add(c.TextToImage("你好，世界!".PadString(100), Color.white).Trim(0, y));
            }

            Texture2D combinedTexture = CombineTexturesVertically(msgTexs);
            var path = OutpAsset(combinedTexture);
            SetTextureImportSettings(path);


            AssetDatabase.Refresh();
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

    }



}