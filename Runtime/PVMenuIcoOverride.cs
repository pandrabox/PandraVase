using UnityEngine;

namespace com.github.pandrabox.pandravase.runtime
{
    public class PVMenuIcoOverride : PandraComponent
    {
        public string ParameterName1;
        public string ParameterName2;
        public float? ParamValue1; //nullならParameterNameだけで確認する
        public Texture2D Ico;
        public string FolderName = null; //フォルダモードならここにフォルダ名を入れる（ParameterNameは無視されます）
        public string RadialParameterName = null; //ラジアルモードならここにパラメータを入れる(他のParameterとFolderNameは無視されます)
    }
}