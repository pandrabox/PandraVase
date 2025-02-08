using com.github.pandrabox.pandravase.runtime;
using nadena.dev.modular_avatar.core;
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
        public class BlendTreeBuilderExec
        {
            public void sample()
            {
                GameObject ExampleObj = null; //実際には存在する何らかのオブジェクト
                bool isAbsolute = false;
                GameObject relativeRoot = null;
                string name = "MyBlendTreeName";
                string suffix = "TestDBT";
                string workFolder = "Packages/com.github.pandrabox.pandravase/"; //実際のパッケージルートパス
                var bb = new BlendTreeBuilder(ExampleObj, isAbsolute, relativeRoot, name, suffix, workFolder);
                bb.RootDBT(() => {
                    bb.Param("1").Add1D("ToggleParameter", () =>
                    {
                        bb.Param(0).AddMotion("InactiveMotion");
                        bb.Param(1).AddMotion("ActiveMotion");
                    });
                });
            }
        }
    }
/* */

namespace com.github.pandrabox.pandravase.editor
{
    public class BlendTreeBuilder
    {
        public List<BlendTree> BuildingTrees;
        public BlendTree RootTree;
        public int CurrentNum = 0;
        public string NextName;
        public bool IsAbsolute;
        public bool IsMMDSafe;
        public GameObject RelativeRoot;
        public string Name;
        public GameObject TargetObject;
        public PandraProject Prj;

        /// <summary>
        /// BlendTreeをビルドする
        /// </summary>
        /// <param name="prj">PandraProject</param>
        /// <param name="isAbsolute">絶対かどうか</param>
        /// <param name="thisTreeName">名称</param>
        /// <param name="relativeRoot">相対ルート</param>
        public BlendTreeBuilder(PandraProject prj, bool isAbsolute, string thisTreeName, GameObject relativeRoot = null)
        {
            IsAbsolute = isAbsolute;
            RelativeRoot = relativeRoot;
            Name = SanitizeStr(thisTreeName);
            TargetObject = prj.CreateObject($@"PMB_{Name}");
            BuildingTrees = new List<BlendTree>() { null, new BlendTree() };
            RootTree = BuildingTrees[1];
            RootTree.name = Name;
            RootTree.blendType = BlendTreeType.Direct;
            OutpAsset(RootTree);

            CurrentNum = 1;
            Prj = prj;
        }
        public int MaxNum => BuildingTrees.Count - 1;
        public BlendTree GetTree(int n) => BuildingTrees[n];
        public BlendTree CurrentTree => BuildingTrees[CurrentNum];
        public BlendTreeType? CurrentType => CurrentTree?.blendType;

        /// <summary>
        /// BlendTree作成を完了し、TargetObjectにアタッチする
        /// </summary>
        public void Apply()
        {
            //Prj.DebugOutp(RootTree);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            PVPanMergeBlendTree PanMBT = TargetObject.AddComponent<PVPanMergeBlendTree>();
            PanMBT.BlendTree = RootTree;
            if (IsAbsolute) PanMBT.PathMode = MergeAnimatorPathMode.Absolute;
            else if (RelativeRoot != null) PanMBT.RelativePathRoot.Set(RelativeRoot);
        }

        /// <summary>
        /// DirectBlendTreeのパラメータ名を設定する
        /// </summary>
        /// <param name="parentBlendTree">設定するツリー</param>
        /// <param name="parameterName">パラメータ名</param>
        /// <param name="setChildNum">子番号(省略時、最終)</param>
        public void SetDirectBlendParameter(BlendTree parentBlendTree, string parameterName, int setChildNum = -1)
        {
            var c = parentBlendTree.children;
            if (setChildNum == -1) setChildNum = c.Length - 1;
            c[setChildNum].directBlendParameter = parameterName;
            parentBlendTree.children = c;
        }

        /// <summary>
        /// parent TreeにchildをDirectとして追加する
        /// </summary>
        /// <param name="parent">親のBlendTree</param>
        /// <param name="child">子のMotion</param>
        /// <param name="parameterName">DirectBlendParameter</param>
        public static void AddDirectChild(BlendTree parent, Motion child, string parameterName = "1")
        {
            if (parent == null)
            {
                throw new ArgumentNullException("blendTree not found");
            }
            if (child == null)
            {
                throw new ArgumentNullException("childTree not found");
            }
            if (string.IsNullOrEmpty(parameterName))
            {
                throw new ArgumentNullException("parameterName not found");
            }
            parent.AddChild(child);
            var c = parent.children;
            c[c.Length - 1].directBlendParameter = parameterName;
            parent.children = c;
        }

        /// <summary>
        /// 次に定義するBlendTreeの名前を指定する
        /// </summary>
        /// <param name="nextName">名称</param>
        /// <returns>作業中のBuilder</returns>
        public BlendTreeBuilder NName(string nextName)
        {
            NextName = nextName;
            return this;
        }

        /// <summary>
        /// RootDBTの前に宣言するとMMDSafe(MMD検出時、RootDBTのWeightが0)になる。
        /// </summary>
        /// <returns></returns>
        public BlendTreeBuilder MMDSafe()
        {
            IsMMDSafe = true;
            return this;
        }

        /// <summary>
        /// 最初のBlendTreeを定義し、確定処理を実施
        /// </summary>
        /// <param name="act">処理</param>
        public void RootDBT(Action act = null)
        {
            if (act != null)
            {
                if (IsMMDSafe)
                {
                    Param("Env/DBTEnable").AddD(() => act());
                    IsMMDSafe = false;
                    Prj.DebugPrint("MMDSafeが指定されています。これはEnvの実装を前提としていますが、自動的には作成しません");
                }
                else
                {
                    act();
                }
            }
            Apply();
        }

        /// <summary>
        /// P命令、C命令によってBlendTreeを接続するときの条件群
        /// </summary>
        public string ParentDirectParameterName;
        public float ParentThreshold, ParentThresholdY;
        public BlendTreeType ParentTreeType;
        public Action ChildAct;
        public string ChildThresholdName, ChildThresholdNameY;
        public Motion ChildMotionClip;
        public BlendTreeType? ChildTreeType;
        private bool _childWait;
        public bool ChildWait
        {
            get => _childWait;
            set
            {
                // Prj.DebugPrint($"ChildWaitを設定しました：{value}",true ,LogType.Log);
                _childWait = value;
            }
        }

        /// <summary>
        /// [P命令] 親ツリーのパラメータ条件をセットする
        /// </summary>
        /// <param name="treeType">親のツリータイプ</param>
        /// <param name="directParameterName">DirectParameterの名称</param>
        /// <param name="threshold">Thresholdの値(1D)</param>
        /// <param name="thresholdY">Thresholdの値(2D)</param>
        /// <returns>作業中のBlendTreeBuilder</returns>
        public BlendTreeBuilder Param(string DirectParameterName) => ParentTreeParameterSet(BlendTreeType.Direct, DirectParameterName, 0, 0);
        public BlendTreeBuilder Param(float Threshold) => ParentTreeParameterSet(BlendTreeType.Simple1D, null, Threshold, 0);
        public BlendTreeBuilder Param(float Threshold, float ThresholdY) => ParentTreeParameterSet(BlendTreeType.SimpleDirectional2D, null, Threshold, ThresholdY);
        public BlendTreeBuilder ParentTreeParameterSet(BlendTreeType treeType, string directParameterName, float threshold, float thresholdY)
        {
            if (ChildWait) Prj.DebugPrint("ChildWaitが設定されています。P命令を使ったらすぐにC命令をつかうべきです。", false);
            ParentTreeType = treeType;
            ParentDirectParameterName = Prj.GetParameterName(directParameterName);
            ParentThreshold = threshold;
            ParentThresholdY = thresholdY;
            ChildWait = true;
            return this;
        }

        /// <summary>
        /// [C命令] 子要素の条件をセットし、生成処理を呼ぶ
        /// </summary>
        /// <param name="treeType">子要素がBlendtreeでなければnull, BlendTreeならばタイプ</param>
        /// <param name="thresholdName">1Dまたは2D_Xのthreshold名</param>
        /// <param name="thresholdNameY">2D_Yのthreshold名</param>
        /// <param name="motionClip">セットするMotion</param>
        /// <param name="act">Action</param>
        public void AddD(Action act = null) => ChildSet(BlendTreeType.Direct, null, null, null, act);
        public void Add1D(string ThresholdName, Action act) => ChildSet(BlendTreeType.Simple1D, ThresholdName, null, null, act);
        public void Add2D(string ThresholdName, string ThresholdNameY, Action act) => ChildSet(BlendTreeType.SimpleDirectional2D, ThresholdName, ThresholdNameY, null, act);
        public void AddAAP(params object[] args) => AddMotion(new AnimationClipsBuilder(Prj).AAP(args));
        public void AddMotion(string motionPath) => AddMotion(Prj.LoadMotion(motionPath));
        public void AddMotion(Motion motionClip) => ChildSet(null, null, null, motionClip, null);
        public void ChildSet(BlendTreeType? treeType, string thresholdName, string thresholdNameY, Motion motionClip, Action act)
        {
            if (CurrentTree.blendType != ParentTreeType) Prj.DebugPrint($@"Type Mismatch Error:親タイプは{ParentTreeType}であるべきところ、カレントタイプは{CurrentType}です。", false, LogType.Error);
            if (!ChildWait) Prj.DebugPrint($"ChildWaitが設定されていません。これが明確な意図に基づかない場合、P命令抜けの可能性が高いです。 \n {treeType},{thresholdName},{thresholdNameY},{motionClip}", false);
            ChildWait = false;
            ChildTreeType = treeType;
            ChildThresholdName = Prj.GetParameterName(thresholdName);
            ChildThresholdNameY = Prj.GetParameterName(thresholdNameY);
            ChildMotionClip = motionClip;
            ChildAct = act;
            MakeChild();
        }

        /// <summary>
        /// P命令、C命令でセットされた条件を元にBlendTreeの子を生成する
        /// C命令のActionをチェインしていくことによってBlendTreeの枝構造を表現する
        /// </summary>
        private void MakeChild()
        {
            int parentTreeNum = CurrentNum;
            var OnStartCurrentTree = CurrentTree;
            if (CurrentType == BlendTreeType.Simple1D && CurrentTree.children.Any(c => c.threshold > ParentThreshold))
            {
                Prj.DebugPrint("[BlendTreeBuilder.1DThresholdError] 登録済のThresholdより小さいThresholdを登録しようとしました。これは複雑な問題を起こすため、禁止されています。", false, LogType.Error);
                return;
            }

            if (ChildTreeType != null)
            {
                MakeChild_BlendTreePart(); // 注意：ここでCurrentが更新される
            }
            else
            {
                MakeChild_MotionPart();
            }
            if (OnStartCurrentTree.blendType == BlendTreeType.Direct) SetDirectBlendParameter(OnStartCurrentTree, ParentDirectParameterName);

            ChildAct?.Invoke(); // ChildActは新しいCurrentで実行される
            CurrentNum = parentTreeNum; //実行後、元のCurrentに戻す
        }

        /// <summary>
        /// MakeChildの内部関数。BlendTreeを生成する場合。
        /// </summary>
        private void MakeChild_BlendTreePart()
        {
            if (CurrentType == BlendTreeType.SimpleDirectional2D)
            {
                BuildingTrees.Add(CurrentTree.CreateBlendTreeChild(new Vector2(ParentThreshold, ParentThresholdY)));
            }
            else
            {
                BuildingTrees.Add(CurrentTree.CreateBlendTreeChild(ParentThreshold));
            }
            CurrentNum = MaxNum; // 注意：ここでCurrentが更新される
            CurrentTree.useAutomaticThresholds = false;
            if (NextName != null)
            {
                CurrentTree.name = NextName;
                NextName = null;
            }
            CurrentTree.blendType = ChildTreeType ?? 0;
            if (ChildTreeType != BlendTreeType.Direct) CurrentTree.blendParameter = ChildThresholdName;
            if (ChildTreeType == BlendTreeType.SimpleDirectional2D) CurrentTree.blendParameterY = ChildThresholdNameY;
            //AddObjectToAssetSafe(CurrentTree, RootTree);
        }

        /// <summary>
        /// MakeChildの内部関数。MotionFieldを生成する場合。
        /// </summary>
        private void MakeChild_MotionPart()
        {
            if (CurrentType == BlendTreeType.Simple1D) CurrentTree.AddChild(ChildMotionClip, ParentThreshold);
            if (CurrentType == BlendTreeType.SimpleDirectional2D) CurrentTree.AddChild(ChildMotionClip, new Vector2(ParentThreshold, ParentThresholdY));
            if (CurrentType == BlendTreeType.Direct) AddDirectChild(CurrentTree, ChildMotionClip, ParentDirectParameterName);
            AddObjectToAssetSafe(ChildMotionClip, RootTree);
        }

        /// <summary>
        /// AAPの値を1Dでコピーする
        /// 0または1以上の値であることが保障されているならばDirectのほうがシンプルだが、0より大きく1未満のDirectWeightはうまく動作しないためこちらを使う
        /// </summary>
        /// <param name="FromAAPName">コピー元AAPの名称</param>
        /// <param name="FromAAPMin">コピー元AAPのレンジ（最小）</param>
        /// <param name="FromAAPMax">コピー元AAPのレンジ（最大）</param>
        /// <param name="ToAAPName">コピー先AAPの名称</param>
        /// <param name="ToAAPMin">コピー先AAPのレンジ（最小、範囲変更する場合のみ指定）</param>
        /// <param name="ToAAPMax">コピー先AAPのレンジ（最大、範囲変更する場合のみ指定）</param>
        public void AssignmentBy1D(string FromAAPName, float FromAAPMin, float FromAAPMax, string ToAAPName, float? ToAAPMin = null, float? ToAAPMax = null)
        {
            if (ToAAPMin == null) ToAAPMin = FromAAPMin;
            if (ToAAPMax == null) ToAAPMax = FromAAPMax;
            Add1D(FromAAPName, () =>
            {
                Param(FromAAPMin).AddAAP(ToAAPName, ToAAPMin);
                Param(FromAAPMax).AddAAP(ToAAPName, ToAAPMax);
            });
        }
    }
}