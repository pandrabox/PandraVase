#if UNITY_EDITOR
using System;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using nadena.dev.modular_avatar.core;
using VRC.SDKBase;
using UnityEditor;
using System.Collections.Generic;
















/// ****基本的にnBitSyncを使って下さい。こちらは後で消します****













namespace com.github.pandrabox.pandravase.runtime
{
    [DisallowMultipleComponent]
    public class PVnBitInt : PandraComponent
    {
        [Serializable] 
        public class PVnBitIntData
        {
            public string TxName = "";
            public string RxName = "";
            public int Bit;
        }
        public PVnBitIntData[] nBitInts = new PVnBitIntData[1];
    }
}
#endif