//コンソールをクリアして規定のプレハブをアップロードする
using System.Linq;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using static com.github.pandrabox.pandravase.editor.Util;

namespace com.github.pandrabox.pandravase.editor
{
    public class ShowOnlyMenuDefinition
    {
        [MenuItem("GameObject/Pan/ShowOnly", false, 0)]
        private static void ShowOnlyM()
        {
            new ShowOnly(Selection.activeGameObject, false);
        }
        [MenuItem("GameObject/Pan/ShowOnlyRelease", false, 0)]
        private static void ShowOnlyReleaseM()
        {
            new ShowOnly(Selection.activeGameObject, true);
        }
    }

    public class ShowOnly
    {
        public ShowOnly(GameObject tgt, bool releace = false)
        {
            var tgtDesc = GetAvatarDescriptor(tgt);
            var allAvatar = Resources.FindObjectsOfTypeAll<VRCAvatarDescriptor>()
                .Where(avatar => avatar.gameObject.scene.IsValid()) // シーンに属しているオブジェクトのみ取得
                .ToArray();
            foreach (var a in allAvatar)
            {
                bool active = releace || (tgtDesc != null && a == tgtDesc);
                SetEditorOnly(a.gameObject, !active);
            }
        }
    }
}