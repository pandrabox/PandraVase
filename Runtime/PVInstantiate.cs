#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace com.github.pandrabox.pandravase.runtime
{
    public class PVInstantiate : PandraComponent
    {
        public List<GameObject> prefabs;
    }
}
#endif