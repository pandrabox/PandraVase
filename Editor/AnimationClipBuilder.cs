using com.github.pandrabox.pandravase.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using static com.github.pandrabox.pandravase.editor.Util;


namespace com.github.pandrabox.pandravase.editor
{
    /// <summary>
    /// 単体のAnimationClipを生成するクラス。基本的にはAnimationClipsBuilderから呼び出してください
    /// </summary>
    public class AnimationClipBuilder
    {
        private AnimationClip _clip;
        private EditorCurveBinding _curveBinding;
        
        /// <summary>
        /// 単体のAnimationClipを生成する。基本的にはAnimationClipsBuilderから呼び出してください
        /// </summary>
        public AnimationClipBuilder(string clipName)
        {
            _clip = new AnimationClip();
            _clip.name = clipName;
        }

        /// <summary>
        /// Bindingの定義
        /// </summary>
        /// <param name="inPath">Bind対象の相対パス</param>
        /// <param name="inType">Bind対象のタイプ</param>
        /// <param name="inPropertyName">Bind対象のプロパティ名</param>
        /// <param name="curveType">Bind対象のタイプ</param>
        /// <returns>this</returns>
        public AnimationClipBuilder Bind(GameObject target, GameObject relativeRoot, Type inType, string inPropertyName) => Bind(GetRelativePath(relativeRoot, target), inType, inPropertyName);
        public AnimationClipBuilder Bind(Transform target, GameObject relativeRoot, Type inType, string inPropertyName) => Bind(GetRelativePath(relativeRoot, target), inType, inPropertyName);
        public AnimationClipBuilder Bind(GameObject target, Transform relativeRoot, Type inType, string inPropertyName) => Bind(GetRelativePath(relativeRoot, target), inType, inPropertyName);
        public AnimationClipBuilder Bind(Transform target, Transform relativeRoot, Type inType, string inPropertyName) => Bind(GetRelativePath(relativeRoot, target), inType, inPropertyName);
        public AnimationClipBuilder Bind(string inPath, Type inType, string inPropertyName)
        {
            _curveBinding = EditorCurveBinding.FloatCurve(inPath, inType, inPropertyName);
            return this;
        }

        /// <summary>
        /// 直前に定義したBindに基づきLinerカーブをセット
        /// </summary>
        /// <param name="time1">key1の時間</param>
        /// <param name="val1">key1の値</param>
        /// <param name="time2">key2の時間</param>
        /// <param name="val2">key2の値</param>
        /// <returns>this</returns>
        public AnimationClipBuilder Liner(float time1, float val1, float time2, float val2)
        {
            if (_curveBinding == null) LowLevelDebugPrint("呼び出し順序が不正です。　事前に「Bind」してください。");
            AnimationCurve curve = AnimationCurve.Linear(time1, val1, time2, val2);
            AnimationUtility.SetEditorCurve(_clip, _curveBinding, curve);
            return this;
        }

        /// <summary>
        /// 直前に定義したBindに基づきSmoothカーブをセット
        /// </summary>
        /// <param name="keyPairs">偶数要素にキーの時間、奇数要素にキーの値</param>
        /// <returns>this</returns>
        public AnimationClipBuilder Smooth(params float[] keyPairs)
        {
            if(_curveBinding==null) LowLevelDebugPrint("呼び出し順序が不正です。　事前に「Bind」してください。");
            var keys = new List<Keyframe>();
            for (int i = 0; i < keyPairs.Length - 1; i += 2)
            {
                Keyframe k = new Keyframe(keyPairs[i], keyPairs[i + 1]);
                keys.Add(k);
            }
            AnimationCurve curve = new AnimationCurve(keys.ToArray());
            AnimationUtility.SetEditorCurve(_clip, _curveBinding, curve);
            return this;
        }

        /// <summary>
        /// 直前に定義したBindに基づき2Fの定数カーブをセット
        /// </summary>
        /// <param name="val">定数</param>
        /// <returns>this</returns>
        public AnimationClipBuilder Const2F(float val)
        {
            if (_curveBinding == null) LowLevelDebugPrint("呼び出し順序が不正です。　事前に「Bind」してください。");
            AnimationCurve curve = AnimationCurve.Constant(0, 1 / FPS, val);
            AnimationUtility.SetEditorCurve(_clip, _curveBinding, curve);
            return this;
        }

        /// <summary>
        /// AnimationClipを出力
        /// </summary>
        /// <returns>出力</returns>
        public AnimationClip Outp()
        {
            return _clip;
        }

        /// <summary>
        /// ループを設定
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public AnimationClipBuilder SetLoop(bool key)
        {
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(_clip);
            settings.loopTime = key;
            AnimationUtility.SetAnimationClipSettings(_clip, settings);
            return this;
        }
    }
}
