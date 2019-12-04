using System;
using System.Collections.Generic;
using System.Text;

namespace AbMath.Calculator.Simplifications
{
    public static class Multiplication
    {
        public static bool setRule(RPN.Node node)
        {
            return node.IsMultiplication();
        }

        public static bool multiplicationToExponentRunnable(RPN.Node node)
        {
            return node.ChildrenAreIdentical();
        }

        public static RPN.Node multiplicationToExponent(RPN.Node node)
        {
            return new RPN.Node(new[] { new RPN.Node(2), node[0] }, new RPN.Token("^", 2, RPN.Type.Operator));
        }

        public static bool multiplicationByOneRunnable(RPN.Node node)
        {
            return node.Children[0].IsNumber(1) || node.Children[1].IsNumber(1);
        }

        public static RPN.Node multiplicationByOne(RPN.Node node)
        {
            return node.Children[1].IsNumber(1) ? node.Children[0] : node.Children[1];
        }

        public static bool multiplicationByZeroRunnable(RPN.Node node)
        {
            return (node.Children[1].IsNumber(0) || node.Children[0].IsNumber(0)) && !node.containsDomainViolation();
        }

        public static RPN.Node multiplicationByZero(RPN.Node node)
        {
            return new RPN.Node(0);
        }

    }
}
