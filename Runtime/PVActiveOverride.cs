#if UNITY_EDITOR
using UnityEngine;

namespace com.github.pandrabox.pandravase.runtime
{
    [DisallowMultipleComponent]
    public class PVActiveOverride : PandraComponent
    {
        public bool active = true;
    }
}
#endif