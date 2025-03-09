/// <summary>
/// AnimatorLayerAnalyzerは、AnimatorControllerLayerを解析するクラスです。
/// </summary>

#region
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using static com.github.pandrabox.pandravase.editor.Util;
#endregion

namespace com.github.pandrabox.pandravase.editor
{
    public class AnimatorLayerAnalyzer
    {
        public HashSet<TransitionDetail> TransitionDetails;

        public AnimatorLayerAnalyzer(AnimatorControllerLayer layer)
        {
            TransitionDetails = new HashSet<TransitionDetail>();
            foreach (var state in layer.stateMachine.states)
            {
                foreach (var transition in state.state.transitions)
                {
                    TransitionDetails.Add(new TransitionDetail { Transition = transition, FromState = state.state });
                }
            }
            foreach (var transition in layer.stateMachine.anyStateTransitions)
            {
                TransitionDetails.Add(new TransitionDetail { Transition = transition, FromState = null });
            }
        }

        /// <summary>
        /// From情報を持つTransition
        /// </summary>
        public class TransitionDetail
        {
            public AnimatorStateTransition Transition { get; set; }
            public AnimatorState ToState => Transition.destinationState;
            public AnimatorState FromState { get; set; }
        }

        /// <summary>
        /// GestureによるTransition
        /// </summary>
        public IEnumerable<TransitionDetail> GestureTransitions => TransitionDetails.Where(x => x.Transition.conditions.Any(c => c.parameter == "GestureRight" || c.parameter == "GestureLeft"));

        /// <summary>
        /// GestureによってBodyMeshの変更へ遷移するTransition
        /// </summary>
        public HashSet<TransitionDetail> EmoTransitions(PandraProject prj)
        {
            var bodyMesh = prj.RootTransform.GetComponentsInChildren<SkinnedMeshRenderer>(true).FirstOrDefault(mesh => mesh.name == "Body");
            if (bodyMesh == null)
            {
                LowLevelExeption("BodyMeshが見つかりませんでした。");
                return new HashSet<TransitionDetail>();
            }
            return EmoTransitions(bodyMesh);
        }
        public HashSet<TransitionDetail> EmoTransitions(SkinnedMeshRenderer bodyMesh)
        {
            HashSet<TransitionDetail> emoTransitions = new HashSet<TransitionDetail>();
            HashSet<string> blendShapes = new HashSet<string>();
            for (int i = 0; i < bodyMesh.sharedMesh.blendShapeCount; i++)
            {
                blendShapes.Add(bodyMesh.sharedMesh.GetBlendShapeName(i));
            }
            foreach (var t in GestureTransitions)
            {
                var m = t?.ToState?.motion;
                if (m == null) continue;
                AnimationClip clip = (AnimationClip)t.ToState.motion;
                if (clip == null) continue;
                EditorCurveBinding[] bodyBindings = AnimationUtility.GetCurveBindings(clip);
                foreach (var binding in bodyBindings)
                {
                    string propertyName = binding.propertyName.Replace("blendShape.", "");
                    if (blendShapes.Contains(propertyName))
                    {
                        emoTransitions.Add(t);
                    }
                }
            }
            return emoTransitions;
        }
    }
}