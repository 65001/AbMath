using System;
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
    }
}
