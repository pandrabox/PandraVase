using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using static com.github.pandrabox.pandravase.editor.Util;

namespace com.github.pandrabox.pandravase.editor
{
    public static class TextureUtil
    {
        /// <summary>
        /// 複数のTexture(正方形)をパッキング
        /// </summary>
        /// <param name="textures">入力テクスチャ</param>
        /// <param name="columns">折り返し列数</param>
        /// <param name="tileWidth">出力画像幅</param>
        /// <param name="tileHeight">出力画像高さ（省略時、正方形）</param>
        /// <returns></returns>
        public static Texture2D PackTexture(List<Texture2D> textures, int columns, int tileWidth, int tileHeight = -1) => PackTexture(textures.ToArray(), columns, tileWidth, tileHeight);
        public static Texture2D PackTexture(Texture2D[] textures, int columns, int tileWidth, int tileHeight = -1)
        {
            int unitSize = tileWidth / columns;
            SetReadable(textures);
            ResizeTextures(textures, unitSize, unitSize);
            if (tileHeight == -1) tileHeight = tileWidth;
            Texture2D tileTexture = new Texture2D(tileWidth, tileHeight);
            Color[] colors = new Color[tileWidth * tileHeight];
            for (int i = 0; i < textures.Length; i++)
            {
                int x = i % columns * unitSize;
                int y = tileHeight - ((i / columns + 1) * unitSize);
                Color[] pixels = textures[i].GetPixels();
                tileTexture.SetPixels(x, y, unitSize, unitSize, pixels);
            }
            tileTexture.wrapMode = TextureWrapMode.Clamp;
            tileTexture.Apply();
            return tileTexture;
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
            LowLevelDebugPrint("Texture saved at: " + path);
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
            LowLevelDebugPrint($"Trimming texture: {texture.width}x{texture.height} -> {width}x{height}");
            Texture2D trimmedTexture = new Texture2D(width, height);
            Color[] pixels = texture.GetPixels(x, y, width, height);
            trimmedTexture.SetPixels(pixels);
            trimmedTexture.Apply();
            return trimmedTexture;
        }
    }
}