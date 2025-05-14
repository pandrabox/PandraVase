#if UNITY_EDITOR
using nadena.dev.modular_avatar.core;
using System.Collections.Generic;

namespace com.github.pandrabox.pandravase.runtime
{
    public class PVMenuOrderOverride : PandraComponent
    {
        public List<string> MenuOrder = new List<string>();
        public string FolderName;
    }
}
#endif