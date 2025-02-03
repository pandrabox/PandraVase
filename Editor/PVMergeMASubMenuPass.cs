/// 複数・同名のGameObjectに「MergeMASubMenu」「MASubMenu」がアタッチされているとき、それをまとめる

using UnityEditor;
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
using VRC.SDK3.Avatars.ScriptableObjects;

namespace com.github.pandrabox.pandravase.editor
{
#if PANDRADBG
    public class MergeMASubMenuDebug
    {
        [MenuItem("PanDbg/PVMergeMASubMenu")]
        public static void MergeMASubMenu_Debug()
        {
            SetDebugMode(true);
            new MergeMASubMenuMain(TopAvatar);
        }
    }
#endif

    internal class PVMergeMASubMenuPass : Pass<PVMergeMASubMenuPass>
    {
        protected override void Execute(BuildContext ctx)
        {
            new MergeMASubMenuMain(ctx.AvatarDescriptor);
        }
    }

    public class MergeMASubMenuMain
    {
        public MergeMASubMenuMain(VRCAvatarDescriptor desc)
        {
            //対象を取得し、GameObject名を変更
            PVMergeMASubMenu[] targets= desc.transform.GetComponentsInChildren<PVMergeMASubMenu>();
            foreach (PVMergeMASubMenu target in targets)
            {
                if (target.OverrideName != null && target.OverrideName.Length>0)
                {
                    var c = target?.gameObject?.GetComponent<ModularAvatarMenuItem>()?.Control;
                    if(c!=null) c.name = target.OverrideName;
                    target.gameObject.name = target.OverrideName;
                }
            }
            //SubMenuItemを取得
            List<ModularAvatarMenuItem> subMenuItems = 
                targets
                .Select(o => o.GetComponent<ModularAvatarMenuItem>())
                .Where(x =>
                    x != null &&
                    x.Control.type == VRCExpressionsMenu.Control.ControlType.SubMenu &&
                    x.MenuSource == SubmenuSource.Children
                )
                .ToList();

            Dictionary<string, List<GameObject>> groupedMenuItems = new Dictionary<string, List<GameObject>>();

            // subMenuItems を名前別にグループ化
            foreach (ModularAvatarMenuItem item in subMenuItems)
            {
                string name = item.gameObject.name;
                if (!groupedMenuItems.ContainsKey(name))
                {
                    groupedMenuItems[name] = new List<GameObject>();
                }
                groupedMenuItems[name].Add(item.gameObject);
            }

            // 重複している GameObject を処理
            foreach (KeyValuePair<string, List<GameObject>> pair in groupedMenuItems)
            {
                if (pair.Value.Count > 1)
                {
                    List<GameObject> objects = pair.Value;
                    Transform parent = objects[0].transform; 
                    for (int i = objects.Count-1; i > 0; i--)
                    {
                        Transform[] children = objects[i].transform.GetComponentsInChildren<Transform>();
                        foreach (Transform child in children)
                        {
                            if (child.parent != objects[i].transform) continue; //孫以下は飛ばす
                            if (PrefabUtility.IsPartOfPrefabInstance(child.gameObject))
                            {
                                //プレハブのルートを見つける
                                GameObject prefabRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(child.gameObject);
                                //アンパック
                                PrefabUtility.UnpackPrefabInstance(prefabRoot, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                            }
                            child.SetParent(parent, false);  //親の設定
                        }
                        GameObject.DestroyImmediate(objects[i]); // 空になったものを消す
                    }
                }
            }
        }
    }
}
