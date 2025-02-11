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
using VRC.SDKBase;

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
    }
#endif

    internal class PVnBitSyncPass : Pass<PVnBitSyncPass>
    {
        protected override void Execute(BuildContext ctx)
        {
            new PVnBitSyncMain(ctx.AvatarDescriptor);
        }
    }

    public class PVnBitSyncMain
    {
        PandraProject _prj;
        PVnBitSync[] _nBitSyncs;


        public PVnBitSyncMain(VRCAvatarDescriptor desc)
        {
            _nBitSyncs = desc.transform.GetComponentsInChildren<PVnBitSync>();
            if (_nBitSyncs.Length == 0) return;
            _prj = VaseProject(desc).SetSuffixMode(false);
            CreateEncoder();
            CreateDecoder();
        }

        private (float min, float max, float step) GetRange(PVnBitSync.PVnBitSyncData tgt)
        {
            float rmin, rmax;
            if (tgt.SyncMode == PVnBitSync.nBitSyncMode.IntMode)
            {
                rmin = 0;
                rmax = tgt.Bit ^ 2 - 1;
            }
            else if (tgt.SyncMode == PVnBitSync.nBitSyncMode.FloatMode)
            {
                rmin = 0;
                rmax = 1;
            }
            else
            {
                rmin = tgt.SyncMin;
                rmax = tgt.SyncMax;
            }
            float rstep = (rmax - rmin) / ((1 << tgt.Bit) - 1);
            return (rmin, rmax, rstep);
        }

        private BlendTreeBuilder bb;
        float _min, _max, _step;
        private void CreateDecoder()
        {
            bb = new BlendTreeBuilder(_prj, false, "Decoder");
            bb.RootDBT(() =>
            {

                foreach (var tgtp in _nBitSyncs) // 各コンポーネントのループ
                {
                    if (tgtp == null || tgtp.nBitSyncs.Count == 0) continue;
                    foreach (var tgt in tgtp.nBitSyncs) // コンポーネントに複数定義されているnBitSyncのループ
                    {
                        if (tgt == null || tgt.TxName == null || tgt.TxName.Length == 0 || tgt.RxName == null || tgt.RxName.Length == 0 || tgt.Bit == 0) continue;
                        (_min, _max, _step) = GetRange(tgt);
                        if (tgt.HostDecode == true)
                        {
                            UnitDecoder(tgt);
                        }
                        else
                        {
                            bb.Param("1").Add1D("IsLocal", () =>
                            {
                                bb.Param(0).AddD(() => UnitDecoder(tgt));
                                bb.Param(1).AssignmentBy1D(tgt.TxName, _min, _max, tgt.RxName);
                            });
                        }
                    }
                }
            });
        }

        private void UnitDecoder(PVnBitSync.PVnBitSyncData tgt)
        {
            bb.Param("1").NName(tgt.RxName).AddD(() => {
                bb.Param("1").AddAAP(tgt.RxName, _min);
                for (int j = 0; j < tgt.Bit; j++) // 各Bitのループ
                {
                    bb.Param("1").Add1D($@"{tgt.TxName}/b{j}", () =>
                    {
                        bb.Param(0).AddAAP(tgt.RxName, 0);
                        bb.Param(1).AddAAP(tgt.RxName, _step * (1 << j));
                    });
                }
            });
        }

        private void CreateEncoder()
        {
            AnimatorBuilder ab = new AnimatorBuilder("Encoder");
            ab.AddLayer("PVnBitSync/Encode").AddState("Local").TransToCurrent(ab.InitialState).AddCondition(AnimatorConditionMode.Greater, 0.5f, "IsLocal");
            //ab.AddLayer("PVnBitSync/Encode").AddState("LocalClear").TransToCurrent(ab.InitialState).AddCondition(AnimatorConditionMode.Greater, 0.5f, "IsLocal");
            //AnimatorState localClear = ab.CurrentState;
            //ab.AddState("Local").TransToCurrent(localClear).MoveInstant();
            AnimatorState localRoot = ab.CurrentState;
            foreach (var tgtp in _nBitSyncs) // 各コンポーネントのループ
            {
                if (tgtp == null || tgtp.nBitSyncs.Count == 0) continue;
                foreach (var tgt in tgtp.nBitSyncs) // コンポーネントに複数定義されているnBitSyncのループ
                {
                    if (tgt == null || tgt.TxName == null || tgt.TxName.Length == 0 || tgt.RxName == null || tgt.RxName.Length == 0 || tgt.Bit == 0) continue;
                    ab.AddAnimatorParameter(tgt.RxName); //Encode処理ではいらないのだが、Decode側に追加機能がないので確認用に定義
                    ab.AddSubStateMachine(tgt.TxName);
                    var (min, max, step) = GetRange(tgt);
                    for (var i = 0; i < 1 << tgt.Bit; i++) // 1つのnBitSyncをFlashEncodeする1Stateのループ
                    {
                        ab.AddState($@"Tx{i}");
                        ab.TransFromCurrent(localRoot).MoveInstant();
                        for (int j = 0; j < tgt.Bit; j++) // 各Bitのループ
                        {
                            ab.TransToCurrent(localRoot);
                            ab.AddCondition(AnimatorConditionMode.Greater, min + step * (i - .5f), tgt.TxName);
                            ab.AddCondition(AnimatorConditionMode.Less, min + step * (i + .50001f), tgt.TxName);
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
                    for (int j = 0; j < tgt.Bit; j++) //各bitの処理
                    {
                        ////初回0クリア
                        //ab.ChangeCurrentState(localClear);
                        //ab.SetParameterDriver($@"{tgt.TxName}/b{j}", 0);
                        //パラメータ定義
                        var MAP = _prj.GetOrCreateComponentObject<ModularAvatarParameters>("EncorderParam", (x) => {
                            if (x.parameters == null) x.parameters = new List<ParameterConfig>();
                        });
                        MAP.parameters.Add(new ParameterConfig() { nameOrPrefix = $@"{tgt.TxName}/b{j}", syncType = ParameterSyncType.Bool });
                    }
                }
            }
            ab.BuildAndAttach(_prj);
        }
    }

}
