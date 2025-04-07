namespace com.github.pandrabox.pandravase.runtime
{
    public class PVDanceController : PandraComponent
    {
        public enum DaceControlType { OFF, Normal, Force };
        public string ParrentFolder;
        public DaceControlType ControlType = DaceControlType.Normal;
        public bool FxEnable = false;
    }
}