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
using static com.github.pandrabox.pandravase.runtime.Util;
using System.Linq;
using VRC.SDK3.Avatars.Components;

namespace com.github.pandrabox.pandravase.editor
{
    /// <summary>
    /// 任意のPlayableLayerを指定したControllerで置換する
    /// </summary>
    internal class ReplacePlayablePass : Pass<ReplacePlayablePass>
    {
        protected override void Execute(BuildContext ctx)
        {
            new ReplacePlayableMain(ctx.AvatarDescriptor);
        }
    }

    public class ReplacePlayableMain 
    {
        private PandraProject _prj;
        public ReplacePlayableMain(VRCAvatarDescriptor desc)
        {
            _prj = VaseProject(desc);

            // ターゲットの取得
            ReplacePlayable[] components = _prj.RootObject.GetComponentsInChildren<ReplacePlayable>(true);

            //なければ終了
            if (components.Length == 0)
            {
                _prj.DebugPrint("NothingToDo");
                return;
            }
            _prj.DebugPrint("SomethingToDo");

            //重複してたら警告
            var duplicateLayerTypes = components.GroupBy(c => c.LayerType).Where(g => g.Count() > 1).Select(g => g.Key);
            foreach (var layerType in duplicateLayerTypes)
            {
                _prj.DebugPrint($@"{layerType}において複数回ReplacePlayableしようとしています。これは、どれか1つしか成功しません。", false);
            }

            //実処理 (指定のとおりに設定、DefaultフラグをOFF)
            foreach (var component in components)
            {
                var index = _prj.PlayableIndex(component.LayerType);
                _prj.BaseAnimationLayers[index].animatorController = component.controller;
                _prj.BaseAnimationLayers[index].isDefault = false;
            }
        }
    }
}