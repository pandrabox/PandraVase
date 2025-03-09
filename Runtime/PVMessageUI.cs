#if UNITY_EDITOR
using UnityEditor.Animations;
using UnityEngine;

namespace com.github.pandrabox.pandravase.runtime
{
    public class PVMessageUI : PandraComponent
    {
        public string Message;
        public float DisplayDuration = 5;
        public bool InactiveByParameter = true;
        public bool IsRemote = false; //基本的には触れない
        public string ParameterName;
        public AnimatorConditionMode ConditionMode;
        public float ParameterValue;
        public Color TextColor = Color.white;
        public Color OutlineColor = Color.black;
    }
}
#endif