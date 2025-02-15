using com.github.pandrabox.pandravase.runtime;
using nadena.dev.modular_avatar.core;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDKBase;
using static com.github.pandrabox.pandravase.editor.Util;


namespace com.github.pandrabox.pandravase.editor
{
    /// <summary>
    /// BlendTreeBuilderの拡張メソッド。ある程度のまとまりを持ったC命令を拡張メソッドとして定義する。
    /// </summary>
    public static class BlendTreeBuilderExtensions
    {
        /// <summary>
        /// targetParamが変更されたらtargetParamIsDiffに1を返す
        /// </summary>
        public static void FDiffChecker(this BlendTreeBuilder bb, string targetParam, string resultSuffix="IsDiff", float min = 0, float max=1)
        {
            string memory = $"{targetParam}Memory";
            string subtracted = $"{targetParam}Subtracted";
            string result = $"{targetParam}{resultSuffix}";
            bb.NName("DiffChecker").AddD(() => {
                bb.NName("Save and Subtract1").Param("1").Add1D(targetParam, () =>
                {
                    bb.Param(min).AddAAP(memory, min, subtracted, -min);
                    bb.Param(max).AddAAP(memory, max, subtracted, -max);
                });
                bb.NName("Subtract2").Param("1").FAssignmentBy1D(memory, min, max, subtracted);
                bb.NName("DiffDetection").Param("1").Add1D(subtracted, () =>
                {
                    bb.Param(-.00001f).AddAAP(result, 1);
                    bb.Param(0).AddAAP(result, 0);
                    bb.Param(.00001f).AddAAP(result, 1);
                });
            });
        }
        /// <summary>
        /// FromAAPNameをToAAPNameに1Dで代入する
        /// </summary>
        public static void FAssignmentBy1D(this BlendTreeBuilder bb, string FromAAPName, float FromAAPMin, float FromAAPMax, string ToAAPName, float? ToAAPMin = null, float? ToAAPMax = null)
        {
            bb.AssignmentBy1D(FromAAPName, FromAAPMin, FromAAPMax, ToAAPName, ToAAPMin, ToAAPMax);
        }
        
        public static void FDummyAAP(this BlendTreeBuilder bb)
        {
            bb.AddAAP("Dummy", 0);
        }
    }
}