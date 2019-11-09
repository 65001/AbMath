using System;
using System.Collections.Generic;
using System.Text;

namespace AbMath.Calculator
{
    public partial class RPN
    {
        public static class SqrtSimplifications
        {
            public static bool SqrtToFuncRunnable(RPN.Node node)
            {
                return node.IsExponent() && node.Children[0].IsNumber(2) && node.Children[1].IsSqrt();
            }

            public static RPN.Node SqrtToFunc(RPN.Node node)
            {
                return node.Children[1].Children[0];
            }

            public static bool SqrtToAbsRunnable(RPN.Node node)
            {
                return node.IsSqrt() && node.Children[0].IsExponent() && node.Children[0].Children[0].IsNumber(2);
            }

            public static RPN.Node SqrtToAbs(RPN.Node node)
            {
                return new RPN.Node(new[] { node.Children[0].Children[1] }, new RPN.Token("abs", 1, RPN.Type.Function));
            }

            public static bool SqrtPowerFourRunnable(RPN.Node node)
            {
                return node.IsSqrt() && node.Children[0].IsExponent() &&
                       node.Children[0].Children[0].IsNumber() &&
                       node.Children[0].Children[0].GetNumber() % 4 == 0;
            }

            public static RPN.Node SqrtPowerFour(RPN.Node node)
            {
                return new RPN.Node(new[] {new RPN.Node(node.Children[0].Children[0].GetNumber() / 2), node.Children[0].Children[1]}, new RPN.Token("^", 2, RPN.Type.Operator));
            }
        }
    }
}
