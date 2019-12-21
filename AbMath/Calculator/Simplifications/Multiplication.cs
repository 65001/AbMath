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

        public static bool increaseExponentRunnable(RPN.Node node)
        {
            /* *
             * | f(x)
             * | ^
             *  |
             *  | > Number
             *  | > f(x)
             */
            return node[1].IsExponent() && node[1,0].IsNumber() && node[0].Matches(node[1,1]);
        }

        public static RPN.Node increaseExponent(RPN.Node node)
        {
            node.Replace(node[0], new RPN.Node(1));
            node.Replace(node[1,0],
                new RPN.Node(node[1,0].GetNumber() + 1));
            return node;
        }

        public static bool increaseExponentTwoRunnable(RPN.Node node)
        {
            /**
             * *
             * | ^
             *  | c > 0
             *  | f(x)
             * | *
             *   f(x)
             */
            return node[0].IsExponent() && node[1].IsMultiplication() &&
                   node[0,0].IsGreaterThanNumber(0) && node[1,0].Matches(node[0,1]);
        }

        public static RPN.Node increaseExponentTwo(RPN.Node node)
        {
            RPN.Node temp = node.Children[0].Children[0];
            temp.Replace(temp.GetNumber() + 1);
            node.Children[1].Children[0].Remove(new RPN.Node(1));
            return node;
        }

        public static bool increaseExponentThreeRunnable(RPN.Node node)
        {
            /**
             * *
             * | > ^
             *   | ?
             *   | f(x)
             * | > *
             *    ?
             *    f
             */
            return node.Children[0].IsExponent() && node.Children[1].IsMultiplication() &&
                   node.Children[0].Children[1].Matches(node.Children[1]);
        }

        public static RPN.Node increaseExponentThree(RPN.Node node)
        {
            RPN.Node temp = node.Children[0].Children[0];
            temp.Replace(temp.GetNumber() + 1);
            node.Children[1].Remove(new RPN.Node(1));
            return node;
        }

        public static bool dualNodeMultiplicationRunnable(RPN.Node node)
        {
            return node[1].IsNumber() && node[0].IsMultiplication() && node[0,1].IsNumber() && !node[0,0].IsNumber();
        }

        public static RPN.Node dualNodeMultiplication(RPN.Node node)
        {
            double num1 = node[0,1].GetNumber();
            double num2 = node[1].GetNumber();

            node[0].Replace(node[0, 1], new RPN.Node(1));
            node.Replace(node[1], new RPN.Node(num1 * num2));
            return node;
        }


    }
}
