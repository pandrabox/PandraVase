using com.github.pandrabox.pandravase.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using static com.github.pandrabox.pandravase.editor.Util;


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