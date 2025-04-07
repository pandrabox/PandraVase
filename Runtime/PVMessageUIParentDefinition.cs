#if UNITY_EDITOR
namespace com.github.pandrabox.pandravase.runtime
{
    public class PVMessageUIParentDefinition : PandraComponent
    {
        public string ParentFolder;
        public bool DefaultActive = true;
        public float DefaultSize = .4f;
    }
}
#endif