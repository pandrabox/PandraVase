/*
 * MIT License
 *
 * Copyright (c) 2022 bd_
 * Copyright (c) 2024 pandra
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */
/*
 * This program was created by pandra based on ModularAvatar(1.10.3) developed by bd_. 
 * The original code is licensed under the MIT License (see above).
 * My modifications are also licensed under the MIT License.
 * 
 * 改変内容：MergeBlendTreePassからBlendTree統合関連機能を削除し、パラメータをMergeAnimatorに渡す処理を追加
 */

using UnityEditor;
using nadena.dev.modular_avatar.core;
using nadena.dev.modular_avatar.core.editor;
using UnityEngine;
using UnityEditor.Animations;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using nadena.dev.ndmf.util;
using nadena.dev.ndmf;
using com.github.pandrabox.pandravase.editor;
using static com.github.pandrabox.pandravase.runtime.Global;
using static com.github.pandrabox.pandravase.runtime.Util;
using com.github.pandrabox.pandravase.runtime;

namespace com.github.pandrabox.pandravase.editor
{
    /// <summary>
    /// すべてのPanMergeBlendTree RuntimeをMergeAnimatorに変換する
    /// </summary>
    public class PanMergeBlendTreePlugin
    {
        private readonly GameObject _avatarRootObject;
        public PanMergeBlendTreePlugin(GameObject avatarRootObject)
        {
            _avatarRootObject = avatarRootObject;
            foreach (var component in _avatarRootObject.GetComponentsInChildren<PanMergeBlendTree>(true))
            {
                new PanMergeBlendTreeMain(component);
            }
        }
    }

    /// <summary>
    /// 1つのPanMergeBlendTree RuntimeをMergeAnimatorに変換する
    /// </summary>
    public class PanMergeBlendTreeMain
    {
        internal const string ALWAYS_ONE = "__ModularAvatarInternal/One";
        private readonly string _blendTreeLayerName;
        private readonly AnimatorController _controller;
        private readonly BlendTree _rootBlendTree;
        private readonly GameObject _mergeHost;
        private readonly PanMergeBlendTree _target;

        public PanMergeBlendTreeMain(PanMergeBlendTree component)
        {
            _rootBlendTree = (BlendTree)component.BlendTree;
            _mergeHost = component.gameObject;
            _blendTreeLayerName = $@"PanMBT/{_rootBlendTree.name}";
            _target = component;
            _controller = new AnimatorController();
            SetBlendTreeToController();
            SetParametersToController();
            ApplyMergeAnimator();
            if(PDEBUGMODE)
            {
                var path = $@"{DebugOutpFolder}PanMBT";
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                    AssetDatabase.Refresh();
                }
                AssetDatabase.CreateAsset(_controller, $@"{DebugOutpFolder}{_blendTreeLayerName}.asset");
            }
        }

        private void SetParametersToController()
        {
            // Get Unique parameter names
            HashSet<string> parameterNames = new HashSet<string>();
            foreach (var asset in _rootBlendTree.ReferencedAssets(includeScene: false))
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
            }

            // Set Parameters
            var parameters = new List<AnimatorControllerParameter>(parameterNames.Count + 1);
            parameterNames.Remove(ALWAYS_ONE);
            parameters.Add(new AnimatorControllerParameter()
            {
                name = ALWAYS_ONE,
                type = AnimatorControllerParameterType.Float,
                defaultFloat = 1
            });

            foreach (var name in parameterNames)
            {
                parameters.Add(new AnimatorControllerParameter()
                {
                    name = name,
                    type = AnimatorControllerParameterType.Float,
                    defaultFloat = 0
                });
            }

            _controller.parameters = parameters.ToArray();
        }

        private void SetBlendTreeToController()
        {
            var newStateMachine = new AnimatorStateMachine();
            var newState = new AnimatorState();

            _controller.layers = new[]
            {
                new AnimatorControllerLayer
                {
                    blendingMode = AnimatorLayerBlendingMode.Override,
                    defaultWeight = 1,
                    name = _blendTreeLayerName,
                    stateMachine = newStateMachine
                }
            };
            newStateMachine.name = "BlendTree";
            newStateMachine.states = new[]
            {
                new ChildAnimatorState
                {
                    state = newState,
                    position = Vector3.zero
                }
            };
            newStateMachine.defaultState = newState;
            newState.writeDefaultValues = true;
            newState.motion = _rootBlendTree;
        }

        private void ApplyMergeAnimator()
        {
            var merger = _mergeHost.AddComponent<ModularAvatarMergeAnimator>();
            merger.animator = _controller;
            merger.layerType = _target.LayerType;
            merger.deleteAttachedAnimator = false;
            merger.pathMode = _target.PathMode;
            merger.matchAvatarWriteDefaults = false;
            merger.relativePathRoot = _target.RelativePathRoot;
            merger.layerPriority = _target.LayerPriority;
        }

    }
}

