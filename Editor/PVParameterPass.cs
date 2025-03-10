///PVParameterは基本的にModularAvatarParametersにそのまま置換されます
///これを使うBenefitはnullableなことです。
///nullを指定している場合は、float,localonly=true,default=0,saved=falseとして扱いますが、重複がある場合は上書きします

using com.github.pandrabox.pandravase.runtime;
using nadena.dev.modular_avatar.core;
using nadena.dev.ndmf;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using VRC.SDK3.Avatars.Components;
using static com.github.pandrabox.pandravase.editor.Util;

namespace com.github.pandrabox.pandravase.editor
{
#if PANDRADBG
    public class PVParameterDebug
    {
        [MenuItem("PanDbg/PVParameter")]
        public static void PVParameter_Debug()
        {
            SetDebugMode(true);
            new PVParameterMain(TopAvatar);
        }
    }
#endif

    internal class PVParameterPass : Pass<PVParameterPass>
    {
        protected override void Execute(BuildContext ctx)
        {
            PanProgressBar.Show();
            new PVParameterMain(ctx.AvatarDescriptor);
        }
    }

    public class PVParameterMain
    {
        VRCAvatarDescriptor _desc;
        List<PVParameter> _tgts;
        List<PVParameter> _uniques;
        PandraProject _prj;
        public PVParameterMain(VRCAvatarDescriptor desc)
        {
            _desc = desc;
            _tgts = _desc.GetComponentsInChildren<PVParameter>().ToList();
            if(_tgts.Count == 0) return;
            GetUniques();
            CreateModularAvatarParameters();
        }
        private void GetUniques()
        {
            _uniques = new List<PVParameter>();
            foreach (var tgt in _tgts)
            {
                if (!_uniques.Any(x => x.name == tgt.name))
                {
                    _uniques.Add(tgt);
                }
                else
                {
                    //重複時の処理
                    var currentParam = _uniques.First(x => x.name == tgt.name);
                    if (tgt.syncType != null)
                    {
                        if (currentParam.syncType == null)
                        {
                            currentParam.syncType = tgt.syncType;
                        }
                        else if (currentParam.syncType != tgt.syncType)
                        {
                            var msg = $"@@WARNING@@,PVParameter,{tgt.name}で型の競合があったので、floatにしました,{currentParam.syncType},{tgt.syncType}";
                            LowLevelDebugPrint(msg);
                            currentParam.syncType = ParameterSyncType.Float;
                        }
                    }
                    if (tgt.defaultValue != null)
                    {
                        if (currentParam.defaultValue == null)
                        {
                            currentParam.defaultValue = tgt.defaultValue;
                        }
                        else if (currentParam.defaultValue != tgt.defaultValue)
                        {
                            var msg = $"@@WARNING@@,PVParameter,{tgt.name}でデフォルト値の競合があったので、先に定義した{currentParam.defaultValue}にしました。";
                            LowLevelDebugPrint(msg);
                        }
                    }
                    if (tgt.localOnly != null)
                    {
                        if (currentParam.localOnly == null)
                        {
                            currentParam.localOnly = tgt.localOnly;
                        }
                        else
                        {
                            currentParam.localOnly &= tgt.localOnly; //localOnlyはGlobalを優先
                        }
                    }
                    if (tgt.saved != null)
                    {
                        if (currentParam.saved == null)
                        {
                            currentParam.saved = tgt.saved;
                        }
                        else
                        {
                            currentParam.saved |= tgt.saved; // SavedはTrueを優先
                        }
                    }

                }
            }
        }
        private void CreateModularAvatarParameters()
        {
            _prj = VaseProject(_desc).SetSuffixMode(false);
            ModularAvatarParameters map = _prj.CreateComponentObject<ModularAvatarParameters>("MAP_BY_PVParameter");
            map.parameters = new List<ParameterConfig>();
            foreach (var param in _uniques)
            {
                var pcon = new ParameterConfig()
                {
                    nameOrPrefix = param.name,
                    syncType = param.syncType ?? ParameterSyncType.Float,
                    defaultValue = param.defaultValue ?? 0,
                    localOnly = param.localOnly ?? true,
                    saved = param.saved ?? false
                };
                map.parameters.Add(pcon);
                LowLevelDebugPrint($"PVParameter,Define Parameter:{pcon.nameOrPrefix},{pcon.syncType}({pcon.defaultValue}),{(pcon.localOnly ? "Local" : "Global")},{(pcon.saved ? "saved" : "temp")}");
            }
        }
    }
}