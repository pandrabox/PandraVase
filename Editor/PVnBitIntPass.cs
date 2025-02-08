/// floatをnBitで他のfloatに仮想同期する
/// 注意：範囲外の値が入ると前の値のままになる

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
using static UnityEngine.Tilemaps.TilemapRenderer;
using VRC.SDKBase;

namespace com.github.pandrabox.pandravase.editor
{
#if PANDRADBG
    public class PVnBitIntDebug
    {
        [MenuItem("PanDbg/PVnBitInt")]
        public static void PVnBitInt_Debug()
        {
            SetDebugMode(true);
            new PVnBitIntMain(TopAvatar);
        }
    }
#endif

    internal class PVnBitIntPass : Pass<PVnBitIntPass>
    {
        protected override void Execute(BuildContext ctx)
        {
            new PVnBitIntMain(ctx.AvatarDescriptor);
        }
    }

    public class PVnBitIntMain
    {
        PandraProject _prj;
        PVnBitInt[] _nBitInts;
        public PVnBitIntMain(VRCAvatarDescriptor desc)
        {
            _nBitInts = desc.transform.GetComponentsInChildren<PVnBitInt>();
            if (_nBitInts.Length == 0) return;
            _prj = VaseProject(desc);
            CreateEncoder();

        }

        private void CreateEncoder()
        {
            AnimatorBuilder ab= new AnimatorBuilder("test");
            ab.AddLayer("PVnBitInt/Encode").AddState("Local").TransToCurrent(ab.InitialState).AddCondition(AnimatorConditionMode.Greater, 0.5f, "IsLocal");
            AnimatorState localRoot = ab.CurrentState;
            foreach (var tgtp in _nBitInts)
            {
                if (tgtp == null || tgtp.nBitInts.Length == 0) continue;
                foreach(var tgt in tgtp.nBitInts)
                {
                    if (tgt == null || tgt.TxName == null || tgt.TxName.Length == 0 || tgt.RxName == null || tgt.RxName.Length == 0 || tgt.Bit == 0) continue;
                    ab.AddSubStateMachine(tgt.TxName);
                    int[] bits = new int[tgt.Bit];
                    for (var i = 0; i < 1 << tgt.Bit; i++)
                    {
                        for (int j = 0; j < tgt.Bit; j++)
                        {
                            bits[j] = (i >> j) & 1;
                        }
                        ab.AddState($@"Tx{i}");
                        ab.TransFromCurrent(localRoot).MoveInstant();
                        for (int j = 0; j < tgt.Bit; j++)
                        {
                            ab.TransToCurrent(localRoot);
                            ab.AddCondition(AnimatorConditionMode.Greater, i - .5f, tgt.TxName);
                            ab.AddCondition(AnimatorConditionMode.Less, i + .5f, tgt.TxName);
                            if (bits[j] == 0) {
                                ab.AddCondition(AnimatorConditionMode.Greater, .5f, $@"{tgt.TxName}/b{j}");
                                ab.SetParameterDriver($@"{tgt.TxName}/b{j}", 0);
                            }
                            else
                            {
                                ab.AddCondition(AnimatorConditionMode.Less, .5f, $@"{tgt.TxName}/b{j}");
                                ab.SetParameterDriver($@"{tgt.TxName}/b{j}", 1);
                            }
                        }
                    }
                    for (int j = 0; j < tgt.Bit; j++)
                    {
                        var MAP = _prj.GetOrCreateComponentObject<ModularAvatarParameters>("EncorderParam", (x) => {
                            if (x.parameters == null) x.parameters = new List<ParameterConfig>();
                        });
                        MAP.parameters.Add(new ParameterConfig() { nameOrPrefix = $@"{tgt.TxName}/b{j}", syncType = ParameterSyncType.Bool });
                    }
                }
            }
            ab.BuildAndAttach(_prj);
            //ab.BuildAndSave("Assets/test.controller");
            //ab.Build();
        }
    }

    public class AnimatorBuilder
    {
        private const int STATEX = 300;
        private const int STATEYDELTA = 60;
        string _animatorName;
        private AnimatorController _ac;
        private AnimationClipsBuilder _clipsBuilder;
        private AnimatorControllerLayer _currentLayer;
        private AnimatorStateMachine _currentStateMachine;
        private AnimatorState _currentState;
        private AnimatorStateTransition _currentTransition;
        private Dictionary<AnimatorStateMachine, int> _stateCounts = new Dictionary<AnimatorStateMachine, int>();
        public AnimatorState CurrentState => _currentState;
        public AnimatorControllerLayer CurrentLayer => _currentLayer;
        public AnimatorStateMachine CurrentRootStateMachine => CurrentLayer.stateMachine;
        public AnimatorState CurrentInitialState => CurrentRootStateMachine.defaultState;
        public AnimationClip DummyClip => _clipsBuilder.AAP("Pan/Dummy", 0);
        public AnimatorState InitialState => CurrentRootStateMachine.defaultState;
        private string _buildPath=null;

        public AnimatorBuilder(string animatorName)
        {
            _animatorName = animatorName;
            _ac = new AnimatorController();
            _clipsBuilder = new AnimationClipsBuilder();
            _ac.name = animatorName;
            AddAnimatorParameter("Pan/One", 1);
            AddAnimatorParameter("__ModularAvatarInternal/One", 1);
            AddAnimatorParameter("Pan/Dummy");
        }

        public AnimatorBuilder SetParameterDriver(string name, float value, VRC_AvatarParameterDriver.ChangeType type= VRC_AvatarParameterDriver.ChangeType.Set)
        {
            var APDParam = new VRC_AvatarParameterDriver.Parameter { type = type, name = name, value = value };
            VRCAvatarParameterDriver parameterDriver = _currentState.behaviours.FirstOrDefault(b => b is VRCAvatarParameterDriver) as VRCAvatarParameterDriver;
            if (parameterDriver == null)
            {
                parameterDriver = ScriptableObject.CreateInstance<VRCAvatarParameterDriver>();
                if (_currentState.behaviours == null)
                {
                    _currentState.behaviours = new [] { parameterDriver };
                }
                else
                {
                    _currentState.behaviours = _currentState.behaviours.Concat(new[] { parameterDriver}).ToArray();
                }
            }
            if (parameterDriver.parameters == null) parameterDriver.parameters = new List<VRC_AvatarParameterDriver.Parameter>();
            if (parameterDriver.parameters.Any(p => p.name == name)) LowLevelDebugPrint($@"既にAPDに登録済みの変数をSetしようとしました{name}");
            parameterDriver.parameters.Add(APDParam);
            return this;
        }

        public AnimatorBuilder TransToCurrent(AnimatorState from, TransitionInfo ti = null) => SetTransition(from, _currentState, ti);
        public AnimatorBuilder TransFromCurrent(AnimatorState to, TransitionInfo ti = null) => SetTransition(_currentState, to, ti);
        public AnimatorBuilder SetTransition(AnimatorState from, AnimatorState to, TransitionInfo transitionInfo = null)
        {
            if (transitionInfo == null) transitionInfo = FastTrans;
            _currentTransition = from.AddTransition(to);
            _currentTransition.canTransitionToSelf=false;
            _currentTransition.hasExitTime = transitionInfo.HasExitTime;
            _currentTransition.exitTime = transitionInfo.ExitTime;
            _currentTransition.hasFixedDuration = transitionInfo.FixedDuration;
            _currentTransition.duration = transitionInfo.TransitionDuration;
            _currentTransition.offset = transitionInfo.TransitionOffset;
            return this;
        }
        public AnimatorBuilder MoveInstant() => AddCondition(AnimatorConditionMode.Less, 9999f, "Pan/Dummy");
        public AnimatorBuilder AddCondition(AnimatorConditionMode mode, float threshold, string param)
        {
            var type = GetParameterTypes(mode);
            var parameter = _ac.parameters.FirstOrDefault(p => p.name == param);
            if (parameter == null)
            {
                AddAnimatorParameter(param, 0, type);
            }
            else
            {
                if (parameter.type !=type)
                {
                    LowLevelDebugPrint($@"{param}は{parameter.type}ですが{type}としてAddConditionしようとしました。({mode})", level:LogType.Exception);
                }
            }
            _currentTransition.AddCondition(mode, threshold, param);
            return this;
        }
        public static AnimatorControllerParameterType GetParameterTypes(AnimatorConditionMode mode)
        {
            if (mode == AnimatorConditionMode.If || mode == AnimatorConditionMode.IfNot) return AnimatorControllerParameterType.Bool;
            if (mode == AnimatorConditionMode.Greater || mode == AnimatorConditionMode.Less) return AnimatorControllerParameterType.Float;
            if (mode == AnimatorConditionMode.Equals || mode == AnimatorConditionMode.NotEqual) return AnimatorControllerParameterType.Int;
            LowLevelDebugPrint("型推定において不明なエラーが発生しました");
            return AnimatorControllerParameterType.Float;
        }

        private int CurrentStateCount()
        {
            if (_stateCounts.ContainsKey(_currentStateMachine))
            {
                return _stateCounts[_currentStateMachine];
            }
            else
            {
                _stateCounts.Add(_currentStateMachine, 0);
                return 0;
            }
        }

        private int NextStateNum()
        {
            int c = CurrentStateCount();
            _stateCounts[_currentStateMachine]++;
            return c+1;
        }

        private Vector3 NextStatePos()
        {
            return new Vector3(STATEX, NextStateNum() * STATEYDELTA);
        }

        public AnimatorBuilder AddState(string name, Motion motion = null)
        {            
            _currentState = _currentStateMachine.AddState(name, NextStatePos());
            _currentState.writeDefaultValues = true;
            if(motion != null)  _currentState.motion = motion;
            return this;
        }

        public AnimatorBuilder AddAnimatorParameter(string name, float defaultFloat = 0, AnimatorControllerParameterType type = AnimatorControllerParameterType.Float)
        {
            _ac.parameters = _ac.parameters.Concat(
                new[] { new AnimatorControllerParameter {
                    name = name, 
                    type = type ,
                    defaultFloat = defaultFloat
                } }
            ).ToArray();
            return this;
        }
        public AnimatorBuilder Layer(string name) => SetCurrentLayer(name);
        public AnimatorBuilder SetCurrentLayer(string name) => SetCurrentLayer(_ac.layers.FirstOrDefault(x => x.name == name));
        public AnimatorBuilder SetCurrentLayer(AnimatorControllerLayer layer)
        {
            _currentLayer = layer;
            _currentStateMachine = _currentLayer.stateMachine;
            return this;
        }
        //現在のレイヤのサブステートマシンを名称探索しあればカレントに設定
        public AnimatorBuilder StateMachine(string name) => SetCurrentStateMachine(name);
        public AnimatorBuilder SetCurrentStateMachine(string name)
        {
            var child = _currentLayer?.stateMachine?.stateMachines?.FirstOrDefault(x => x.stateMachine?.name == name);
            SetCurrentStateMachine(child);
            return this;
        }
        public AnimatorBuilder SetCurrentStateMachine(ChildAnimatorStateMachine? c){
            if(c == null)
            {
                LowLevelDebugPrint($@"ステートマシン{c}を取得しようとし、失敗しました");
                _currentStateMachine = null;
                return this;
            }
            return SetCurrentStateMachine((ChildAnimatorStateMachine)c);
        }
        public AnimatorBuilder SetCurrentStateMachine(ChildAnimatorStateMachine c) => SetCurrentStateMachine(c.stateMachine);
        public AnimatorBuilder SetCurrentStateMachine(AnimatorStateMachine stateMachine)
        {
            _currentStateMachine = stateMachine;
            return this;
        }
        public AnimatorBuilder AddSubStateMachine(string name, bool toRoot = true)
        {
            if (toRoot) _currentStateMachine = CurrentRootStateMachine;
            _currentStateMachine = _currentStateMachine.AddStateMachine(name, NextStatePos());
            return this;
        }


        public AnimatorBuilder AddLayer(string name)
        {
            var newStateMachine = new AnimatorStateMachine();
            SetCurrentStateMachine(newStateMachine);
            var layer = new AnimatorControllerLayer
            {
                blendingMode = AnimatorLayerBlendingMode.Override,
                defaultWeight = 1,
                name = name,
                stateMachine = newStateMachine
            };
            _ac.layers = _ac.layers.Concat(new[] { layer }).ToArray();
            SetCurrentLayer(name);
            AddState("Initial", DummyClip);
            return this;
        }



        public AnimatorController Build(string buildPath = null)
        {
            if (_buildPath != null)
            {
                AssetDatabase.SaveAssets(); // AnimatorController の変更を保存
                AssetDatabase.Refresh();    // アセットデータベースをリフレッシュ
                LowLevelDebugPrint($@"Rebuild to {_buildPath}"); // リビルドしたことを通知
            }
            else
            {
                if (buildPath != null)
                {
                    _buildPath = buildPath;
                }
                else
                {
                    _buildPath = $@"Assets/Pan/Temp/{Guid.NewGuid()}.controller";
                }
                var dirPath = _buildPath.Substring(0, _buildPath.LastIndexOf("/") + 1);
                CreateDir(dirPath);
                AssetDatabase.CreateAsset(_ac, _buildPath);
                LowLevelDebugPrint($@"Build to {_buildPath}");
            }
            return _ac;
        }

        public ModularAvatarMergeAnimator BuildAndAttach(PandraProject prj) => BuildAndAttach(prj.PrjRootObj);
        public ModularAvatarMergeAnimator BuildAndAttach(GameObject tgt) => BuildAndAttach(tgt.transform);
        public ModularAvatarMergeAnimator BuildAndAttach(Transform tgt)
        {
            return ReCreateComponentObject<ModularAvatarMergeAnimator>(tgt, _animatorName, (x) =>
            {
                x.animator = Build();
            });
        }

        private TransitionInfo FastTrans = new TransitionInfo(false, 0, false, 0, 0);

        public class TransitionInfo
        {
            public bool HasExitTime;
            public float ExitTime;
            public bool FixedDuration;
            public float TransitionDuration;
            public float TransitionOffset;

            public TransitionInfo(bool hasExitTime, float exitTime, bool fixedDuration, float transitionDuration, float transitionOffset)
            {
                HasExitTime = hasExitTime;
                ExitTime = exitTime;
                FixedDuration = fixedDuration;
                TransitionDuration = transitionDuration;
                TransitionOffset = transitionOffset;
            }
        }
    }
}
