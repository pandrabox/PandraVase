///PVParameterは基本的にModularAvatarParametersにそのまま置換されます
///これを使うBenefitはnullableなことです。
///nullを指定している場合は、float,localonly=true,default=0,saved=falseとして扱いますが、重複がある場合は上書きします
#if UNITY_EDITOR
using nadena.dev.modular_avatar.core;

namespace com.github.pandrabox.pandravase.runtime
{
    public class PVParameter : PandraComponent
    {
        public string ParameterName;
        public ParameterSyncType? syncType;
        public bool? localOnly;
        public float? defaultValue;
        public bool? saved;
    }
}
#endif

//public enum ParameterSyncType
//{
//    NotSynced,
//    Int,
//    Float,
//    Bool,
//}