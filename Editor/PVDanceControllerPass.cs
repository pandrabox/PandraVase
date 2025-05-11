
using com.github.pandrabox.pandravase.runtime;
using nadena.dev.modular_avatar.core;
using nadena.dev.ndmf;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using VRC.SDK3.Avatars.Components;
using static com.github.pandrabox.pandravase.editor.Localizer;
using static com.github.pandrabox.pandravase.editor.Util;


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
            bb.RootDBT(() =>
            {
                bb.Param("IsLocal").Add1D(_prj.DanceDetectMode, () =>
                {
                    bb.NName("OFF").Param(0).AddAAP(_prj.IsDanceHost, 0);
                    bb.NName("AUTO1").Param(1).FMultiplicationBy1D(ac.AAP(_prj.IsDanceHost, 1), _prj.IsFxLayer1Off, 0, 1, 1, 0);
                    bb.NName("AUTO2").Param(2).Add1D(_prj.IsFxLayer1Off, () =>
                    {
                        bb.Param(0).AddAAP(_prj.IsDanceHost, 1);
                        bb.Param(1).Add1D("InStation", () =>
                        {
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

            ab.AddState("Host")
                .TransToCurrent(ab.InitialState)
                    .AddCondition(AnimatorConditionMode.Greater, 0.5f, "IsLocal")
                    ;
            ControlDanceMode_Inner(ab, _prj.IsDanceHost);

            ab.AddState("Remote")
                .TransToCurrent(ab.InitialState)
                    .AddCondition(AnimatorConditionMode.Less, 0.5f, "IsLocal")
                    ;

            ControlDanceMode_Inner(ab, _prj.IsDance);

            ab.Attach(_prj.RootObject);
        }

        private void ControlDanceMode_Inner(AnimatorBuilder ab, string isDanceStr)
        {
            var rootState = ab.CurrentState;
            ab.AddState("Off")
                .SetPlayableLayerControl(VRC.SDKBase.VRC_PlayableLayerControl.BlendableLayer.FX, true)
                .TransToCurrent(rootState)
                    .AddCondition(AnimatorConditionMode.Less, 0.5f, isDanceStr, true)
                ;

            ab.AddState("On_FxOff")
                .SetPlayableLayerControl(VRC.SDKBase.VRC_PlayableLayerControl.BlendableLayer.FX, false)
                .TransToCurrent(rootState)
                    .AddCondition(AnimatorConditionMode.Greater, 0.5f, isDanceStr)
                    .AddCondition(AnimatorConditionMode.Less, 0.5f, _prj.OnDanceFxEnable)

                .TransFromCurrent(rootState)
                    .AddCondition(AnimatorConditionMode.Less, 0.5f, isDanceStr)
                .TransFromCurrent(rootState)
                    .AddCondition(AnimatorConditionMode.Greater, 0.5f, _prj.OnDanceFxEnable)
                ;

            ab.AddState("On_FxOn")
                .SetPlayableLayerControl(VRC.SDKBase.VRC_PlayableLayerControl.BlendableLayer.FX, true)
                .TransToCurrent(rootState)
                    .AddCondition(AnimatorConditionMode.Greater, 0.5f, isDanceStr)
                    .AddCondition(AnimatorConditionMode.Greater, 0.5f, _prj.OnDanceFxEnable)

                .TransFromCurrent(rootState)
                    .AddCondition(AnimatorConditionMode.Less, 0.5f, isDanceStr)
                .TransFromCurrent(rootState)
                    .AddCondition(AnimatorConditionMode.Less, 0.5f, _prj.OnDanceFxEnable)
                ;
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
            mb.AddFolder("Menu/Dance".LL());
            int initState = (int)_tgt.ControlType;
            mb.AddToggle(_prj.DanceDetectMode, "Menu/Dance/MODE/Off".LL(), 0, ParameterSyncType.Int, initState).SetMessage(L("Menu/Dance/Message/Off"), duration: 1);
            mb.AddToggle(_prj.DanceDetectMode, "Menu/Dance/MODE/Normal".LL(), 1, ParameterSyncType.Int, initState).SetMessage(L("Menu/Dance/Message/Normal"));
            mb.AddToggle(_prj.DanceDetectMode, "Menu/Dance/MODE/Enhance".LL(), 2, ParameterSyncType.Int, initState).SetMessage(L("Menu/Dance/Message/Enhance"));
            mb.AddToggle(_prj.OnDanceFxEnable, "Menu/Dance/FxEnable".LL(), 1, ParameterSyncType.Int, (_tgt.FxEnable ? 1 : 0), false).SetMessage(L("Menu/Dance/Message/FxEnableOn"), L("Menu/Dance/Message/FxEnableOff"));
        }
    }
}