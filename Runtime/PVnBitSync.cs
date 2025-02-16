#if UNITY_EDITOR
using System;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using nadena.dev.modular_avatar.core;
using VRC.SDKBase;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace com.github.pandrabox.pandravase.runtime
{
    [DisallowMultipleComponent]
    public class PVnBitSync : PandraComponent
    {
        [Serializable]
        public class PVnBitSyncData
        {
            public string TxName = "";
            public string RxName = ""; //後でEditor拡張で{TxName}Rxに固定する
            public int Bit;
            public bool HostDecode = false;
            public nBitSyncMode SyncMode = nBitSyncMode.IntMode;
            public float SyncMin = 0.0f;
            public float SyncMax = 1.0f;
            public float Min => SyncMode == nBitSyncMode.Custom ? SyncMin : 0.0f;
            public float Max => SyncMode == nBitSyncMode.Custom ? SyncMax : SyncMode == nBitSyncMode.IntMode ? (1 << Bit) - 1 : 1.0f;
            public float Step => (Max - Min) / ((1 << Bit) - 1);
            public bool ToggleSync = false; //trueなら{TxName}SyncがONの時だけ同期する
            public string SyncParameter => $@"{TxName}Sync";
        }

        public enum nBitSyncMode
        {
            IntMode = 0,
            FloatMode = 1,
            Custom = 2
        }

        public List<PVnBitSyncData> nBitSyncs = new List<PVnBitSyncData> {};

        /// <summary>
        /// スクリプトアクセス用
        /// </summary>
        /// <param name="txName">同期するパラメータ名。同期先は{TxName}Rxです。</param>
        /// <param name="Bit">同期に使用するBit数</param>
        /// <param name="hostDecode">ON：HostとRemoteのRx値(状態)が一致する、OFF：HostのRxはTxが入り、滑らかで低負荷だが状態がずれる</param>
        /// <param name="syncMode">intなら0～2^bit-1, floatなら0～1, Customならminmax範囲を同期</param>
        /// <param name="min">Custom時の最小値</param>
        /// <param name="max">Custom時の最大値</param>
        /// <param name="ToggleSync">trueなら{TxName}SyncがONの時だけ同期する</param>
        public PVnBitSyncData Set(string txName, int Bit, nBitSyncMode syncMode, bool hostDecode = false, float min = 0.0f, float max = 1.0f, bool ToggleSync = false)
        {
            if (nBitSyncs == null) nBitSyncs = new List<PVnBitSyncData>();
            var p = new PVnBitSyncData();
            p.TxName = txName;
            p.RxName = $@"{txName}Rx";
            p.Bit = Bit;
            p.SyncMode = syncMode;
            p.HostDecode = hostDecode;
            p.SyncMin = min;
            p.SyncMax = max;
            p.ToggleSync = ToggleSync;
            nBitSyncs.Add(p);
            return p;
        }
    }
}
#endif