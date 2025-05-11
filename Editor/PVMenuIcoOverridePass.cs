
using com.github.pandrabox.pandravase.runtime;
using nadena.dev.modular_avatar.core;
using nadena.dev.ndmf;
using System.Text;
using VRC.SDK3.Avatars.Components;


namespace com.github.pandrabox.pandravase.editor
{
    public class PVMenuIcoOverridePass : Pass<PVMenuIcoOverridePass>
    {
        protected override void Execute(BuildContext ctx)
        {
            PanProgressBar.Show();
            new PVMenuIcoOverridePassMain(ctx.AvatarDescriptor);
        }
    }
    public class PVMenuIcoOverridePassMain
    {
        public PVMenuIcoOverridePassMain(VRCAvatarDescriptor desc)
        {
            var icoOverrides = desc.GetComponentsInChildren<PVMenuIcoOverride>();
            if (icoOverrides == null || icoOverrides.Length == 0) return;
            var maMenus = desc.GetComponentsInChildren<ModularAvatarMenuItem>();
            if (maMenus == null || maMenus.Length == 0) return;
            foreach (PVMenuIcoOverride icoOverride in icoOverrides)
            {
                int overrideCount = 0;
                //icoOverrideにはParameterName1, ParameterName2, Icoが入っている
                foreach (var maMenu in maMenus)
                {
                    if (icoOverride.FolderName != null) //フォルダモード
                    {
                        var folderName = maMenu.gameObject.name;
                        if (folderName != icoOverride.FolderName) continue;
                    }
                    else if (icoOverride.RadialParameterName != null) //ラジアルモード
                    {
                        var subParams = maMenu.Control.subParameters;
                        var subParam = subParams != null && subParams.Length > 0 ? subParams[0] : null;
                        if (subParam == null) continue;
                        var radialParamName = subParam.name;
                        if (radialParamName != icoOverride.RadialParameterName) continue;
                    }
                    else
                    {
                        var menuParam = maMenu.Control.parameter;
                        if (menuParam.name != icoOverride.ParameterName1) continue;
                        var subParams = maMenu.Control.subParameters;
                        var subParam = subParams != null && subParams.Length > 0 ? subParams[0] : null;

                        var safeSubParamName = (subParam == null) ? "" : subParam.name;
                        var safeParam2Name = (icoOverride.ParameterName2 == null) ? "" : icoOverride.ParameterName2;
                        if (safeSubParamName != safeParam2Name) continue;

                        if (icoOverride.ParamValue1 != null)
                        {
                            if (maMenu.Control.value != icoOverride.ParamValue1) continue;
                        }
                    }
                    maMenu.Control.icon = icoOverride.Ico;
                    overrideCount++;
                    Log.I.Info($@"Override Ico: {icoOverride.Ico.name} to {maMenu.Control.name}");
                }
                if (overrideCount != 1)
                {
                    StringBuilder warnMsg = new StringBuilder();
                    warnMsg.Append($@"想定外の上書き回数：{overrideCount},");
                    if (icoOverride.ParameterName1 != null)
                    {
                        warnMsg.Append($@"\nParameterName1: {icoOverride.ParameterName1},");
                        warnMsg.Append($@"\nParameterName2: {icoOverride.ParameterName2},");
                        warnMsg.Append($@"\nParamValue1: {icoOverride.ParamValue1}");
                    }
                    else
                    {
                        warnMsg.Append($@"\nFolderName: {icoOverride.FolderName}");
                    }
                    Log.I.Warning(warnMsg.ToString());
                }
            }
        }
    }
}