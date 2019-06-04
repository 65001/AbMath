using System;
using System.Collections.Generic;
using System.Text;
using AbMath.Calculator;

namespace AbMath.Calculator
{
    public static class MetaCommands
    {
        public static double Integrate(RPN rpn, RPN.Node expression, RPN.Node variable, RPN.Node a, RPN.Node b, RPN.Node frequencey)
        {
            PostFix math = new PostFix(rpn);

            RPN.Token[] Polish = expression.ToPostFix().ToArray();

            double start = math.Compute(a.ToPostFix().ToArray());
            double end = math.Compute(b.ToPostFix().ToArray());
            double freq = math.Compute(frequencey.ToPostFix().ToArray());

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

            return 0;
        }
    }
}
