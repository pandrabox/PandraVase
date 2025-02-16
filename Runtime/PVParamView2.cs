#if UNITY_EDITOR
using System;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using nadena.dev.modular_avatar.core;
using UnityEditor;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEditor.Animations;
using AnimatorController = UnityEditor.Animations.AnimatorController;
using BlendTree = UnityEditor.Animations.BlendTree;
using nadena.dev.ndmf.util;

namespace com.github.pandrabox.pandravase.runtime
{
    [DisallowMultipleComponent]
    public class PVParamView2 : PandraComponent
    {
        public int MinValue = -1;
        public int MaxValue = 255;
        public List<string> parameterName = new List<string>() { "" };
        public UnityEngine.Object animatorAsset; // AnimatorController と BlendTree の両方をセット可能
    }

    [CustomEditor(typeof(PVParamView2))]
    public class PVParamView2Editor : Editor
    {
        private ReorderableList reorderableList;

        private void OnEnable()
        {
            PVParamView2 script = (PVParamView2)target;
            reorderableList = new ReorderableList(script.parameterName, typeof(string), true, true, true, true);

            reorderableList.drawHeaderCallback = (Rect rect) =>
            {
                EditorGUI.LabelField(rect, "Parameters:");
            };

            reorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                script.parameterName[index] = EditorGUI.TextField(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight), script.parameterName[index]);
            };

            reorderableList.onAddCallback = (ReorderableList list) =>
            {
                script.parameterName.Add("");
            };

            reorderableList.onRemoveCallback = (ReorderableList list) =>
            {
                script.parameterName.RemoveAt(list.index);
            };
        }

        public override void OnInspectorGUI()
        {
            PVParamView2 script = (PVParamView2)target;

            script.MinValue = EditorGUILayout.IntField("Min Value", script.MinValue);
            script.MaxValue = EditorGUILayout.IntField("Max Value", script.MaxValue);

            
            reorderableList.DoLayoutList();

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("全削除"))
            {
                if (EditorUtility.DisplayDialog("全削除", "全パラメータを削除します。よろしいですか?", "Yes", "No"))
                {
                    script.parameterName.Clear();
                }
            }
            if (GUILayout.Button("整列"))
            {
                script.parameterName.Sort();
            }
            EditorGUILayout.EndHorizontal();


            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("", GUILayout.Width(0)); // ラベルを表示しない
            script.animatorAsset = EditorGUILayout.ObjectField("", script.animatorAsset, typeof(UnityEngine.Object), false);
            if (GUILayout.Button("パラメータ読み込み (AnimationController or BlendTree)"))
            {
                AddParametersFromAsset(script);
            }
            EditorGUILayout.EndHorizontal();

            if (GUI.changed)
            {
                EditorUtility.SetDirty(script);
            }
        }

        private void AddParametersFromAsset(PVParamView2 script)
        {
            HashSet<string> parameters = new HashSet<string>();

            // オブジェクトフィールドに設定されたアセットが AnimatorController または BlendTree か確認
            if (script.animatorAsset is AnimatorController animatorController)
            {
                foreach (var param in animatorController.parameters)
                {
                    parameters.Add(param.name);
                }
            }
            else if (script.animatorAsset is BlendTree blendTree)
            {
                var ps = ExtractBlendTreeParameters(blendTree);
                foreach (var param in ps)
                {
                    parameters.Add(param);
                }
            }

            // パラメータ名を既存のリストに追加し、重複を削除
            foreach (var param in parameters)
            {
                if (!script.parameterName.Contains(param)) // 既にリストに含まれていない場合のみ追加
                {
                    script.parameterName.Add(param);
                }
            }

            // 重複を削除
            RemoveDuplicates(script.parameterName);
        }


        /// <summary>
        /// BlendTreeのパラメータを取得
        /// </summary>
        /// <param name="blendTree"></param>
        /// <returns></returns>
        private string[] ExtractBlendTreeParameters(BlendTree blendTree)
        {
            HashSet<string> parameterNames = new HashSet<string>();

            foreach (var asset in blendTree.ReferencedAssets(includeScene: false))
            {
                if (asset is BlendTree bt2)
                {
                    if (!string.IsNullOrEmpty(bt2.blendParameter) && bt2.blendType != BlendTreeType.Direct)
                    {
                        parameterNames.Add(bt2.blendParameter);
                    }

                    if (bt2.blendType != BlendTreeType.Direct && bt2.blendType != BlendTreeType.Simple1D)
                    {
                        if (!string.IsNullOrEmpty(bt2.blendParameterY))
                        {
                            parameterNames.Add(bt2.blendParameterY);
                        }
                    }

                    if (bt2.blendType == BlendTreeType.Direct)
                    {
                        foreach (var childMotion in bt2.children)
                        {
                            if (!string.IsNullOrEmpty(childMotion.directBlendParameter))
                            {
                                parameterNames.Add(childMotion.directBlendParameter);
                            }
                        }
                    }
                }
                else if (asset is AnimationClip clip)
                {
                    // AnimationClip内のfloatパラメータを抽出
                    ExtractFloatParametersFromAnimationClip(clip, parameterNames);
                }
            }
            return new List<string>(parameterNames).ToArray();
        }

        /// <summary>
        /// AnimationClip内のfloatパラメータを抽出
        /// </summary>
        /// <param name="clip"></param>
        /// <param name="parameterNames"></param>
        private void ExtractFloatParametersFromAnimationClip(AnimationClip clip, HashSet<string> parameterNames)
        {
            // AnimationClip内のパラメータ（Keyframeなど）を解析し、float型のパラメータを追加
            if (clip != null)
            {
                foreach (var binding in AnimationUtility.GetCurveBindings(clip))
                {
                    // float型のパラメータを抽出
                    if (binding.type == typeof(Animator))
                    {
                        parameterNames.Add(binding.propertyName);
                    }
                }
            }
        }

        private void RemoveDuplicates(List<string> parameterNames)
        {
            HashSet<string> uniqueParams = new HashSet<string>(parameterNames);
            parameterNames.Clear();
            parameterNames.AddRange(uniqueParams);
        }
    }
}
#endif
