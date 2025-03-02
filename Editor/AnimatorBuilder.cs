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
        private AnimatorState _currentTransitionFrom,_currentTransitionTo;
        private TransitionInfo _currentTransitionInfo;
        private Dictionary<AnimatorStateMachine, int> _stateCounts = new Dictionary<AnimatorStateMachine, int>();
        public AnimatorState CurrentState => _currentState;
        public AnimatorStateMachine CurrentStateMachine => _currentStateMachine;
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
            Build(); //実体がないとAddAssetできないので一度ビルド
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

        public AnimatorBuilder SetTemporaryPoseSpace(bool isActive)
        {
            VRCAnimatorTemporaryPoseSpace poseSpace = _currentState.behaviours.FirstOrDefault(b => b is VRCAnimatorTemporaryPoseSpace) as VRCAnimatorTemporaryPoseSpace;
            if (poseSpace == null)
            {
                poseSpace = ScriptableObject.CreateInstance<VRCAnimatorTemporaryPoseSpace>();
                poseSpace.enterPoseSpace = isActive;
                if (_currentState.behaviours == null)
                {
                    _currentState.behaviours = new[] { poseSpace };
                }
                else
                {
                    _currentState.behaviours = _currentState.behaviours.Concat(new[] { poseSpace }).ToArray();
                }
            }
            else
            {
                LowLevelExeption($@"{_currentState.name}には既にTemporaryPoseSpaceが設定されています");
            }
            return this;
        }

        public AnimatorBuilder SetLocomotionControl(bool isActive)
        {
            VRCAnimatorLocomotionControl locomotionControl = _currentState.behaviours.FirstOrDefault(b => b is VRCAnimatorLocomotionControl) as VRCAnimatorLocomotionControl;
            if (locomotionControl == null)
            {
                locomotionControl = ScriptableObject.CreateInstance<VRCAnimatorLocomotionControl>();
                locomotionControl.disableLocomotion = !isActive;
                if (_currentState.behaviours == null)
                {
                    _currentState.behaviours = new[] { locomotionControl };
                }
                else
                {
                    _currentState.behaviours = _currentState.behaviours.Concat(new[] { locomotionControl }).ToArray();
                }
            }
            else
            {
                LowLevelExeption($@"{_currentState.name}には既にLocomotionControlが設定されています");
            }
            return this;
        }

        public AnimatorBuilder SetTrackingControl(
            bool? all = null,
            bool? head = null,
            bool? leftHand = null,
            bool? rightHand = null,
            bool? hip = null,
            bool? leftFoot = null,
            bool? rightFoot = null,
            bool? leftFingers = null,
            bool? rightFingers = null,
            bool? eyes = null,
            bool? mouth = null)
        {
            VRCAnimatorTrackingControl trackingControl = _currentState.behaviours.FirstOrDefault(b => b is VRCAnimatorTrackingControl) as VRCAnimatorTrackingControl;
            if (trackingControl == null)
            {
                trackingControl = ScriptableObject.CreateInstance<VRCAnimatorTrackingControl>();
                if (_currentState.behaviours == null)
                {
                    _currentState.behaviours = new[] { trackingControl };
                }
                else
                {
                    _currentState.behaviours = _currentState.behaviours.Concat(new[] { trackingControl }).ToArray();
                }
            }

            void SetTracking(ref VRC_AnimatorTrackingControl.TrackingType trackingType, bool? value)
            {
                if (value.HasValue || all.HasValue)
                {
                    trackingType = (value ?? all).Value ? VRC_AnimatorTrackingControl.TrackingType.Tracking : VRC_AnimatorTrackingControl.TrackingType.Animation;
                }
            }

            SetTracking(ref trackingControl.trackingHead, head);
            SetTracking(ref trackingControl.trackingLeftHand, leftHand);
            SetTracking(ref trackingControl.trackingRightHand, rightHand);
            SetTracking(ref trackingControl.trackingHip, hip);
            SetTracking(ref trackingControl.trackingLeftFoot, leftFoot);
            SetTracking(ref trackingControl.trackingRightFoot, rightFoot);
            SetTracking(ref trackingControl.trackingLeftFingers, leftFingers);
            SetTracking(ref trackingControl.trackingRightFingers, rightFingers);
            SetTracking(ref trackingControl.trackingEyes, eyes);
            SetTracking(ref trackingControl.trackingMouth, mouth);

            return this;
        }





        /// <summary>
        /// CurrentStateへの遷移を定義 ExitTime等を指定する場合はtiを指定する、しないとほぼ全0
        /// </summary>
        public AnimatorBuilder TransToCurrent(AnimatorState from, bool hasExitTime = false, float exitTime = 0, bool fixedDuration = true, float transitionDuration = 0, float transitionOffset = 0) 
                => SetTransition(from, _currentState, hasExitTime, exitTime, fixedDuration, transitionDuration, transitionOffset);

        /// <summary>
        /// CurrentStateから遷移を定義 ExitTime等を指定する場合はtiを指定する、しないとほぼ全0
        /// </summary>
        public AnimatorBuilder TransFromCurrent(AnimatorState to, bool hasExitTime = false, float exitTime = 0, bool fixedDuration = true, float transitionDuration = 0, float transitionOffset = 0) 
                => SetTransition(_currentState, to, hasExitTime, exitTime, fixedDuration, transitionDuration, transitionOffset);

        public AnimatorBuilder TransFromAny(bool hasExitTime = false, float exitTime = 0, bool fixedDuration = true, float transitionDuration = 0, float transitionOffset = 0)
        {
            TransitionInfo transitionInfo = new TransitionInfo(hasExitTime, exitTime, fixedDuration, transitionDuration, transitionOffset);
            _currentTransition = _currentStateMachine.AddAnyStateTransition(_currentState);
            _currentTransition.canTransitionToSelf = false;
            _currentTransition.hasExitTime = transitionInfo.HasExitTime;
            _currentTransition.exitTime = transitionInfo.ExitTime;
            _currentTransition.hasFixedDuration = transitionInfo.FixedDuration;
            _currentTransition.duration = transitionInfo.TransitionDuration;
            _currentTransition.offset = transitionInfo.TransitionOffset;
            _currentTransitionFrom = null;
            _currentTransitionTo = _currentState;
            _currentTransitionInfo = transitionInfo;
            return this;
        }

        /// <summary>
        /// 任意２ステート間の遷移を定義 ExitTime等を指定する場合はtiを指定する、しないとほぼ全0
        /// </summary>
        /// <param name="from">遷移元</param>
        /// <param name="to">遷移先</param>
        /// <param name="hasExitTime">trueに設定すると、現在のアニメーション状態が指定された終了時間（ExitTime）に達したときにトランジションが発生します。</param>
        /// <param name="exitTime">トランジションが発生する時間（正規化された時間）。例えば、0.75に設定すると、アニメーションの75%が終了した時点でトランジションが発生します。</param>
        /// <param name="fixedDuration">trueにするとtransitionDurationの単位がsになります。</param>
        /// <param name="transitionDuration">トランジションの期間（秒）。例えば、1.0に設定すると、トランジションに1秒かかります。</param>
        /// <param name="transitionOffset">トランジションの開始オフセット（正規化された時間）。例えば、0.1に設定すると、トランジションが通常よりも早く開始されます。</param>
        public AnimatorBuilder SetTransition(AnimatorState from, AnimatorState to, bool hasExitTime = false, float exitTime = 0, bool fixedDuration = true, float transitionDuration = 0, float transitionOffset = 0)
        {
            TransitionInfo transitionInfo = new TransitionInfo(hasExitTime, exitTime, fixedDuration, transitionDuration, transitionOffset);
            _currentTransition = from.AddTransition(to);
            _currentTransition.canTransitionToSelf = false;
            _currentTransition.hasExitTime = transitionInfo.HasExitTime;
            _currentTransition.exitTime = transitionInfo.ExitTime;
            _currentTransition.hasFixedDuration = transitionInfo.FixedDuration;
            _currentTransition.duration = transitionInfo.TransitionDuration;
            _currentTransition.offset = transitionInfo.TransitionOffset;
            _currentTransitionFrom = from;
            _currentTransitionTo = to;
            _currentTransitionInfo = transitionInfo;
            return this;
        }

        /// <summary>
        /// CurrentTransitionに遷移条件：即座を定義
        /// </summary>
        public AnimatorBuilder MoveInstant() => AddCondition(AnimatorConditionMode.Less, 9999f, "Pan/Dummy");

        /// <summary>
        /// CurrentTransitionに遷移条件を定義　Andなら複数呼ぶ。Orなら新しくTransitionを定義する
        /// ここで条件に使ったパラメータは自動でアニメータパラメータとして定義される
        /// roundTripがONだと逆方向のTransitionを自動で定義する(CurrentStateは維持、CurrentTransitionは移動)
        ///     modeは自動で反転する。floatはless<->greaterのため丁度のとき問題があるかもしれない(作成時ユースケースがない為一旦放置)
        /// </summary>
        public AnimatorBuilder AddCondition(AnimatorConditionMode mode, float threshold, string param, bool roundTrip=false)
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
            if (roundTrip)
            {
                SetTransition(_currentTransitionTo, _currentTransitionFrom, _currentTransitionInfo.HasExitTime, _currentTransitionInfo.ExitTime, _currentTransitionInfo.FixedDuration, _currentTransitionInfo.TransitionDuration, _currentTransitionInfo.TransitionOffset);
                if (mode == AnimatorConditionMode.If)
                {
                    AddCondition(AnimatorConditionMode.IfNot, threshold, param);
                }
                if (mode == AnimatorConditionMode.IfNot)
                {
                    AddCondition(AnimatorConditionMode.If, threshold, param);
                }
                if (mode == AnimatorConditionMode.Greater)
                {
                    AddCondition(AnimatorConditionMode.Less, threshold, param);
                }
                if (mode == AnimatorConditionMode.Less)
                {
                    AddCondition(AnimatorConditionMode.Greater, threshold, param);
                }
                if (mode == AnimatorConditionMode.Equals)
                {
                    AddCondition(AnimatorConditionMode.NotEqual, threshold, param);
                }
                if (mode == AnimatorConditionMode.NotEqual)
                {
                    AddCondition(AnimatorConditionMode.Equals, threshold, param);
                }
            }
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
        public AnimatorBuilder AddState(string name, Motion motion = null, bool nullMotion=false, Vector3? position=null)
        {
            if(_currentStateMachine == null)
            {
                LowLevelExeption("Current state machineが設定されていません。レイヤを作成したか確認してください");
                return this;
            }
            _currentState = _currentStateMachine.AddState(name, position ?? NextStatePos());
            _currentState.writeDefaultValues = true;
            SetMotion(nullMotion ? null : motion ?? DummyClip);
            return this;
        }

        public AnimatorBuilder SetMotion(Motion motion)
        {
            if (motion == null) return this;
            _currentState.motion = motion;
            AddObjectToAssetSafe(motion, _ac);
            return this;
        }

        /// <summary>
        /// カレントステートを変更
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public AnimatorBuilder ChangeCurrentState(AnimatorState state)
        {
            _currentState = state;
            return this;
        }

        /// <summary>
        /// アニメータパラメータを追加する。基本的にはAddConditionから自動追加される
        /// </summary>
        public AnimatorBuilder AddAnimatorParameter(string name, float defaultFloat = 0, AnimatorControllerParameterType type = AnimatorControllerParameterType.Float)
        {
            if (_ac.parameters.Any(p => p.name == name))
            {
                LowLevelDebugPrint($@"アニメータパラメータ{name}は既に存在します", level: LogType.Log);
                return this;
            }
            LowLevelDebugPrint($@"アニメータパラメータ{name}({type}, {defaultFloat})を追加します");
            _ac.parameters = _ac.parameters.Concat(
                new[] { new AnimatorControllerParameter {
                    name = name,
                    type = type ,
                    defaultFloat = defaultFloat ,
                    defaultBool = defaultFloat == 1 ,
                    defaultInt = (int)defaultFloat
                } }
            ).ToArray();
            return this;
        }

        /// <summary>
        /// レイヤを追加する
        /// </summary>
        public AnimatorBuilder AddLayer(string name = null)
        {
            if (name == null) name = _animatorName;
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
            if (_currentLayer == null)
            {
                LowLevelExeption("Current layer is null.");
                return this;
            }

            if (_currentLayer.stateMachine == null)
            {
                LowLevelExeption("Current layer's state machine is null.");
                return this;
            }

            if (_currentLayer.stateMachine.stateMachines == null || _currentLayer.stateMachine.stateMachines.Length == 0)
            {
                LowLevelExeption("Current layer's state machine has no sub-state machines.");
                return this;
            }

            AnimatorStateMachine stateMachine = _currentLayer.stateMachine.stateMachines
                .FirstOrDefault(sm => sm.stateMachine.name == name).stateMachine;

            if (stateMachine == null)
            {
                LowLevelExeption($"State machine '{name}' not found.");
                return this;
            }

            SetCurrentStateMachine(stateMachine);
            return this;
        }





        /// <summary>
        /// カレントサブステートマシンを設定する
        /// </summary>
        public AnimatorBuilder SetCurrentStateMachine(ChildAnimatorStateMachine? c)
        {
            if (c == null)
            {
                LowLevelExeption($@"ステートマシン{c}を取得しようとし、失敗しました");
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
            if (stateMachine == null)
            {
                LowLevelExeption($@"カレントサブステートマシンの設定に、失敗しました");
                _currentStateMachine = null;
                return this;
            }
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
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            else
            {
                _buildPath = OutpAsset(_ac, _buildPath);
            }
            return _ac;
        }

        /// <summary>
        /// ビルドしアバターにアタッチ
        /// </summary>
        public ModularAvatarMergeAnimator Attach(PandraProject prj, bool fixWD = false) => Attach(prj.CreateObject($@"Anim{_animatorName}"), fixWD);
        public ModularAvatarMergeAnimator Attach(Transform tgt, bool fixWD = false) => Attach(tgt.transform, fixWD);
        public ModularAvatarMergeAnimator Attach(GameObject tgt, bool fixWD = false)
        {
            ModularAvatarMergeAnimator x = tgt.AddComponent<ModularAvatarMergeAnimator>();
            x.animator = Build();
            return x;
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
