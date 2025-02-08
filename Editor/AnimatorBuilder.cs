using com.github.pandrabox.pandravase.runtime;
using nadena.dev.modular_avatar.core;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDKBase;
using static com.github.pandrabox.pandravase.editor.Util;


namespace com.github.pandrabox.pandravase.editor
{
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
        private string _buildPath = null;

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

        /// <summary>
        /// CurrentStateにAPDの値をアタッチする。APDそのものがなければ追加する
        /// </summary>
        /// <param name="name">変数名</param>
        /// <param name="value">値</param>
        /// <param name="type">タイプ（省略時Set）</param>
        /// <returns></returns>
        public AnimatorBuilder SetParameterDriver(string name, float value, VRC_AvatarParameterDriver.ChangeType type = VRC_AvatarParameterDriver.ChangeType.Set)
        {
            var APDParam = new VRC_AvatarParameterDriver.Parameter { type = type, name = name, value = value };
            VRCAvatarParameterDriver parameterDriver = _currentState.behaviours.FirstOrDefault(b => b is VRCAvatarParameterDriver) as VRCAvatarParameterDriver;
            if (parameterDriver == null)
            {
                parameterDriver = ScriptableObject.CreateInstance<VRCAvatarParameterDriver>();
                if (_currentState.behaviours == null)
                {
                    _currentState.behaviours = new[] { parameterDriver };
                }
                else
                {
                    _currentState.behaviours = _currentState.behaviours.Concat(new[] { parameterDriver }).ToArray();
                }
            }
            if (parameterDriver.parameters == null) parameterDriver.parameters = new List<VRC_AvatarParameterDriver.Parameter>();
            if (parameterDriver.parameters.Any(p => p.name == name)) LowLevelDebugPrint($@"既にAPDに登録済みの変数をSetしようとしました{name}");
            parameterDriver.parameters.Add(APDParam);
            return this;
        }

        /// <summary>
        /// CurrentStateへの遷移を定義 ExitTime等を指定する場合はtiを指定する、しないとほぼ全0
        /// </summary>
        public AnimatorBuilder TransToCurrent(AnimatorState from, TransitionInfo ti = null) => SetTransition(from, _currentState, ti);

        /// <summary>
        /// CurrentStateから遷移を定義 ExitTime等を指定する場合はtiを指定する、しないとほぼ全0
        /// </summary>
        public AnimatorBuilder TransFromCurrent(AnimatorState to, TransitionInfo ti = null) => SetTransition(_currentState, to, ti);


        /// <summary>
        /// 任意２ステート間の遷移を定義 ExitTime等を指定する場合はtiを指定する、しないとほぼ全0
        /// </summary>
        public AnimatorBuilder SetTransition(AnimatorState from, AnimatorState to, TransitionInfo transitionInfo = null)
        {
            if (transitionInfo == null) transitionInfo = FastTrans;
            _currentTransition = from.AddTransition(to);
            _currentTransition.canTransitionToSelf = false;
            _currentTransition.hasExitTime = transitionInfo.HasExitTime;
            _currentTransition.exitTime = transitionInfo.ExitTime;
            _currentTransition.hasFixedDuration = transitionInfo.FixedDuration;
            _currentTransition.duration = transitionInfo.TransitionDuration;
            _currentTransition.offset = transitionInfo.TransitionOffset;
            return this;
        }

        /// <summary>
        /// CurrentTransitionに遷移条件：即座を定義
        /// </summary>
        public AnimatorBuilder MoveInstant() => AddCondition(AnimatorConditionMode.Less, 9999f, "Pan/Dummy");

        /// <summary>
        /// CurrentTransitionに遷移条件を定義　Andなら複数呼ぶ。Orなら新しくTransitionを定義する
        /// ここで条件に使ったパラメータは自動でアニメータパラメータとして定義される
        /// </summary>
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
                if (parameter.type != type)
                {
                    LowLevelDebugPrint($@"{param}は{parameter.type}ですが{type}としてAddConditionしようとしました。({mode})", level: LogType.Exception);
                }
            }
            _currentTransition.AddCondition(mode, threshold, param);
            return this;
        }

        /// <summary>
        /// 渡されたパラメータの型を推定し返す
        /// </summary>
        public static AnimatorControllerParameterType GetParameterTypes(AnimatorConditionMode mode)
        {
            if (mode == AnimatorConditionMode.If || mode == AnimatorConditionMode.IfNot) return AnimatorControllerParameterType.Bool;
            if (mode == AnimatorConditionMode.Greater || mode == AnimatorConditionMode.Less) return AnimatorControllerParameterType.Float;
            if (mode == AnimatorConditionMode.Equals || mode == AnimatorConditionMode.NotEqual) return AnimatorControllerParameterType.Int;
            LowLevelDebugPrint("型推定において不明なエラーが発生しました");
            return AnimatorControllerParameterType.Float;
        }

        /// <summary>
        /// CurrentStateMachineに作成したStateとSubStateMachineの合計数を返す
        /// </summary>
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

        /// <summary>
        /// CurrentStateMachineに作成したStateとSubStateMachineの合計数をインクリメント
        /// </summary>
        private int NextStateNum()
        {
            int c = CurrentStateCount();
            _stateCounts[_currentStateMachine]++;
            return c + 1;
        }

        /// <summary>
        /// 次に作成するstateのあるべき座標を返す
        /// </summary>
        private Vector3 NextStatePos()
        {
            return new Vector3(STATEX, NextStateNum() * STATEYDELTA);
        }

        /// <summary>
        /// stateを作成する
        /// motion省略時自動でDummyClipが入る。明示的にnullにしたい場合はnullMotionをtrueにする
        /// </summary>
        public AnimatorBuilder AddState(string name, Motion motion = null, bool nullMotion=false)
        {
            if (motion == null && !nullMotion) motion = DummyClip;
            _currentState = _currentStateMachine.AddState(name, NextStatePos());
            _currentState.writeDefaultValues = true;
            if (motion != null) _currentState.motion = motion;
            return this;
        }

        /// <summary>
        /// アニメータパラメータを追加する。基本的にはAddConditionから自動追加される
        /// </summary>
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

        /// <summary>
        /// レイヤを追加する
        /// </summary>
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

        /// <summary>
        /// カレントレイヤを設定する
        /// </summary>
        public AnimatorBuilder Layer(string name) => SetCurrentLayer(name);

        /// <summary>
        /// カレントレイヤを設定する
        /// </summary>
        public AnimatorBuilder SetCurrentLayer(string name) => SetCurrentLayer(_ac.layers.FirstOrDefault(x => x.name == name));

        /// <summary>
        /// カレントレイヤを設定する
        /// </summary>
        public AnimatorBuilder SetCurrentLayer(AnimatorControllerLayer layer)
        {
            _currentLayer = layer;
            _currentStateMachine = _currentLayer.stateMachine;
            return this;
        }

        /// <summary>
        /// カレントサブステートマシンを設定する
        /// </summary>
        public AnimatorBuilder StateMachine(string name) => SetCurrentStateMachine(name);

        /// <summary>
        /// カレントサブステートマシンを設定する
        /// </summary>
        public AnimatorBuilder SetCurrentStateMachine(string name)
        {
            var child = _currentLayer?.stateMachine?.stateMachines?.FirstOrDefault(x => x.stateMachine?.name == name);
            SetCurrentStateMachine(child);
            return this;
        }

        /// <summary>
        /// カレントサブステートマシンを設定する
        /// </summary>
        public AnimatorBuilder SetCurrentStateMachine(ChildAnimatorStateMachine? c)
        {
            if (c == null)
            {
                LowLevelDebugPrint($@"ステートマシン{c}を取得しようとし、失敗しました");
                _currentStateMachine = null;
                return this;
            }
            return SetCurrentStateMachine((ChildAnimatorStateMachine)c);
        }

        /// <summary>
        /// カレントサブステートマシンを設定する
        /// </summary>
        public AnimatorBuilder SetCurrentStateMachine(ChildAnimatorStateMachine c) => SetCurrentStateMachine(c.stateMachine);

        /// <summary>
        /// カレントサブステートマシンを設定する
        /// </summary>
        public AnimatorBuilder SetCurrentStateMachine(AnimatorStateMachine stateMachine)
        {
            _currentStateMachine = stateMachine;
            return this;
        }

        /// <summary>
        /// サブステートマシンを追加する
        /// </summary>
        public AnimatorBuilder AddSubStateMachine(string name, bool toRoot = true)
        {
            if (toRoot) _currentStateMachine = CurrentRootStateMachine;
            _currentStateMachine = _currentStateMachine.AddStateMachine(name, NextStatePos());
            return this;
        }

        /// <summary>
        /// ビルドする
        /// </summary>
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
                    _buildPath = $@"Assets/Pan/Temp/{_animatorName}_{Guid.NewGuid()}.controller";
                }
                var dirPath = _buildPath.Substring(0, _buildPath.LastIndexOf("/") + 1);
                CreateDir(dirPath);
                AssetDatabase.CreateAsset(_ac, _buildPath);
                LowLevelDebugPrint($@"Build to {_buildPath}");
            }
            return _ac;
        }

        /// <summary>
        /// ビルドしアバターにアタッチ
        /// </summary>
        public ModularAvatarMergeAnimator BuildAndAttach(PandraProject prj) => BuildAndAttach(prj.PrjRootObj);

        /// <summary>
        /// ビルドしアバターにアタッチ
        /// </summary>
        public ModularAvatarMergeAnimator BuildAndAttach(GameObject tgt) => BuildAndAttach(tgt.transform);

        /// <summary>
        /// ビルドしアバターにアタッチ
        /// </summary>
        public ModularAvatarMergeAnimator BuildAndAttach(Transform tgt)
        {
            return ReCreateComponentObject<ModularAvatarMergeAnimator>(tgt, _animatorName, (x) =>
            {
                x.animator = Build();
            });
        }

        /// <summary>
        /// 遷移条件省略時の遷移条件
        /// </summary>
        private TransitionInfo FastTrans = new TransitionInfo(false, 0, false, 0, 0);

        /// <summary>
        /// 遷移条件を管掌するクラス
        /// </summary>
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
