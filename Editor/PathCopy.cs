using UnityEditor;
using UnityEngine;


namespace com.github.pandrabox.pandravase.editor
{

#if PANDRADBG
    public class PathCopy
    {
        [MenuItem("Assets/PanDev/PathCopy")]
        public static void AnimationClipAnalyzer()
        {
            var selectedObject = Selection.activeObject;
            if (selectedObject == null) return;
            string result = AssetDatabase.GetAssetPath(selectedObject);
            Debug.LogWarning(result);
            EditorGUIUtility.systemCopyBuffer = result;
        }
    }
#endif
}