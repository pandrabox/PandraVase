using System;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using nadena.dev.modular_avatar.core;
using VRC.SDKBase;

namespace com.github.pandrabox.pandravase.runtime
{
    public class PVGridUI : PandraComponent
    {
        public string ParameterName;
        public int xMax, yMax;
        public Texture2D MainTex, LockTex;
        public bool nVirtualSync;
        public float speed = 0.3f;
        public float deadZone = 0.2f;
        //[HideInInspector]
        public bool CreateSampleMenu;

        public string Inputx => $@"{ParameterName}/x";
        public string Inputy => $@"{ParameterName}/y";
        public string Currentx => $@"{ParameterName}/Cx";
        public string Currenty => $@"{ParameterName}/Cy";
        public string Quantizedx => $@"{Currentx}Quantized";
        public string Quantizedy => $@"{Currenty}Quantized";
        public string n => $@"{ParameterName}/n";
        public string IsMode0 => $@"{ParameterName}/isMode0";
        public string IsEnable => $@"{ParameterName}/isEnable";
    }
}