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
            _prj = VaseProject(desc).SetSuffixMode(false);
            CreateEncoder();
            CreateDecoder();
        }

        private void CreateDecoder()
        {
            BlendTreeBuilder bb = new BlendTreeBuilder(_prj, false, "Decoder", noSuffix : true);
            bb.RootDBT(() =>
            {
                foreach (var tgtp in _nBitInts) // 各コンポーネントのループ
                {
                    if (tgtp == null || tgtp.nBitInts.Length == 0) continue;
                    foreach (var tgt in tgtp.nBitInts) // コンポーネントに複数定義されているnBitIntのループ
                    {
                        if (tgt == null || tgt.TxName == null || tgt.TxName.Length == 0 || tgt.RxName == null || tgt.RxName.Length == 0 || tgt.Bit == 0) continue;
                        bb.Param("1").NName(tgt.RxName).AddD(() => {
                            for (int j = 0; j < tgt.Bit; j++) // 各Bitのループ
                            {
                                bb.Param("1").Add1D($@"{tgt.TxName}/b{j}", () =>
                                {
                                    bb.Param(0).AddAAP(tgt.RxName, 0);
                                    bb.Param(1).AddAAP(tgt.RxName, 1<<j);
                                });
                            }
                        });
                    }
                }
            });
        }
        private void CreateEncoder()
        {
            AnimatorBuilder ab= new AnimatorBuilder("Encoder");
            ab.AddLayer("PVnBitInt/Encode").AddState("Local").TransToCurrent(ab.InitialState).AddCondition(AnimatorConditionMode.Greater, 0.5f, "IsLocal");
            AnimatorState localRoot = ab.CurrentState;
            foreach (var tgtp in _nBitInts) // 各コンポーネントのループ
            {
                if (tgtp == null || tgtp.nBitInts.Length == 0) continue;
                foreach(var tgt in tgtp.nBitInts) // コンポーネントに複数定義されているnBitIntのループ
                {
                    if (tgt == null || tgt.TxName == null || tgt.TxName.Length == 0 || tgt.RxName == null || tgt.RxName.Length == 0 || tgt.Bit == 0) continue;
                    ab.AddSubStateMachine(tgt.TxName);
                    for (var i = 0; i < 1 << tgt.Bit; i++) // 1つのnBitIntをFlashEncodeする1Stateのループ
                    {
                        ab.AddState($@"Tx{i}");
                        ab.TransFromCurrent(localRoot).MoveInstant();
                        for (int j = 0; j < tgt.Bit; j++) // 各Bitのループ
                        {
                            ab.TransToCurrent(localRoot);
                            ab.AddCondition(AnimatorConditionMode.Greater, i - .5f, tgt.TxName);
                            ab.AddCondition(AnimatorConditionMode.Less, i + .5f, tgt.TxName);
                            var bit = (i >> j) & 1;
                            if (bit == 0) {
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
                    for (int j = 0; j < tgt.Bit; j++) //各bitのパラメータ定義
                    {
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
