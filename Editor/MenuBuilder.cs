﻿using UnityEditor;
using nadena.dev.modular_avatar.core;
using UnityEngine;
using UnityEditor.Animations;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using nadena.dev.ndmf.util;
using nadena.dev.ndmf;
using com.github.pandrabox.pandravase.runtime;
using static com.github.pandrabox.pandravase.editor.Util;
using System.Linq;
using VRC.SDK3.Avatars.Components;
using static com.github.pandrabox.pandravase.editor.TextureUtil;
using System.Text.RegularExpressions;
using com.github.pandrabox.pandravase.editor;

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
            mb.AddToggle("test1", 1, ParameterSyncType.Bool);
            mb.AddButton("test2", 2, ParameterSyncType.Int);
            mb.AddRadial("test3");
        }
    }
#endif

    public class MenuBuilder
    {
        private PandraProject _prj;
        private Transform CurrentFolder => folderTree.Last();
        public bool IsRoot => folderTree.Count == 0;
        private List<Transform> folderTree;
        private bool _parameterDef;
        private ModularAvatarParameters _param;
        private ModularAvatarMenuItem _currentMenu;

        public MenuBuilder(PandraProject prj, bool parameterDef = true)
        {
            _prj = prj;
            _parameterDef = parameterDef;
            folderTree = new List<Transform>();
        }

        /// <summary>
        /// Toggleの定義
        /// </summary>
        public MenuBuilder AddToggle(string parameterName, float val, ParameterSyncType parameterSyncType = ParameterSyncType.NotSynced, string menuName = null, float defaultVal = 0, bool localOnly = true)
            => AddToggleOrButton(false, parameterName, val, parameterSyncType, menuName, defaultVal, localOnly);

        /// <summary>
        /// Buttonの定義
        /// </summary>
        public MenuBuilder AddButton(string parameterName, float val, ParameterSyncType parameterSyncType = ParameterSyncType.NotSynced, string menuName = null, float defaultVal = 0, bool localOnly = true)
            => AddToggleOrButton(true, parameterName, val, parameterSyncType, menuName, defaultVal, localOnly);

        /// <summary>
        /// Radialの定義
        /// </summary>
        public MenuBuilder AddRadial(string parameterName, string menuName = null, float defaultVal = 0, bool localOnly = true)
        {
            menuName = menuName ?? parameterName;
            AddGenericMenu(menuName, (x) =>
            {
                x.Control.type = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.ControlType.RadialPuppet;
                var p = new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.Parameter();
                p.name = parameterName;
                x.Control.subParameters = new[] { p };
            });
            AddParameter(parameterName, ParameterSyncType.Float, defaultVal, localOnly);
            return this;
        }

        /// <summary>
        /// フォルダ(SubMenu)の定義
        /// </summary>
        public MenuBuilder AddFolder(string folderName, bool merge = false)
        {
            AddGenericMenu(folderName, (x) => {
                if (IsRoot)
                {
                    x.gameObject.AddComponent<ModularAvatarMenuInstaller>();
                    if (_parameterDef)
                    {
                        _param = x.gameObject.AddComponent<ModularAvatarParameters>();
                        _param.parameters = new List<ParameterConfig>();
                    }
                }
                if (merge) x.gameObject.AddComponent<PVMergeMASubMenu>();
                x.Control.type = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.ControlType.SubMenu;
                x.MenuSource = SubmenuSource.Children;
                folderTree.Add(x.transform);
            }, true);
            return this;
        }

        /// <summary>
        /// アイコンの設定
        /// </summary>
        public MenuBuilder SetIco(Texture2D ico)
        {
            _currentMenu.Control.icon = ico;
            return this;
        }

        private MenuBuilder AddToggleOrButton(bool isButton, string parameterName, float val, ParameterSyncType parameterSyncType = ParameterSyncType.NotSynced, string menuName = null, float defaultVal = 0, bool localOnly = true)
        {
            menuName = menuName ?? parameterName;
            AddGenericMenu(menuName, (x) =>
            {
                x.Control.type = isButton ? VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.ControlType.Button: VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.ControlType.Toggle;
                var p = new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.Parameter();
                p.name = parameterName;
                x.Control.parameter = p;
                x.Control.value = val;
            });
            AddParameter(parameterName, parameterSyncType, defaultVal, localOnly);
            return this;
        }

        private MenuBuilder AddParameter(string parameterName, ParameterSyncType parameterSyncType, float defaultVal = 0, bool localOnly = true)
        {
            if (!_parameterDef) return this;
            var p = new ParameterConfig();
            p.defaultValue = defaultVal;
            p.nameOrPrefix = parameterName;
            p.syncType = parameterSyncType;
            p.localOnly = localOnly;
            _param.parameters.Add(p);
            return this;
        }

        /// <summary>
        /// メニュー追加の基本操作
        /// </summary>
        public MenuBuilder AddGenericMenu(string menuName, Action<ModularAvatarMenuItem> x, bool allowRoot=false)
        {
            if (!allowRoot && IsRoot)
            {
                LowLevelDebugPrint("Root状態でMenu作成が呼ばれました。これは許可されていません");
                return this;
            }
            Transform parent = IsRoot ? _prj.PrjRootObj.transform : CurrentFolder;
            _currentMenu = CreateComponentObject<ModularAvatarMenuItem>(parent, menuName, (z) => x(z));
            _currentMenu.name = menuName;
            return this;
        }

        /// <summary>
        /// 現在のツリーの編集を完了しCurrentを上に移動する
        /// </summary>
        public MenuBuilder ExitFolder()
        {
            folderTree.RemoveAt(folderTree.Count - 1);
            return this;
        }
    }
}