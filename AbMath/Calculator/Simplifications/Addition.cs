using System;
using System.Collections.Generic;
using System.Text;

namespace AbMath.Calculator.Simplifications
{
    public static class Addition
    {
        public static bool setRule(RPN.Node node)
        {
            return node.IsAddition();
        }

        public static bool AdditionToMultiplicationRunnable(RPN.Node node)
        {
            return node[0].Matches(node[1]);
        }

        public static RPN.Node AdditionToMultiplication(RPN.Node node)
        {
            RPN.Node multiply = new RPN.Node(new[] { node[0], new RPN.Node(2) },
                new RPN.Token("*", 2, RPN.Type.Operator));
            return multiply;
        }

        public static bool ZeroAdditionRunnable(RPN.Node node)
        {
            return !(node[0].IsMultiplication() && node[1].IsMultiplication()) && (node[0].IsNumber(0) || node[1].IsNumber(0));
        }

        public static RPN.Node ZeroAddition(RPN.Node node)
        {
            if (node[0].IsNumber(0))
            {
                return node[1];
            }

            return node[0];
        }

        public static bool AdditionSwapRunnable(RPN.Node node)
        {
            return !(node[0].IsMultiplication() && node[1].IsMultiplication()) && node[1].IsMultiplication() &&
                   node[1, 1].IsNumber(-1);
        }

        public static RPN.Node AdditionSwap(RPN.Node node)
        {
            node[1, 1].Replace(1);
            node.Swap(0, 1);
            node.Replace(new RPN.Token("-", 2, RPN.Type.Operator));
            return node;
        }

        public static bool SimpleCoefficientRunnable(RPN.Node node)
        {
            return !(node[0].IsMultiplication() && node[1].IsMultiplication()) && node[1].IsMultiplication() &&
                   node[1,1].IsNumber() && node[1,0].Matches(node[0]);
        }

        public static RPN.Node SimpleCoefficient(RPN.Node node)
        {
            node[0].Remove(new RPN.Node(0));
            node[1].Replace(node[1, 1], new RPN.Node(node[1,1].GetNumber() + 1));
            return node;
        }

        public static bool ComplexCoefficientRunnable(RPN.Node node)
        {
            return node[0].IsMultiplication() && node[1].IsMultiplication() &&
                   node[0, 1].IsNumber() && node[1, 1].IsNumber() &&
                   node[0, 0].Matches(node[1, 0]);
        }

        public static RPN.Node ComplexCoefficient(RPN.Node node)
        {
            double sum = (node.Children[0].Children[1].GetNumber() +
                          node.Children[1].Children[1].GetNumber());
            node.Children[1].Replace(node.Children[1].Children[1], new RPN.Node(sum));
            node[0].Remove(new RPN.Node(0));
            return node;
        }

        public static bool AdditionToSubtractionRuleOneRunnable(RPN.Node node)
        {
            return !(node[0].IsMultiplication() && node[1].IsMultiplication()) && node[0].IsMultiplication() &&
                   node[0, 1].IsNumber(-1);
        }

        public static RPN.Node AdditionToSubtractionRuleOne(RPN.Node node)
        {
            node.Replace("-");
            node[0].Replace(node[0,1], new RPN.Node(1));
            return node;
        }

        public static bool AdditionToSubtractionRuleTwoRunnable(RPN.Node node)
        {
            return !(node[0].IsMultiplication() && node[1].IsMultiplication()) && node[0].IsLessThanNumber(0) &&
                   node[1].IsMultiplication();
        }

        public static RPN.Node AdditionToSubtractionRuleTwo(RPN.Node node)
        {
            node.Replace("-");
            node.Replace(node[0], new RPN.Node(System.Math.Abs(node[0].GetNumber())));
            return node;
        }

        public static bool ComplexNodeAdditionRunnable(RPN.Node node)
        {
            return !(node[0].IsMultiplication() && node[1].IsMultiplication()) && node[0].IsSubtraction() &&
                   node[1].Matches(node[0, 1]);
        }

        public static RPN.Node ComplexNodeAddition(RPN.Node node)
        {
            node[0].Replace(node[0, 1], new RPN.Node(0));
            RPN.Node multiplication = new RPN.Node(new[] { node[1], new RPN.Node(2) },
                new RPN.Token("*", 2, RPN.Type.Operator));
            return node;
        }
    }
}
