using UnityEngine;

namespace com.github.pandrabox.pandravase.runtime
{
    public class PVGridUI : PandraComponent
    {
        public string ParameterName;
        public int xMax, yMax, ItemCount;
        public Texture2D MainTex, LockTex;
        public bool nVirtualSync;

        public float MenuSize = 0.35f;
        public float LockSize = 0.08f;
        public float MenuOpacity = 0.85f;
        public Color SelectColor = new Color(0, 210, 255, 200);

        public float Speed = 0.03f;
        public float DeadZone = 0.3f;
        //[HideInInspector]
        public bool CreateSampleMenu;

        public string Inputx => $@"{ParameterName}/x";
        public string Inputy => $@"{ParameterName}/y";
        public string Currentx => $@"{ParameterName}/Cx";
        public string Currenty => $@"{ParameterName}/Cy";
        public string Quantizedx => $@"{Currentx}Quantized";
        public string Quantizedy => $@"{Currenty}Quantized";
        public string QuantLimitedy => $@"{Currenty}QuantLimitedy";
        public string n => $@"{ParameterName}/n";
        public string nRx => $@"{ParameterName}/nRx";
        public string IsMode0 => $@"{ParameterName}/isMode0";
        public string IsEnable => $@"{ParameterName}/isEnable";
        public string Reset => $@"{ParameterName}/reset";
    }
}