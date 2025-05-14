using com.github.pandrabox.pandravase.runtime;
using nadena.dev.modular_avatar.core;
using nadena.dev.ndmf;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace com.github.pandrabox.pandravase.editor
{
    internal class PVMenuOrderOverridePass : Pass<PVMenuOrderOverridePass>
    {
        protected override void Execute(BuildContext ctx)
        {
            PanProgressBar.Show();
            new PVMenuOrderOverrideMain(ctx.AvatarDescriptor);
        }
    }

    internal class PVMenuOrderOverrideMain
    {
        private VRCAvatarDescriptor _avatarDescriptor;

        public PVMenuOrderOverrideMain(VRCAvatarDescriptor avatarDescriptor)
        {
            Log.I.StartMethod();
            _avatarDescriptor = avatarDescriptor;
            var menuOrderOverrides = _avatarDescriptor.GetComponentsInChildren<PVMenuOrderOverride>(true);
            Log.I.Info($"PVMenuOrderOverride found: {menuOrderOverrides?.Length ?? 0}");
            if (menuOrderOverrides == null || menuOrderOverrides.Length == 0) return;
            var maMenus = _avatarDescriptor.GetComponentsInChildren<ModularAvatarMenuItem>(true);
            Log.I.Info($"ModularAvatarMenuItem found: {maMenus?.Length ?? 0}");
            if (maMenus == null || maMenus.Length == 0) return;

            int folderSuccess = 0;
            int folderFail = 0;
            int reorderCount = 0;

            foreach (var orderOverride in menuOrderOverrides)
            {
                if (string.IsNullOrEmpty(orderOverride.FolderName)) continue;
                // FolderName‚Éˆê’v‚·‚éGameObject‚ð’T‚·
                var folderObj = maMenus.FirstOrDefault(m => m.gameObject.name == orderOverride.FolderName && m.Control != null && m.Control.type == VRCExpressionsMenu.Control.ControlType.SubMenu && m.MenuSource == SubmenuSource.Children)?.gameObject;
                if (folderObj == null)
                {
                    Log.I.Info($"Folder not found: {orderOverride.FolderName}");
                    folderFail++;
                    continue;
                }
                folderSuccess++;
                // Žq‚ðŽæ“¾
                var children = new List<GameObject>();
                foreach (Transform child in folderObj.transform)
                {
                    children.Add(child.gameObject);
                }
                Log.I.Info($"Children found in folder '{orderOverride.FolderName}': {children.Count}");
                // MenuOrder‚É]‚Á‚Ä•À‚×‘Ö‚¦
                var ordered = children.OrderBy(go =>
                {
                    int idx = orderOverride.MenuOrder.IndexOf(go.name);
                    return idx >= 0 ? idx : int.MaxValue;
                }).ToList();
                // •À‚×‘Ö‚¦‚ðHierarchy‚É”½‰f
                for (int i = 0; i < ordered.Count; i++)
                {
                    ordered[i].transform.SetSiblingIndex(i);
                }
                reorderCount++;
                Log.I.Info($"Reordered folder '{orderOverride.FolderName}'");
            }
            Log.I.Info($"Folders reordered: {reorderCount}, Folders not found: {folderFail}, Folders matched: {folderSuccess}");
        }
    }
}
