using com.github.pandrabox.pandravase.runtime;
using System.Diagnostics;
using UnityEditor;


namespace com.github.pandrabox.pandravase.editor
{

    public static class PanProgressBar
    {
        private static int TotalCount;
        private static int CurrentCount;
        private static float progress => (float)CurrentCount / (TotalCount == 0 ? 1 : TotalCount);
        private const string Title = "PandraVase Running";
        public static void SetTotalCount(int count)
        {
            TotalCount = count;
            CurrentCount = 0;
        }
        public static void Show(int? count = null)
        {
            if (count != null) SetTotalCount((int)count);
            CurrentCount++;
            StackTrace stackTrace = new StackTrace();
            string callerClassName = stackTrace.GetFrame(1).GetMethod().DeclaringType.Name;
            if (callerClassName.StartsWith("PV"))
            {
                callerClassName = callerClassName.Substring(2);
            }
            if (callerClassName.EndsWith("Pass"))
            {
                callerClassName = callerClassName.Substring(0, callerClassName.Length - 4);
            }
            Log.I.Info($"PanProgressBar.Show,{callerClassName}");
            EditorUtility.DisplayProgressBar(Title, callerClassName, progress);
        }
        public static void Hide()
        {
            EditorUtility.ClearProgressBar();
        }
    }
}