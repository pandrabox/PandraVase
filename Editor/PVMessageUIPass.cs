
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
            new MsgTexture();
        }
    }
#endif

    public class MsgTexture
    {
        public MsgTexture()
        {
            int padding = 3;
            int width = 2048;
            float heightRatio = 0.1f;
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
            OutpAsset(combinedTexture);


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


    }



}