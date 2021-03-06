﻿using System;
using System.Collections.Generic;
using System.Text;

namespace AbMath.Calculator.Simplifications
{
    public static class Division
    {
        public static bool setRule(RPN.Node node)
        {
            return node.IsDivision();
        }

        public static bool DivisionByZeroRunnable(RPN.Node node)
        {
            return node[0].IsNumber(0);
        }

        public static RPN.Node DivisionByZero(RPN.Node node)
        {
            return new RPN.Node(double.NaN);
        }

        public static bool DivisionByOneRunnable(RPN.Node node)
        {
            return node[0].IsNumber(1);
        }

        public static RPN.Node DivisionByOne(RPN.Node node)
        {
            return node[1];
        }

        public static bool GCDRunnable(RPN.Node node)
        {
            return node.Children[0].IsInteger() && node.Children[1].IsInteger();
        }

        public static RPN.Node GCD(RPN.Node node)
        {
            double num1 = node.Children[0].GetNumber();
            double num2 = node.Children[1].GetNumber();
            double gcd = RPN.DoFunctions.Gcd(new double[] { num1, num2 });

            node.Replace(node.Children[0], new RPN.Node((num1 / gcd)));
            node.Replace(node.Children[1], new RPN.Node((num2 / gcd)));
            return node;
        }

        public static bool DivisionFlipRunnable(RPN.Node node)
        {
            return node.Children[0].IsDivision() && node.Children[1].IsDivision();
        }

        public static RPN.Node DivisionFlip(RPN.Node node)
        {
            RPN.Node[] numerator = { node.Children[0].Children[1], node.Children[1].Children[1] };
            RPN.Node[] denominator = { node.Children[0].Children[0], node.Children[1].Children[0] };

            RPN.Node top = new RPN.Node(new[] { denominator[0], numerator[1] },
                new RPN.Token("*", 2, RPN.Type.Operator));
            RPN.Node bottom = new RPN.Node(new[] { denominator[1], numerator[0] },
                new RPN.Token("*", 2, RPN.Type.Operator));
            RPN.Node division = new RPN.Node(new[] { bottom, top }, new RPN.Token("/", 2, RPN.Type.Operator));
            return division;
        }

        public static bool DivisionFlipTwoRunnable(RPN.Node node)
        {
            return node[1].IsDivision();
        }

        public static RPN.Node DivisionFlipTwo(RPN.Node node)
        {
            RPN.Node numerator = node[1, 1];
            RPN.Node denominator = new RPN.Node(new[] { node[0], node[1, 0] },
                new RPN.Token("*", 2, RPN.Type.Operator));
            RPN.Node division = new RPN.Node(new[] { denominator, numerator },
                new RPN.Token("/", 2, RPN.Type.Operator));
            return division;
        }

        public static bool DivisionCancelingRunnable(RPN.Node node)
        {
            return node[1].IsMultiplication() && (node[0].IsNumberOrConstant()) && node[0].Matches(node[1, 1]) && !node[1, 1].IsNumber(0);
        }

        public static RPN.Node DivisionCanceling(RPN.Node node)
        {
            return node[1, 0];
        }

        public static bool PowerReductionRunnable(RPN.Node node)
        {
            return node[0].IsExponent() && node[1].IsExponent() && node[0, 0].IsInteger() && node[1, 0].IsInteger() && node[0, 1].Matches(node[1, 1]);
        }

        public static RPN.Node PowerReduction(RPN.Node node)
        {
            int reduction = System.Math.Min((int)node[0, 0].GetNumber(), (int)node[1, 0].GetNumber()) - 1;
            node[0, 0].Replace(node[0, 0].GetNumber() - reduction);
            node[1, 0].Replace(node[1, 0].GetNumber() - reduction);
            return node;
        }

        //f(x)!/f(x)! -> 1
        public static bool FactorialCancellationRunnable(RPN.Node node)
        {
            return node[0].IsOperator("!") && node[1].IsOperator("!") && node[0,0].Matches(node[1,0]) && !node[0,0].ContainsDomainViolation();
        }

        public static RPN.Node FactorialCancellation(RPN.Node node)
        {
            return new RPN.Node(1);
        }

        //[f(x)(x!)]/x! -> f(x)
        public static bool FactorialRemovedRunnable(RPN.Node node)
        {
            return node[1].IsMultiplication() && node[1, 0].IsOperator("!") && node[0].Matches(node[1, 0]);
        }

        public static RPN.Node FactorialRemoved(RPN.Node node)
        {
            return node[1, 1];
        }
    }
}
