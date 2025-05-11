using com.github.pandrabox.pandravase.runtime;
using nadena.dev.modular_avatar.core;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using static com.github.pandrabox.pandravase.editor.Util;

namespace com.github.pandrabox.pandravase.editor
{
#if PANDRADBG
    public static class MenuBuilderDebugC
    {
        [MenuItem("PanDbg/MenuBuilder")]
        public static void MenuBuilderDebug()
        {
            var prj = new PandraProject(TopAvatar, "test", ProjectTypes.VPM); //ProjectRootObj以下に作成するのでprjは本質的に必要。
            prj.SetDebugMode(true);

            var mb = new MenuBuilder(prj);
            mb.AddFolder("FlatsPlus", true).AddFolder("test");
            mb.AddToggle("test1");
            mb.AddButton("test2", 2f, ParameterSyncType.Int);
            mb.AddRadial("test3");
        }
    }
#endif

    public class MenuBuilder
    {
        private PandraProject _prj;
        private Transform CurrentFolder => folderTree.Last();
        private bool IsRoot => folderTree.Count == 0;
        private List<Transform> folderTree;
        private bool _parameterDef;
        private ModularAvatarParameters _param;
        private ModularAvatarMenuItem _currentMenu;
        private string _currentParameterName;
        private float _currentValue;
        private string _parentFolder;
        private bool _isPC;

        public MenuBuilder(PandraProject prj, bool parameterDef = true, string parentFolder = null)
        {
            BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
            _isPC = (target == BuildTarget.StandaloneWindows ||
                   target == BuildTarget.StandaloneWindows64 ||
                   target == BuildTarget.StandaloneOSX ||
                   target == BuildTarget.StandaloneLinux64);
            if (!_isPC)
            {
                Log.I.Info("Questビルド時MenuBuilderはスキップされます。");
                _prj = null;
                folderTree = null;
                return;
            }
            _prj = prj;
            _parameterDef = parameterDef;
            _parentFolder = parentFolder;
            folderTree = new List<Transform>();


            CreateParent();
        }

        private MenuBuilder CreateParent()
        {
            if (!_isPC || _prj == null || folderTree == null)  return this;
            if (_parentFolder == null || _parentFolder == "") return this;
            var pf = _parentFolder.Split('/');
            foreach (var p in pf)
            {
                AddFolder(p, true);
            }
            return this;
        }

        /// <summary>
        /// Radialの定義
        /// </summary>
        public MenuBuilder AddRadial(string parameterName, string menuName = null, float defaultVal = 0, bool localOnly = true, string mainParameterName = null)
        {
            if (!_isPC || _prj == null || folderTree == null) return this;
            menuName = menuName ?? parameterName;
            AddGenericMenu(menuName, (x) =>
            {
                x.Control.type = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.ControlType.RadialPuppet;
                var p = new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.Parameter();
                p.name = parameterName;
                x.Control.subParameters = new[] { p };

                _currentParameterName = mainParameterName ?? $@"Vase/MessageUI/Radial/{parameterName}";
                _currentValue = 1;
                var p2 = new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.Parameter();
                p2.name = _currentParameterName;
                x.Control.parameter = p2;
                x.Control.value = _currentValue;
                _prj.AddParameter(_currentParameterName, ParameterSyncType.Bool, localOnly, 0);
                //AddParameter(_currentParameterName, ParameterSyncType.Bool, 0, localOnly);
            });


            _prj.AddParameter(parameterName, ParameterSyncType.Float, localOnly, defaultVal);
            //AddParameter(parameterName, ParameterSyncType.Float, defaultVal, localOnly);
            return this;
        }

        public MenuBuilder Add2Axis(string parameter1, string parameter2, string mainParameter, string menuName = null, float defaultVal1 = 0, float defaultVal2 = 0, bool localOnly = true)
        {
            if (!_isPC || _prj == null || folderTree == null) return this;
            menuName = menuName ?? parameter1;
            AddGenericMenu(menuName, (x) =>
            {
                x.Control.type = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.ControlType.TwoAxisPuppet;
                var p1 = new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.Parameter();
                p1.name = parameter1;
                var p2 = new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.Parameter();
                p2.name = parameter2;
                x.Control.subParameters = new[] { p1, p2 };

                _currentParameterName = mainParameter;
                _currentValue = 1;
                var p3 = new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.Parameter();
                p3.name = _currentParameterName;
                x.Control.parameter = p3;
                x.Control.value = _currentValue;
                _prj.AddParameter(_currentParameterName, ParameterSyncType.Bool, localOnly, 0);
                //AddParameter(_currentParameterName, ParameterSyncType.Bool, 0, localOnly);
            });
            _prj.AddParameter(parameter1, ParameterSyncType.Float, localOnly, defaultVal1);
            //AddParameter(parameter1, ParameterSyncType.Float, defaultVal1, localOnly);
            _prj.AddParameter(parameter2, ParameterSyncType.Float, localOnly, defaultVal2);
            //AddParameter(parameter2, ParameterSyncType.Float, defaultVal2, localOnly);
            return this;
        }

        /// <summary>
        /// フォルダ(SubMenu)の定義
        /// </summary>
        public MenuBuilder AddFolder(string folderName, bool merge = false)
        {
            if (!_isPC || _prj == null || folderTree == null) return this;
            AddGenericMenu(folderName, (x) =>
            {
                if (IsRoot)
                {
                    x.gameObject.AddComponent<ModularAvatarMenuInstaller>();
                    if (_parameterDef)
                    {
                        _param = _prj.CreateComponentObject<ModularAvatarParameters>("MenuBuilderParam"); //既存ObjだとMergeで削除されるので作成
                        _param.parameters = new List<ParameterConfig>();
                    }
                }
                if (merge) x.gameObject.AddComponent<PVMergeMASubMenu>();
                x.Control.type = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.ControlType.SubMenu;
                x.MenuSource = SubmenuSource.Children;


                _currentParameterName = $@"Vase/MessageUI/Folder/{folderName}";
                _currentValue = 1;
                var p = new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.Parameter();
                p.name = _currentParameterName;
                x.Control.parameter = p;
                x.Control.value = _currentValue;
                _prj.AddParameter(_currentParameterName, ParameterSyncType.Bool, true, 0);

                folderTree.Add(x.transform);
            }, true);
            return this;
        }

        /// <summary>
        /// カレントメニューに対するメッセージの設定
        /// </summary>
        public MenuBuilder SetMessage(string message
            , string inverseMessage = null
            , float duration = 3
            , bool inactiveByParameter = true
            , bool isRemote = false
            , Color? textColor = null
            , Color? outlineColor = null)
        {
            if (!_isPC || _prj == null || folderTree == null) return this;
            _prj.SetMessage(message, _currentParameterName, AnimatorConditionMode.Equals, _currentValue, duration, inactiveByParameter, isRemote, textColor, outlineColor);
            if (inverseMessage != null) _prj.SetMessage(inverseMessage, _currentParameterName, AnimatorConditionMode.NotEqual, _currentValue, duration, inactiveByParameter, isRemote, textColor, outlineColor);
            return this;
        }

        /// <summary>
        /// アイコンの設定
        /// </summary>
        public MenuBuilder SetIco(Texture2D ico)
        {
            if (!_isPC || _prj == null || folderTree == null) return this;
            ico.wrapMode = TextureWrapMode.Clamp;
            _currentMenu.Control.icon = ico;
            return this;
        }

        /// <summary>
        /// 現在のツリーの編集を完了しCurrentを上に移動する
        /// </summary>
        public MenuBuilder ExitFolder()
        {
            if (!_isPC || _prj == null || folderTree == null) return this;
            folderTree.RemoveAt(folderTree.Count - 1);
            return this;
        }


        #region AddToggleOrButton

        /// <summary>
        /// Toggleの定義
        /// </summary>
        public MenuBuilder AddToggle(string parameterName, string menuName = null, float? val = null, ParameterSyncType? parameterSyncType = null, float? defaultVal = null, bool? localOnly = null)
            => AddToggleOrButton(false, parameterName, val, parameterSyncType, menuName, defaultVal, localOnly);

        /// <summary>
        /// Buttonの定義
        /// </summary>
        public MenuBuilder AddButton(string parameterName, string menuName = null, float? val = null) => AddButton(parameterName, val, null, menuName, null, null);
        public MenuBuilder AddButton(string parameterName, float? val = null, ParameterSyncType? parameterSyncType = null, string menuName = null, float? defaultVal = null, bool? localOnly = null)
            => AddToggleOrButton(true, parameterName, val, parameterSyncType, menuName, defaultVal, localOnly);

        private MenuBuilder AddToggleOrButton(bool isButton, string parameterName, float? val = null, ParameterSyncType? parameterSyncType = null, string menuName = null, float? defaultVal = null, bool? localOnly = null)
        {
            if (!_isPC || _prj == null || folderTree == null) return this;
            _currentValue = val ?? 1;
            _currentParameterName = parameterName;
            Log.I.Info($@"AddToggleOrButton({(isButton ? "Button" : "Toggle")},Param:{_currentParameterName},Val:{_currentValue},SyncType:{parameterSyncType},MenuName:{menuName},Default:{defaultVal},Local:{localOnly})");
            menuName = menuName ?? parameterName;
            AddGenericMenu(menuName, (x) =>
            {
                x.Control.type = isButton ? VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.ControlType.Button : VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.ControlType.Toggle;
                var p = new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.Parameter();
                p.name = _currentParameterName;
                x.Control.parameter = p;
                x.Control.value = _currentValue;
            });
            _prj.AddParameter(_currentParameterName, parameterSyncType, localOnly, defaultVal);
            return this;
        }

        #endregion

        /// <summary>
        /// ExpressionParameterの定義
        /// </summary>
        [Obsolete("Use _prj.AddParameter")]
        private MenuBuilder AddParameter(string parameterName, ParameterSyncType parameterSyncType, float defaultVal = 0, bool localOnly = true)
        {

            if (!_parameterDef) return this;
            var p = new ParameterConfig();
            p.defaultValue = defaultVal;
            p.nameOrPrefix = parameterName;
            p.syncType = parameterSyncType;
            p.localOnly = localOnly;
            _param.parameters.Add(p);
            _prj.DebugPrint($@"MenuBuilderはExパラメータ{parameterName}({parameterSyncType},{defaultVal},{localOnly})を定義しました");
            return this;
        }

        /// <summary>
        /// メニュー追加の基本操作
        /// </summary>
        private MenuBuilder AddGenericMenu(string menuName, Action<ModularAvatarMenuItem> x, bool allowRoot = false)
        {
            if (!_isPC || _prj == null || folderTree == null) return this;
            if (!allowRoot && IsRoot)
            {
                Log.I.Error("Root状態でMenu作成が呼ばれました。これは許可されていません。最低１回のAddFolderを実行してください。");
                return this;
            }
            menuName = menuName.LastName();
            Transform parent = IsRoot ? _prj.PrjRootObj.transform : CurrentFolder;
            _currentMenu = CreateComponentObject<ModularAvatarMenuItem>(parent, menuName, (z) => x(z));
            _currentMenu.name = menuName;

            Log.I.Info($@"MenuBuilderはメニュー{menuName}を生成しました");
            return this;
        }
    }
}