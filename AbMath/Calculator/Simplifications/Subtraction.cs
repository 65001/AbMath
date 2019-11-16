using System;
using System.Collections.Generic;
using System.Text;

namespace AbMath.Calculator.Simplifications
{
    public static class Subtraction
    {
        public static bool setRule(RPN.Node node)
        {
            return node.IsSubtraction();
        }

        public static bool SameFunctionRunnable(RPN.Node node)
        {
            return node.ChildrenAreIdentical() && !node.containsDomainViolation();
        }

        public static RPN.Node SameFunction(RPN.Node node)
        {
            return new RPN.Node(0);
        }

        public static bool CoefficientOneReductionRunnable(RPN.Node node)
        {
            return node.Children[1].IsMultiplication() && node.Children[1].Children[1].IsNumber() &&
                   node.Children[1].Children[0].Matches(node.Children[0]);
        }

        public static RPN.Node CoefficientOneReduction(RPN.Node node)
        {
            node.Replace(node.Children[0], new RPN.Node(0));
            node.Children[1].Replace(node.Children[1].Children[1],
                new RPN.Node(node.Children[1].Children[1].GetNumber() - 1));
            return node;
        }

        public static bool SubtractionByZeroRunnable(RPN.Node node)
        {
            return node.Children[0].IsNumber(0);
        }

        public static RPN.Node SubtractionByZero(RPN.Node node)
        {
            return node.Children[1];
        }
    }
}
