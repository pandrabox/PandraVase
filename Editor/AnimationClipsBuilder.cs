using com.github.pandrabox.pandravase.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using static com.github.pandrabox.pandravase.editor.Util;

/* Sample *
namespace com.github.pandrabox.pandravase.editor
{
    public class AnimationClipsBuilderSample
    {
        private void sample()
        {
            var ab = new AnimationClipsBuilder(); // 基本的には引数なしで呼び出します

            //On,Offのサンプル
            ab.CreateToggleAnim("Armature/Hips/Acc"); // これは省略することもできます
            AnimationClip onAnim = ab.OnAnim("Armature/Hips/Acc"); //上を省略すると、ここで生成して返します。生成順の問題でDebugOutpは失敗します　（DebugモードではOutp時にファイル出力します）
            //////------もはや事前宣言は不要になりました(20250209)---------///////////
            AnimationClip offAnim = ab.OffAnim("Armature/Hips/Acc");

            //AAPのサンプル
            var abForAAP = new AnimationClipsBuilder("MyProject"); // AAPを使うときはsuffixを指定してください
            abForAAP.CreateAAP("Param1", 1, "Inverce1", 0); // これも省略できます
            AnimationClip aapAnim = abForAAP.AAP("Param1", 1, "Inverce1", 0); // これが本来の書き方ですが、多くの場合はBlendTreeBuilderからAddAAPで呼び出すでしょう。

            //一般的なサンプル 一定値のAnimationを定義、2Fの場合
            ab.Clip("Const2FSample").Bind("Armature/Hips/Collider", typeof(Transform), "m_LocalPosition.z").Const2F(3);

            //一般的なサンプル 直線変動のAnimationを定義
            ab.Clip("LinerSmaple").Bind("Armature/Hips/Collider", typeof(Transform), "m_LocalPosition.y").Liner(0, 0, 10 / FPS, 1);

            //一般的なサンプル スムーズなAnimationを定義
            ab.Clip("SmoothSample").Bind("Armature/Hips/Collider", typeof(Transform), "m_LocalPosition.x").Smooth(0, 0, 1, 1, 2, 2, 3, 2, 4, 0);

            //一般的なサンプル Animationの読み取り
            AnimationClip Const2FAnim = ab.Outp("Const2FSample");
        }
    }
}
/* */

namespace com.github.pandrabox.pandravase.editor
{
    /// <summary>
    /// 複数のAnimationClipを生成・ロード・保存するクラス
    /// </summary>
    public class AnimationClipsBuilder
    {
        public Dictionary<string, AnimationClipBuilder> AnimationClipBuilders;
        public PandraProject Prj;
        private string _currentCripName;
        public AnimationClipBuilder CurrentClip => AnimationClipBuilders[_currentCripName];

        /// <summary>
        /// 複数のAnimationClipを生成・ロード・保存するクラス
        /// </summary>
        /// <param name="suffix">AAPの生成をする場合、suffix</param>
        public AnimationClipsBuilder(PandraProject prj) => Init(prj);
        public AnimationClipsBuilder(string suffix) => Init(new PandraProject(suffix));
        public AnimationClipsBuilder() => Init(new PandraProject());
        private void Init(PandraProject prj)
        {
            Prj = prj;
            AnimationClipBuilders = new Dictionary<string, AnimationClipBuilder>();
        }

        /// <summary>
        /// Create Clip
        /// </summary>
        /// <param name="clipName">クリップ名</param>
        /// <returns>Builder</returns>
        public AnimationClipBuilder Clip(string clipName)
        {
            if (!AnimationClipBuilders.ContainsKey(clipName))
            {
                AnimationClipBuilders[clipName] = new AnimationClipBuilder(clipName);
            }
            _currentCripName = clipName;
            return AnimationClipBuilders[clipName];
        }

        /// <summary>
        /// Output AnimationClip
        /// </summary>
        /// <param name="clipName">クリップ名</param>
        /// <returns>クリップ</returns>
        public AnimationClip Outp(string clipName)
        {
            if (!AnimationClipBuilders.ContainsKey(clipName))
            {
                return null;
            }
            return AnimationClipBuilders[clipName].Outp();
        }

        /// <summary>
        /// すべての定義済みAnimationClipを保存する
        /// </summary>
        public void DebugSave()
        {
            foreach (var pair in AnimationClipBuilders)
            {
                Prj.DebugOutp(pair.Value.Outp());
            }
        }

        /////////////////////////ObjectOnOff関連の既定関数/////////////////////////
        /// <summary>
        /// Object Toggle Animの生成
        /// </summary>
        private string ToggleOnName(string relativePath) => $@"o{SanitizeStr(relativePath)}";
        private string ToggleOffName(string relativePath) => $@"x{SanitizeStr(relativePath)}";
        public void CreateToggleAnim(GameObject target, GameObject relativeRoot) => CreateToggleAnim(GetRelativePath(relativeRoot, target));
        public void CreateToggleAnim(Transform target, GameObject relativeRoot) => CreateToggleAnim(GetRelativePath(relativeRoot, target));
        public void CreateToggleAnim(GameObject target, Transform relativeRoot) => CreateToggleAnim(GetRelativePath(relativeRoot, target));
        public void CreateToggleAnim(Transform target, Transform relativeRoot) => CreateToggleAnim(GetRelativePath(relativeRoot, target));
        public void CreateToggleAnim(string relativePath)
        {
            Clip(ToggleOnName(relativePath)).Bind(relativePath, typeof(GameObject), "m_IsActive").Const2F(1);
            Clip(ToggleOffName(relativePath)).Bind(relativePath, typeof(GameObject), "m_IsActive").Const2F(0);
        }

        /// <summary>
        /// Object Toggle Animの取得
        /// </summary>
        public AnimationClip OnAnim(GameObject target, GameObject relativeRoot) => OnAnim(GetRelativePath(relativeRoot, target));
        public AnimationClip OnAnim(Transform target, GameObject relativeRoot) => OnAnim(GetRelativePath(relativeRoot, target));
        public AnimationClip OnAnim(GameObject target, Transform relativeRoot) => OnAnim(GetRelativePath(relativeRoot, target));
        public AnimationClip OnAnim(Transform target, Transform relativeRoot) => OnAnim(GetRelativePath(relativeRoot, target));
        public AnimationClip OnAnim(string relativePath) => OutpToggle(true, relativePath);
        public AnimationClip OffAnim(GameObject target, GameObject relativeRoot) => OffAnim(GetRelativePath(relativeRoot, target));
        public AnimationClip OffAnim(Transform target, GameObject relativeRoot) => OffAnim(GetRelativePath(relativeRoot, target));
        public AnimationClip OffAnim(GameObject target, Transform relativeRoot) => OffAnim(GetRelativePath(relativeRoot, target));
        public AnimationClip OffAnim(Transform target, Transform relativeRoot) => OffAnim(GetRelativePath(relativeRoot, target));
        public AnimationClip OffAnim(string relativePath) => OutpToggle(false, relativePath);
        public AnimationClip OutpToggle(bool isOn, string relativePath)
        {
            var name = isOn ? ToggleOnName(relativePath) : ToggleOffName(relativePath);
            var clip = Outp(name);
            if (clip == null)
            {
                CreateToggleAnim(relativePath);
                clip = Outp(name);
                //Prj.DebugPrint($@"ToggleAnim({name})を未定義で呼び出したためインスタント生成しました。DebugSave対象外です。"); 埋め込むようにしたので大丈夫
            }
            return clip;
        }

        /////////////////////////AAP関連の既定関数/////////////////////////
        /// <summary>
        /// AAPを生成する
        /// </summary>
        /// <param name="args">偶引数にAAP名、奇引数に定義値を交互入力</param>
        public void CreateAAP(params object[] args)
        {
            var ab = Clip(AAPName(args));
            for (int i = 0; i < args.Length; i += 2)
            {
                if (args[i + 1] is IConvertible)
                {
                    ab.Bind("", typeof(Animator), $@"{args[i]}").Const2F(Convert.ToSingle(args[i + 1]));
                }
                else
                {
                    throw new InvalidCastException($"The value for {args[i]} cannot be converted to float.");
                }
            }
        }

        /// <summary>
        /// AAPを返す
        /// </summary>
        /// <param name="args">偶引数にAAP名、奇引数に定義値を交互入力</param>
        /// <returns>AAP Clip</returns>
        public AnimationClip AAP(params object[] args)
        {
            var name = AAPName(args);
            var clip = Outp(name);
            if (clip == null)
            {
                CreateAAP(args);
                clip = Outp(name);
                //Prj.DebugPrint($@"ToggleAnim({name})を未定義で呼び出したためインスタント生成しました。DebugSave対象外です。");埋め込むようにしたので大丈夫
            }
            return clip;
        }

        /// AAP名を返す(内部呼び出し用)
        private string AAPName(params object[] args)
        {
            string name = "";
            for (int i = 0; i < args.Length; i += 2)
            {
                string paramName = Prj.GetParameterName((string)args[i]);
                float val = Convert.ToSingle(args[i + 1]);
                name = $@"{name}{paramName}{val}";
            }
            return name;
        }
    }
}
