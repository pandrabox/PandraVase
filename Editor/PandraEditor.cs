#if UNITY_EDITOR
using com.github.pandrabox.pandravase.runtime;
using UnityEditor;
using UnityEngine;
using static com.github.pandrabox.pandravase.editor.Util;

namespace com.github.pandrabox.pandravase.editor
{
    public abstract class PandraEditor : Editor
    {
        private bool _inAvatarOnly;
        private bool? _customEnableCondition;
        private string _customDisableMsg;
        private string _logoPath;
        private string _projectName;
        private ProjectTypes _projectType;

        public PandraEditor(bool inAvatarOnly, string projectName = null, ProjectTypes projectType = 0, bool? customEnableCondition = null, string customDisableMsg = null)
        {
            _inAvatarOnly = inAvatarOnly;
            _customEnableCondition = customEnableCondition;
            _customDisableMsg = customDisableMsg;
            _projectName = projectName;
            _projectType = projectType;
        }

        private bool Enable
        {
            get
            {
                if (_customEnableCondition != null) return _customEnableCondition ?? false;
                return !_inAvatarOnly || IsInAvatar(((Component)target).gameObject);
            }
        }
        private void DrawDisableMsg()
        {
            if (_customDisableMsg == null)
            {
                EditorGUILayout.HelpBox("この機能はAvatarの下に定義されている場合のみ有効です。", MessageType.Error);
            }
            else
            {
                EditorGUILayout.HelpBox(_customDisableMsg, MessageType.Error);
            }
        }

        public void OnEnable()
        {
            if (!Enable) return;

            if (!string.IsNullOrEmpty(_logoPath) && !string.IsNullOrEmpty(_projectName))
            {
                var prj = new PandraProject(_projectName, _projectType);
                _logoPath = $@"{prj.ImgFolder}InspectorLogo.png";
            }

            DefineSerial();
            OnInnerEnable();
        }

        /// <summary>
        /// SerializedPropertyを定義
        /// </summary>
        protected abstract void DefineSerial();

        /// <summary>
        /// 初期化処理を定義(任意)
        /// </summary>
        protected virtual void OnInnerEnable() { }

        /// <summary>
        /// ロゴを描いてから普通のInspector処理
        /// </summary>
        public sealed override void OnInspectorGUI()
        {
            DrawLogo();
            if (!Enable)
            {
                DrawDisableMsg();
                return;
            }
            serializedObject.Update();
            OnInnerInspectorGUI();
            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// 普通のInspector処理
        /// </summary>
        public abstract void OnInnerInspectorGUI();

        /// <summary>
        /// ロゴ描画
        /// </summary>
        private Texture2D _inspectorLogo;
        protected void DrawLogo()
        {
            if (_inspectorLogo == null && !string.IsNullOrEmpty(_logoPath))
            {
                _inspectorLogo = AssetDatabase.LoadAssetAtPath<Texture2D>(_logoPath);
                if (_inspectorLogo == null)
                {
                    Log.I.Warning($@"ロゴ{_logoPath}の取得に失敗しました");
                }
                _logoPath = null;
            }
            if (_inspectorLogo != null)
            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label(_inspectorLogo);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.Space(4);
            }
        }

        /// <summary>
        /// Inspector用のタイトルデザイン
        /// </summary>
        /// <param name="t"></param>
        protected static void Title(string t)
        {
            GUILayout.BeginHorizontal();
            var lineRect = GUILayoutUtility.GetRect(0, EditorGUIUtility.singleLineHeight, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            int leftBorderSize = 5;
            var leftRect = new Rect(lineRect.x, lineRect.y, leftBorderSize, lineRect.height);
            var rightRect = new Rect(lineRect.x + leftBorderSize, lineRect.y, lineRect.width - leftBorderSize, lineRect.height);
            Color leftColor = new Color32(0xF4, 0xAD, 0x39, 0xFF);
            Color rightColor = new Color32(0x39, 0xA7, 0xF4, 0xFF);
            EditorGUI.DrawRect(leftRect, leftColor);
            EditorGUI.DrawRect(rightRect, rightColor);
            var textStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(5, 0, 0, 0),
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.black },
            };
            GUI.Label(rightRect, t, textStyle);
            GUILayout.EndHorizontal();
        }
    }
}
#endif