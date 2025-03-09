/// floatをnBitで他のfloatに仮想同期する
/// 注意：範囲外の値が入ると前の値のままになる

using com.github.pandrabox.pandravase.runtime;
using nadena.dev.modular_avatar.core;
using nadena.dev.ndmf;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using VRC.SDK3.Avatars.Components;
using static com.github.pandrabox.pandravase.editor.Util;

namespace com.github.pandrabox.pandravase.editor
{
#if PANDRADBG
    public class PVnBitSyncDebug
    {
        [MenuItem("PanDbg/PVnBitSync")]
        public static void PVnBitSync_Debug()
        {
            SetDebugMode(true);
            new PVnBitSyncMain(TopAvatar);
        }
        [MenuItem("PanDbg/PVnBitSync2")]
        public static void PVnBitSync2_Debug()
        {
            var prj = VaseProject(TopAvatar);
            prj.SetDebugMode(true);
            var s = prj.VirtualSync("test", 3, PVnBitSync.nBitSyncMode.FloatMode, toggleSync: true, hostDecode: true);
            new MenuBuilder(prj).AddFolder("test").AddRadial("test").AddToggle(s.SyncParameter, 1, ParameterSyncType.Bool);
            if (Msgbox("実体化しますか？", true) == true)
            {
                new PVnBitSyncMain(TopAvatar);
            }

        }
    }
#endif

    internal class PVnBitSyncPass : Pass<PVnBitSyncPass>
    {
        protected override void Execute(BuildContext ctx)
        {
            PanProgressBar.Show();
            new PVnBitSyncMain(ctx.AvatarDescriptor);
        }
    }

    public class PVnBitSyncMain
    {
        private PandraProject _prj;
        private PVnBitSync[] _nBitSyncs;


        public PVnBitSyncMain(VRCAvatarDescriptor desc)
        {
            _nBitSyncs = desc.transform.GetComponentsInChildren<PVnBitSync>();
            if (_nBitSyncs.Length == 0) return;
            _prj = VaseProject(desc).SetSuffixMode(false);
            CreateEncoder();
            CreateDecoder();
        }

        private void CreateDecoder()
        {
            var bb = new BlendTreeBuilder("Decoder");
            bb.RootDBT(() =>
            {
                bb.NName("nBitSync");
                foreach (var tgtp in _nBitSyncs) // 各コンポーネントのループ
                {
                    if (tgtp == null || tgtp.nBitSyncs.Count == 0) continue;
                    foreach (var tgt in tgtp.nBitSyncs) // コンポーネントに複数定義されているnBitSyncのループ
                    {
                        if (tgt == null || tgt.TxName == null || tgt.TxName.Length == 0 || tgt.RxName == null || tgt.RxName.Length == 0 || tgt.Bit == 0) continue;
                        if (tgt.HostDecode == true)
                        {
                            bb.Param("1");
                            UnitDecoder(bb, tgt);
                        }
                        else
                        {
                            bb.Param("1").Add1D("IsLocal", () =>
                            {
                                bb.Param(0);
                                UnitDecoder(bb, tgt);
                                bb.NName("ParameterCopy").Param(1).AssignmentBy1D(tgt.TxName, tgt.Min, tgt.Max, tgt.RxName);
                            });
                        }
                    }
                }
            });
            bb.Attach(_prj);
        }

        private void UnitDecoder(BlendTreeBuilder bb, PVnBitSync.PVnBitSyncData tgt)
        {
            if (tgt == null || tgt.RxName == null || tgt.RxName.Length == 0) return;
            bb.NName($@"Decode{tgt.RxName}").AddD(() =>
            {
                bb.Param("1").AddAAP(tgt.RxName, tgt.Min);
                for (int j = 0; j < tgt.Bit; j++)
                {
                    bb.Param("1").Add1D($@"{tgt.TxName}/b{j}", () =>
                    {
                        bb.Param(0).AddAAP(tgt.RxName, 0);
                        bb.Param(1).AddAAP(tgt.RxName, tgt.Step * (1 << j));
                    });
                }
            });
        }

        private void CreateEncoder()
        {
            AnimatorBuilder ab = new AnimatorBuilder("Encoder");
            ab.AddLayer("PVnBitSync/Encode").AddState("Local")
                .TransToCurrent(ab.InitialState).AddCondition(AnimatorConditionMode.Greater, 0.5f, "IsLocal");
            AnimatorState localRoot = ab.CurrentState;
            foreach (var tgtp in _nBitSyncs) // 各コンポーネントのループ
            {
                if (tgtp == null || tgtp.nBitSyncs.Count == 0) continue;
                foreach (var tgt in tgtp.nBitSyncs) // コンポーネントに複数定義されているnBitSyncのループ
                {
                    if (tgt == null || tgt.TxName == null || tgt.TxName.Length == 0 || tgt.RxName == null || tgt.RxName.Length == 0 || tgt.Bit == 0) continue;
                    ab.AddAnimatorParameter(tgt.RxName); //Encode処理ではいらないのだが、Decode側で追加する機能がないので確認用に定義
                    ab.AddSubStateMachine(tgt.TxName);
                    for (var i = 0; i < 1 << tgt.Bit; i++) // 1つのnBitSyncをFlashEncodeする1Stateのループ
                    {
                        ab.AddState($@"Tx{i}");
                        ab.TransFromCurrent(localRoot).MoveInstant();
                        for (int j = 0; j < tgt.Bit; j++) // 各Bitのループ
                        {
                            ab.TransToCurrent(localRoot);
                            ab.AddCondition(AnimatorConditionMode.Greater, tgt.Min + tgt.Step * (i - .5f), tgt.TxName);
                            ab.AddCondition(AnimatorConditionMode.Less, tgt.Min + tgt.Step * (i + .50001f), tgt.TxName);
                            if (tgt.ToggleSync)
                            {
                                ab.AddCondition(AnimatorConditionMode.Greater, .5f, tgt.SyncParameter);
                            }
                            var bit = (i >> j) & 1;
                            if (bit == 0)
                            {
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
                    if (tgt.ToggleSync) //SyncSwitchがあり、OFFの時の処理（最小値を送信）
                    {
                        ab.AddState($@"NoSync");
                        ab.TransFromCurrent(localRoot).MoveInstant();
                        int i = 0;
                        for (int j = 0; j < tgt.Bit; j++)
                        {
                            ab.TransToCurrent(localRoot);
                            ab.AddCondition(AnimatorConditionMode.Less, .5f, tgt.SyncParameter);
                            var bit = (i >> j) & 1;
                            if (bit == 0)
                            {
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
                    //各bitを同期boolで定義
                    for (int j = 0; j < tgt.Bit; j++)
                    {
                        var MAP = _prj.GetOrCreateComponentObject<ModularAvatarParameters>("EncorderParam", (x) =>
                        {
                            if (x.parameters == null) x.parameters = new List<ParameterConfig>();
                        });
                        MAP.parameters.Add(new ParameterConfig() { nameOrPrefix = $@"{tgt.TxName}/b{j}", syncType = ParameterSyncType.Bool });
                    }
                }
            }
            ab.Attach(_prj);
        }
    }
}
