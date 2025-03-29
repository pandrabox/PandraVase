using com.github.pandrabox.pandravase.runtime;
/// GameObjectを管理する基底クラス ※このクラスは親のクラスがPandraProjectを持っていて初期化済みであることを前提としています
using System;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using static com.github.pandrabox.pandravase.editor.Util;

namespace com.github.pandrabox.pandravase.editor
{
    public abstract class PanGameObjects<T, R> where T : class where R : PandraComponent
    {
        public T Parent;
        public R Tgt;
        public R[] Tgts => Desc.GetComponentsInChildren<R>();
        public VRCAvatarDescriptor Desc;
        public PandraProject Prj;
        public bool Enable
        {
            get
            {
                if (Status == status.Succeeded) return true;
                Debug.LogWarning($"[PandraVase] {Parent}のデータは正常に初期化されていません【{Status}】");
                return false;
            }
        }
        private status Status = status.Undefined;
        private enum status
        {
            Undefined, Succeeded, Failed
        }
        public PanGameObjects(T p)
        {
            try
            {
                Parent = p.NullCheck();
                Prj = (p as dynamic)._prj.NullCheck("PanGameObjectsは親クラスで_prjが初期済の状態で初期化せねばなりません。");
                Desc = Prj.Descriptor.NullCheck();
                Tgt = Desc.GetComponentInChildren<R>().NullCheck();
                OnGetObjects();
                Status = status.Succeeded;
                Debug.Log($"[PandraVase] {Parent}のPanGameObjectsを取得しました");
            }
            catch (ArgumentNullException ex)
            {
                Debug.LogError(ex.Message);
                Status = status.Failed;
            }
        }
        protected abstract void OnGetObjects();
    }

    public static class NullCheckExtensions
    {
        public static T NullCheck<T>(this T obj, string additionalMsg = "")
        {
            if (obj != null)
            {
                LowLevelDebugPrint($"{obj}を取得しました {additionalMsg}");
                return obj;
            }

            var msg = $@"nullを取得しました：{obj} {additionalMsg}";
            UnityEngine.Object unityObj = obj as UnityEngine.Object;
            if (unityObj == null)
            {
                Debug.LogError(msg);
            }
            else
            {
                Debug.LogError(msg, unityObj);
            }

            EditorUtility.DisplayDialog("NullCheckExtensions", msg, "OK");

            throw new ArgumentNullException(nameof(obj), msg);
        }
    }
}