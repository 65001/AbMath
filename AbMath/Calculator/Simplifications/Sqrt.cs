using System;
using System.Collections.Generic;
using System.Text;
using AbMath.Calculator.Operators;

namespace AbMath.Calculator.Simplifications
{
    public static class Sqrt
    {
        public static bool SqrtNegativeNumbersRunnable(RPN.Node node)
        {
            return node.IsSqrt() && node[0].IsLessThanNumber(0);
        }

        public static RPN.Node SqrtNegativeNumbers(RPN.Node node)
        {
            return new RPN.Node(double.NaN);
        }

        public static bool SqrtToFuncRunnable(RPN.Node node)
        {
            return node.IsExponent() && node[0].IsNumber(2) && node[1].IsSqrt();
        }

        public static RPN.Node SqrtToFunc(RPN.Node node)
        {
            return node[1, 0];
        }

        public static bool SqrtToAbsRunnable(RPN.Node node)
        {
            return node.IsSqrt() && node[0].IsExponent() && node[0,0].IsNumber(2);
        }

        public static RPN.Node SqrtToAbs(RPN.Node node)
        {
            return new RPN.Node(new[] { node[0,1] }, new RPN.Token("abs", 1, RPN.Type.Function));
        }

        public static bool SqrtPowerFourRunnable(RPN.Node node)
        {
            return node.IsSqrt() && node[0].IsExponent() &&
                   node[0,0].IsNumber() &&
                   node[0,0].GetNumber() % 4 == 0;
        }

        public static RPN.Node SqrtPowerFour(RPN.Node node)
        {
            return new Pow(node[0, 1], new RPN.Node(node[0,0].GetNumber() / 2));
        }
    }
}
