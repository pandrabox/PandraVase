using UnityEngine;


namespace com.github.pandrabox.pandravase.editor
{
    /// <summary>
    /// BlendTreeBuilderの拡張メソッド。ある程度のまとまりを持ったC命令を拡張メソッドとして定義する。
    /// </summary>
    public static class BlendTreeBuilderExtensions
    {
        const float epsilon = 1.401298e-45F;
        /// <summary>
        /// targetParamが変更されたらtargetParamIsDiffに1を返す
        /// </summary>
        public static void FDiffChecker(this BlendTreeBuilder bb, string targetParam, string resultName = null, float min = 0, float max = 1)
        {
            string memory = $"{targetParam}Memory";
            string subtracted = $"{targetParam}Subtracted";
            string result = resultName ?? $"{targetParam}IsDiff";
            bb.NName("DiffChecker").AddD(() =>
            {
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

        /// <summary>
        /// DirectによるAnimation倍率変更の代替
        /// </summary>
        public static void FMultiplicationBy1D(this BlendTreeBuilder bb, Motion baseMotion, string weightParam, float weightMin, float weightMax, float? resMin = null, float? resMax = null)
        {
            bb.NName($@"{baseMotion} x {weightParam}").Add1D(weightParam, () =>
            {
                bb.Param(weightMin).AddMotion(((AnimationClip)baseMotion).Multiplication(resMin ?? weightMin));
                bb.Param(weightMax).AddMotion(((AnimationClip)baseMotion).Multiplication(resMax ?? weightMax));
            });
        }


        public static void FDummyAAP(this BlendTreeBuilder bb)
        {
            bb.AddAAP("Dummy", 0);
        }

        /// <summary>
        /// fromName(0～1)をstep段階のintに変換する(8段階なら0～7)
        /// </summary>
        /// <param name="bb"></param>
        /// <param name="fromName"></param>
        /// <param name="step"></param>
        public static void Quantization01(this BlendTreeBuilder bb, string fromName, int step)
        {
            string ToName = $"{fromName}Quantized";
            string MemoryName = $"{fromName}QuantMemory";
            bb.NName("Quantization").AddD(() =>
            {
                bb.NName("Minimization").Param("1").Add1D(fromName, () =>
                {
                    bb.Param(0.5f / step).AddAAP(MemoryName, 0);
                    bb.Param(0.5f / step + 1.000001f).AddAAP(MemoryName, epsilon * step);
                });

                bb.NName("Restoration").Param("1").Add1D(MemoryName, () =>
                {
                    bb.Param(0).AddAAP(ToName, 0);
                    bb.Param(epsilon * step).AddAAP(ToName, step);
                });
            });
        }
    }
}