#if UNITY_EDITOR
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