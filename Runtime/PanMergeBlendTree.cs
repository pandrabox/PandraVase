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
 * 改変内容：MergeAnimatorを元に、RuntimeAnimatorをBlendTreeに変更、不要な設定項目・重複する定義を削除
 */


using System;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using nadena.dev.modular_avatar.core;

namespace com.github.pandrabox.pandravase.runtime
{
    public class PanMergeBlendTree : AvatarTagComponent
    {
        public UnityEngine.Object BlendTree;
        public VRCAvatarDescriptor.AnimLayerType LayerType = VRCAvatarDescriptor.AnimLayerType.FX;
        // public bool deleteAttachedAnimator;
        public MergeAnimatorPathMode PathMode = MergeAnimatorPathMode.Relative;
        // public bool matchAvatarWriteDefaults;
        public AvatarObjectReference RelativePathRoot = new AvatarObjectReference();
        public int LayerPriority = 0;
    }
}