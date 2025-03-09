#if UNITY_EDITOR
using UnityEngine;

namespace com.github.pandrabox.pandravase.runtime
{
    [DisallowMultipleComponent]
    public class PVPlayableRemover : PandraComponent
    {
        public string[] BaseLayer;
        public string[] AdditiveLayer;
        public string[] GestureLayer;
        public string[] ActionLayer;
        public string[] FXLayer;
    }
}
#endif