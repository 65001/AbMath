﻿using System;
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
            return (node.Children[1].IsNumber(0) || node.Children[0].IsNumber(0)) && !node.ContainsDomainViolation();
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

        public static bool multiplicationByOneComplexRunnable(RPN.Node node)
        {
            return node[0].IsNumber(1) || node[1].IsNumber(1);
        }

        public static RPN.Node multiplicationByOneComplex(RPN.Node node)
        {
            return node[1].IsNumber(1) ? node.Children[0] : node.Children[1];
        }

        public static bool expressionTimesDivisionRunnable(RPN.Node node)
        {
            return node[0].IsDivision() ^ node[1].IsDivision();
        }

        public static RPN.Node expressionTimesDivision(RPN.Node node)
        {
            RPN.Node division;
            RPN.Node expression;
            if (node.Children[0].IsDivision())
            {
                division = node.Children[0];
                expression = node.Children[1];
            }
            else
            {
                division = node.Children[1];
                expression = node.Children[0];
            }

            RPN.Node numerator = division.Children[1];
            RPN.Node multiply = new RPN.Node(new[] { numerator.Clone(), expression.Clone() },
                new RPN.Token("*", 2, RPN.Type.Operator));
            numerator.Remove(multiply);
            expression.Remove(new RPN.Node(1));
            return node;
        }

        public static bool divisionTimesDivisionRunnable(RPN.Node node)
        {
            return node[0].IsDivision() && node[1].IsDivision();
        }

        public static RPN.Node divisionTimesDivision(RPN.Node node)
        {
            RPN.Node[] numerator = { node.Children[0].Children[1], node.Children[1].Children[1] };
            RPN.Node[] denominator = { node.Children[0].Children[0], node.Children[1].Children[0] };

            RPN.Token multiply = new RPN.Token("*", 2, RPN.Type.Operator);
            RPN.Token div = new RPN.Token("/", 2, RPN.Type.Operator);

            RPN.Node top = new RPN.Node(numerator, multiply);
            RPN.Node bottom = new RPN.Node(denominator, multiply);
            RPN.Node division = new RPN.Node(new[] { bottom, top }, div);

            node.Children[0].Remove(division);
            node.Children[1].Remove(new RPN.Node(1));
            return node;
        }


        public static bool negativeTimesnegativeRunnable(RPN.Node node)
        {
            return node[0].IsLessThanNumber(0) && node[1].IsLessThanNumber(0);
        }

        public static RPN.Node negativeTimesnegative(RPN.Node node)
        {
            node[0].Remove(new RPN.Node(node[0].GetNumber() * -1));
            node[1].Remove(new RPN.Node(node[1].GetNumber() * -1));
            return node;
        }

        public static bool complexNegativeNegativeRunnable(RPN.Node node)
        {
            return node[0].IsMultiplication() && node[0, 1].IsLessThanNumber(0) &&
                   node[1].IsLessThanNumber(0);
        }

        public static RPN.Node complexNegativeNegative(RPN.Node node)
        {
            node.Replace(node[0, 1], new RPN.Node(System.Math.Abs(node[0, 1].GetNumber())));
            node.Replace(node[1], new RPN.Node(System.Math.Abs(node[1].GetNumber())));
            return node;
        }

        public static bool negativeTimesConstantRunnable(RPN.Node node)
        {
            return node[0].IsNumber(-1) && node[1].IsNumber();
        }

        public static RPN.Node negativeTimesConstant(RPN.Node node)
        {
            node.Replace(node[0], new RPN.Node(1));
            node.Replace(node[1], new RPN.Node(node[1].GetNumber() * -1));
            return node;
        }

        public static bool constantTimesNegativeRunnable(RPN.Node node)
        {
            return node[0].IsNumber() && node[1].IsNumber(-1);
        }

        public static RPN.Node constantTimesNegative(RPN.Node node)
        {
            node.Replace(node[1], new RPN.Node(1));
            node.Replace(node[0], new RPN.Node(node[0].GetNumber() * -1));
            return node;
        }

        public static bool negativeOneDistributedRunnable(RPN.Node node)
        {
            return node[0].IsSubtraction() && node[1].IsNumber(-1);
        }

        public static RPN.Node negativeOneDistributed(RPN.Node node)
        {
            node[0].Swap(0, 1);
            node[1].Replace(1);
            return node;
        }
    }
}