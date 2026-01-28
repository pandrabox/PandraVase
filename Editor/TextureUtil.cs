using com.github.pandrabox.pandravase.runtime;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static com.github.pandrabox.pandravase.editor.Util;

namespace com.github.pandrabox.pandravase.editor
{
    public static class TextureUtil
    {

#if PANDRADBG
        public class TextureUtilDebug
        {
            [MenuItem("PanDbg/TextureUtil/PackTexture")]
            public static void FaceMaker_Debug()
            {
                SetDebugMode(true);
                Texture2D[] testTex = new Texture2D[6];
                for (int i = 0; i < 6; i++)
                {
                    testTex[i] = new Texture2D(256, 256);
                    Color color = new Color(i / 5f, 0, 0);
                    Color[] pixels = Enumerable.Repeat(color, 256 * 256).ToArray();
                    testTex[i].SetPixels(pixels);
                    testTex[i].Apply();
                    OutpAsset(testTex[i]);
                }
                int x = 4;
                var pack = PackTexture(testTex, x, 256 * x, 256 * 2, Color.green, true, 6);
                OutpAsset(pack);
            }
        }
#endif

        /// <summary>
        /// 複数のTexture(正方形)をパッキング
        /// </summary>
        /// <param name="textures">入力テクスチャ</param>
        /// <param name="columns">折り返し列数</param>
        /// <param name="tileWidth">出力画像幅</param>
        /// <param name="tileHeight">出力画像高さ（省略時、正方形）</param>
        /// <param name="baseColor">背景色（省略時透明）</param>
        /// <param name="margin">マージン幅(2以上の偶数、省略時0)</param>
        /// <returns></returns>
        public static Texture2D PackTexture(List<Texture2D> textures, int columns, int tileWidth, int tileHeight = -1, Color? baseColor = null, bool blend = false, int margin = 0)
            => PackTexture(textures.ToArray(), columns, tileWidth, tileHeight, baseColor, blend, margin);
        public static Texture2D PackTexture(Texture2D[] textures, int columns, int tileWidth, int tileHeight = -1, Color? baseColor = null, bool blend = false, int margin = 0)
        {
            Log.I.Info($"Packing textures: {textures.Length} textures, {columns} columns, {tileWidth}x{tileHeight}");
            Color backColor = baseColor ?? new Color(1, 1, 1, 0);
            int unitSize = tileWidth / columns;
            SetReadable(textures);
            textures = AddMargins(textures, margin / 2, backColor);
            ResizeTextures(textures, unitSize, unitSize);
            if (tileHeight == -1) tileHeight = tileWidth;
            Texture2D tileTexture = new Texture2D(tileWidth, tileHeight);
            Color[] colors = Enumerable.Repeat(backColor, tileWidth * tileHeight).ToArray();
            tileTexture.SetPixels(colors);
            for (int i = 0; i < textures.Length; i++)
            {
                int x = i % columns * unitSize;
                int y = tileHeight - ((i / columns + 1) * unitSize);
                Color[] pixels = textures[i].GetPixels();
                if (!blend)
                {
                    tileTexture.SetPixels(x, y, unitSize, unitSize, pixels);
                }
                else
                {
                    for (int px = 0; px < unitSize; px++)
                    {
                        for (int py = 0; py < unitSize; py++)
                        {
                            Color srcColor = pixels[py * unitSize + px];
                            Color dstColor = tileTexture.GetPixel(x + px, y + py);
                            Color blendedColor = Color.Lerp(dstColor, srcColor, srcColor.a);
                            tileTexture.SetPixel(x + px, y + py, blendedColor);
                        }
                    }
                }
            }
            tileTexture.wrapMode = TextureWrapMode.Clamp;
            tileTexture.Apply();
            tileTexture = AddMargin(tileTexture, margin / 2, backColor);
            
            // Save to file and enable mip streaming for VRC SDK compatibility
            string tempPath = "Assets/_PandraVase_Temp_PackedTexture.png";
            byte[] bytes = tileTexture.EncodeToPNG();
            System.IO.File.WriteAllBytes(tempPath, bytes);
            UnityEditor.AssetDatabase.ImportAsset(tempPath, UnityEditor.ImportAssetOptions.ForceUpdate);
            
            UnityEditor.TextureImporter importer = UnityEditor.AssetImporter.GetAtPath(tempPath) as UnityEditor.TextureImporter;
            if (importer != null)
            {
                importer.streamingMipmaps = true;
                importer.mipmapEnabled = true;
                importer.SaveAndReimport();
            }
            
            Texture2D result = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(tempPath);
            return result;
        }

        /// <summary>
        /// テクスチャを保存する
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="path"></param>
        public static void SaveTexture(Texture2D texture, string path)
        {
            byte[] bytes = texture.EncodeToPNG();
            File.WriteAllBytes(path, bytes);
            AssetDatabase.Refresh();
            Log.I.Info("Texture saved at: " + path);
        }

        /// <summary>
        /// Texture複数に同じマージンを付与
        /// </summary>
        /// <param name="textures"></param>
        /// <param name="margin"></param>
        /// <param name="color"></param>
        /// <returns></returns>
        public static Texture2D[] AddMargins(Texture2D[] textures, int margin, Color? color = null)
        {
            for (int i = 0; i < textures.Length; i++)
            {
                textures[i] = AddMargin(textures[i], margin, color);
                //OutpAsset(textures[i]);
            }
            return textures;
        }

        /// <summary>
        /// Textureにマージンを付与
        /// </summary>
        /// <param name="texture">元テクスチャ</param>
        /// <param name="margin">マージン幅</param>
        /// <param name="color">マージン色（省略時、テクスチャ左上の色）</param>
        /// <returns></returns>
        public static Texture2D AddMargin(Texture2D texture, int margin, Color? color = null)
        {
            if (color == null) color = texture.GetPixel(0, texture.height - 1);
            Texture2D mergedTexture = new Texture2D(texture.width + margin * 2, texture.height + margin * 2);
            mergedTexture.SetPixels(Enumerable.Repeat((Color)color, mergedTexture.width * mergedTexture.height).ToArray());
            mergedTexture.SetPixels(margin, margin, texture.width, texture.height, texture.GetPixels());
            mergedTexture.Apply();
            return mergedTexture;
        }

        /// <summary>
        /// 入力テクスチャすべてを指定サイズにリサイズ
        /// </summary>
        /// <param name="textures">入力テクスチャ</param>
        /// <param name="width">出力幅</param>
        /// <param name="height">出力高さ（省略時、縦横比を維持）</param>
        /// <returns></returns>
        private static Texture2D[] ResizeTextures(Texture2D[] textures, int width, int height = -1)
        {
            for (int i = 0; i < textures.Length; i++)
            {
                textures[i] = ResizeTexture(textures[i], width, height);
            }
            return textures;
        }

        /// <summary>
        /// 入力テクスチャを指定サイズにリサイズ
        /// </summary>
        /// <param name="texture">入力テクスチャ</param>
        /// <param name="width">出力幅</param>
        /// <param name="height">出力高さ（省略時、縦横比を維持）</param>
        /// <returns></returns>
        public static Texture2D ResizeTexture(Texture2D texture, int width, int height = -1)
        {
            if (height == -1) height = (int)(texture.height * (float)width / texture.width);
            RenderTexture rt = RenderTexture.GetTemporary(width, height);
            rt.filterMode = FilterMode.Bilinear;
            RenderTexture.active = rt;
            Graphics.Blit(texture, rt);
            Texture2D resizedTexture = new Texture2D(width, height);
            resizedTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            resizedTexture.Apply();
            RenderTexture.ReleaseTemporary(rt);
            return resizedTexture;
        }

        /// <summary>
        /// 入力テクスチャすべてをReadableに設定する
        /// </summary>
        /// <param name="textures">入力テクスチャ</param>
        /// <returns></returns>
        public static Texture2D[] SetReadable(Texture2D[] textures)
        {
            for (int i = 0; i < textures.Length; i++)
            {
                textures[i] = SetReadable(textures[i]);
            }
            return textures;
        }

        /// <summary>
        /// 指定されたテクスチャをReadableに設定する
        /// </summary>
        /// <param name="texture">対象のテクスチャ</param>
        /// <returns>修正後のテクスチャを返す</returns>
        public static Texture2D SetReadable(Texture2D texture)
        {
            string texturePath = AssetDatabase.GetAssetPath(texture);
            TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if (importer == null) return texture;
            if (!importer.isReadable)
            {
                importer.isReadable = true;
                AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceUpdate);
            }
            return texture;
        }

        /// <summary>
        /// テクスチャに矩形を描画（このあとtexture.Apply()が必要）
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="color">色　省略時白</param>
        /// <param name="width">幅　省略時テクスチャ幅</param>
        /// <param name="height">高さ　省略時正方形</param>
        /// <param name="x">矩形左上座標 省略時0</param>
        /// <param name="y">矩形左上座標 省略時x</param>
        public static void DrawRect(this Texture2D texture, Color? color = null, int x = 0, int y = 0, int width = -1, int height = -1)
        {
            if (width == -1) width = texture.width;
            if (height == -1) height = texture.height;
            Color fillColor = color ?? Color.clear;
            Color[] fillPixels = Enumerable.Repeat(fillColor, width * height).ToArray();
            texture.SetPixels(x, y, width, height, fillPixels);
            texture.Apply();
        }

        /// <summary>
        /// テクスチャをトリムする
        /// </summary>
        /// <param name="texture">入力</param>
        /// <param name="x">切り出しx座標</param>
        /// <param name="y">切り出しy座標</param>
        /// <param name="width">切り出し幅（省略時、テクスチャ幅-x*2）</param>
        /// <param name="height">切り出し高さ（省略時、テクスチャ高さ-y*2）</param>
        /// <returns></returns>
        public static Texture2D Trim(this Texture2D texture, int x, int y, int width = -1, int height = -1)
        {
            if (width == -1) width = texture.width - x * 2;
            if (height == -1) height = texture.height - y * 2;
            Log.I.Info($"Trimming texture: {texture.width}x{texture.height} -> {width}x{height}");
            Texture2D trimmedTexture = new Texture2D(width, height);
            Color[] pixels = texture.GetPixels(x, y, width, height);
            trimmedTexture.SetPixels(pixels);
            trimmedTexture.Apply();
            return trimmedTexture;
        }
    }
}