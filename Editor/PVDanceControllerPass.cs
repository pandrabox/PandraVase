
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

namespace com.github.pandrabox.pandravase.editor
{
#if PANDRADBG
    public class PVDanceDetectDebug
    {
        [MenuItem("PanDbg/PVDanceDetect")]
        public static void DanceDetect_Debug()
        {
            SetDebugMode(true);
            new PVDanceControllerMain(TopAvatar);
        }
    }
#endif

    internal class PVDanceControllerPass : Pass<PVDanceControllerPass>
    {
        protected override void Execute(BuildContext ctx)
        {
            PanProgressBar.Show();
            new PVDanceControllerMain(ctx.AvatarDescriptor);
        }
    }
    public class PVDanceControllerMain
    {
        PandraProject _prj;
        PVDanceController _tgt;
        public PVDanceControllerMain(VRCAvatarDescriptor desc)
        {
            _tgt = desc.transform.GetComponentInChildren<PVDanceController>();
            if (_tgt == null) return;
            _prj = VaseProject(desc);
            Layer1Detect();
            DanceDetect();
            SyncDanceCondition();
            ControlDanceMode();
            //DebugControl();
            CreateMenu();
        }
        private void Layer1Detect()
        {
            var ac = new AnimationClipsBuilder();
            var ab = new AnimatorBuilder("Vase/DanceControl");

            // FxLayer1Detectの状態検出
            string detectLayerName = "Vase/FxLayer1Detector";
            ab.AddLayer(detectLayerName);
            ab.SetMotion(ac.AAP(_prj.IsFxLayer1Off, 1));
            ab.Attach(_tgt.gameObject);
            var s = _tgt.gameObject.AddComponent<PVFxSort>();
            s.SortOrder = new[] { "blank", detectLayerName };
            s.AddBlank = true;
        }

        private void DanceDetect()
        {
            // DanceDetectModeに応じてDanceを検出(mode自体はローカルなのでHostで処理)
            var bb = new BlendTreeBuilder("Vase/DanceDetect");
            var ac = new AnimationClipsBuilder();
            bb.RootDBT(() => {
                bb.Param("IsLocal").Add1D(_prj.DanceDetectMode, () => {
                    bb.NName("OFF").Param(0).AddAAP(_prj.IsDanceHost, 0);
                    bb.NName("AUTO1").Param(1).FMultiplicationBy1D(ac.AAP(_prj.IsDanceHost, 1), _prj.IsFxLayer1Off, 0, 1, 1, 0);
                    bb.NName("AUTO2").Param(2).Add1D(_prj.IsFxLayer1Off, () => {
                        bb.Param(0).AddAAP(_prj.IsDanceHost, 1);
                        bb.Param(1).Add1D("InStation", () => {
                            bb.Param(0).AddAAP(_prj.IsDanceHost, 0);
                            bb.Param(1).Add1D("Seated", () =>
                            {
                                bb.Param(0).AddAAP(_prj.IsDanceHost, 1);
                                bb.Param(1).AddAAP(_prj.IsDanceHost, 0);
                            });
                        });
                    });
                });
            });
            bb.Attach(_prj.RootObject);
        }

        private void SyncDanceCondition()
        {
            var ab = new AnimatorBuilder("Vase/DanceConditionSync");
            // 検出結果の同期
            ab.AddLayer("Vase/DanceConditionSync");
            ab.AddState("Host")
                .TransToCurrent(ab.InitialState)
                    .AddCondition(AnimatorConditionMode.Greater, 0.5f, "IsLocal");
            var hostState = ab.CurrentState;

            var mama = _prj.CreateComponentObject<ModularAvatarParameters>("DanceConditionSyncParam");
            mama.parameters = new List<ParameterConfig>();
            void syncParam(string fromParam, string toParam)
            {
                ab.AddState($@"Sync{fromParam}IsTrue")
                    .SetParameterDriver(toParam, 1)
                    .TransToCurrent(hostState)
                        .AddCondition(AnimatorConditionMode.Greater, 0.5f, fromParam)
                        .AddCondition(AnimatorConditionMode.Less, 0.5f, toParam)
                    .TransFromCurrent(hostState)
                        .MoveInstant();
                ab.AddState($@"Sync{fromParam}IsFalse")
                    .SetParameterDriver(toParam, 0)
                    .TransToCurrent(hostState)
                        .AddCondition(AnimatorConditionMode.Less, 0.5f, fromParam)
                        .AddCondition(AnimatorConditionMode.Greater, 0.5f, toParam)
                    .TransFromCurrent(hostState)
                        .MoveInstant();
                mama.parameters.Add(new ParameterConfig() { defaultValue = 0, nameOrPrefix = toParam, syncType = ParameterSyncType.Bool, localOnly = false });
            }
            syncParam(_prj.IsDanceHost, _prj.IsDance);

            ab.Attach(_prj.RootObject);
        }

        private void ControlDanceMode()
        {
            var ab = new AnimatorBuilder("Vase/DanceControl");

            // 状態制御(表情のクリアはFPで管掌)
            ab.AddLayer("Vase/DanceControl");


            ab.AddSubStateMachine("Host");
            ab.AddState("Host")
                .TransToCurrent(ab.InitialState)
                    .AddCondition(AnimatorConditionMode.Greater, 0.5f, "IsLocal")
                    ;
            {
                var rootState = ab.CurrentState;
                ab.AddState("Off")
                    .SetPlayableLayerControl(VRC.SDKBase.VRC_PlayableLayerControl.BlendableLayer.FX, true)
                    .TransToCurrent(rootState)
                        .AddCondition(AnimatorConditionMode.Less, 0.5f, _prj.IsDanceHost, true)
                    ;

                ab.AddState("On_FxOff")
                    .SetPlayableLayerControl(VRC.SDKBase.VRC_PlayableLayerControl.BlendableLayer.FX, false)
                    .TransToCurrent(rootState)
                        .AddCondition(AnimatorConditionMode.Greater, 0.5f, _prj.IsDanceHost)
                        .AddCondition(AnimatorConditionMode.Less, 0.5f, _prj.OnDanceFxEnable)

                    .TransFromCurrent(rootState)
                        .AddCondition(AnimatorConditionMode.Less, 0.5f, _prj.IsDanceHost)
                    .TransFromCurrent(rootState)
                        .AddCondition(AnimatorConditionMode.Greater, 0.5f, _prj.OnDanceFxEnable)
                    ;

                ab.AddState("On_FxOn")
                    .SetPlayableLayerControl(VRC.SDKBase.VRC_PlayableLayerControl.BlendableLayer.FX, true)
                    .TransToCurrent(rootState)
                        .AddCondition(AnimatorConditionMode.Greater, 0.5f, _prj.IsDanceHost)
                        .AddCondition(AnimatorConditionMode.Greater, 0.5f, _prj.OnDanceFxEnable)

                    .TransFromCurrent(rootState)
                        .AddCondition(AnimatorConditionMode.Less, 0.5f, _prj.IsDanceHost)
                    .TransFromCurrent(rootState)
                        .AddCondition(AnimatorConditionMode.Less, 0.5f, _prj.OnDanceFxEnable)
                    ;
            }


            ab.AddSubStateMachine("Remote");
            ab.AddState("Remote")
                .TransToCurrent(ab.InitialState)
                    .AddCondition(AnimatorConditionMode.Less, 0.5f, "IsLocal")
                    ;
            {
                var rootState = ab.CurrentState;
                ab.AddState("Off")
                    .SetPlayableLayerControl(VRC.SDKBase.VRC_PlayableLayerControl.BlendableLayer.FX, true)
                    .TransToCurrent(rootState)
                        .AddCondition(AnimatorConditionMode.Less, 0.5f, _prj.IsDance, true)
                    ;
                ab.AddState("On_FxOff")
                    .SetPlayableLayerControl(VRC.SDKBase.VRC_PlayableLayerControl.BlendableLayer.FX, false)
                    .TransToCurrent(rootState)
                        .AddCondition(AnimatorConditionMode.Greater, 0.5f, _prj.IsDance)
                        .AddCondition(AnimatorConditionMode.Greater, 0.5f, _prj.OnDanceFxEnable)
                    .TransFromCurrent(rootState)
                        .AddCondition(AnimatorConditionMode.Less, 0.5f, _prj.IsDance)
                    .TransFromCurrent(rootState)
                        .AddCondition(AnimatorConditionMode.Less, 0.5f, _prj.OnDanceFxEnable)
                    ;

                ab.AddState("On_FxOn")
                    .SetPlayableLayerControl(VRC.SDKBase.VRC_PlayableLayerControl.BlendableLayer.FX, true)
                    .TransToCurrent(rootState)
                        .AddCondition(AnimatorConditionMode.Greater, 0.5f, _prj.IsDance)
                        .AddCondition(AnimatorConditionMode.Less, 0.5f, _prj.OnDanceFxEnable)
                    .TransFromCurrent(rootState)
                        .AddCondition(AnimatorConditionMode.Less, 0.5f, _prj.IsDance)
                    .TransFromCurrent(rootState)
                        .AddCondition(AnimatorConditionMode.Greater, 0.5f, _prj.OnDanceFxEnable)
                    ;

            }
            ab.Attach(_prj.RootObject);
        }

        private void DebugControl()
        {
            var ac = new AnimationClipsBuilder();
            var bb2 = new BlendTreeBuilder("Vase/DanceControl");
            bb2.RootDBT(() =>
            {
                bb2.Param("1").FMultiplicationBy1D(ac.OnAnim("IsDance"), _prj.IsDance, 0, 1);
            });
            bb2.Attach(_tgt.gameObject);
        }
        
        private void CreateMenu()
        {
            var mb = new MenuBuilder(_prj, parentFolder: _tgt.ParrentFolder);
            mb.AddFolder("DanceMode").SetMessage("ダンス対応の設定");
            mb.AddToggle(_prj.DanceDetectMode, 0, ParameterSyncType.Int, "MODE:Off", 1).SetMessage("OFF");
            mb.AddToggle(_prj.DanceDetectMode, 1, ParameterSyncType.Int, "MODE:Normal", 1).SetMessage("ダンスを自動検出(通常)");
            mb.AddToggle(_prj.DanceDetectMode, 2, ParameterSyncType.Int, "MODE:Enhance", 1).SetMessage("ダンスを自動検出(より多く検出できるが、誤検出あり)");
            mb.AddToggle(_prj.OnDanceFxEnable, 1, ParameterSyncType.Int, "FxEnable", 1, false).SetMessage("ダンス中FXをON(通常)", "ダンス中FXをOFF(より多くのアバターでダンスできるが、同期問題あり)");
        }
    }
}