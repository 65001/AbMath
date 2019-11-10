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

        public static class LogSimplifications
        {
            public static bool LogOneRunnable(RPN.Node node)
            {
                return node.IsLog() && node.Children[0].IsNumber(1);
            }

            public static RPN.Node LogOne(RPN.Node node)
            {
                return new RPN.Node(0);
            }

            public static bool LogIdentitcalRunnable(RPN.Node node)
            {
                return node.IsLog() && node.ChildrenAreIdentical();
            }

            public static RPN.Node LogIdentitcal(RPN.Node node)
            {
                return new RPN.Node(1);
            }

            public static bool LogPowerRunnable(RPN.Node node)
            {
                return node.IsExponent() && node.Children[0].IsLog() && node.Children[0].Children[1].Matches(node.Children[1]);
            }

            public static RPN.Node LogPower(RPN.Node node)
            {
                return node.Children[0].Children[0];
            }

            public static bool LogExponentExpansionRunnable(RPN.Node node)
            {
                return node.IsLog() && node.Children[0].IsExponent() && !node.Children[0].Children[1].IsVariable();
            }

            public static RPN.Node LogExponentExpansion(RPN.Node node)
            {
                RPN.Node exponent = node.Children[0];
                RPN.Node baseNode = exponent.Children[1];
                RPN.Node power = exponent.Children[0];

                RPN.Node log = new RPN.Node(new[] { baseNode.Clone(), node.Children[1] }, new RPN.Token("log", 2, RPN.Type.Function));
                return new RPN.Node(new[] { log, power }, new RPN.Token("*", 2, RPN.Type.Operator));
            }

            public static bool LogToLnRunnable(RPN.Node node)
            {
                return node.IsLog() && node[1].IsConstant("e");
            }

            public static RPN.Node LogToLn(RPN.Node node)
            {
                return new Node(new RPN.Node[] { node[0] }, new Token("ln", 1, Type.Function));
            }

            public static bool LnToLogRunnable(RPN.Node node)
            {
                return node.IsLn();
            }

            public static RPN.Node LnToLog(RPN.Node node)
            {
                RPN.Node e = new Node(new Token("e", 0 , Type.Function));
                RPN.Node log = new Node(new RPN.Node[] { e ,node[0] }, new Token("log", 2, Type.Function));
                return log;
            }
        }
    }
}
