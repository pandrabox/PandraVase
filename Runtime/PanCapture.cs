using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;
using static com.github.pandrabox.pandravase.runtime.Util;

namespace com.github.pandrabox.pandravase.runtime
{
    public class PanCapture : IDisposable
    {
        public static string CaptureLayerName = "PanCaptureLayer";
        private Camera _camera;
        private RenderTexture _renderTexture;
        private int _margin = 0, _padding = 0, _width = 1024, _height;
        public int Margin => _margin;
        public int Padding => _padding;
        public int Width => _width;
        public int Height => (_height == -1) ? Width : _height;
        public int MarginedWidth => Width - Margin * 2;
        public int MarginedHeight => Height - Margin * 2;
        public int RenderWidth => MarginedWidth - Padding * 2;
        public int RenderHeight => MarginedHeight - Padding * 2;
        public int RenderOrigin => Margin+Padding;
        private Color _bgColor, _marginColor;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="BGColor">背景色(省略時白透明)</param>
        /// <param name="marginColor">パディングカラー(省略時白透明)</param>
        /// <param name="margin">画像padding(Width-Height内にとる。PaddingColorになる)</param>
        /// <param name="padding">画像内padding(paddingより内側にとる。BGColorになる)</param>
        /// <param name="width">画像横幅(省略時1024)</param>
        /// <param name="height">画像縦幅(省略時正方形)</param>
        public PanCapture(Color? BGColor = null, Color? marginColor = null, int margin = 0, int padding =0, int width = 1024, int height = -1)
        {
            if (height == -1) height = width;
            _camera = CreateComponentObject<Camera>((GameObject)null, "camera");
            PrepareLayer(CaptureLayerName);
            _camera.cullingMask = 1 << LayerMask.NameToLayer(CaptureLayerName);
            SetBackground(BGColor, marginColor);
            _camera.orthographic = true;
            SetSize(margin, padding, width, height);
        }

        /// <summary>
        /// テキストを画像化
        /// </summary>
        /// <param name="text">テキスト</param>
        /// <param name="FontColor">フォントカラー(省略時黒)</param>
        /// <returns></returns>
        public Texture2D TextToImage(string text, Color? FontColor = null)
        {
            GameObject textObj = DrawText(text, FontColor);
            return Run(textObj, true);
        }

        /// <summary>
        /// ゲームオブジェクトを画像化
        /// </summary>
        /// <param name="obj">対象</param>
        /// <returns></returns>
        public Texture2D GameObjectToImage(GameObject obj)
        {
            GameObject copyObj = GameObject.Instantiate(obj);
            return Run(copyObj, true);
        }

        ///-----ここより下はLowLevel------------------
        /// <summary>
        /// 背景色の指定
        /// </summary>
        public void SetBackground(Color? BGColor = null, Color? PaddingColor=null)
        {
            _bgColor = BGColor ?? new Color(1, 1, 1, 0);
            _marginColor = PaddingColor ?? new Color(1, 1, 1, 0);
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = _bgColor;
        }

        /// <summary>
        /// 画像サイズの指定
        /// </summary>
        public void SetSize(int margin = 0, int padding=0, int width = 1024, int height = -1)
        {
            _margin = margin;
            _padding = padding;
            _width = width;
            _height = height;
            if (_renderTexture == null)
            {
                _renderTexture = new RenderTexture(RenderWidth, RenderHeight, 32, RenderTextureFormat.ARGB32);
            }
            else
            {
                _renderTexture.width = RenderWidth;
                _renderTexture.height = RenderHeight;
            }
        }

        /// <summary>
        /// テキストを描画
        /// </summary>
        /// <param name="text">描画テキスト</param>
        /// <param name="fontColor">色　省略時黒</param>
        /// <returns></returns>
        public GameObject DrawText(string text, Color? fontColor = null)
        {
            GameObject textGO = new GameObject("Text");
            textGO.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
            textGO.transform.position = new Vector3(0, 0, 10);
            TextMesh textMesh = textGO.AddComponent<TextMesh>();
            textMesh.text = text;
            textMesh.fontSize = 500;
            textMesh.color = fontColor ?? Color.black;
            textMesh.anchor = TextAnchor.MiddleCenter;
            return textGO;
        }

        /// <summary>
        /// 対象をキャプチャしてTexture2Dを返す
        /// </summary>
        /// <param name="target">対象</param>
        /// <param name="removeTarget">処理後対象削除（省略時false）</param>
        /// <param name="padding">パディング(省略時0)</param>
        /// <param name="width">出力テクスチャサイズ（省略時1024）</param>
        /// <param name="height">出力テクスチャサイズ（省略時横幅合わせ）</param>
        /// <returns></returns>
        public Texture2D Run(GameObject target, bool removeTarget = false)
        {
            try
            {
                //対象だけ映るようにする
                int layer = LayerMask.NameToLayer(CaptureLayerName);
                if (layer == -1)
                {
                    Debug.LogError($"Layer '{CaptureLayerName}' not found.");
                    return null;
                }
                target.layer = layer;

                //対象がぴったり映るようにする
                Bounds bounds = GetEncapsulatedCubeBoundsByGameObject(target);
                _camera.orthographicSize = bounds.size.y / 2f;
                _camera.transform.position = new Vector3(bounds.center.x, bounds.center.y, target.transform.position.z - 10);

                //キャプチャ処理
                _camera.targetTexture = _renderTexture;
                _camera.Render();
                Texture2D texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
                texture.DrawRect(_marginColor);
                texture.DrawRect(width: MarginedWidth, x: Margin);
                RenderTexture.active = _renderTexture;
                texture.ReadPixels(new Rect(0, 0, RenderWidth, RenderHeight), RenderOrigin, RenderOrigin);
                texture.Apply();
                RenderTexture.active = null;
                return texture;
            }
            finally
            {
                if (RenderTexture.active != null) RenderTexture.active = null;
                _renderTexture.Release();
                if (removeTarget && target != null) GameObject.DestroyImmediate(target);
            }
        }

        /// <summary>
        /// クラスの破棄
        /// </summary>
        public void Dispose()
        {
            if (RenderTexture.active != null) RenderTexture.active = null;
            _renderTexture.Release();
            if (_camera != null) GameObject.DestroyImmediate(_camera.gameObject);
        }

        /// <summary>
        /// GameObjectのBoundsを使って立方体を取得
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        private Bounds GetEncapsulatedCubeBoundsByGameObject(GameObject target)
        {
            return GetEncapsulatedCubeBounds(GetObjectBounds(target));
        }

        /// <summary>
        /// GameObjectのBoundsを取得
        /// </summary>
        /// <param name="target">対象</param>
        /// <returns></returns>
        private Bounds GetObjectBounds(GameObject target)
        {
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return new Bounds(target.transform.position, Vector3.zero);
            }
            Bounds bounds = new Bounds(renderers[0].bounds.center, renderers[0].bounds.size);
            foreach (Renderer renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
            }
            return bounds;
        }

        /// <summary>
        /// Boundsのセンターを維持し、最大の辺に合わせた立方体にする
        /// </summary>
        /// <param name="bounds"></param>
        /// <returns></returns>
        private Bounds GetEncapsulatedCubeBounds(Bounds bounds)
        {
            float maxSide = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            Vector3 size = new Vector3(maxSide, maxSide, maxSide);
            return new Bounds(bounds.center, size);
        }

        /// <summary>
        /// レイヤーがなければ作成
        /// </summary>
        /// <param name="layerName"></param>
        private void PrepareLayer(string layerName)
        {
            try
            {
                // Tags and Layers設定を開く
                UnityEngine.Object tagManagerAsset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset").FirstOrDefault();
                if (tagManagerAsset == null)
                {
                    Debug.LogError("TagManager.asset not found.");
                    return;
                }

                SerializedObject tagManager = new SerializedObject(tagManagerAsset);
                SerializedProperty layers = tagManager.FindProperty("layers");

                // レイヤーが既に存在するか確認
                for (int i = 8; i < layers.arraySize; i++)
                {
                    SerializedProperty layer = layers.GetArrayElementAtIndex(i);
                    if (layer.stringValue == layerName)
                    {
                        // レイヤーが既に存在する場合、何もしない
                        return;
                    }
                }

                // まだ使用されていないレイヤー番号を取得
                int layerIndex = -1;
                for (int i = 8; i < layers.arraySize; i++)
                {
                    SerializedProperty layer = layers.GetArrayElementAtIndex(i);
                    if (string.IsNullOrEmpty(layer.stringValue))
                    {
                        layerIndex = i;
                        break;
                    }
                }

                // レイヤー番号が見つからない場合はエラー
                if (layerIndex == -1)
                {
                    Debug.LogError("No available layer to create.");
                    return;
                }

                // レイヤーを作成
                SerializedProperty newLayer = layers.GetArrayElementAtIndex(layerIndex);
                newLayer.stringValue = layerName;

                // 設定を保存
                tagManager.ApplyModifiedProperties();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to prepare layer: {ex.Message}");
            }
        }
    }
}