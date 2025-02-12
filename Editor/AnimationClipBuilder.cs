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
        private bool _compositing = false;
        private int _compositeMode = 0;
        private string _axisId = null;
        private Axis _axis;
        private string axisName => _axis.ToString().ToLower();

        /// <summary>
        /// 単体のAnimationClipを生成する。基本的にはAnimationClipsBuilderから呼び出してください
        /// </summary>
        public AnimationClipBuilder(string clipName)
        {
            _clip = new AnimationClip();
            _clip.name = clipName;
        }

        public AnimationClipBuilder IsVector3(Action<AnimationClipBuilder> a, string axisId="@a")
        {
            _compositing= true;
            _axisId = axisId;
            for (int i = 0; i < 3; i++)
            {
                _axis=(Axis)i;
                a(this);
            }
            _compositing = false;
            return this;
        }
        public AnimationClipBuilder IsQuaternion(Action<AnimationClipBuilder> a, string axisId = "@a")
        {
            _compositing = true;
            _axisId = axisId;
            for (int i = 0; i < 4; i++)
            {
                _axis = (Axis)i;
                a(this);
            }
            _compositing = false;
            return this;
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
            if(_compositing) inPropertyName = inPropertyName.Replace("@a", axisName);
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
        public AnimationClipBuilder Liner(float time1, Vector3 val1, float time2, float val2) => Liner(time1, val1.GetAxis(_axis), time2, val2);
        public AnimationClipBuilder Liner(float time1, float val1, float time2, Vector3 val2) => Liner(time1, val1, time2, val2.GetAxis(_axis));
        public AnimationClipBuilder Liner(float time1, Vector3 val1, float time2, Vector3 val2) => Liner(time1, val1.GetAxis(_axis), time2, val2.GetAxis(_axis));
        public AnimationClipBuilder Liner(float time1, Quaternion val1, float time2, float val2) => Liner(time1, val1.GetAxis(_axis), time2, val2);
        public AnimationClipBuilder Liner(float time1, float val1, float time2, Quaternion val2) => Liner(time1, val1, time2, val2.GetAxis(_axis));
        public AnimationClipBuilder Liner(float time1, Quaternion val1, float time2, Quaternion val2) => Liner(time1, val1.GetAxis(_axis), time2, val2.GetAxis(_axis));
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
        public AnimationClipBuilder Smooth(params System.Object[] keyPairs)
        {
            if (_curveBinding == null) LowLevelDebugPrint("呼び出し順序が不正です。　事前に「Bind」してください。");
            var keys = new List<Keyframe>();
            for (int i = 0; i < keyPairs.Length - 1; i += 2)
            {
                float? time=null, value = null;
                if (keyPairs[i] is float t)
                {
                    time = t;
                }
                else
                {
                    LowLevelDebugPrint($@"Smoothキーペアの時間の指定が不正です({keyPairs[i].GetType()})。floatで指定してください。", true, LogType.Exception);
                }
                if (keyPairs[i+1] is float f)
                {
                    value = f;
                }
                if (keyPairs[i+1] is Vector3 v)
                {
                    value = v.GetAxis(_axis);
                }
                if(keyPairs[i + 1] is Quaternion q)
                {
                    value = q.GetAxis(_axis);
                }
                if(time.HasValue && value.HasValue)
                {
                    Keyframe k = new Keyframe(time.Value, value.Value);
                    keys.Add(k);
                }
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
        public AnimationClipBuilder Const2F(Vector3 vals) => Const2F(vals.GetAxis(_axis));
        public AnimationClipBuilder Const2F(Quaternion vals) => Const2F(vals.GetAxis(_axis));
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

        /// <summary>
        /// n番目のキーの接線をFlatに設定
        /// </summary>
        public AnimationClipBuilder SetFlat(int keyIndex)
        {
            if (_curveBinding == null) LowLevelDebugPrint("呼び出し順序が不正です。 事前に「Bind」してください。");
            AnimationCurve curve = AnimationUtility.GetEditorCurve(_clip, _curveBinding);
            if (curve == null || keyIndex < 0 || keyIndex >= curve.length) return this;

            Keyframe key = curve.keys[keyIndex];
            key.inTangent = 0f;
            key.outTangent = 0f;
            curve.MoveKey(keyIndex, key);

            AnimationUtility.SetEditorCurve(_clip, _curveBinding, curve);
            return this;
        }

        /// <summary>
        /// 全てのキーをフラット
        /// </summary>
        public AnimationClipBuilder SetAllFlat()
        {
            AnimationCurve curve = AnimationUtility.GetEditorCurve(_clip, _curveBinding);
            for (int i = 0; i < curve.length; i++)
            {
                SetFlat(i);
            }
            return this;
        }

        /// <summary>
        /// n番目のキーの接線をAutoに設定
        /// </summary>
        public AnimationClipBuilder SetAuto(int keyIndex)
        {
            if (_curveBinding == null) LowLevelDebugPrint("呼び出し順序が不正です。 事前に「Bind」してください。");
            AnimationCurve curve = AnimationUtility.GetEditorCurve(_clip, _curveBinding);
            if (curve == null || keyIndex < 0 || keyIndex >= curve.length) return this;

            curve.SmoothTangents(keyIndex, 0);

            AnimationUtility.SetEditorCurve(_clip, _curveBinding, curve);
            return this;
        }
    }
}
