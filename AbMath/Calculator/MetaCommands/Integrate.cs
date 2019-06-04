using System;
using System.Collections.Generic;
using System.Text;
using AbMath.Calculator;

namespace AbMath.Calculator
{
    public static class MetaCommands
    {

        public enum ApproximationModes
        {
            Left,Right,Midpoint,Trapezoidal,Simpson
        }

        public static double Integrate(RPN rpn, RPN.Node expression, RPN.Node variable, RPN.Node a, RPN.Node b,RPN.Node frequencey)
        {
            return Approximate(rpn, expression, variable, a,b, frequencey, new List<ApproximationModes>() {ApproximationModes.Simpson, ApproximationModes.Midpoint});
        }

        public static double Approximate(RPN rpn, RPN.Node expression, RPN.Node variable, RPN.Node a, RPN.Node b, RPN.Node frequencey, List<ApproximationModes> modes )
        {
            PostFix math = new PostFix(rpn);

            RPN.Token[] Polish = expression.ToPostFix().ToArray();

            double start = math.Compute(a.ToPostFix().ToArray());
            double end = math.Compute(b.ToPostFix().ToArray());

            bool multiplyByNegativeOne = end < start;

            if (multiplyByNegativeOne)
            {
                double temp = start;
                start = end;
                end = temp;
            }

            double freq = math.Compute(frequencey.ToPostFix().ToArray());

            math.SetPolish(Polish);

            double Rsum = 0;
            double Lsum = 0;
            double MidSum = 0;
            double PrevAnswer = 0;

            double f_a = 0;
            int count = 0;

            double DeltaX = end - start;
            double n = DeltaX / freq;
            int max = (int)Math.Ceiling(n);

            for (int x = 0; x <= max; x++)
            {
                double RealX = start + x * DeltaX / n;
                math.SetVariable("ans", PrevAnswer);
                math.SetVariable(variable.Token.Value, RealX);
                double answer = math.Compute();

                if (x == 0)
                {
                    f_a = answer;
                }

                if (x % 2 == 0)
                {
                    if (x < max)
                    {
                        Rsum += answer;
                    }

                    if (x > 0)
                    {
                        Lsum += answer;
                    }
                }
                else
                {
                    MidSum += answer;
                }

                PrevAnswer = answer;
                math.Reset();
                count++;
            }

            double LApprox = (2 * Rsum * DeltaX / n);
            double RApprox = (2 * Lsum * DeltaX / n);
            double MApprox = (2 * MidSum * DeltaX / n);
            double TApprox = (LApprox + RApprox) / 2;

            freq = freq * 2;
            n = DeltaX / freq;
            double Simpson = double.NaN;

            if (n % 2 == 0)
            {
                Simpson = (TApprox + 2 * MApprox) / 3;
            }

            Dictionary<ApproximationModes, double> approximations = new Dictionary<ApproximationModes, double>()
            {
                { ApproximationModes.Simpson, Simpson } ,
                { ApproximationModes.Midpoint, MApprox },
                { ApproximationModes.Trapezoidal, TApprox },
                { ApproximationModes.Left, LApprox },
                { ApproximationModes.Right, RApprox }
            };

            //Return based on mode requested
            for (int i = 0; i < modes.Count; i++)
            {
                double approximation = approximations[modes[i]];
                if (!double.IsNaN(approximation) )
                {
                    return multiplyByNegativeOne ? approximation * -1 : approximation;
                }
            }

            return multiplyByNegativeOne ? approximations[modes[0]] * -1 : approximations[modes[0]];
        }
    }
}